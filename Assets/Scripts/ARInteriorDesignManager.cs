using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;

namespace ARInteriorDesign
{
    public class ARInteriorDesignManager : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private ARCamera arCamera;
        
        [Header("Furniture System")]
        [SerializeField] private FurnitureManager furnitureManager;
        [SerializeField] private FurnitureCatalog furnitureCatalog;
        [SerializeField] private PhysicalFurnitureDetector physicalFurnitureDetector;
        
        [Header("UI")]
        [SerializeField] private GameObject furnitureUI;
        [SerializeField] private GameObject placementIndicator;
        
        [Header("Settings")]
        [SerializeField] private LayerMask groundLayer = 1;
        [SerializeField] private float raycastDistance = 10f;
        
        private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
        private GameObject selectedFurniture;
        private FurnitureItem currentFurnitureItem;
        private bool isPlacementMode = false;
        private Camera mainCamera;
        
        // Events
        public System.Action<Vector3, Quaternion> OnValidPlacementFound;
        public System.Action OnValidPlacementLost;
        public System.Action<FurnitureObject> OnFurniturePlaced;
        public System.Action<FurnitureObject> OnFurnitureSelected;
        
        private void Start()
        {
            InitializeAR();
            SetupEventHandlers();
        }
        
        private void InitializeAR()
        {
            mainCamera = Camera.main;
            
            // Enable plane detection
            planeManager.planesChanged += OnPlanesChanged;
            
            // Initialize placement indicator
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }
            
