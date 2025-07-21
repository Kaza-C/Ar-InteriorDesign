using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ARInteriorDesign
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Main Menu UI")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Text gameTitleText;
        [SerializeField] private Button customizeRoomButton; // AR Mode
        [SerializeField] private Button blankRoomButton; // VR Mode
        [SerializeField] private Button loadRoomButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitGameButton;
        
        [Header("Load Room UI")]
        [SerializeField] private GameObject loadRoomPanel;
        [SerializeField] private Transform savedRoomsContainer;
        [SerializeField] private GameObject savedRoomItemPrefab;
        [SerializeField] private Button closeLoadRoomButton;
        [SerializeField] private Button deleteAllRoomsButton;
        [SerializeField] private Text noRoomsFoundText;
        
        [Header("Settings UI")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Button resetSettingsButton;
        [SerializeField] private Button closeSettingsButton;
        
        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private Text confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        
        [Header("Version Info")]
        [SerializeField] private Text versionText;
        [SerializeField] private string gameVersion = "1.0.0";
        
        [Header("Animation")]
        [SerializeField] private float buttonAnimationDuration = 0.3f;
        [SerializeField] private AnimationCurve buttonAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private List<SavedRoomData> savedRooms = new List<SavedRoomData>();
        private System.Action currentConfirmationAction;
        private AudioSource audioSource;
        
        [System.Serializable]
        public class SavedRoomData
        {
            public string roomName;
            public string fileName;
            public System.DateTime saveDate;
            public int furnitureCount;
            public string previewImagePath;
        }
        
        private void Start()
        {
            InitializeMainMenu();
            SetupEventHandlers();
            LoadSettings();
        }
        
        private void InitializeMainMenu()
        {
            // Get audio source component
            audioSource = GetComponent<AudioSource>();
            
            // Set game title (placeholder for now)
            if (gameTitleText != null)
            {
                gameTitleText.text = "AR Interior Designer"; // We'll customize this later
            }
            
            // Set version info
            if (versionText != null)
            {
                versionText.text = $"Version {gameVersion}";
            }
            
            // Show main menu panel
            ShowMainMenu();
            
            // Hide other panels
            if (loadRoomPanel != null) loadRoomPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (confirmationDialog != null) confirmationDialog.SetActive(false);
        }
        
        private void SetupEventHandlers()
        {
            // Main menu buttons
            if (customizeRoomButton != null)
                customizeRoomButton.onClick.AddListener(() => StartGame(RoomMode.CustomRoom));
            
            if (blankRoomButton != null)
                blankRoomButton.onClick.AddListener(() => StartGame(RoomMode.BlankRoom));
            
            if (loadRoomButton != null)
                loadRoomButton.onClick.AddListener(ShowLoadRoomPanel);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(ShowSettingsPanel);
            
            if (quitGameButton != null)
                quitGameButton.onClick.AddListener(ShowQuitConfirmation);
            
            // Load room panel buttons
            if (closeLoadRoomButton != null)
                closeLoadRoomButton.onClick.AddListener(HideLoadRoomPanel);
            
            if (deleteAllRoomsButton != null)
                deleteAllRoomsButton.onClick.AddListener(ShowDeleteAllRoomsConfirmation);
            
            // Settings panel buttons
            if (closeSettingsButton != null)
                closeSettingsButton.onClick.AddListener(HideSettingsPanel);
            
            if (resetSettingsButton != null)
                resetSettingsButton.onClick.AddListener(ShowResetSettingsConfirmation);
            
            // Settings controls
            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
            
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            
            // Confirmation dialog buttons
            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmationYes);
            
            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnConfirmationNo);
        }
        
        public void ShowMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
                AnimateMenuAppearance();
            }
        }
        
        public void HideMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }
        }
        
        private void AnimateMenuAppearance()
        {
            // Simple scale animation for menu appearance
            if (mainMenuPanel != null)
            {
                StartCoroutine(AnimateScale(mainMenuPanel.transform, Vector3.zero, Vector3.one, buttonAnimationDuration));
            }
        }
        
        private System.Collections.IEnumerator AnimateScale(Transform target, Vector3 startScale, Vector3 endScale, float duration)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = buttonAnimationCurve.Evaluate(elapsedTime / duration);
                target.localScale = Vector3.Lerp(startScale, endScale, progress);
                yield return null;
            }
            
            target.localScale = endScale;
        }
        
        private void StartGame(RoomMode roomMode)
        {
            PlayButtonSound();
            
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.StartGame(roomMode);
            }
            else
            {
                Debug.LogError("GameStateManager not found!");
            }
        }
        
        private void ShowLoadRoomPanel()
        {
            PlayButtonSound();
            
            if (loadRoomPanel != null)
            {
                loadRoomPanel.SetActive(true);
                LoadSavedRooms();
            }
        }
        
        private void HideLoadRoomPanel()
        {
            PlayButtonSound();
            
            if (loadRoomPanel != null)
            {
                loadRoomPanel.SetActive(false);
            }
        }
        
        private void LoadSavedRooms()
        {
            savedRooms.Clear();
            
            // Clear existing room items
            if (savedRoomsContainer != null)
            {
                foreach (Transform child in savedRoomsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Load saved rooms from PlayerPrefs
            string savedRoomsData = PlayerPrefs.GetString("SavedRooms", "");
            
            if (string.IsNullOrEmpty(savedRoomsData))
            {
                ShowNoRoomsMessage();
                return;
            }
            
            string[] roomNames = savedRoomsData.Split(',');
            
            foreach (string roomName in roomNames)
            {
                if (string.IsNullOrEmpty(roomName)) continue;
                
                string roomData = PlayerPrefs.GetString($"Room_{roomName}", "");
                if (!string.IsNullOrEmpty(roomData))
                {
                    SavedRoomData savedRoom = ParseSavedRoomData(roomName, roomData);
                    savedRooms.Add(savedRoom);
                    CreateSavedRoomItem(savedRoom);
                }
            }
            
            if (savedRooms.Count == 0)
            {
                ShowNoRoomsMessage();
            }
            else
            {
                HideNoRoomsMessage();
            }
        }
        
        private SavedRoomData ParseSavedRoomData(string roomName, string jsonData)
        {
            SavedRoomData roomData = new SavedRoomData
            {
                roomName = roomName,
                fileName = roomName,
                saveDate = System.DateTime.Now // Default to now, could parse from data
            };
            
            try
            {
                // Try to parse JSON to get furniture count
                var roomDataJson = JsonUtility.FromJson<FurnitureManager.RoomData>(jsonData);
                if (roomDataJson != null && roomDataJson.furnitureData != null)
                {
                    roomData.furnitureCount = roomDataJson.furnitureData.Count;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not parse room data for {roomName}: {e.Message}");
                roomData.furnitureCount = 0;
            }
            
            return roomData;
        }
        
        private void CreateSavedRoomItem(SavedRoomData roomData)
        {
            if (savedRoomItemPrefab == null || savedRoomsContainer == null) return;
            
            GameObject roomItem = Instantiate(savedRoomItemPrefab, savedRoomsContainer);
            
            // Setup room item components
            Text nameText = roomItem.transform.Find("RoomName")?.GetComponent<Text>();
            Text dateText = roomItem.transform.Find("SaveDate")?.GetComponent<Text>();
            Text furnitureCountText = roomItem.transform.Find("FurnitureCount")?.GetComponent<Text>();
            Button loadButton = roomItem.transform.Find("LoadButton")?.GetComponent<Button>();
            Button deleteButton = roomItem.transform.Find("DeleteButton")?.GetComponent<Button>();
            
            if (nameText != null)
                nameText.text = roomData.roomName;
            
            if (dateText != null)
                dateText.text = roomData.saveDate.ToString("MM/dd/yyyy HH:mm");
            
            if (furnitureCountText != null)
                furnitureCountText.text = $"{roomData.furnitureCount} items";
            
            if (loadButton != null)
            {
                loadButton.onClick.AddListener(() => LoadRoom(roomData));
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() => ShowDeleteRoomConfirmation(roomData));
            }
        }
        
        private void LoadRoom(SavedRoomData roomData)
        {
            PlayButtonSound();
            
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.LoadRoom(roomData.roomName);
            }
        }
        
        private void ShowNoRoomsMessage()
        {
            if (noRoomsFoundText != null)
            {
                noRoomsFoundText.gameObject.SetActive(true);
            }
        }
        
        private void HideNoRoomsMessage()
        {
            if (noRoomsFoundText != null)
            {
                noRoomsFoundText.gameObject.SetActive(false);
            }
        }
        
        private void ShowSettingsPanel()
        {
            PlayButtonSound();
            
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                LoadSettingsUI();
            }
        }
        
        private void HideSettingsPanel()
        {
            PlayButtonSound();
            
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            
            SaveSettings();
        }
        
        private void LoadSettingsUI()
        {
            // Load volume setting
            if (volumeSlider != null)
            {
                volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            }
            
            // Load fullscreen setting
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
            }
            
            // Load quality setting
            if (qualityDropdown != null)
            {
                qualityDropdown.value = QualitySettings.GetQualityLevel();
            }
        }
        
        private void LoadSettings()
        {
            // Apply saved settings
            float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            AudioListener.volume = volume;
        }
        
        private void SaveSettings()
        {
            // Save current settings
            if (volumeSlider != null)
            {
                PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);
            }
            
            PlayerPrefs.Save();
        }
        
        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
        }
        
        private void OnFullscreenToggleChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }
        
        private void OnQualityChanged(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
        }
        
        private void ShowQuitConfirmation()
        {
            ShowConfirmation("Are you sure you want to quit the game?", () => QuitGame());
        }
        
        private void ShowDeleteRoomConfirmation(SavedRoomData roomData)
        {
            ShowConfirmation($"Are you sure you want to delete room '{roomData.roomName}'?", () => DeleteRoom(roomData));
        }
        
        private void ShowDeleteAllRoomsConfirmation()
        {
            ShowConfirmation("Are you sure you want to delete ALL saved rooms? This action cannot be undone.", () => DeleteAllRooms());
        }
        
        private void ShowResetSettingsConfirmation()
        {
            ShowConfirmation("Are you sure you want to reset all settings to default?", () => ResetSettings());
        }
        
        private void ShowConfirmation(string message, System.Action confirmAction)
        {
            if (confirmationDialog != null && confirmationText != null)
            {
                confirmationDialog.SetActive(true);
                confirmationText.text = message;
                currentConfirmationAction = confirmAction;
            }
        }
        
        private void OnConfirmationYes()
        {
            PlayButtonSound();
            
            currentConfirmationAction?.Invoke();
            HideConfirmation();
        }
        
        private void OnConfirmationNo()
        {
            PlayButtonSound();
            HideConfirmation();
        }
        
        private void HideConfirmation()
        {
            if (confirmationDialog != null)
            {
                confirmationDialog.SetActive(false);
            }
            
            currentConfirmationAction = null;
        }
        
        private void QuitGame()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.QuitGame();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
        
        private void DeleteRoom(SavedRoomData roomData)
        {
            // Remove from PlayerPrefs
            PlayerPrefs.DeleteKey($"Room_{roomData.roomName}");
            
            // Update saved rooms list
            string savedRoomsData = PlayerPrefs.GetString("SavedRooms", "");
            string[] roomNames = savedRoomsData.Split(',');
            var updatedRooms = roomNames.Where(name => name != roomData.roomName).ToArray();
            PlayerPrefs.SetString("SavedRooms", string.Join(",", updatedRooms));
            PlayerPrefs.Save();
            
            // Refresh the UI
            LoadSavedRooms();
        }
        
        private void DeleteAllRooms()
        {
            // Get all saved rooms
            string savedRoomsData = PlayerPrefs.GetString("SavedRooms", "");
            if (!string.IsNullOrEmpty(savedRoomsData))
            {
                string[] roomNames = savedRoomsData.Split(',');
                foreach (string roomName in roomNames)
                {
                    if (!string.IsNullOrEmpty(roomName))
                    {
                        PlayerPrefs.DeleteKey($"Room_{roomName}");
                    }
                }
            }
            
            // Clear the saved rooms list
            PlayerPrefs.DeleteKey("SavedRooms");
            PlayerPrefs.Save();
            
            // Refresh the UI
            LoadSavedRooms();
        }
        
        private void ResetSettings()
        {
            // Reset to default values
            if (volumeSlider != null)
            {
                volumeSlider.value = 1f;
                AudioListener.volume = 1f;
            }
            
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = true;
                Screen.fullScreen = true;
            }
            
            if (qualityDropdown != null)
            {
                qualityDropdown.value = QualitySettings.names.Length - 1; // Highest quality
                QualitySettings.SetQualityLevel(qualityDropdown.value);
            }
            
            // Clear saved settings
            PlayerPrefs.DeleteKey("MasterVolume");
            PlayerPrefs.Save();
        }
        
        private void PlayButtonSound()
        {
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }
        
        private void Update()
        {
            // Handle back button on mobile/Quest
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackButton();
            }
        }
        
        private void HandleBackButton()
        {
            if (confirmationDialog != null && confirmationDialog.activeInHierarchy)
            {
                OnConfirmationNo();
            }
            else if (settingsPanel != null && settingsPanel.activeInHierarchy)
            {
                HideSettingsPanel();
            }
            else if (loadRoomPanel != null && loadRoomPanel.activeInHierarchy)
            {
                HideLoadRoomPanel();
            }
            else
            {
                ShowQuitConfirmation();
            }
        }
    }
} 