using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace ARInteriorDesign
{
    public class FurnitureCatalog : MonoBehaviour
    {
        [Header("Catalog Data")]
        [SerializeField] private List<FurnitureItem> furnitureItems = new List<FurnitureItem>();
        [SerializeField] private FurnitureCategory selectedCategory = FurnitureCategory.Miscellaneous;
        
        [Header("UI References")]
        [SerializeField] private Transform catalogPanel;
        [SerializeField] private Transform categoryButtonContainer;
        [SerializeField] private Transform furnitureButtonContainer;
        [SerializeField] private ScrollRect furnitureScrollView;
        
        [Header("UI Prefabs")]
        [SerializeField] private GameObject categoryButtonPrefab;
        [SerializeField] private GameObject furnitureButtonPrefab;
        [SerializeField] private GameObject furnitureInfoPanel;
        
        [Header("Search")]
        [SerializeField] private InputField searchField;
        [SerializeField] private Button searchButton;
        [SerializeField] private Button clearSearchButton;
        
        [Header("Filters")]
        [SerializeField] private Dropdown priceRangeDropdown;
        [SerializeField] private Toggle favoriteToggle;
        [SerializeField] private Toggle availableOnlyToggle;
        
        private Dictionary<FurnitureCategory, List<FurnitureItem>> categorizedItems;
        private List<FurnitureItem> filteredItems;
        private List<FurnitureItem> favoriteItems = new List<FurnitureItem>();
        private string currentSearchTerm = "";
        
        // Events
        public System.Action<FurnitureItem> OnFurnitureSelected;
        public System.Action<FurnitureCategory> OnCategoryChanged;
        public System.Action<string> OnSearchPerformed;
        
        private void Start()
        {
            InitializeCatalog();
            SetupUI();
            LoadFavorites();
        }
        
        private void InitializeCatalog()
        {
            // Load furniture items from Resources or ScriptableObjects
            LoadFurnitureItems();
            
            // Categorize items
            CategorizeItems();
            
            // Set initial category
            SetCategory(selectedCategory);
        }
        
        private void LoadFurnitureItems()
        {
            // Load furniture items from Resources folder
            FurnitureItem[] items = Resources.LoadAll<FurnitureItem>("FurnitureItems");
            furnitureItems.AddRange(items);
            
            // If no items found, create some default ones
            if (furnitureItems.Count == 0)
            {
                CreateDefaultFurnitureItems();
            }
            
            Debug.Log($"Loaded {furnitureItems.Count} furniture items");
        }
        
        private void CreateDefaultFurnitureItems()
        {
            // Create some default furniture items for testing
            furnitureItems.Add(new FurnitureItem
            {
                name = "ModernChair",
                displayName = "Modern Chair",
                description = "A comfortable modern chair for your living room",
                category = FurnitureCategory.Seating,
                prefabName = "Chair_Modern",
                defaultScale = Vector3.one,
                price = 299.99f,
                brand = "ModernFurniture Co.",
                tags = new List<string> { "chair", "modern", "comfort" },
                isAvailable = true
            });
            
            furnitureItems.Add(new FurnitureItem
            {
                name = "DiningTable",
                displayName = "Dining Table",
                description = "Elegant dining table for family meals",
                category = FurnitureCategory.Tables,
                prefabName = "Table_Dining",
                defaultScale = Vector3.one,
                price = 799.99f,
                brand = "Classic Furniture",
                tags = new List<string> { "table", "dining", "family" },
                isAvailable = true
            });
            
            furnitureItems.Add(new FurnitureItem
            {
                name = "Sofa",
                displayName = "Leather Sofa",
                description = "Luxurious leather sofa with premium comfort",
                category = FurnitureCategory.Seating,
                prefabName = "Sofa_Leather",
                defaultScale = Vector3.one,
                price = 1499.99f,
                brand = "Luxury Living",
                tags = new List<string> { "sofa", "leather", "luxury" },
                isAvailable = true
            });
            
            furnitureItems.Add(new FurnitureItem
            {
                name = "Bookshelf",
                displayName = "Wooden Bookshelf",
                description = "Spacious wooden bookshelf for your books and decorations",
                category = FurnitureCategory.Storage,
                prefabName = "Bookshelf_Wood",
                defaultScale = Vector3.one,
                price = 399.99f,
                brand = "Wood Craft",
                tags = new List<string> { "bookshelf", "storage", "wood" },
                isAvailable = true
            });
            
            furnitureItems.Add(new FurnitureItem
            {
                name = "FloorLamp",
                displayName = "Modern Floor Lamp",
                description = "Stylish floor lamp with adjustable brightness",
                category = FurnitureCategory.Lighting,
                prefabName = "Lamp_Floor",
                defaultScale = Vector3.one,
                price = 199.99f,
                brand = "Light Design",
                tags = new List<string> { "lamp", "lighting", "modern" },
                isAvailable = true
            });
            
            furnitureItems.Add(new FurnitureItem
            {
                name = "CoffeeTable",
                displayName = "Glass Coffee Table",
                description = "Modern glass coffee table with metal legs",
                category = FurnitureCategory.Tables,
                prefabName = "Table_Coffee",
                defaultScale = Vector3.one,
                price = 459.99f,
                brand = "Glass & Metal Co.",
                tags = new List<string> { "table", "coffee", "glass" },
                isAvailable = true
            });
            
            furnitureItems.Add(new FurnitureItem
            {
                name = "Bed",
                displayName = "Queen Size Bed",
                description = "Comfortable queen size bed with upholstered headboard",
                category = FurnitureCategory.Bedroom,
                prefabName = "Bed_Queen",
                defaultScale = Vector3.one,
                price = 899.99f,
                brand = "Sleep Well",
                tags = new List<string> { "bed", "bedroom", "queen" },
                isAvailable = true
            });
            
            furnitureItems.Add(new FurnitureItem
            {
                name = "Wardrobe",
                displayName = "Large Wardrobe",
                description = "Spacious wardrobe with multiple compartments",
                category = FurnitureCategory.Storage,
                prefabName = "Wardrobe_Large",
                defaultScale = Vector3.one,
                price = 1299.99f,
                brand = "Storage Solutions",
                tags = new List<string> { "wardrobe", "storage", "clothes" },
                isAvailable = true
            });
        }
        
        private void CategorizeItems()
        {
            categorizedItems = new Dictionary<FurnitureCategory, List<FurnitureItem>>();
            
            foreach (FurnitureCategory category in System.Enum.GetValues(typeof(FurnitureCategory)))
            {
                categorizedItems[category] = new List<FurnitureItem>();
            }
            
            foreach (FurnitureItem item in furnitureItems)
            {
                categorizedItems[item.category].Add(item);
            }
        }
        
        private void SetupUI()
        {
            // Setup category buttons
            CreateCategoryButtons();
            
            // Setup search functionality
            if (searchField != null)
            {
                searchField.onValueChanged.AddListener(OnSearchFieldChanged);
            }
            
            if (searchButton != null)
            {
                searchButton.onClick.AddListener(PerformSearch);
            }
            
            if (clearSearchButton != null)
            {
                clearSearchButton.onClick.AddListener(ClearSearch);
            }
            
            // Setup filters
            if (priceRangeDropdown != null)
            {
                priceRangeDropdown.onValueChanged.AddListener(OnPriceRangeChanged);
            }
            
            if (favoriteToggle != null)
            {
                favoriteToggle.onValueChanged.AddListener(OnFavoriteToggleChanged);
            }
            
            if (availableOnlyToggle != null)
            {
                availableOnlyToggle.onValueChanged.AddListener(OnAvailableOnlyToggleChanged);
            }
            
            // Update UI
            UpdateFurnitureDisplay();
        }
        
        private void CreateCategoryButtons()
        {
            if (categoryButtonContainer == null || categoryButtonPrefab == null) return;
            
            // Clear existing buttons
            foreach (Transform child in categoryButtonContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create category buttons
            foreach (FurnitureCategory category in System.Enum.GetValues(typeof(FurnitureCategory)))
            {
                GameObject buttonObj = Instantiate(categoryButtonPrefab, categoryButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                Text buttonText = buttonObj.GetComponentInChildren<Text>();
                
                if (buttonText != null)
                {
                    buttonText.text = category.ToString();
                }
                
                if (button != null)
                {
                    FurnitureCategory categoryCapture = category;
                    button.onClick.AddListener(() => SetCategory(categoryCapture));
                    
                    // Highlight selected category
                    if (category == selectedCategory)
                    {
                        button.interactable = false;
                    }
                }
            }
        }
        
        private void UpdateFurnitureDisplay()
        {
            if (furnitureButtonContainer == null || furnitureButtonPrefab == null) return;
            
            // Clear existing buttons
            foreach (Transform child in furnitureButtonContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Get filtered items
            List<FurnitureItem> itemsToShow = GetFilteredItems();
            
            // Create furniture buttons
            foreach (FurnitureItem item in itemsToShow)
            {
                GameObject buttonObj = Instantiate(furnitureButtonPrefab, furnitureButtonContainer);
                
                // Setup button components
                Button button = buttonObj.GetComponent<Button>();
                Text nameText = buttonObj.transform.Find("Name")?.GetComponent<Text>();
                Text priceText = buttonObj.transform.Find("Price")?.GetComponent<Text>();
                Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
                Button favoriteButton = buttonObj.transform.Find("FavoriteButton")?.GetComponent<Button>();
                
                if (nameText != null)
                {
                    nameText.text = item.displayName;
                }
                
                if (priceText != null)
                {
                    priceText.text = $"${item.price:F2}";
                }
                
                if (iconImage != null && item.icon != null)
                {
                    iconImage.sprite = item.icon;
                }
                
                if (button != null)
                {
                    FurnitureItem itemCapture = item;
                    button.onClick.AddListener(() => SelectFurniture(itemCapture));
                }
                
                if (favoriteButton != null)
                {
                    FurnitureItem itemCapture = item;
                    favoriteButton.onClick.AddListener(() => ToggleFavorite(itemCapture));
                    
                    // Update favorite button appearance
                    bool isFavorite = favoriteItems.Contains(item);
                    favoriteButton.GetComponent<Image>().color = isFavorite ? Color.red : Color.white;
                }
            }
        }
        
        private List<FurnitureItem> GetFilteredItems()
        {
            List<FurnitureItem> items = categorizedItems[selectedCategory];
            
            // Apply search filter
            if (!string.IsNullOrEmpty(currentSearchTerm))
            {
                items = items.Where(item => 
                    item.displayName.ToLower().Contains(currentSearchTerm.ToLower()) ||
                    item.description.ToLower().Contains(currentSearchTerm.ToLower()) ||
                    item.tags.Any(tag => tag.ToLower().Contains(currentSearchTerm.ToLower()))
                ).ToList();
            }
            
            // Apply availability filter
            if (availableOnlyToggle != null && availableOnlyToggle.isOn)
            {
                items = items.Where(item => item.isAvailable).ToList();
            }
            
            // Apply favorite filter
            if (favoriteToggle != null && favoriteToggle.isOn)
            {
                items = items.Where(item => favoriteItems.Contains(item)).ToList();
            }
            
            // Apply price filter
            if (priceRangeDropdown != null)
            {
                items = ApplyPriceFilter(items);
            }
            
            return items;
        }
        
        private List<FurnitureItem> ApplyPriceFilter(List<FurnitureItem> items)
        {
            int selectedRange = priceRangeDropdown.value;
            
            switch (selectedRange)
            {
                case 0: // All prices
                    return items;
                case 1: // Under $100
                    return items.Where(item => item.price < 100).ToList();
                case 2: // $100 - $500
                    return items.Where(item => item.price >= 100 && item.price < 500).ToList();
                case 3: // $500 - $1000
                    return items.Where(item => item.price >= 500 && item.price < 1000).ToList();
                case 4: // Over $1000
                    return items.Where(item => item.price >= 1000).ToList();
                default:
                    return items;
            }
        }
        
        public void SetCategory(FurnitureCategory category)
        {
            selectedCategory = category;
            OnCategoryChanged?.Invoke(category);
            UpdateFurnitureDisplay();
            CreateCategoryButtons(); // Refresh category buttons to show selection
        }
        
        public void SelectFurniture(FurnitureItem item)
        {
            Debug.Log($"Selected furniture: {item.displayName}");
            OnFurnitureSelected?.Invoke(item);
            
            // Show furniture info panel
            ShowFurnitureInfo(item);
        }
        
        private void ShowFurnitureInfo(FurnitureItem item)
        {
            if (furnitureInfoPanel == null) return;
            
            furnitureInfoPanel.SetActive(true);
            
            // Update info panel with item details
            Text nameText = furnitureInfoPanel.transform.Find("Name")?.GetComponent<Text>();
            Text descriptionText = furnitureInfoPanel.transform.Find("Description")?.GetComponent<Text>();
            Text priceText = furnitureInfoPanel.transform.Find("Price")?.GetComponent<Text>();
            Text brandText = furnitureInfoPanel.transform.Find("Brand")?.GetComponent<Text>();
            Image iconImage = furnitureInfoPanel.transform.Find("Icon")?.GetComponent<Image>();
            
            if (nameText != null) nameText.text = item.displayName;
            if (descriptionText != null) descriptionText.text = item.description;
            if (priceText != null) priceText.text = $"${item.price:F2}";
            if (brandText != null) brandText.text = item.brand;
            if (iconImage != null && item.icon != null) iconImage.sprite = item.icon;
        }
        
        private void OnSearchFieldChanged(string searchTerm)
        {
            currentSearchTerm = searchTerm;
            if (string.IsNullOrEmpty(searchTerm))
            {
                UpdateFurnitureDisplay();
            }
        }
        
        private void PerformSearch()
        {
            OnSearchPerformed?.Invoke(currentSearchTerm);
            UpdateFurnitureDisplay();
        }
        
        private void ClearSearch()
        {
            currentSearchTerm = "";
            if (searchField != null)
            {
                searchField.text = "";
            }
            UpdateFurnitureDisplay();
        }
        
        private void OnPriceRangeChanged(int value)
        {
            UpdateFurnitureDisplay();
        }
        
        private void OnFavoriteToggleChanged(bool isOn)
        {
            UpdateFurnitureDisplay();
        }
        
        private void OnAvailableOnlyToggleChanged(bool isOn)
        {
            UpdateFurnitureDisplay();
        }
        
        private void ToggleFavorite(FurnitureItem item)
        {
            if (favoriteItems.Contains(item))
            {
                favoriteItems.Remove(item);
            }
            else
            {
                favoriteItems.Add(item);
            }
            
            SaveFavorites();
            UpdateFurnitureDisplay();
        }
        
        private void SaveFavorites()
        {
            // Save favorites to PlayerPrefs
            string favoriteNames = string.Join(",", favoriteItems.Select(item => item.name));
            PlayerPrefs.SetString("FavoriteFurniture", favoriteNames);
            PlayerPrefs.Save();
        }
        
        private void LoadFavorites()
        {
            string favoriteNames = PlayerPrefs.GetString("FavoriteFurniture", "");
            if (!string.IsNullOrEmpty(favoriteNames))
            {
                string[] names = favoriteNames.Split(',');
                foreach (string name in names)
                {
                    FurnitureItem item = furnitureItems.FirstOrDefault(f => f.name == name);
                    if (item != null && !favoriteItems.Contains(item))
                    {
                        favoriteItems.Add(item);
                    }
                }
            }
        }
        
        public void ShowCatalog()
        {
            if (catalogPanel != null)
            {
                catalogPanel.gameObject.SetActive(true);
            }
        }
        
        public void HideCatalog()
        {
            if (catalogPanel != null)
            {
                catalogPanel.gameObject.SetActive(false);
            }
        }
        
        public void ToggleCatalog()
        {
            if (catalogPanel != null)
            {
                catalogPanel.gameObject.SetActive(!catalogPanel.gameObject.activeInHierarchy);
            }
        }
        
        public List<FurnitureItem> GetFurnitureByCategory(FurnitureCategory category)
        {
            return categorizedItems.ContainsKey(category) ? categorizedItems[category] : new List<FurnitureItem>();
        }
        
        public FurnitureItem GetFurnitureByName(string name)
        {
            return furnitureItems.FirstOrDefault(item => item.name == name);
        }
        
        public List<FurnitureItem> GetAllFurniture()
        {
            return new List<FurnitureItem>(furnitureItems);
        }
        
        public List<FurnitureItem> GetFavorites()
        {
            return new List<FurnitureItem>(favoriteItems);
        }
        
        public void RefreshCatalog()
        {
            LoadFurnitureItems();
            CategorizeItems();
            UpdateFurnitureDisplay();
        }
        
        public void SortByPrice(bool ascending = true)
        {
            if (ascending)
            {
                furnitureItems = furnitureItems.OrderBy(item => item.price).ToList();
            }
            else
            {
                furnitureItems = furnitureItems.OrderByDescending(item => item.price).ToList();
            }
            
            CategorizeItems();
            UpdateFurnitureDisplay();
        }
        
        public void SortByName(bool ascending = true)
        {
            if (ascending)
            {
                furnitureItems = furnitureItems.OrderBy(item => item.displayName).ToList();
            }
            else
            {
                furnitureItems = furnitureItems.OrderByDescending(item => item.displayName).ToList();
            }
            
            CategorizeItems();
            UpdateFurnitureDisplay();
        }
    }
} 