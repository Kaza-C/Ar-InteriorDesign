using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;

namespace ARInteriorDesign
{
    public class PhysicalFurnitureDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float minObjectSize = 0.3f;
        [SerializeField] private float maxObjectSize = 3f;
        [SerializeField] private float heightThreshold = 0.1f;
        [SerializeField] private LayerMask detectionLayer = -1;
        
        [Header("Visualization")]
        [SerializeField] private Material physicalFurnitureOutlineMaterial;
        [SerializeField] private Material hiddenFurnitureMaterial;
        [SerializeField] private GameObject boundingBoxPrefab;
        
        [Header("AR Components")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private Camera arCamera;
        
        private List<PhysicalFurnitureItem> detectedFurniture = new List<PhysicalFurnitureItem>();
        private List<ARPlane> floorPlanes = new List<ARPlane>();
        private bool isDetectionEnabled = true;
        private bool isHidingMode = false;
        
        [System.Serializable]
        public class PhysicalFurnitureItem
        {
            public string id;
            public Vector3 position;
            public Vector3 size;
            public Bounds bounds;
            public GameObject visualRepresentation;
            public GameObject boundingBox;
            public bool isHidden = false;
            public bool isMarkedForRemoval = false;
            public FurnitureType estimatedType;
            public float confidence;
        }
        
        public enum FurnitureType
        {
            Unknown,
            Table,
            Chair,
            Sofa,
            Bed,
            Storage,
            Lamp,
            Plant
        }
        
        // Events
        public System.Action<PhysicalFurnitureItem> OnPhysicalFurnitureDetected;
        public System.Action<PhysicalFurnitureItem> OnPhysicalFurnitureHidden;
        public System.Action<PhysicalFurnitureItem> OnPhysicalFurnitureRestored;
        public System.Action<List<PhysicalFurnitureItem>> OnDetectionComplete;
        
        private void Start()
        {
            InitializeDetection();
        }
        
        private void InitializeDetection()
        {
            if (planeManager != null)
            {
                planeManager.planesChanged += OnPlanesChanged;
            }
            
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }
            
            // Start detection coroutine
            StartCoroutine(DetectionRoutine());
        }
        
        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            // Update floor planes for reference
            foreach (var plane in args.added)
            {
                if (plane.alignment == PlaneAlignment.HorizontalUp && plane.size.magnitude > 1f)
                {
                    floorPlanes.Add(plane);
                }
            }
            