            // Setup MR Utility Kit for Quest 3
            if (MRUK.Instance != null)
            {
                MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
            }
        }
        
        private void SetupEventHandlers()
        {
            if (furnitureCatalog != null)
            {
                furnitureCatalog.OnFurnitureSelected += OnFurnitureItemSelected;
            }
            
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.OnPhysicalFurnitureDetected += OnPhysicalFurnitureDetected;
            }
        }
        
        private void Update()
        {
            if (isPlacementMode)
            {
                HandlePlacementMode();
            }
            
            HandleInput();
        }
        
        private void HandlePlacementMode()
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            
            if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
            {
                var hit = raycastHits[0];
                var hitPoint = hit.pose.position;
                var hitRotation = hit.pose.rotation;
                
                if (placementIndicator != null)
                {
                    placementIndicator.SetActive(true);
                    placementIndicator.transform.position = hitPoint;
                    placementIndicator.transform.rotation = hitRotation;
                }
                
                OnValidPlacementFound?.Invoke(hitPoint, hitRotation);
            }
            else
            {
                if (placementIndicator != null)
                {
                    placementIndicator.SetActive(false);
                }
                
                OnValidPlacementLost?.Invoke();
            }
        }
        
        private void HandleInput()
        {
            // Handle touch input for placement
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                if (touch.phase == TouchPhase.Began)
                {
                    if (isPlacementMode)
                    {
                        PlaceFurniture();
                    }
                    else
                    {
                        SelectFurniture(touch.position);
                    }
                }
            }
            
            // Handle controller input for Quest 3
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            {
                if (isPlacementMode)
                {
                    PlaceFurniture();
                }
                else
                {
                    SelectFurnitureWithController();
                }
            }
        }
        
        private void PlaceFurniture()
        {
            if (currentFurnitureItem == null || placementIndicator == null || !placementIndicator.activeInHierarchy)
                return;
            
            Vector3 position = placementIndicator.transform.position;
            Quaternion rotation = placementIndicator.transform.rotation;
            
            GameObject furnitureObj = furnitureManager.PlaceFurniture(currentFurnitureItem, position, rotation);
            
            if (furnitureObj != null)
            {
                FurnitureObject furnitureComponent = furnitureObj.GetComponent<FurnitureObject>();
                OnFurniturePlaced?.Invoke(furnitureComponent);
                
                // Create anchor for tracking
                CreateAnchor(position, rotation, furnitureObj);
            }
            
            ExitPlacementMode();
        }
        
        private void SelectFurniture(Vector2 screenPosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, raycastDistance))
            {
                FurnitureObject furniture = hit.collider.GetComponent<FurnitureObject>();
                if (furniture != null)
                {
                    selectedFurniture = furniture.gameObject;
                    OnFurnitureSelected?.Invoke(furniture);
                }
            }
        }
        
        private void SelectFurnitureWithController()
        {
            Vector3 controllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            Vector3 controllerForward = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.forward;
            
            Ray ray = new Ray(controllerPosition, controllerForward);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, raycastDistance))
            {
                FurnitureObject furniture = hit.collider.GetComponent<FurnitureObject>();
                if (furniture != null)
                {
                    selectedFurniture = furniture.gameObject;
                    OnFurnitureSelected?.Invoke(furniture);
                }
            }
        }
        
        private void CreateAnchor(Vector3 position, Quaternion rotation, GameObject furnitureObj)
        {
            var anchorRequest = new ARAnchor(new Pose(position, rotation));
            // In a real implementation, you'd use anchorManager.AddAnchor()
            
            // For Quest 3, use spatial anchors
            FurnitureObject furniture = furnitureObj.GetComponent<FurnitureObject>();
            if (furniture != null)
            {
                furniture.SetSpatialAnchor(position, rotation);
            }
        }
        
        private void OnFurnitureItemSelected(FurnitureItem item)
        {
            currentFurnitureItem = item;
            EnterPlacementMode();
        }
        
        private void EnterPlacementMode()
        {
            isPlacementMode = true;
            if (furnitureUI != null)
            {
                furnitureUI.SetActive(false);
            }
        }
        
        private void ExitPlacementMode()
        {
            isPlacementMode = false;
            currentFurnitureItem = null;
            
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }
            
            if (furnitureUI != null)
            {
                furnitureUI.SetActive(true);
            }
        }
        
        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            // Handle plane detection changes
            foreach (var plane in args.added)
            {
                // Configure plane visualization
                ConfigurePlane(plane);
            }
        }
        
        private void ConfigurePlane(ARPlane plane)
        {
            // Make floor planes more visible and walls less visible
            if (plane.alignment == PlaneAlignment.HorizontalUp)
            {
                // This is likely a floor
                plane.gameObject.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 0.1f);
            }
            else if (plane.alignment == PlaneAlignment.Vertical)
            {
                // This is likely a wall
                plane.gameObject.GetComponent<Renderer>().material.color = new Color(1, 0, 0, 0.05f);
            }
        }
        
        private void OnSceneLoaded()
        {
            // Handle MR Utility Kit scene loading
            Debug.Log("MR scene loaded successfully");
            
            // Start physical furniture detection after scene is loaded
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.SetDetectionEnabled(true);
            }
        }
        
        private void OnPhysicalFurnitureDetected(PhysicalFurnitureDetector.PhysicalFurnitureItem furniture)
        {
            Debug.Log($"Physical furniture detected: {furniture.estimatedType} at {furniture.position}");
            
            // Check for collision with virtual furniture placement
            if (furnitureManager != null)
            {
                var virtualFurniture = furnitureManager.GetFurnitureInRadius(furniture.position, furniture.size.magnitude);
                if (virtualFurniture.Count > 0)
                {
                    Debug.LogWarning($"Virtual furniture conflicts with physical furniture at {furniture.position}");
                }
            }
        }
        
        public void TogglePlaneVisualization()
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(!plane.gameObject.activeInHierarchy);
            }
        }
        
        public void DeleteSelectedFurniture()
        {
            if (selectedFurniture != null)
            {
                furnitureManager.RemoveFurniture(selectedFurniture);
                selectedFurniture = null;
            }
        }
        
        public void ClearAllFurniture()
        {
            furnitureManager.ClearAllFurniture();
            selectedFurniture = null;
        }
        
        private void OnDestroy()
        {
            // Cleanup
            if (planeManager != null)
            {
                planeManager.planesChanged -= OnPlanesChanged;
            }
            
            if (furnitureCatalog != null)
            {
                furnitureCatalog.OnFurnitureSelected -= OnFurnitureItemSelected;
            }
            
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.OnPhysicalFurnitureDetected -= OnPhysicalFurnitureDetected;
            }
        }
    }
} 