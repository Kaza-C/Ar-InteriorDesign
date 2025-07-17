using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ARInteriorDesign
{
    public class FurnitureInventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private string inventoryFileName = "FurnitureInventory.json";
        [SerializeField] private int maxInventoryItems = 100;
        [SerializeField] private bool autoBackup = true;
        [SerializeField] private float autoSaveInterval = 30f;
        
        [Header("UI References")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform inventoryGridContainer;
        [SerializeField] private GameObject inventoryItemPrefab;
        [SerializeField] private InputField searchField;
        [SerializeField] private Dropdown categoryFilter;
        [SerializeField] private Button addToInventoryButton;
        [SerializeField] private Button clearInventoryButton;
        [SerializeField] private Button exportInventoryButton;
        [SerializeField] private Button importInventoryButton;
        [SerializeField] private Text inventoryCountText;
        
        [Header("Item Details UI")]
        [SerializeField] private GameObject itemDetailsPanel;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemTypeText;
        [SerializeField] private Text itemSizeText;
        [SerializeField] private Text itemLocationText;
        [SerializeField] private Text itemNotesText;
        [SerializeField] private InputField itemNameInput;
        [SerializeField] private InputField itemNotesInput;
        [SerializeField] private Button saveDetailsButton;
        [SerializeField] private Button deleteItemButton;
        [SerializeField] private Button placeItemButton;
        [SerializeField] private RawImage itemPhotoImage;
        [SerializeField] private Button takePhotoButton;
        
        private List<InventoryItem> inventory = new List<InventoryItem>();
        private InventoryItem selectedItem;
        private string currentSearchTerm = "";
        private FurnitureCategory selectedCategory = FurnitureCategory.Miscellaneous;
        private float lastAutoSaveTime;
        
        // References to other systems
        private PhysicalFurnitureDetector physicalFurnitureDetector;
        private FurnitureManager furnitureManager;
        private ARUIManager arUIManager;
        
        [System.Serializable]
        public class InventoryItem
        {
            public string id;
            public string name;
            public string customName;
            public FurnitureCategory category;
            public PhysicalFurnitureDetector.FurnitureType type;
            public Vector3 size;
            public Vector3 originalPosition;
            public string room;
            public string notes;
            public string photoPath;
            public System.DateTime dateAdded;
            public System.DateTime lastUsed;
            public int usageCount;
            public bool isFavorite;
            public List<string> tags;
            public Color color;
            public string material;
            public string brand;
            public float estimatedValue;
            public FurnitureCondition condition;
            public bool isAvailable; // true if not currently placed in a room
            
            public InventoryItem()
            {
                id = System.Guid.NewGuid().ToString();
                dateAdded = System.DateTime.Now;
                lastUsed = System.DateTime.Now;
                usageCount = 0;
                isFavorite = false;
                tags = new List<string>();
                color = Color.white;
                material = "";
                brand = "";
                estimatedValue = 0f;
                condition = FurnitureCondition.Good;
                isAvailable = true;
            }
        }
        
        public enum FurnitureCondition
        {
            Excellent,
            Good,
            Fair,
            Poor,
            NeedsRepair
        }
        
        // Events
        public System.Action<InventoryItem> OnItemAddedToInventory;
        public System.Action<InventoryItem> OnItemRemovedFromInventory;
        public System.Action<InventoryItem> OnItemPlacedFromInventory;
        public System.Action<InventoryItem> OnItemUpdated;
        public System.Action OnInventoryLoaded;
        public System.Action OnInventoryExported;
        
        private void Start()
        {
            InitializeInventory();
            FindSystemReferences();
            SetupUI();
            LoadInventory();
            
            lastAutoSaveTime = Time.time;
        }
        
        private void Update()
        {
            // Auto-save inventory periodically
            if (autoBackup && Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                SaveInventory();
                lastAutoSaveTime = Time.time;
            }
        }
        
        private void InitializeInventory()
        {
            inventory = new List<InventoryItem>();
        }
        
        private void FindSystemReferences()
        {
            physicalFurnitureDetector = FindObjectOfType<PhysicalFurnitureDetector>();
            furnitureManager = FindObjectOfType<FurnitureManager>();
            arUIManager = FindObjectOfType<ARUIManager>();
            
            // Subscribe to physical furniture detection events
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.OnPhysicalFurnitureDetected += OnPhysicalFurnitureDetected;
            }
        }
        
        private void SetupUI()
        {
            // Setup button events
            if (addToInventoryButton != null)
                addToInventoryButton.onClick.AddListener(ShowAddToInventoryDialog);
            
            if (clearInventoryButton != null)
                clearInventoryButton.onClick.AddListener(ClearInventory);
            
            if (exportInventoryButton != null)
                exportInventoryButton.onClick.AddListener(ExportInventory);
            
            if (importInventoryButton != null)
                importInventoryButton.onClick.AddListener(ImportInventory);
            
            if (saveDetailsButton != null)
                saveDetailsButton.onClick.AddListener(SaveItemDetails);
            
            if (deleteItemButton != null)
                deleteItemButton.onClick.AddListener(DeleteSelectedItem);
            
            if (placeItemButton != null)
                placeItemButton.onClick.AddListener(PlaceSelectedItem);
            
            if (takePhotoButton != null)
                takePhotoButton.onClick.AddListener(TakeItemPhoto);
            
            // Setup search and filter
            if (searchField != null)
                searchField.onValueChanged.AddListener(OnSearchChanged);
            
            if (categoryFilter != null)
                categoryFilter.onValueChanged.AddListener(OnCategoryFilterChanged);
            
            UpdateInventoryDisplay();
        }
        
        private void OnPhysicalFurnitureDetected(PhysicalFurnitureDetector.PhysicalFurnitureItem physicalItem)
        {
            // Check if this furniture is already in inventory
            bool alreadyExists = inventory.Any(item => 
                Vector3.Distance(item.originalPosition, physicalItem.position) < 0.5f &&
                item.type == physicalItem.estimatedType);
            
            if (!alreadyExists)
            {
                // Show option to add to inventory
                ShowAddToInventoryPrompt(physicalItem);
            }
        }
        
        private void ShowAddToInventoryPrompt(PhysicalFurnitureDetector.PhysicalFurnitureItem physicalItem)
        {
            // Create a simple dialog asking if user wants to add to inventory
            // For now, we'll auto-suggest adding high-confidence detections
            if (physicalItem.confidence > 0.8f)
            {
                Debug.Log($"Suggesting to add {physicalItem.estimatedType} to inventory");
                // In a real implementation, show a popup dialog
            }
        }
        
        public void AddPhysicalFurnitureToInventory(PhysicalFurnitureDetector.PhysicalFurnitureItem physicalItem)
        {
            var inventoryItem = new InventoryItem
            {
                name = physicalItem.estimatedType.ToString(),
                customName = $"My {physicalItem.estimatedType}",
                category = GetCategoryFromFurnitureType(physicalItem.estimatedType),
                type = physicalItem.estimatedType,
                size = physicalItem.size,
                originalPosition = physicalItem.position,
                room = "Current Room", // Could be detected or user-specified
                notes = $"Detected with {physicalItem.confidence:P0} confidence"
            };
            
            AddItemToInventory(inventoryItem);
        }
        
        public void AddItemToInventory(InventoryItem item)
        {
            if (inventory.Count >= maxInventoryItems)
            {
                Debug.LogWarning("Inventory is full!");
                return;
            }
            
            inventory.Add(item);
            OnItemAddedToInventory?.Invoke(item);
            UpdateInventoryDisplay();
            SaveInventory();
            
            Debug.Log($"Added {item.name} to inventory");
        }
        
        public void RemoveItemFromInventory(string itemId)
        {
            var item = inventory.FirstOrDefault(i => i.id == itemId);
            if (item != null)
            {
                inventory.Remove(item);
                OnItemRemovedFromInventory?.Invoke(item);
                UpdateInventoryDisplay();
                SaveInventory();
                
                Debug.Log($"Removed {item.name} from inventory");
            }
        }
        
        public void PlaceItemFromInventory(string itemId)
        {
            var item = inventory.FirstOrDefault(i => i.id == itemId);
            if (item != null && item.isAvailable)
            {
                // Create a virtual furniture item from the inventory item
                var furnitureItem = CreateFurnitureItemFromInventory(item);
                
                // Place it in the scene
                if (furnitureManager != null)
                {
                    // Let the user place it interactively
                    StartPlacementMode(furnitureItem);
                }
                
                // Update usage statistics
                item.lastUsed = System.DateTime.Now;
                item.usageCount++;
                
                OnItemPlacedFromInventory?.Invoke(item);
                SaveInventory();
                
                Debug.Log($"Placed {item.name} from inventory");
            }
        }
        
        private FurnitureItem CreateFurnitureItemFromInventory(InventoryItem inventoryItem)
        {
            return new FurnitureItem
            {
                name = inventoryItem.id,
                displayName = inventoryItem.customName,
                description = inventoryItem.notes,
                category = inventoryItem.category,
                prefabName = GetPrefabNameFromType(inventoryItem.type),
                defaultScale = inventoryItem.size,
                price = inventoryItem.estimatedValue,
                brand = inventoryItem.brand,
                tags = inventoryItem.tags,
                isAvailable = true
            };
        }
        
        private void StartPlacementMode(FurnitureItem furnitureItem)
        {
            // This would integrate with the existing furniture placement system
            if (arUIManager != null)
            {
                // Signal to enter placement mode with this furniture
                Debug.Log($"Starting placement mode for {furnitureItem.displayName}");
                // The ARUIManager would handle the actual placement logic
            }
        }
        
        private void UpdateInventoryDisplay()
        {
            if (inventoryGridContainer == null || inventoryItemPrefab == null)
                return;
            
            // Clear existing items
            foreach (Transform child in inventoryGridContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Apply filters
            var filteredItems = GetFilteredInventoryItems();
            
            // Create UI items
            foreach (var item in filteredItems)
            {
                CreateInventoryItemUI(item);
            }
            
            // Update count
            if (inventoryCountText != null)
            {
                inventoryCountText.text = $"Inventory: {filteredItems.Count}/{inventory.Count}";
            }
        }
        
        private List<InventoryItem> GetFilteredInventoryItems()
        {
            var filtered = inventory.AsEnumerable();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(currentSearchTerm))
            {
                filtered = filtered.Where(item =>
                    item.name.ToLower().Contains(currentSearchTerm.ToLower()) ||
                    item.customName.ToLower().Contains(currentSearchTerm.ToLower()) ||
                    item.notes.ToLower().Contains(currentSearchTerm.ToLower()) ||
                    item.tags.Any(tag => tag.ToLower().Contains(currentSearchTerm.ToLower())));
            }
            
            // Apply category filter
            if (selectedCategory != FurnitureCategory.Miscellaneous)
            {
                filtered = filtered.Where(item => item.category == selectedCategory);
            }
            
            // Sort by most recently used
            return filtered.OrderByDescending(item => item.lastUsed).ToList();
        }
        
        private void CreateInventoryItemUI(InventoryItem item)
        {
            GameObject itemObj = Instantiate(inventoryItemPrefab, inventoryGridContainer);
            
            // Setup item components
            Text nameText = itemObj.transform.Find("Name")?.GetComponent<Text>();
            Text typeText = itemObj.transform.Find("Type")?.GetComponent<Text>();
            Text sizeText = itemObj.transform.Find("Size")?.GetComponent<Text>();
            Text roomText = itemObj.transform.Find("Room")?.GetComponent<Text>();
            Button selectButton = itemObj.GetComponent<Button>();
            Button placeButton = itemObj.transform.Find("PlaceButton")?.GetComponent<Button>();
            Button favoriteButton = itemObj.transform.Find("FavoriteButton")?.GetComponent<Button>();
            RawImage photoImage = itemObj.transform.Find("Photo")?.GetComponent<RawImage>();
            
            if (nameText != null)
                nameText.text = item.customName;
            
            if (typeText != null)
                typeText.text = item.type.ToString();
            
            if (sizeText != null)
                sizeText.text = $"{item.size.x:F1}×{item.size.y:F1}×{item.size.z:F1}m";
            
            if (roomText != null)
                roomText.text = item.room;
            
            if (selectButton != null)
                selectButton.onClick.AddListener(() => SelectInventoryItem(item));
            
            if (placeButton != null)
            {
                placeButton.onClick.AddListener(() => PlaceItemFromInventory(item.id));
                placeButton.interactable = item.isAvailable;
            }
            
            if (favoriteButton != null)
            {
                favoriteButton.onClick.AddListener(() => ToggleFavorite(item.id));
                favoriteButton.GetComponent<Image>().color = item.isFavorite ? Color.red : Color.white;
            }
            
            if (photoImage != null && !string.IsNullOrEmpty(item.photoPath))
            {
                LoadItemPhoto(item.photoPath, photoImage);
            }
            
            // Visual indicators
            if (!item.isAvailable)
            {
                itemObj.GetComponent<Image>().color = Color.gray;
            }
        }
        
        private void SelectInventoryItem(InventoryItem item)
        {
            selectedItem = item;
            ShowItemDetails(item);
        }
        
        private void ShowItemDetails(InventoryItem item)
        {
            if (itemDetailsPanel == null) return;
            
            itemDetailsPanel.SetActive(true);
            
            if (itemNameText != null) itemNameText.text = item.customName;
            if (itemTypeText != null) itemTypeText.text = item.type.ToString();
            if (itemSizeText != null) itemSizeText.text = $"{item.size.x:F1} × {item.size.y:F1} × {item.size.z:F1}m";
            if (itemLocationText != null) itemLocationText.text = item.room;
            if (itemNotesText != null) itemNotesText.text = item.notes;
            
            if (itemNameInput != null) itemNameInput.text = item.customName;
            if (itemNotesInput != null) itemNotesInput.text = item.notes;
            
            if (itemPhotoImage != null && !string.IsNullOrEmpty(item.photoPath))
            {
                LoadItemPhoto(item.photoPath, itemPhotoImage);
            }
        }
        
        private void SaveItemDetails()
        {
            if (selectedItem == null) return;
            
            if (itemNameInput != null) selectedItem.customName = itemNameInput.text;
            if (itemNotesInput != null) selectedItem.notes = itemNotesInput.text;
            
            OnItemUpdated?.Invoke(selectedItem);
            UpdateInventoryDisplay();
            SaveInventory();
            
            Debug.Log($"Updated details for {selectedItem.customName}");
        }
        
        private void DeleteSelectedItem()
        {
            if (selectedItem != null)
            {
                RemoveItemFromInventory(selectedItem.id);
                itemDetailsPanel?.SetActive(false);
                selectedItem = null;
            }
        }
        
        private void PlaceSelectedItem()
        {
            if (selectedItem != null)
            {
                PlaceItemFromInventory(selectedItem.id);
                itemDetailsPanel?.SetActive(false);
            }
        }
        
        private void ToggleFavorite(string itemId)
        {
            var item = inventory.FirstOrDefault(i => i.id == itemId);
            if (item != null)
            {
                item.isFavorite = !item.isFavorite;
                UpdateInventoryDisplay();
                SaveInventory();
            }
        }
        
        private void TakeItemPhoto()
        {
            if (selectedItem == null) return;
            
            // This would integrate with the device camera
            Debug.Log($"Taking photo for {selectedItem.customName}");
            // Implementation would capture photo and save to selectedItem.photoPath
        }
        
        private void LoadItemPhoto(string photoPath, RawImage targetImage)
        {
            // Load photo from file system
            if (File.Exists(photoPath))
            {
                byte[] photoData = File.ReadAllBytes(photoPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(photoData);
                targetImage.texture = texture;
            }
        }
        
        private void OnSearchChanged(string searchTerm)
        {
            currentSearchTerm = searchTerm;
            UpdateInventoryDisplay();
        }
        
        private void OnCategoryFilterChanged(int categoryIndex)
        {
            selectedCategory = (FurnitureCategory)categoryIndex;
            UpdateInventoryDisplay();
        }
        
        private void ShowAddToInventoryDialog()
        {
            // Show dialog to manually add furniture to inventory
            Debug.Log("Show add to inventory dialog");
        }
        
        private void ClearInventory()
        {
            inventory.Clear();
            UpdateInventoryDisplay();
            SaveInventory();
            Debug.Log("Inventory cleared");
        }
        
        private void SaveInventory()
        {
            try
            {
                string json = JsonUtility.ToJson(new SerializableInventory(inventory));
                string filePath = Path.Combine(Application.persistentDataPath, inventoryFileName);
                File.WriteAllText(filePath, json);
                Debug.Log($"Inventory saved to {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save inventory: {e.Message}");
            }
        }
        
        private void LoadInventory()
        {
            try
            {
                string filePath = Path.Combine(Application.persistentDataPath, inventoryFileName);
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    SerializableInventory loadedInventory = JsonUtility.FromJson<SerializableInventory>(json);
                    inventory = loadedInventory.items;
                    UpdateInventoryDisplay();
                    OnInventoryLoaded?.Invoke();
                    Debug.Log($"Inventory loaded from {filePath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load inventory: {e.Message}");
            }
        }
        
        private void ExportInventory()
        {
            try
            {
                string json = JsonUtility.ToJson(new SerializableInventory(inventory), true);
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"FurnitureInventory_Export_{timestamp}.json";
                string filePath = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllText(filePath, json);
                
                OnInventoryExported?.Invoke();
                Debug.Log($"Inventory exported to {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export inventory: {e.Message}");
            }
        }
        
        private void ImportInventory()
        {
            // This would show a file picker to import inventory
            Debug.Log("Import inventory dialog");
        }
        
        private FurnitureCategory GetCategoryFromFurnitureType(PhysicalFurnitureDetector.FurnitureType type)
        {
            switch (type)
            {
                case PhysicalFurnitureDetector.FurnitureType.Table:
                    return FurnitureCategory.Tables;
                case PhysicalFurnitureDetector.FurnitureType.Chair:
                    return FurnitureCategory.Seating;
                case PhysicalFurnitureDetector.FurnitureType.Sofa:
                    return FurnitureCategory.Seating;
                case PhysicalFurnitureDetector.FurnitureType.Bed:
                    return FurnitureCategory.Bedroom;
                case PhysicalFurnitureDetector.FurnitureType.Storage:
                    return FurnitureCategory.Storage;
                case PhysicalFurnitureDetector.FurnitureType.Lamp:
                    return FurnitureCategory.Lighting;
                default:
                    return FurnitureCategory.Miscellaneous;
            }
        }
        
        private string GetPrefabNameFromType(PhysicalFurnitureDetector.FurnitureType type)
        {
            // Map furniture types to prefab names
            switch (type)
            {
                case PhysicalFurnitureDetector.FurnitureType.Table:
                    return "Table_Generic";
                case PhysicalFurnitureDetector.FurnitureType.Chair:
                    return "Chair_Generic";
                case PhysicalFurnitureDetector.FurnitureType.Sofa:
                    return "Sofa_Generic";
                case PhysicalFurnitureDetector.FurnitureType.Bed:
                    return "Bed_Generic";
                case PhysicalFurnitureDetector.FurnitureType.Storage:
                    return "Storage_Generic";
                case PhysicalFurnitureDetector.FurnitureType.Lamp:
                    return "Lamp_Generic";
                default:
                    return "Furniture_Generic";
            }
        }
        
        public List<InventoryItem> GetInventoryItems()
        {
            return new List<InventoryItem>(inventory);
        }
        
        public List<InventoryItem> GetAvailableItems()
        {
            return inventory.Where(item => item.isAvailable).ToList();
        }
        
        public List<InventoryItem> GetFavoriteItems()
        {
            return inventory.Where(item => item.isFavorite).ToList();
        }
        
        public InventoryItem GetItemById(string id)
        {
            return inventory.FirstOrDefault(item => item.id == id);
        }
        
        public void ShowInventoryPanel()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(true);
                UpdateInventoryDisplay();
            }
        }
        
        public void HideInventoryPanel()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            if (physicalFurnitureDetector != null)
            {
                physicalFurnitureDetector.OnPhysicalFurnitureDetected -= OnPhysicalFurnitureDetected;
            }
            
            SaveInventory();
        }
        
        [System.Serializable]
        private class SerializableInventory
        {
            public List<InventoryItem> items;
            
            public SerializableInventory(List<InventoryItem> inventoryItems)
            {
                items = inventoryItems;
            }
        }
    }
} 