            foreach (var plane in args.removed)
            {
                floorPlanes.Remove(plane);
            }
        }
        
        private System.Collections.IEnumerator DetectionRoutine()
        {
            while (isDetectionEnabled)
            {
                yield return new WaitForSeconds(1f); // Check every second
                
                if (floorPlanes.Count > 0)
                {
                    DetectFurnitureObjects();
                }
            }
        }
        
        private void DetectFurnitureObjects()
        {
            // Use AR plane detection and computer vision to identify furniture
            var newDetections = new List<PhysicalFurnitureItem>();
            
            foreach (var floorPlane in floorPlanes)
            {
                // Cast rays from the floor plane upward to detect objects
                var detections = ScanForObjectsOnPlane(floorPlane);
                newDetections.AddRange(detections);
            }
            
            // Filter and validate detections
            var validDetections = FilterDetections(newDetections);
            
            // Update our detected furniture list
            UpdateDetectedFurniture(validDetections);
        }
        
        private List<PhysicalFurnitureItem> ScanForObjectsOnPlane(ARPlane plane)
        {
            var detections = new List<PhysicalFurnitureItem>();
            var planeCenter = plane.center;
            var planeSize = plane.size;
            
            // Grid-based scanning across the plane
            int gridResolution = 20;
            float stepX = planeSize.x / gridResolution;
            float stepZ = planeSize.y / gridResolution;
            
            for (int x = 0; x < gridResolution; x++)
            {
                for (int z = 0; z < gridResolution; z++)
                {
                    Vector3 scanPoint = planeCenter + new Vector3(
                        (x - gridResolution / 2f) * stepX,
                        0,
                        (z - gridResolution / 2f) * stepZ
                    );
                    
                    var detection = ScanPointForFurniture(scanPoint, plane);
                    if (detection != null)
                    {
                        detections.Add(detection);
                    }
                }
            }
            
            return detections;
        }
        
        private PhysicalFurnitureItem ScanPointForFurniture(Vector3 scanPoint, ARPlane floorPlane)
        {
            // Cast ray upward from floor to detect objects
            Ray ray = new Ray(scanPoint, Vector3.up);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, maxObjectSize, detectionLayer))
            {
                // Check if this hit represents a furniture object
                if (hit.distance > heightThreshold && hit.distance < maxObjectSize)
                {
                    return AnalyzeHitForFurniture(hit, floorPlane);
                }
            }
            
            return null;
        }
        
        private PhysicalFurnitureItem AnalyzeHitForFurniture(RaycastHit hit, ARPlane floorPlane)
        {
            // Analyze the hit to determine if it's furniture
            var collider = hit.collider;
            var bounds = collider.bounds;
            
            // Check size constraints
            if (bounds.size.magnitude < minObjectSize || bounds.size.magnitude > maxObjectSize)
            {
                return null;
            }
            
            // Create furniture item
            var furniture = new PhysicalFurnitureItem
            {
                id = System.Guid.NewGuid().ToString(),
                position = bounds.center,
                size = bounds.size,
                bounds = bounds,
                estimatedType = EstimateFurnitureType(bounds),
                confidence = CalculateConfidence(bounds, hit)
            };
            
            // Create visual representation
            CreateFurnitureVisualization(furniture);
            
            return furniture;
        }
        
        private FurnitureType EstimateFurnitureType(Bounds bounds)
        {
            // Simple heuristic-based furniture type estimation
            float width = bounds.size.x;
            float height = bounds.size.y;
            float depth = bounds.size.z;
            
            // Table: wider than tall, moderate height
            if (height > 0.6f && height < 1.2f && (width > height || depth > height))
            {
                return FurnitureType.Table;
            }
            
            // Chair: moderate height, smaller footprint
            if (height > 0.8f && height < 1.3f && width < 0.8f && depth < 0.8f)
            {
                return FurnitureType.Chair;
            }
            
            // Sofa: long, moderate height
            if (height > 0.6f && height < 1.0f && (width > 1.5f || depth > 1.5f))
            {
                return FurnitureType.Sofa;
            }
            
            // Bed: very long, low height
            if (height > 0.4f && height < 0.8f && (width > 1.8f || depth > 1.8f))
            {
                return FurnitureType.Bed;
            }
            
            // Storage: tall, boxy
            if (height > 1.2f && width > 0.4f && depth > 0.4f)
            {
                return FurnitureType.Storage;
            }
            
            // Lamp: tall, narrow
            if (height > 1.0f && width < 0.5f && depth < 0.5f)
            {
                return FurnitureType.Lamp;
            }
            
            return FurnitureType.Unknown;
        }
        
        private float CalculateConfidence(Bounds bounds, RaycastHit hit)
        {
            // Calculate confidence based on various factors
            float confidence = 0.5f; // Base confidence
            
            // Size-based confidence
            if (bounds.size.magnitude > 0.5f && bounds.size.magnitude < 2f)
            {
                confidence += 0.2f;
            }
            
            // Height-based confidence
            if (bounds.size.y > 0.3f && bounds.size.y < 2f)
            {
                confidence += 0.2f;
            }
            
            // Surface normal confidence (furniture usually has horizontal surfaces)
            if (Vector3.Dot(hit.normal, Vector3.up) > 0.7f)
            {
                confidence += 0.1f;
            }
            
            return Mathf.Clamp01(confidence);
        }
        
        private void CreateFurnitureVisualization(PhysicalFurnitureItem furniture)
        {
            // Create bounding box visualization
            if (boundingBoxPrefab != null)
            {
                furniture.boundingBox = Instantiate(boundingBoxPrefab);
                furniture.boundingBox.transform.position = furniture.position;
                furniture.boundingBox.transform.localScale = furniture.size;
                
                // Add outline material
                var renderer = furniture.boundingBox.GetComponent<Renderer>();
                if (renderer != null && physicalFurnitureOutlineMaterial != null)
                {
                    renderer.material = physicalFurnitureOutlineMaterial;
                }
            }
            
            // Create visual representation for hiding
            furniture.visualRepresentation = CreateOcclusionObject(furniture);
        }
        
        private GameObject CreateOcclusionObject(PhysicalFurnitureItem furniture)
        {
            // Create a simple box that can occlude the physical furniture
            var occluder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            occluder.transform.position = furniture.position;
            occluder.transform.localScale = furniture.size;
            occluder.name = $"PhysicalFurniture_Occluder_{furniture.id}";
            
            // Make it invisible by default
            var renderer = occluder.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
            
            return occluder;
        }
        
        private List<PhysicalFurnitureItem> FilterDetections(List<PhysicalFurnitureItem> detections)
        {
            // Remove duplicates and low-confidence detections
            var filtered = new List<PhysicalFurnitureItem>();
            
            foreach (var detection in detections)
            {
                if (detection.confidence > 0.6f)
                {
                    // Check if we already have a similar detection
                    bool isDuplicate = filtered.Any(existing => 
                        Vector3.Distance(existing.position, detection.position) < 0.5f);
                    
                    if (!isDuplicate)
                    {
                        filtered.Add(detection);
                    }
                }
            }
            
            return filtered;
        }
        
        private void UpdateDetectedFurniture(List<PhysicalFurnitureItem> newDetections)
        {
            // Add new detections
            foreach (var detection in newDetections)
            {
                if (!detectedFurniture.Any(existing => existing.id == detection.id))
                {
                    detectedFurniture.Add(detection);
                    OnPhysicalFurnitureDetected?.Invoke(detection);
                }
            }
            
            OnDetectionComplete?.Invoke(detectedFurniture);
        }
        
        public void HidePhysicalFurniture(string furnitureId)
        {
            var furniture = detectedFurniture.FirstOrDefault(f => f.id == furnitureId);
            if (furniture != null && !furniture.isHidden)
            {
                furniture.isHidden = true;
                
                // Enable the occlusion object
                if (furniture.visualRepresentation != null)
                {
                    var renderer = furniture.visualRepresentation.GetComponent<Renderer>();
                    if (renderer != null && hiddenFurnitureMaterial != null)
                    {
                        renderer.enabled = true;
                        renderer.material = hiddenFurnitureMaterial;
                    }
                }
                
                OnPhysicalFurnitureHidden?.Invoke(furniture);
            }
        }
        
        public void RestorePhysicalFurniture(string furnitureId)
        {
            var furniture = detectedFurniture.FirstOrDefault(f => f.id == furnitureId);
            if (furniture != null && furniture.isHidden)
            {
                furniture.isHidden = false;
                
                // Disable the occlusion object
                if (furniture.visualRepresentation != null)
                {
                    var renderer = furniture.visualRepresentation.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                }
                
                OnPhysicalFurnitureRestored?.Invoke(furniture);
            }
        }
        
        public void ToggleHidingMode()
        {
            isHidingMode = !isHidingMode;
            
            foreach (var furniture in detectedFurniture)
            {
                if (furniture.boundingBox != null)
                {
                    furniture.boundingBox.SetActive(isHidingMode);
                }
            }
        }
        
        public void MarkForRemoval(string furnitureId)
        {
            var furniture = detectedFurniture.FirstOrDefault(f => f.id == furnitureId);
            if (furniture != null)
            {
                furniture.isMarkedForRemoval = !furniture.isMarkedForRemoval;
                
                // Update visual indication
                if (furniture.boundingBox != null)
                {
                    var renderer = furniture.boundingBox.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = furniture.isMarkedForRemoval ? Color.red : Color.yellow;
                    }
                }
            }
        }
        
        public List<PhysicalFurnitureItem> GetDetectedFurniture()
        {
            return new List<PhysicalFurnitureItem>(detectedFurniture);
        }
        
        public List<PhysicalFurnitureItem> GetHiddenFurniture()
        {
            return detectedFurniture.Where(f => f.isHidden).ToList();
        }
        
        public void ClearAllDetections()
        {
            foreach (var furniture in detectedFurniture)
            {
                if (furniture.boundingBox != null)
                {
                    Destroy(furniture.boundingBox);
                }
                if (furniture.visualRepresentation != null)
                {
                    Destroy(furniture.visualRepresentation);
                }
            }
            
            detectedFurniture.Clear();
        }
        
        public void SetDetectionEnabled(bool enabled)
        {
            isDetectionEnabled = enabled;
            
            if (!enabled)
            {
                StopAllCoroutines();
            }
            else
            {
                StartCoroutine(DetectionRoutine());
            }
        }
        
        private void OnDestroy()
        {
            if (planeManager != null)
            {
                planeManager.planesChanged -= OnPlanesChanged;
            }
            
            ClearAllDetections();
        }
    }
} 