using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ARInteriorDesign
{
    public class FurnitureSearchManager : MonoBehaviour
    {
        [Header("Search UI")]
        [SerializeField] private InputField searchInputField;
        [SerializeField] private Button searchButton;
        [SerializeField] private Button clearSearchButton;
        [SerializeField] private Button voiceSearchButton;
        [SerializeField] private Transform searchResultsContainer;
        [SerializeField] private GameObject searchResultItemPrefab;
        [SerializeField] private Text searchStatusText;
        [SerializeField] private Text suggestionsText;
        
        [Header("Filter Options")]
        [SerializeField] private Toggle exactMatchToggle;
        [SerializeField] private Slider priceMinSlider;
        [SerializeField] private Slider priceMaxSlider;
        [SerializeField] private Text priceRangeText;
        [SerializeField] private Dropdown categoryFilterDropdown;
        [SerializeField] private Toggle availableOnlyToggle;
        
        [Header("Search Configuration")]
        [SerializeField] private float searchDelay = 0.5f;
        [SerializeField] private int maxSearchResults = 20;
        [SerializeField] private bool enableFuzzyMatching = true;
        [SerializeField] private bool enableSmartSearch = true;
        
        [Header("Smart Search Keywords")]
        [SerializeField] private List<ColorKeyword> colorKeywords = new List<ColorKeyword>();
        [SerializeField] private List<MaterialKeyword> materialKeywords = new List<MaterialKeyword>();
        [SerializeField] private List<StyleKeyword> styleKeywords = new List<StyleKeyword>();
        [SerializeField] private List<SizeKeyword> sizeKeywords = new List<SizeKeyword>();
        
        private FurnitureCatalog furnitureCatalog;
        private List<FurnitureItem> allFurnitureItems = new List<FurnitureItem>();
        private List<FurnitureItem> currentSearchResults = new List<FurnitureItem>();
        private List<string> searchHistory = new List<string>();
        private Coroutine searchDelayCoroutine;
        
        // Events
        public System.Action<List<FurnitureItem>> OnSearchCompleted;
        public System.Action<FurnitureItem> OnItemSelected;
        public System.Action<string> OnSearchStarted;
        
        [System.Serializable]
        public class ColorKeyword
        {
            public string keyword;
            public List<string> synonyms = new List<string>();
            public Color color = Color.white;
        }
        
        [System.Serializable]
        public class MaterialKeyword
        {
            public string keyword;
            public List<string> synonyms = new List<string>();
            public List<string> relatedTags = new List<string>();
        }
        
        [System.Serializable]
        public class StyleKeyword
        {
            public string keyword;
            public List<string> synonyms = new List<string>();
            public List<string> relatedTags = new List<string>();
        }
        
        [System.Serializable]
        public class SizeKeyword
        {
            public string keyword;
            public List<string> synonyms = new List<string>();
            public Vector3 approximateSize;
            public float tolerance = 0.5f;
        }
        
        private void Start()
        {
            InitializeSearch();
            SetupEventHandlers();
            LoadSearchKeywords();
        }
        
        private void InitializeSearch()
        {
            furnitureCatalog = FindObjectOfType<FurnitureCatalog>();
            
            if (furnitureCatalog != null)
            {
                allFurnitureItems = furnitureCatalog.GetAllFurniture();
            }
            
            // Initialize price range sliders
            if (priceMinSlider != null && priceMaxSlider != null)
            {
                float minPrice = allFurnitureItems.Count > 0 ? allFurnitureItems.Min(item => item.price) : 0f;
                float maxPrice = allFurnitureItems.Count > 0 ? allFurnitureItems.Max(item => item.price) : 2000f;
                
                priceMinSlider.minValue = minPrice;
                priceMinSlider.maxValue = maxPrice;
                priceMinSlider.value = minPrice;
                
                priceMaxSlider.minValue = minPrice;
                priceMaxSlider.maxValue = maxPrice;
                priceMaxSlider.value = maxPrice;
                
                UpdatePriceRangeText();
            }
            
            UpdateSearchStatus("Ready to search");
        }
        
        private void SetupEventHandlers()
        {
            if (searchInputField != null)
            {
                searchInputField.onValueChanged.AddListener(OnSearchInputChanged);
                searchInputField.onEndEdit.AddListener(OnSearchInputEndEdit);
            }
            
            if (searchButton != null)
                searchButton.onClick.AddListener(PerformSearch);
            
            if (clearSearchButton != null)
                clearSearchButton.onClick.AddListener(ClearSearch);
            
            if (voiceSearchButton != null)
                voiceSearchButton.onClick.AddListener(StartVoiceSearch);
            
            if (priceMinSlider != null)
                priceMinSlider.onValueChanged.AddListener(OnPriceRangeChanged);
            
            if (priceMaxSlider != null)
                priceMaxSlider.onValueChanged.AddListener(OnPriceRangeChanged);
            
            if (categoryFilterDropdown != null)
                categoryFilterDropdown.onValueChanged.AddListener(OnCategoryFilterChanged);
            
            if (availableOnlyToggle != null)
                availableOnlyToggle.onValueChanged.AddListener(OnAvailableOnlyChanged);
        }
        
        private void LoadSearchKeywords()
        {
            // Load default color keywords if none are set
            if (colorKeywords.Count == 0)
            {
                colorKeywords.AddRange(new[]
                {
                    new ColorKeyword { keyword = "black", synonyms = new List<string> { "dark", "ebony", "charcoal" }, color = Color.black },
                    new ColorKeyword { keyword = "white", synonyms = new List<string> { "ivory", "cream", "pearl" }, color = Color.white },
                    new ColorKeyword { keyword = "brown", synonyms = new List<string> { "tan", "beige", "walnut", "oak" }, color = new Color(0.6f, 0.4f, 0.2f) },
                    new ColorKeyword { keyword = "red", synonyms = new List<string> { "crimson", "burgundy", "cherry" }, color = Color.red },
                    new ColorKeyword { keyword = "blue", synonyms = new List<string> { "navy", "azure", "sapphire" }, color = Color.blue },
                    new ColorKeyword { keyword = "green", synonyms = new List<string> { "emerald", "forest", "olive" }, color = Color.green },
                    new ColorKeyword { keyword = "grey", synonyms = new List<string> { "gray", "silver", "slate" }, color = Color.grey },
                    new ColorKeyword { keyword = "yellow", synonyms = new List<string> { "gold", "amber", "brass" }, color = Color.yellow }
                });
            }
            
            // Load default material keywords if none are set
            if (materialKeywords.Count == 0)
            {
                materialKeywords.AddRange(new[]
                {
                    new MaterialKeyword { keyword = "leather", synonyms = new List<string> { "hide", "suede" }, relatedTags = new List<string> { "leather", "luxury" } },
                    new MaterialKeyword { keyword = "wood", synonyms = new List<string> { "wooden", "timber", "oak", "pine", "mahogany" }, relatedTags = new List<string> { "wood", "wooden", "natural" } },
                    new MaterialKeyword { keyword = "metal", synonyms = new List<string> { "steel", "iron", "aluminum", "chrome" }, relatedTags = new List<string> { "metal", "metallic", "steel" } },
                    new MaterialKeyword { keyword = "fabric", synonyms = new List<string> { "cloth", "textile", "upholstered" }, relatedTags = new List<string> { "fabric", "soft", "textile" } },
                    new MaterialKeyword { keyword = "glass", synonyms = new List<string> { "crystal", "transparent" }, relatedTags = new List<string> { "glass", "transparent", "crystal" } },
                    new MaterialKeyword { keyword = "plastic", synonyms = new List<string> { "synthetic", "polymer" }, relatedTags = new List<string> { "plastic", "synthetic" } }
                });
            }
            
            // Load default style keywords if none are set
            if (styleKeywords.Count == 0)
            {
                styleKeywords.AddRange(new[]
                {
                    new StyleKeyword { keyword = "modern", synonyms = new List<string> { "contemporary", "sleek", "minimalist" }, relatedTags = new List<string> { "modern", "contemporary" } },
                    new StyleKeyword { keyword = "vintage", synonyms = new List<string> { "retro", "classic", "antique" }, relatedTags = new List<string> { "vintage", "classic" } },
                    new StyleKeyword { keyword = "industrial", synonyms = new List<string> { "rustic", "urban" }, relatedTags = new List<string> { "industrial", "rustic" } },
                    new StyleKeyword { keyword = "luxury", synonyms = new List<string> { "premium", "high-end", "elegant" }, relatedTags = new List<string> { "luxury", "premium" } },
                    new StyleKeyword { keyword = "comfortable", synonyms = new List<string> { "cozy", "soft", "plush" }, relatedTags = new List<string> { "comfort", "soft" } }
                });
            }
            
            // Load default size keywords if none are set
            if (sizeKeywords.Count == 0)
            {
                sizeKeywords.AddRange(new[]
                {
                    new SizeKeyword { keyword = "small", synonyms = new List<string> { "compact", "mini", "tiny" }, approximateSize = new Vector3(1f, 1f, 1f), tolerance = 0.5f },
                    new SizeKeyword { keyword = "medium", synonyms = new List<string> { "mid-size", "standard" }, approximateSize = new Vector3(1.5f, 1.5f, 1.5f), tolerance = 0.5f },
                    new SizeKeyword { keyword = "large", synonyms = new List<string> { "big", "oversized", "jumbo" }, approximateSize = new Vector3(2f, 2f, 2f), tolerance = 0.7f },
                    new SizeKeyword { keyword = "extra large", synonyms = new List<string> { "xl", "huge", "massive" }, approximateSize = new Vector3(3f, 3f, 3f), tolerance = 1f }
                });
            }
        }
        
        private void OnSearchInputChanged(string searchTerm)
        {
            if (searchDelayCoroutine != null)
            {
                StopCoroutine(searchDelayCoroutine);
            }
            
            if (!string.IsNullOrEmpty(searchTerm) && searchTerm.Length >= 2)
            {
                searchDelayCoroutine = StartCoroutine(DelayedSearch(searchTerm));
                ShowSearchSuggestions(searchTerm);
            }
            else
            {
                ClearSearchResults();
            }
        }
        
        private void OnSearchInputEndEdit(string searchTerm)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                PerformSearch();
            }
        }
        
        private System.Collections.IEnumerator DelayedSearch(string searchTerm)
        {
            yield return new WaitForSeconds(searchDelay);
            PerformSmartSearch(searchTerm);
        }
        
        public void PerformSearch()
        {
            if (searchInputField != null)
            {
                string searchTerm = searchInputField.text;
                PerformSmartSearch(searchTerm);
            }
        }
        
        private void PerformSmartSearch(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                ClearSearchResults();
                return;
            }
            
            OnSearchStarted?.Invoke(searchTerm);
            UpdateSearchStatus($"Searching for '{searchTerm}'...");
            
            // Add to search history
            if (!searchHistory.Contains(searchTerm))
            {
                searchHistory.Insert(0, searchTerm);
                if (searchHistory.Count > 10)
                {
                    searchHistory.RemoveAt(searchHistory.Count - 1);
                }
            }
            
            // Perform the search
            List<FurnitureItem> results = enableSmartSearch ? 
                SmartSearchFurniture(searchTerm) : 
                BasicSearchFurniture(searchTerm);
            
            // Apply additional filters
            results = ApplyFilters(results);
            
            // Limit results
            if (results.Count > maxSearchResults)
            {
                results = results.Take(maxSearchResults).ToList();
            }
            
            currentSearchResults = results;
            DisplaySearchResults(results);
            
            string statusMessage = results.Count == 0 ? 
                $"No results found for '{searchTerm}'" : 
                $"Found {results.Count} result{(results.Count == 1 ? "" : "s")} for '{searchTerm}'";
            
            UpdateSearchStatus(statusMessage);
            OnSearchCompleted?.Invoke(results);
        }
        
        private List<FurnitureItem> SmartSearchFurniture(string searchTerm)
        {
            var results = new Dictionary<FurnitureItem, float>();
            searchTerm = searchTerm.ToLower().Trim();
            
            // Parse the search term to extract keywords
            var searchKeywords = ParseSearchTerm(searchTerm);
            
            foreach (var item in allFurnitureItems)
            {
                float score = CalculateRelevanceScore(item, searchTerm, searchKeywords);
                
                if (score > 0)
                {
                    results[item] = score;
                }
            }
            
            // Sort by relevance score (highest first)
            return results.OrderByDescending(pair => pair.Value)
                         .Select(pair => pair.Key)
                         .ToList();
        }
        
        private SearchKeywords ParseSearchTerm(string searchTerm)
        {
            var keywords = new SearchKeywords();
            string[] words = searchTerm.Split(' ');
            
            foreach (string word in words)
            {
                string cleanWord = word.Trim().ToLower();
                
                // Check for colors
                var colorMatch = colorKeywords.FirstOrDefault(c => 
                    c.keyword.Equals(cleanWord, System.StringComparison.OrdinalIgnoreCase) || 
                    c.synonyms.Any(s => s.Equals(cleanWord, System.StringComparison.OrdinalIgnoreCase)));
                if (colorMatch != null)
                {
                    keywords.Colors.Add(colorMatch);
                }
                
                // Check for materials
                var materialMatch = materialKeywords.FirstOrDefault(m => 
                    m.keyword.Equals(cleanWord, System.StringComparison.OrdinalIgnoreCase) || 
                    m.synonyms.Any(s => s.Equals(cleanWord, System.StringComparison.OrdinalIgnoreCase)));
                if (materialMatch != null)
                {
                    keywords.Materials.Add(materialMatch);
                }
                
                // Check for styles
                var styleMatch = styleKeywords.FirstOrDefault(s => 
                    s.keyword.Equals(cleanWord, System.StringComparison.OrdinalIgnoreCase) || 
                    s.synonyms.Any(syn => syn.Equals(cleanWord, System.StringComparison.OrdinalIgnoreCase)));
                if (styleMatch != null)
                {
                    keywords.Styles.Add(styleMatch);
                }
                
                // Check for sizes
                var sizeMatch = sizeKeywords.FirstOrDefault(s => 
                    s.keyword.Equals(cleanWord, System.StringComparison.OrdinalIgnoreCase) || 
                    s.synonyms.Any(syn => syn.Equals(cleanWord, System.StringComparison.OrdinalIgnoreCase)));
                if (sizeMatch != null)
                {
                    keywords.Sizes.Add(sizeMatch);
                }
                
                // Add as general keyword if not matched above
                if (colorMatch == null && materialMatch == null && styleMatch == null && sizeMatch == null)
                {
                    keywords.GeneralKeywords.Add(cleanWord);
                }
            }
            
            return keywords;
        }
        
        private float CalculateRelevanceScore(FurnitureItem item, string searchTerm, SearchKeywords keywords)
        {
            float score = 0f;
            
            // Exact name match gets highest score
            if (item.displayName.ToLower().Contains(searchTerm))
            {
                score += 100f;
            }
            
            // Check description match
            if (item.description.ToLower().Contains(searchTerm))
            {
                score += 50f;
            }
            
            // Check tag matches
            foreach (string tag in item.tags)
            {
                if (tag.ToLower().Contains(searchTerm))
                {
                    score += 30f;
                }
            }
            
            // Color matching
            foreach (var color in keywords.Colors)
            {
                if (item.tags.Any(tag => color.relatedTags?.Contains(tag.ToLower()) == true ||
                                        tag.ToLower().Contains(color.keyword) ||
                                        color.synonyms.Any(syn => tag.ToLower().Contains(syn))))
                {
                    score += 40f;
                }
            }
            
            // Material matching
            foreach (var material in keywords.Materials)
            {
                if (item.tags.Any(tag => material.relatedTags.Any(relTag => tag.ToLower().Contains(relTag)) ||
                                        tag.ToLower().Contains(material.keyword) ||
                                        material.synonyms.Any(syn => tag.ToLower().Contains(syn))))
                {
                    score += 35f;
                }
            }
            
            // Style matching
            foreach (var style in keywords.Styles)
            {
                if (item.tags.Any(tag => style.relatedTags.Any(relTag => tag.ToLower().Contains(relTag)) ||
                                        tag.ToLower().Contains(style.keyword) ||
                                        style.synonyms.Any(syn => tag.ToLower().Contains(syn))))
                {
                    score += 25f;
                }
            }
            
            // Size matching (based on default scale)
            foreach (var size in keywords.Sizes)
            {
                float sizeDifference = Vector3.Distance(item.defaultScale, size.approximateSize);
                if (sizeDifference <= size.tolerance)
                {
                    score += 20f * (1f - (sizeDifference / size.tolerance));
                }
            }
            
            // General keyword matching
            foreach (var keyword in keywords.GeneralKeywords)
            {
                if (item.displayName.ToLower().Contains(keyword) ||
                    item.description.ToLower().Contains(keyword) ||
                    item.tags.Any(tag => tag.ToLower().Contains(keyword)))
                {
                    score += 15f;
                }
            }
            
            // Brand matching
            if (item.brand != null && item.brand.ToLower().Contains(searchTerm))
            {
                score += 10f;
            }
            
            // Fuzzy matching for typos
            if (enableFuzzyMatching)
            {
                score += CalculateFuzzyMatchScore(item, searchTerm) * 5f;
            }
            
            return score;
        }
        
        private float CalculateFuzzyMatchScore(FurnitureItem item, string searchTerm)
        {
            // Simple Levenshtein distance-based fuzzy matching
            string itemName = item.displayName.ToLower();
            int distance = LevenshteinDistance(itemName, searchTerm);
            float maxLength = Mathf.Max(itemName.Length, searchTerm.Length);
            
            if (maxLength == 0) return 0f;
            
            float similarity = 1f - (distance / maxLength);
            return similarity > 0.6f ? similarity : 0f; // Only return score if similarity is above threshold
        }
        
        private int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;
            
            int[,] distance = new int[source.Length + 1, target.Length + 1];
            
            for (int i = 0; i <= source.Length; distance[i, 0] = i++) { }
            for (int j = 0; j <= target.Length; distance[0, j] = j++) { }
            
            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i, j] = Mathf.Min(
                        distance[i - 1, j] + 1,
                        Mathf.Min(distance[i, j - 1] + 1, distance[i - 1, j - 1] + cost));
                }
            }
            
            return distance[source.Length, target.Length];
        }
        
        private List<FurnitureItem> BasicSearchFurniture(string searchTerm)
        {
            return allFurnitureItems.Where(item =>
                item.displayName.ToLower().Contains(searchTerm.ToLower()) ||
                item.description.ToLower().Contains(searchTerm.ToLower()) ||
                item.tags.Any(tag => tag.ToLower().Contains(searchTerm.ToLower()))
            ).ToList();
        }
        
        private List<FurnitureItem> ApplyFilters(List<FurnitureItem> items)
        {
            var filteredItems = items;
            
            // Price range filter
            if (priceMinSlider != null && priceMaxSlider != null)
            {
                float minPrice = priceMinSlider.value;
                float maxPrice = priceMaxSlider.value;
                filteredItems = filteredItems.Where(item => item.price >= minPrice && item.price <= maxPrice).ToList();
            }
            
            // Category filter
            if (categoryFilterDropdown != null && categoryFilterDropdown.value > 0)
            {
                var selectedCategory = (FurnitureCategory)(categoryFilterDropdown.value - 1);
                filteredItems = filteredItems.Where(item => item.category == selectedCategory).ToList();
            }
            
            // Available only filter
            if (availableOnlyToggle != null && availableOnlyToggle.isOn)
            {
                filteredItems = filteredItems.Where(item => item.isAvailable).ToList();
            }
            
            return filteredItems;
        }
        
        private void DisplaySearchResults(List<FurnitureItem> results)
        {
            ClearSearchResults();
            
            if (searchResultsContainer == null || searchResultItemPrefab == null) return;
            
            foreach (var item in results)
            {
                CreateSearchResultItem(item);
            }
        }
        
        private void CreateSearchResultItem(FurnitureItem item)
        {
            GameObject resultItem = Instantiate(searchResultItemPrefab, searchResultsContainer);
            
            // Setup result item components
            Text nameText = resultItem.transform.Find("Name")?.GetComponent<Text>();
            Text descriptionText = resultItem.transform.Find("Description")?.GetComponent<Text>();
            Text priceText = resultItem.transform.Find("Price")?.GetComponent<Text>();
            Image iconImage = resultItem.transform.Find("Icon")?.GetComponent<Image>();
            Button selectButton = resultItem.GetComponent<Button>();
            
            if (nameText != null) nameText.text = item.displayName;
            if (descriptionText != null) descriptionText.text = item.description;
            if (priceText != null) priceText.text = $"${item.price:F2}";
            if (iconImage != null && item.icon != null) iconImage.sprite = item.icon;
            
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => SelectSearchResult(item));
            }
        }
        
        private void SelectSearchResult(FurnitureItem item)
        {
            OnItemSelected?.Invoke(item);
            
            // Optionally close search results or hide the search panel
            if (furnitureCatalog != null)
            {
                furnitureCatalog.SelectFurniture(item);
            }
        }
        
        private void ClearSearchResults()
        {
            if (searchResultsContainer != null)
            {
                foreach (Transform child in searchResultsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            
            currentSearchResults.Clear();
        }
        
        public void ClearSearch()
        {
            if (searchInputField != null)
            {
                searchInputField.text = "";
            }
            
            ClearSearchResults();
            UpdateSearchStatus("Search cleared");
            HideSearchSuggestions();
        }
        
        private void ShowSearchSuggestions(string searchTerm)
        {
            if (suggestionsText == null) return;
            
            // Generate suggestions based on search history and available furniture
            var suggestions = GenerateSearchSuggestions(searchTerm);
            
            if (suggestions.Count > 0)
            {
                suggestionsText.gameObject.SetActive(true);
                suggestionsText.text = "Suggestions: " + string.Join(", ", suggestions.Take(3));
            }
            else
            {
                HideSearchSuggestions();
            }
        }
        
        private List<string> GenerateSearchSuggestions(string searchTerm)
        {
            var suggestions = new List<string>();
            
            // Add suggestions from search history
            suggestions.AddRange(searchHistory.Where(h => h.ToLower().StartsWith(searchTerm.ToLower())));
            
            // Add suggestions from furniture names
            suggestions.AddRange(allFurnitureItems
                .Where(item => item.displayName.ToLower().StartsWith(searchTerm.ToLower()))
                .Select(item => item.displayName)
                .Distinct());
            
            // Add suggestions from popular search terms
            var popularTerms = new[] { "chair", "table", "sofa", "bed", "lamp", "shelf", "modern", "leather", "wood" };
            suggestions.AddRange(popularTerms.Where(term => term.StartsWith(searchTerm.ToLower())));
            
            return suggestions.Distinct().Take(5).ToList();
        }
        
        private void HideSearchSuggestions()
        {
            if (suggestionsText != null)
            {
                suggestionsText.gameObject.SetActive(false);
            }
        }
        
        private void StartVoiceSearch()
        {
            // Placeholder for voice search functionality
            // This would integrate with platform-specific speech recognition
            Debug.Log("Voice search not implemented yet");
            UpdateSearchStatus("Voice search not available");
        }
        
        private void OnPriceRangeChanged(float value)
        {
            UpdatePriceRangeText();
            
            if (!string.IsNullOrEmpty(searchInputField?.text))
            {
                PerformSearch();
            }
        }
        
        private void UpdatePriceRangeText()
        {
            if (priceRangeText != null && priceMinSlider != null && priceMaxSlider != null)
            {
                priceRangeText.text = $"${priceMinSlider.value:F0} - ${priceMaxSlider.value:F0}";
            }
        }
        
        private void OnCategoryFilterChanged(int categoryIndex)
        {
            if (!string.IsNullOrEmpty(searchInputField?.text))
            {
                PerformSearch();
            }
        }
        
        private void OnAvailableOnlyChanged(bool isOn)
        {
            if (!string.IsNullOrEmpty(searchInputField?.text))
            {
                PerformSearch();
            }
        }
        
        private void UpdateSearchStatus(string status)
        {
            if (searchStatusText != null)
            {
                searchStatusText.text = status;
            }
        }
        
        public List<FurnitureItem> GetCurrentResults()
        {
            return new List<FurnitureItem>(currentSearchResults);
        }
        
        public List<string> GetSearchHistory()
        {
            return new List<string>(searchHistory);
        }
        
        private class SearchKeywords
        {
            public List<ColorKeyword> Colors = new List<ColorKeyword>();
            public List<MaterialKeyword> Materials = new List<MaterialKeyword>();
            public List<StyleKeyword> Styles = new List<StyleKeyword>();
            public List<SizeKeyword> Sizes = new List<SizeKeyword>();
            public List<string> GeneralKeywords = new List<string>();
        }
    }
} 