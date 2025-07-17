using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace ARInteriorDesign
{
    public class ARUIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject catalogPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject furnitureInfoPanel;
        [SerializeField] private GameObject manipulationPanel;
        
        [Header("Main Menu Buttons")]
        [SerializeField] private Button catalogButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button saveRoomButton;
        [SerializeField] private Button loadRoomButton;
        [SerializeField] private Button clearRoomButton;
        [SerializeField] private Button togglePlanesButton;
        
        [Header("Manipulation UI")]
        [SerializeField] private Button moveButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button scaleButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button duplicateButton;
        [SerializeField] private Slider rotationSlider;
        [SerializeField] private Slider scaleSlider;
        
        [Header("Information Display")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text instructionText;
        [SerializeField] private Text furnitureCountText;
        [SerializeField] private Text roomNameText;
        
        [Header("AR Feedback")]
        [SerializeField] private GameObject placementIndicator;
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private ParticleSystem placementEffect;
        
        [Header("Hand Tracking UI")]
        [SerializeField] private GameObject handTrackingPanel;
        [SerializeField] private Text handTrackingStatus;
        [SerializeField] private Button recalibrateButton;
        
        [Header("Physical Furniture UI")]
        [SerializeField] private GameObject physicalFurniturePanel;
        [SerializeField] private Button detectFurnitureButton;
        [SerializeField] private Button hideAllPhysicalButton;
        [SerializeField] private Button restoreAllPhysicalButton;
        [SerializeField] private Button toggleHidingModeButton;
        [SerializeField] private Transform physicalFurnitureListContainer;
        [SerializeField] private GameObject physicalFurnitureItemPrefab;
        [SerializeField] private Text physicalFurnitureCountText;
        
        [Header("Inventory UI")]
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button addToInventoryButton;
        [SerializeField] private Text inventoryCountText;
        
        private ARInteriorDesignManager arManager;
        private FurnitureManager furnitureManager;
        private FurnitureCatalog furnitureCatalog;
        private PhysicalFurnitureDetector physicalFurnitureDetector;
        private FurnitureInventory furnitureInventory;
        private FurnitureObject selectedFurniture;
        private bool isManipulating = false;
        private ManipulationMode currentMode = ManipulationMode.None;
        
        public enum ManipulationMode
        {
            None,
            Move,
            Rotate,
            Scale
        }
        
        // Events
        public System.Action OnCatalogOpened;
        public System.Action OnCatalogClosed;
        public System.Action<FurnitureObject> OnFurnitureManipulated;
        
        private void Start()
        {
            InitializeUI();
            SetupEventHandlers();
            FindManagers();
        }
        
        private void InitializeUI()
        {
            // Initialize UI panels
            ShowMainMenu();
            HideAllPanels();
            
            // Set initial instruction text
            if (instructionText != null)
            {
                instructionText.text = "Look around to scan your room, then tap to place furniture";
            }
            
            // Setup initial status
            UpdateStatus("Ready to scan room");
        }
        
        private void SetupEventHandlers()
        {
            // Main menu buttons
            if (catalogButton != null)
                catalogButton.onClick.AddListener(OpenCatalog);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            
            if (saveRoomButton != null)
                saveRoomButton.onClick.AddListener(SaveRoom);
            
            if (loadRoomButton != null)
                loadRoomButton.onClick.AddListener(LoadRoom);
            
            if (clearRoomButton != null)
                clearRoomButton.onClick.AddListener(ClearRoom);
            
            if (togglePlanesButton != null)
                togglePlanesButton.onClick.AddListener(TogglePlanes);
            
            // Manipulation buttons
            if (moveButton != null)
                moveButton.onClick.AddListener(() => SetManipulationMode(ManipulationMode.Move));
            
            if (rotateButton != null)
                rotateButton.onClick.AddListener(() => SetManipulationMode(ManipulationMode.Rotate));
            
            if (scaleButton != null)
                scaleButton.onClick.AddListener(() => SetManipulationMode(ManipulationMode.Scale));
            
            if (deleteButton != null)
                deleteButton.onClick.AddListener(DeleteSelectedFurniture);
            
            if (duplicateButton != null)
                duplicateButton.onClick.AddListener(DuplicateSelectedFurniture);
            
            // Sliders
            if (rotationSlider != null)
                rotationSlider.onValueChanged.AddListener(OnRotationSliderChanged);
            
            if (scaleSlider != null)
                scaleSlider.onValueChanged.AddListener(OnScaleSliderChanged);
            
            // Hand tracking
            if (recalibrateButton != null)
                recalibrateButton.onClick.AddListener(RecalibrateHandTracking);
            
            // Physical furniture buttons
            if (detectFurnitureButton != null)
                detectFurnitureButton.onClick.AddListener(StartPhysicalFurnitureDetection);
            
            if (hideAllPhysicalButton != null)
                hideAllPhysicalButton.onClick.AddListener(HideAllPhysicalFurniture);
            
            if (restoreAllPhysicalButton != null)
                restoreAllPhysicalButton.onClick.AddListener(RestoreAllPhysicalFurniture);
            
            if (toggleHidingModeButton != null)
                toggleHidingModeButton.onClick.AddListener(TogglePhysicalFurnitureHidingMode);
            
            // Inventory buttons
            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(ToggleInventory);
            
            if (addToInventoryButton != null)
                addToInventoryButton.onClick.AddListener(AddSelectedFurnitureToInventory);
        }
        
        private void FindManagers()
        {
            arManager = FindObjectOfType<ARInteriorDesignManager>();
            furnitureManager = FindObjectOfType<FurnitureManager>();
            furnitureCatalog = FindObjectOfType<FurnitureCatalog>();
            physicalFurnitureDetector = FindObjectOfType<PhysicalFurnitureDetector>();
            furnitureInventory = FindObjectOfType<FurnitureInventory>();
            
            if (arManager != null)
            {
                arManager.OnFurniturePlaced += OnFurniturePlaced;
                arManager.OnFurnitureSelected += OnFurnitureSelected;
                arManager.OnValidPlacementFound += OnValidPlacementFound;
                arManager.OnValidPlacementLost += OnValidPlacementLost;
            }
            
            if (furnitureManager != null)
            {
                furnitureManager.OnFurniturePlaced += OnFurniturePlaced;
                furnitureManager.OnFurnitureSelected += OnFurnitureSelected;
                furnitureManager.OnFurnitureRemoved += OnFurnitureRemoved;
            }
            
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.OnPhysicalFurnitureDetected += OnPhysicalFurnitureDetected;
                physicalFurnitureDetector.OnPhysicalFurnitureHidden += OnPhysicalFurnitureHidden;
                physicalFurnitureDetector.OnPhysicalFurnitureRestored += OnPhysicalFurnitureRestored;
                physicalFurnitureDetector.OnDetectionComplete += OnPhysicalFurnitureDetectionComplete;
            }
            
            if (furnitureInventory != null)
            {
                furnitureInventory.OnItemAddedToInventory += OnItemAddedToInventory;
                furnitureInventory.OnItemRemovedFromInventory += OnItemRemovedFromInventory;
                furnitureInventory.OnItemPlacedFromInventory += OnItemPlacedFromInventory;
                furnitureInventory.OnInventoryLoaded += OnInventoryLoaded;
            }
        }
        
        private void Update()
        {
            UpdateHandTrackingUI();
            UpdateFurnitureCount();
            HandleInput();
        }
        
        private void HandleInput()
        {
            // Handle Quest 3 controller input
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                ToggleCatalog();
            }
            
            if (OVRInput.GetDown(OVRInput.Button.Two))
            {
                if (selectedFurniture != null)
                {
                    DeselectFurniture();
                }
            }
            
            // Handle manipulation input
            if (currentMode != ManipulationMode.None && selectedFurniture != null)
            {
                HandleManipulationInput();
            }
        }
        
        private void HandleManipulationInput()
        {
            Vector2 thumbstick = OVRInput.Get(OVRInput.Vector2.PrimaryThumbstick);
            
            switch (currentMode)
            {
                case ManipulationMode.Move:
                    if (thumbstick.magnitude > 0.1f)
                    {
                        Vector3 movement = new Vector3(thumbstick.x, 0, thumbstick.y) * Time.deltaTime * 2f;
                        Vector3 newPosition = selectedFurniture.transform.position + movement;
                        furnitureManager.MoveFurniture(selectedFurniture, newPosition);
                    }
                    break;
                    
                case ManipulationMode.Rotate:
                    if (Mathf.Abs(thumbstick.x) > 0.1f)
                    {
                        float rotationSpeed = 90f; // degrees per second
                        float newRotation = selectedFurniture.transform.eulerAngles.y + (thumbstick.x * rotationSpeed * Time.deltaTime);
                        furnitureManager.RotateFurniture(selectedFurniture, newRotation);
                    }
                    break;
                    
                case ManipulationMode.Scale:
                    if (Mathf.Abs(thumbstick.y) > 0.1f)
                    {
                        float scaleSpeed = 1f;
                        Vector3 currentScale = selectedFurniture.transform.localScale;
                        float scaleFactor = 1f + (thumbstick.y * scaleSpeed * Time.deltaTime);
                        Vector3 newScale = currentScale * scaleFactor;
                        furnitureManager.ScaleFurniture(selectedFurniture, newScale);
                    }
                    break;
            }
        }
        
        private void UpdateHandTrackingUI()
        {
            if (handTrackingPanel == null) return;
            
            // Check if hand tracking is active
            bool isHandTrackingActive = OVRInput.IsControllerConnected(OVRInput.Controller.Hands);
            
            if (handTrackingStatus != null)
            {
                handTrackingStatus.text = isHandTrackingActive ? "Hand Tracking: Active" : "Hand Tracking: Inactive";
                handTrackingStatus.color = isHandTrackingActive ? Color.green : Color.red;
            }
        }
        
        private void UpdateFurnitureCount()
        {
            if (furnitureCountText != null && furnitureManager != null)
            {
                int count = furnitureManager.GetFurnitureCount();
                furnitureCountText.text = $"Furniture: {count}";
            }
            
            if (physicalFurnitureCountText != null && physicalFurnitureDetector != null)
            {
                int detectedCount = physicalFurnitureDetector.GetDetectedFurniture().Count;
                int hiddenCount = physicalFurnitureDetector.GetHiddenFurniture().Count;
                physicalFurnitureCountText.text = $"Physical: {detectedCount} | Hidden: {hiddenCount}";
            }
            
            if (inventoryCountText != null && furnitureInventory != null)
            {
                int inventoryCount = furnitureInventory.GetInventoryItems().Count;
                int availableCount = furnitureInventory.GetAvailableItems().Count;
                inventoryCountText.text = $"Inventory: {inventoryCount} | Available: {availableCount}";
            }
        }
        
        public void OpenCatalog()
        {
            if (catalogPanel != null)
            {
                catalogPanel.SetActive(true);
                OnCatalogOpened?.Invoke();
            }
            
            if (furnitureCatalog != null)
            {
                furnitureCatalog.ShowCatalog();
            }
            
            UpdateStatus("Select furniture to place");
        }
        
        public void CloseCatalog()
        {
            if (catalogPanel != null)
            {
                catalogPanel.SetActive(false);
                OnCatalogClosed?.Invoke();
            }
            
            if (furnitureCatalog != null)
            {
                furnitureCatalog.HideCatalog();
            }
            
            UpdateStatus("Catalog closed");
        }
        
        public void ToggleCatalog()
        {
            if (catalogPanel != null)
            {
                if (catalogPanel.activeInHierarchy)
                {
                    CloseCatalog();
                }
                else
                {
                    OpenCatalog();
                }
            }
        }
        
        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
            
            UpdateStatus("Settings opened");
        }
        
        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            
            UpdateStatus("Settings closed");
        }
        
        public void SaveRoom()
        {
            if (furnitureManager != null)
            {
                string roomName = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var roomData = furnitureManager.SaveRoom(roomName);
                
                // Save to PlayerPrefs or file system
                string jsonData = JsonUtility.ToJson(roomData);
                PlayerPrefs.SetString($"Room_{roomName}", jsonData);
                PlayerPrefs.Save();
                
                UpdateStatus($"Room saved: {roomName}");
            }
        }
        
        public void LoadRoom()
        {
            // This would typically show a room selection dialog
            // For now, load the most recent room
            string[] keys = PlayerPrefs.GetString("SavedRooms", "").Split(',');
            if (keys.Length > 0 && !string.IsNullOrEmpty(keys[0]))
            {
                string jsonData = PlayerPrefs.GetString($"Room_{keys[0]}", "");
                if (!string.IsNullOrEmpty(jsonData))
                {
                    var roomData = JsonUtility.FromJson<FurnitureManager.RoomData>(jsonData);
                    furnitureManager.LoadRoom(roomData);
                    UpdateStatus($"Room loaded: {keys[0]}");
                }
            }
        }
        
        public void ClearRoom()
        {
            if (furnitureManager != null)
            {
                furnitureManager.ClearAllFurniture();
                UpdateStatus("Room cleared");
            }
        }
        
        public void TogglePlanes()
        {
            if (arManager != null)
            {
                arManager.TogglePlaneVisualization();
                UpdateStatus("Plane visualization toggled");
            }
        }
        
        public void SetManipulationMode(ManipulationMode mode)
        {
            currentMode = mode;
            
            // Update UI to show active mode
            if (moveButton != null)
                moveButton.interactable = (mode != ManipulationMode.Move);
            
            if (rotateButton != null)
                rotateButton.interactable = (mode != ManipulationMode.Rotate);
            
            if (scaleButton != null)
                scaleButton.interactable = (mode != ManipulationMode.Scale);
            
            // Show/hide relevant UI elements
            if (rotationSlider != null)
                rotationSlider.gameObject.SetActive(mode == ManipulationMode.Rotate);
            
            if (scaleSlider != null)
                scaleSlider.gameObject.SetActive(mode == ManipulationMode.Scale);
            
            UpdateStatus($"Manipulation mode: {mode}");
        }
        
        public void DeleteSelectedFurniture()
        {
            if (selectedFurniture != null)
            {
                furnitureManager.RemoveFurniture(selectedFurniture);
                DeselectFurniture();
                UpdateStatus("Furniture deleted");
            }
        }
        
        public void DuplicateSelectedFurniture()
        {
            if (selectedFurniture != null)
            {
                furnitureManager.DuplicateFurniture(selectedFurniture);
                UpdateStatus("Furniture duplicated");
            }
        }
        
        public void SelectFurniture(FurnitureObject furniture)
        {
            selectedFurniture = furniture;
            
            if (manipulationPanel != null)
            {
                manipulationPanel.SetActive(true);
            }
            
            if (furnitureInfoPanel != null)
            {
                ShowFurnitureInfo(furniture);
            }
            
            UpdateStatus($"Selected: {furniture.Item.displayName}");
        }
        
        public void DeselectFurniture()
        {
            selectedFurniture = null;
            currentMode = ManipulationMode.None;
            
            if (manipulationPanel != null)
            {
                manipulationPanel.SetActive(false);
            }
            
            if (furnitureInfoPanel != null)
            {
                furnitureInfoPanel.SetActive(false);
            }
            
            UpdateStatus("Furniture deselected");
        }
        
        private void ShowFurnitureInfo(FurnitureObject furniture)
        {
            if (furnitureInfoPanel == null) return;
            
            furnitureInfoPanel.SetActive(true);
            
            // Update info display
            Text nameText = furnitureInfoPanel.transform.Find("Name")?.GetComponent<Text>();
            Text descriptionText = furnitureInfoPanel.transform.Find("Description")?.GetComponent<Text>();
            Text priceText = furnitureInfoPanel.transform.Find("Price")?.GetComponent<Text>();
            
            if (nameText != null) nameText.text = furniture.Item.displayName;
            if (descriptionText != null) descriptionText.text = furniture.Item.description;
            if (priceText != null) priceText.text = $"${furniture.Item.price:F2}";
        }
        
        private void OnRotationSliderChanged(float value)
        {
            if (selectedFurniture != null)
            {
                furnitureManager.RotateFurniture(selectedFurniture, value * 360f);
            }
        }
        
        private void OnScaleSliderChanged(float value)
        {
            if (selectedFurniture != null)
            {
                float scale = Mathf.Lerp(0.5f, 2f, value);
                furnitureManager.ScaleFurniture(selectedFurniture, Vector3.one * scale);
            }
        }
        
        private void RecalibrateHandTracking()
        {
            // Recalibrate hand tracking
            UpdateStatus("Recalibrating hand tracking...");
        }
        
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            
            Debug.Log($"AR UI Status: {message}");
        }
        
        private void ShowMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }
        }
        
        private void HideAllPanels()
        {
            if (catalogPanel != null) catalogPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (furnitureInfoPanel != null) furnitureInfoPanel.SetActive(false);
            if (manipulationPanel != null) manipulationPanel.SetActive(false);
        }
        
        // Event handlers
        private void OnFurniturePlaced(FurnitureObject furniture)
        {
            UpdateStatus($"Placed: {furniture.Item.displayName}");
            
            // Play placement effect
            if (placementEffect != null)
            {
                placementEffect.transform.position = furniture.transform.position;
                placementEffect.Play();
            }
        }
        
        private void OnFurnitureSelected(FurnitureObject furniture)
        {
            SelectFurniture(furniture);
            OnFurnitureManipulated?.Invoke(furniture);
        }
        
        private void OnFurnitureRemoved(FurnitureObject furniture)
        {
            if (selectedFurniture == furniture)
            {
                DeselectFurniture();
            }
            
            UpdateStatus($"Removed: {furniture.Item.displayName}");
        }
        
        private void OnValidPlacementFound(Vector3 position, Quaternion rotation)
        {
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(true);
                placementIndicator.transform.position = position;
                placementIndicator.transform.rotation = rotation;
            }
        }
        
        private void OnValidPlacementLost()
        {
            if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }
        }
        
        // Physical furniture methods
        public void StartPhysicalFurnitureDetection()
        {
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.SetDetectionEnabled(true);
                UpdateStatus("Scanning for existing furniture...");
            }
        }
        
        public void StopPhysicalFurnitureDetection()
        {
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.SetDetectionEnabled(false);
                UpdateStatus("Furniture detection stopped");
            }
        }
        
        public void HideAllPhysicalFurniture()
        {
            if (physicalFurnitureDetector != null)
            {
                var detectedFurniture = physicalFurnitureDetector.GetDetectedFurniture();
                foreach (var furniture in detectedFurniture)
                {
                    if (!furniture.isHidden)
                    {
                        physicalFurnitureDetector.HidePhysicalFurniture(furniture.id);
                    }
                }
                UpdateStatus("All physical furniture hidden");
            }
        }
        
        public void RestoreAllPhysicalFurniture()
        {
            if (physicalFurnitureDetector != null)
            {
                var hiddenFurniture = physicalFurnitureDetector.GetHiddenFurniture();
                foreach (var furniture in hiddenFurniture)
                {
                    physicalFurnitureDetector.RestorePhysicalFurniture(furniture.id);
                }
                UpdateStatus("All physical furniture restored");
            }
        }
        
        public void TogglePhysicalFurnitureHidingMode()
        {
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.ToggleHidingMode();
                UpdateStatus("Physical furniture hiding mode toggled");
            }
        }
        
        public void ShowPhysicalFurniturePanel()
        {
            if (physicalFurniturePanel != null)
            {
                physicalFurniturePanel.SetActive(true);
                UpdatePhysicalFurnitureList();
            }
        }
        
        public void HidePhysicalFurniturePanel()
        {
            if (physicalFurniturePanel != null)
            {
                physicalFurniturePanel.SetActive(false);
            }
        }
        
        private void UpdatePhysicalFurnitureList()
        {
            if (physicalFurnitureListContainer == null || physicalFurnitureItemPrefab == null)
                return;
            
            // Clear existing items
            foreach (Transform child in physicalFurnitureListContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (physicalFurnitureDetector == null)
                return;
            
            // Create items for detected furniture
            var detectedFurniture = physicalFurnitureDetector.GetDetectedFurniture();
            foreach (var furniture in detectedFurniture)
            {
                CreatePhysicalFurnitureListItem(furniture);
            }
        }
        
        private void CreatePhysicalFurnitureListItem(PhysicalFurnitureDetector.PhysicalFurnitureItem furniture)
        {
            GameObject itemObj = Instantiate(physicalFurnitureItemPrefab, physicalFurnitureListContainer);
            
            // Setup item components
            Text nameText = itemObj.transform.Find("Name")?.GetComponent<Text>();
            Text typeText = itemObj.transform.Find("Type")?.GetComponent<Text>();
            Text sizeText = itemObj.transform.Find("Size")?.GetComponent<Text>();
            Button hideButton = itemObj.transform.Find("HideButton")?.GetComponent<Button>();
            Button markButton = itemObj.transform.Find("MarkButton")?.GetComponent<Button>();
            
            if (nameText != null)
                nameText.text = $"Furniture #{furniture.id.Substring(0, 8)}";
            
            if (typeText != null)
                typeText.text = furniture.estimatedType.ToString();
            
            if (sizeText != null)
                sizeText.text = $"{furniture.size.x:F1}x{furniture.size.y:F1}x{furniture.size.z:F1}m";
            
            if (hideButton != null)
            {
                hideButton.onClick.AddListener(() => TogglePhysicalFurnitureVisibility(furniture.id));
                var buttonText = hideButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = furniture.isHidden ? "Show" : "Hide";
            }
            
            if (markButton != null)
            {
                markButton.onClick.AddListener(() => TogglePhysicalFurnitureMark(furniture.id));
                var buttonText = markButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = furniture.isMarkedForRemoval ? "Unmark" : "Mark";
            }
        }
        
        private void TogglePhysicalFurnitureVisibility(string furnitureId)
        {
            if (physicalFurnitureDetector == null) return;
            
            var detectedFurniture = physicalFurnitureDetector.GetDetectedFurniture();
            var furniture = detectedFurniture.FirstOrDefault(f => f.id == furnitureId);
            
            if (furniture != null)
            {
                if (furniture.isHidden)
                {
                    physicalFurnitureDetector.RestorePhysicalFurniture(furnitureId);
                }
                else
                {
                    physicalFurnitureDetector.HidePhysicalFurniture(furnitureId);
                }
                
                UpdatePhysicalFurnitureList();
            }
        }
        
        private void TogglePhysicalFurnitureMark(string furnitureId)
        {
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.MarkForRemoval(furnitureId);
                UpdatePhysicalFurnitureList();
            }
        }
        
        // Physical furniture event handlers
        private void OnPhysicalFurnitureDetected(PhysicalFurnitureDetector.PhysicalFurnitureItem furniture)
        {
            UpdateStatus($"Detected {furniture.estimatedType}: {furniture.confidence:P0} confidence");
            UpdatePhysicalFurnitureList();
        }
        
        private void OnPhysicalFurnitureHidden(PhysicalFurnitureDetector.PhysicalFurnitureItem furniture)
        {
            UpdateStatus($"Hidden {furniture.estimatedType}");
            UpdatePhysicalFurnitureList();
        }
        
        private void OnPhysicalFurnitureRestored(PhysicalFurnitureDetector.PhysicalFurnitureItem furniture)
        {
            UpdateStatus($"Restored {furniture.estimatedType}");
            UpdatePhysicalFurnitureList();
        }
        
        private void OnPhysicalFurnitureDetectionComplete(List<PhysicalFurnitureDetector.PhysicalFurnitureItem> detectedFurniture)
        {
            UpdateStatus($"Detection complete: {detectedFurniture.Count} items found");
            UpdatePhysicalFurnitureList();
        }
        
        // Inventory methods
        public void ToggleInventory()
        {
            if (furnitureInventory != null)
            {
                // Toggle inventory panel visibility
                furnitureInventory.ShowInventoryPanel();
                UpdateStatus("Inventory opened");
            }
        }
        
        public void AddSelectedFurnitureToInventory()
        {
            if (physicalFurnitureDetector != null && furnitureInventory != null)
            {
                var detectedFurniture = physicalFurnitureDetector.GetDetectedFurniture();
                
                // For now, add the first available detected furniture
                // In a real implementation, this would be the currently selected furniture
                if (detectedFurniture.Count > 0)
                {
                    var firstItem = detectedFurniture[0];
                    furnitureInventory.AddPhysicalFurnitureToInventory(firstItem);
                    UpdateStatus($"Added {firstItem.estimatedType} to inventory");
                }
                else
                {
                    UpdateStatus("No furniture detected to add to inventory");
                }
            }
        }
        
        public void AddPhysicalFurnitureToInventory(PhysicalFurnitureDetector.PhysicalFurnitureItem physicalItem)
        {
            if (furnitureInventory != null)
            {
                furnitureInventory.AddPhysicalFurnitureToInventory(physicalItem);
                UpdateStatus($"Added {physicalItem.estimatedType} to inventory");
            }
        }
        
        // Inventory event handlers
        private void OnItemAddedToInventory(FurnitureInventory.InventoryItem item)
        {
            UpdateStatus($"Added {item.customName} to inventory");
        }
        
        private void OnItemRemovedFromInventory(FurnitureInventory.InventoryItem item)
        {
            UpdateStatus($"Removed {item.customName} from inventory");
        }
        
        private void OnItemPlacedFromInventory(FurnitureInventory.InventoryItem item)
        {
            UpdateStatus($"Placed {item.customName} from inventory");
        }
        
        private void OnInventoryLoaded()
        {
            UpdateStatus("Inventory loaded");
        }
        
        private void OnDestroy()
        {
            // Cleanup event handlers
            if (arManager != null)
            {
                arManager.OnFurniturePlaced -= OnFurniturePlaced;
                arManager.OnFurnitureSelected -= OnFurnitureSelected;
                arManager.OnValidPlacementFound -= OnValidPlacementFound;
                arManager.OnValidPlacementLost -= OnValidPlacementLost;
            }
            
            if (furnitureManager != null)
            {
                furnitureManager.OnFurniturePlaced -= OnFurniturePlaced;
                furnitureManager.OnFurnitureSelected -= OnFurnitureSelected;
                furnitureManager.OnFurnitureRemoved -= OnFurnitureRemoved;
            }
            
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.OnPhysicalFurnitureDetected -= OnPhysicalFurnitureDetected;
                physicalFurnitureDetector.OnPhysicalFurnitureHidden -= OnPhysicalFurnitureHidden;
                physicalFurnitureDetector.OnPhysicalFurnitureRestored -= OnPhysicalFurnitureRestored;
                physicalFurnitureDetector.OnDetectionComplete -= OnPhysicalFurnitureDetectionComplete;
            }
            
            if (furnitureInventory != null)
            {
                furnitureInventory.OnItemAddedToInventory -= OnItemAddedToInventory;
                furnitureInventory.OnItemRemovedFromInventory -= OnItemRemovedFromInventory;
                furnitureInventory.OnItemPlacedFromInventory -= OnItemPlacedFromInventory;
                furnitureInventory.OnInventoryLoaded -= OnInventoryLoaded;
            }
        }
    }
} 