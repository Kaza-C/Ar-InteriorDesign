using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq; // Added for .First()

namespace ARInteriorDesign
{
    public class InGameMenuManager : MonoBehaviour
    {
        [Header("In-Game Menu UI")]
        [SerializeField] private GameObject inGameMenuPanel;
        [SerializeField] private Button menuToggleButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveRoomButton;
        [SerializeField] private Button saveAndExitButton;
        [SerializeField] private Button exitWithoutSavingButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button quitToDesktopButton;
        
        [Header("Quick Save UI")]
        [SerializeField] private GameObject quickSavePanel;
        [SerializeField] private InputField roomNameInput;
        [SerializeField] private Button saveConfirmButton;
        [SerializeField] private Button saveCancelButton;
        [SerializeField] private Text saveStatusText;
        
        [Header("Settings Menu")]
        [SerializeField] private GameObject settingsSubPanel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Toggle handTrackingToggle;
        [SerializeField] private Toggle planeVisualizationToggle;
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Button resetSettingsButton;
        [SerializeField] private Button closeSettingsButton;
        
        [Header("Help Menu")]
        [SerializeField] private GameObject helpSubPanel;
        [SerializeField] private Text helpContentText;
        [SerializeField] private Button closeHelpButton;
        [SerializeField] private Transform helpTopicsContainer;
        [SerializeField] private GameObject helpTopicButtonPrefab;
        
        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private Text confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        
        [Header("Menu Animation")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private CanvasGroup menuCanvasGroup;
        
        [Header("Input Settings")]
        [SerializeField] private KeyCode menuToggleKey = KeyCode.Escape;
        [SerializeField] private bool pauseOnMenuOpen = true;
        
        private bool isMenuOpen = false;
        private System.Action currentConfirmationAction;
        private GameStateManager gameStateManager;
        private ARInteriorDesignManager arManager;
        private ARUIManager uiManager;
        private AudioSource audioSource;
        private string lastSavedRoomName = "";
        
        // Help topics data
        private Dictionary<string, string> helpTopics = new Dictionary<string, string>();
        
        // Events
        public System.Action OnMenuOpened;
        public System.Action OnMenuClosed;
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;
        
        private void Start()
        {
            InitializeInGameMenu();
            SetupEventHandlers();
            LoadHelpTopics();
            SetupInputField();
        }
        
        private void InitializeInGameMenu()
        {
            // Find required managers
            gameStateManager = GameStateManager.Instance;
            arManager = FindObjectOfType<ARInteriorDesignManager>();
            uiManager = FindObjectOfType<ARUIManager>();
            audioSource = GetComponent<AudioSource>();
            
            // Initialize UI state
            HideInGameMenu();
            if (quickSavePanel != null) quickSavePanel.SetActive(false);
            if (settingsSubPanel != null) settingsSubPanel.SetActive(false);
            if (helpSubPanel != null) helpSubPanel.SetActive(false);
            if (confirmationDialog != null) confirmationDialog.SetActive(false);
            
            // Generate default room name
            GenerateDefaultRoomName();
        }
        
        private void SetupEventHandlers()
        {
            // Main menu buttons
            if (menuToggleButton != null)
                menuToggleButton.onClick.AddListener(ToggleInGameMenu);
            
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);
            
            if (saveRoomButton != null)
                saveRoomButton.onClick.AddListener(ShowQuickSavePanel);
            
            if (saveAndExitButton != null)
                saveAndExitButton.onClick.AddListener(ShowSaveAndExitConfirmation);
            
            if (exitWithoutSavingButton != null)
                exitWithoutSavingButton.onClick.AddListener(ShowExitWithoutSavingConfirmation);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(ShowSettingsSubPanel);
            
            if (helpButton != null)
                helpButton.onClick.AddListener(ShowHelpSubPanel);
            
            if (quitToDesktopButton != null)
                quitToDesktopButton.onClick.AddListener(ShowQuitToDesktopConfirmation);
            
            // Quick save panel buttons
            if (saveConfirmButton != null)
                saveConfirmButton.onClick.AddListener(ConfirmSaveRoom);
            
            if (saveCancelButton != null)
                saveCancelButton.onClick.AddListener(CancelSaveRoom);
            
            // Settings buttons
            if (closeSettingsButton != null)
                closeSettingsButton.onClick.AddListener(HideSettingsSubPanel);
            
            if (resetSettingsButton != null)
                resetSettingsButton.onClick.AddListener(ShowResetSettingsConfirmation);
            
            // Help buttons
            if (closeHelpButton != null)
                closeHelpButton.onClick.AddListener(HideHelpSubPanel);
            
            // Confirmation dialog buttons
            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmationYes);
            
            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnConfirmationNo);
            
            // Settings controls
            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            
            if (brightnessSlider != null)
                brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            
            if (handTrackingToggle != null)
                handTrackingToggle.onValueChanged.AddListener(OnHandTrackingToggled);
            
            if (planeVisualizationToggle != null)
                planeVisualizationToggle.onValueChanged.AddListener(OnPlaneVisualizationToggled);
            
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }
        
        private void SetupInputField()
        {
            if (roomNameInput != null)
            {
                roomNameInput.onValueChanged.AddListener(OnRoomNameChanged);
            }
        }
        
        private void LoadHelpTopics()
        {
            helpTopics.Clear();
            
            helpTopics.Add("Getting Started", 
                "Welcome to AR Interior Designer!\n\n" +
                "1. Look around to scan your room\n" +
                "2. Tap the catalog button to browse furniture\n" +
                "3. Select an item and tap to place it\n" +
                "4. Use gestures or controllers to manipulate objects");
            
            helpTopics.Add("Placing Furniture", 
                "To place furniture:\n\n" +
                "1. Select an item from the catalog\n" +
                "2. Point your device at a flat surface\n" +
                "3. When the placement indicator appears, tap to place\n" +
                "4. Use the manipulation tools to adjust position");
            
            helpTopics.Add("Moving Objects", 
                "To move furniture:\n\n" +
                "1. Tap on an object to select it\n" +
                "2. Use the move tool from the manipulation panel\n" +
                "3. Drag with your finger or use controller thumbstick\n" +
                "4. Tap elsewhere to deselect");
            
            helpTopics.Add("Search Function", 
                "Advanced Search:\n\n" +
                "1. Use natural language like 'black leather couch'\n" +
                "2. Search by color, material, or style\n" +
                "3. Use filters to narrow down results\n" +
                "4. Voice search available on supported devices");
            
            helpTopics.Add("Saving Rooms", 
                "Save your designs:\n\n" +
                "1. Use the menu button (≡) during gameplay\n" +
                "2. Select 'Save Room' and enter a name\n" +
                "3. Rooms are saved locally on your device\n" +
                "4. Load saved rooms from the main menu");
            
            helpTopics.Add("AR vs VR Mode", 
                "Choose your mode:\n\n" +
                "AR Mode: Customize your real room\n" +
                "- Uses your actual room as the environment\n" +
                "- Place virtual furniture in real space\n\n" +
                "VR Mode: Design in a blank room\n" +
                "- Start with an empty virtual space\n" +
                "- Full creative freedom");
            
            CreateHelpTopicButtons();
        }
        
        private void CreateHelpTopicButtons()
        {
            if (helpTopicsContainer == null || helpTopicButtonPrefab == null) return;
            
            // Clear existing buttons
            foreach (Transform child in helpTopicsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create buttons for each help topic
            foreach (var topic in helpTopics)
            {
                GameObject buttonObj = Instantiate(helpTopicButtonPrefab, helpTopicsContainer);
                Button button = buttonObj.GetComponent<Button>();
                Text buttonText = buttonObj.GetComponentInChildren<Text>();
                
                if (buttonText != null)
                    buttonText.text = topic.Key;
                
                if (button != null)
                {
                    string topicContent = topic.Value; // Capture for closure
                    button.onClick.AddListener(() => ShowHelpTopic(topicContent));
                }
            }
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        private void HandleInput()
        {
            // Toggle menu with keyboard/controller
            if (Input.GetKeyDown(menuToggleKey))
            {
                ToggleInGameMenu();
            }
            
            // Handle Quest 3 controller input
            if (OVRInput.GetDown(OVRInput.Button.Start))
            {
                ToggleInGameMenu();
            }
            
            // Handle back button navigation
            if (Input.GetKeyDown(KeyCode.Escape) && isMenuOpen)
            {
                HandleBackNavigation();
            }
        }
        
        private void HandleBackNavigation()
        {
            if (confirmationDialog != null && confirmationDialog.activeInHierarchy)
            {
                OnConfirmationNo();
            }
            else if (helpSubPanel != null && helpSubPanel.activeInHierarchy)
            {
                HideHelpSubPanel();
            }
            else if (settingsSubPanel != null && settingsSubPanel.activeInHierarchy)
            {
                HideSettingsSubPanel();
            }
            else if (quickSavePanel != null && quickSavePanel.activeInHierarchy)
            {
                CancelSaveRoom();
            }
            else
            {
                ResumeGame();
            }
        }
        
        public void ToggleInGameMenu()
        {
            if (isMenuOpen)
            {
                HideInGameMenu();
            }
            else
            {
                ShowInGameMenu();
            }
        }
        
        public void ShowInGameMenu()
        {
            if (inGameMenuPanel == null) return;
            
            isMenuOpen = true;
            inGameMenuPanel.SetActive(true);
            
            if (pauseOnMenuOpen && gameStateManager != null)
            {
                gameStateManager.PauseGameMenu();
            }
            
            StartCoroutine(AnimateMenuAppearance(true));
            OnMenuOpened?.Invoke();
            OnGamePaused?.Invoke();
            
            PlayMenuSound();
        }
        
        public void HideInGameMenu()
        {
            if (inGameMenuPanel == null) return;
            
            StartCoroutine(AnimateMenuAppearance(false, () => {
                isMenuOpen = false;
                inGameMenuPanel.SetActive(false);
                
                // Hide sub-panels
                if (quickSavePanel != null) quickSavePanel.SetActive(false);
                if (settingsSubPanel != null) settingsSubPanel.SetActive(false);
                if (helpSubPanel != null) helpSubPanel.SetActive(false);
                if (confirmationDialog != null) confirmationDialog.SetActive(false);
            }));
            
            OnMenuClosed?.Invoke();
            OnGameResumed?.Invoke();
            
            PlayMenuSound();
        }
        
        private IEnumerator AnimateMenuAppearance(bool showing, System.Action onComplete = null)
        {
            if (menuCanvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }
            
            float startAlpha = showing ? 0f : 1f;
            float endAlpha = showing ? 1f : 0f;
            float elapsedTime = 0f;
            
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = animationCurve.Evaluate(elapsedTime / animationDuration);
                menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                yield return null;
            }
            
            menuCanvasGroup.alpha = endAlpha;
            onComplete?.Invoke();
        }
        
        private void ResumeGame()
        {
            if (gameStateManager != null)
            {
                gameStateManager.ResumeGame();
            }
            
            HideInGameMenu();
        }
        
        private void ShowQuickSavePanel()
        {
            if (quickSavePanel != null)
            {
                quickSavePanel.SetActive(true);
                
                if (roomNameInput != null)
                {
                    roomNameInput.text = lastSavedRoomName;
                    roomNameInput.Select();
                }
            }
            
            PlayMenuSound();
        }
        
        private void CancelSaveRoom()
        {
            if (quickSavePanel != null)
            {
                quickSavePanel.SetActive(false);
            }
            
            PlayMenuSound();
        }
        
        private void ConfirmSaveRoom()
        {
            string roomName = roomNameInput?.text ?? GenerateDefaultRoomName();
            
            if (string.IsNullOrEmpty(roomName.Trim()))
            {
                ShowSaveStatus("Please enter a room name", Color.red);
                return;
            }
            
            // Save the room using UIManager
            if (uiManager != null)
            {
                uiManager.SaveRoom();
                lastSavedRoomName = roomName;
                ShowSaveStatus($"Room '{roomName}' saved successfully!", Color.green);
                
                StartCoroutine(DelayedCloseSavePanel());
            }
            else
            {
                ShowSaveStatus("Failed to save room", Color.red);
            }
            
            PlayMenuSound();
        }
        
        private IEnumerator DelayedCloseSavePanel()
        {
            yield return new WaitForSecondsRealtime(2f);
            CancelSaveRoom();
        }
        
        private void ShowSaveStatus(string message, Color color)
        {
            if (saveStatusText != null)
            {
                saveStatusText.text = message;
                saveStatusText.color = color;
                saveStatusText.gameObject.SetActive(true);
            }
        }
        
        private string GenerateDefaultRoomName()
        {
            return $"Room_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        }
        
        private void OnRoomNameChanged(string roomName)
        {
            // Hide save status when user starts typing
            if (saveStatusText != null)
            {
                saveStatusText.gameObject.SetActive(false);
            }
        }
        
        private void ShowSettingsSubPanel()
        {
            if (settingsSubPanel != null)
            {
                settingsSubPanel.SetActive(true);
                LoadCurrentSettings();
            }
            
            PlayMenuSound();
        }
        
        private void HideSettingsSubPanel()
        {
            if (settingsSubPanel != null)
            {
                settingsSubPanel.SetActive(false);
                SaveCurrentSettings();
            }
            
            PlayMenuSound();
        }
        
        private void LoadCurrentSettings()
        {
            if (volumeSlider != null)
                volumeSlider.value = AudioListener.volume;
            
            if (brightnessSlider != null)
                brightnessSlider.value = PlayerPrefs.GetFloat("Brightness", 1f);
            
            if (handTrackingToggle != null)
                handTrackingToggle.isOn = PlayerPrefs.GetInt("HandTracking", 1) == 1;
            
            if (planeVisualizationToggle != null)
                planeVisualizationToggle.isOn = PlayerPrefs.GetInt("PlaneVisualization", 1) == 1;
            
            if (qualityDropdown != null)
                qualityDropdown.value = QualitySettings.GetQualityLevel();
        }
        
        private void SaveCurrentSettings()
        {
            if (volumeSlider != null)
                PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);
            
            if (brightnessSlider != null)
                PlayerPrefs.SetFloat("Brightness", brightnessSlider.value);
            
            if (handTrackingToggle != null)
                PlayerPrefs.SetInt("HandTracking", handTrackingToggle.isOn ? 1 : 0);
            
            if (planeVisualizationToggle != null)
                PlayerPrefs.SetInt("PlaneVisualization", planeVisualizationToggle.isOn ? 1 : 0);
            
            PlayerPrefs.Save();
        }
        
        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
        }
        
        private void OnBrightnessChanged(float value)
        {
            // Adjust screen brightness or lighting
            RenderSettings.ambientIntensity = value;
        }
        
        private void OnHandTrackingToggled(bool isOn)
        {
            // Enable/disable hand tracking features
            Debug.Log($"Hand tracking {(isOn ? "enabled" : "disabled")}");
        }
        
        private void OnPlaneVisualizationToggled(bool isOn)
        {
            if (arManager != null)
            {
                arManager.TogglePlaneVisualization();
            }
        }
        
        private void OnQualityChanged(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
        }
        
        private void ShowHelpSubPanel()
        {
            if (helpSubPanel != null)
            {
                helpSubPanel.SetActive(true);
                ShowHelpTopic(helpTopics.Values.First()); // Show first topic by default
            }
            
            PlayMenuSound();
        }
        
        private void HideHelpSubPanel()
        {
            if (helpSubPanel != null)
            {
                helpSubPanel.SetActive(false);
            }
            
            PlayMenuSound();
        }
        
        private void ShowHelpTopic(string content)
        {
            if (helpContentText != null)
            {
                helpContentText.text = content;
            }
        }
        
        private void ShowSaveAndExitConfirmation()
        {
            ShowConfirmation("Save your room and return to main menu?", () => SaveAndExitToMenu());
        }
        
        private void ShowExitWithoutSavingConfirmation()
        {
            ShowConfirmation("Exit without saving? Any unsaved changes will be lost.", () => ExitWithoutSaving());
        }
        
        private void ShowQuitToDesktopConfirmation()
        {
            ShowConfirmation("Quit to desktop? Any unsaved changes will be lost.", () => QuitToDesktop());
        }
        
        private void ShowResetSettingsConfirmation()
        {
            ShowConfirmation("Reset all settings to default values?", () => ResetSettings());
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
            PlayMenuSound();
            currentConfirmationAction?.Invoke();
            HideConfirmation();
        }
        
        private void OnConfirmationNo()
        {
            PlayMenuSound();
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
        
        private void SaveAndExitToMenu()
        {
            if (gameStateManager != null)
            {
                gameStateManager.SaveAndExitToMenu();
            }
        }
        
        private void ExitWithoutSaving()
        {
            if (gameStateManager != null)
            {
                gameStateManager.ExitWithoutSaving();
            }
        }
        
        private void QuitToDesktop()
        {
            if (gameStateManager != null)
            {
                gameStateManager.QuitGame();
            }
        }
        
        private void ResetSettings()
        {
            // Reset all settings to defaults
            if (volumeSlider != null)
            {
                volumeSlider.value = 1f;
                AudioListener.volume = 1f;
            }
            
            if (brightnessSlider != null)
            {
                brightnessSlider.value = 1f;
                RenderSettings.ambientIntensity = 1f;
            }
            
            if (handTrackingToggle != null)
                handTrackingToggle.isOn = true;
            
            if (planeVisualizationToggle != null)
                planeVisualizationToggle.isOn = true;
            
            if (qualityDropdown != null)
            {
                qualityDropdown.value = QualitySettings.names.Length - 1;
                QualitySettings.SetQualityLevel(qualityDropdown.value);
            }
            
            // Clear saved settings
            PlayerPrefs.DeleteKey("MasterVolume");
            PlayerPrefs.DeleteKey("Brightness");
            PlayerPrefs.DeleteKey("HandTracking");
            PlayerPrefs.DeleteKey("PlaneVisualization");
            PlayerPrefs.Save();
        }
        
        private void PlayMenuSound()
        {
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }
        
        // Public methods for external access
        public bool IsMenuOpen => isMenuOpen;
        
        public void AddCustomMenuItem(string buttonText, System.Action buttonAction)
        {
            // Future expansion: Allow other scripts to add custom menu items
            Debug.Log($"Custom menu item '{buttonText}' would be added here");
        }
        
        public void SetMenuToggleButton(Button customButton)
        {
            menuToggleButton = customButton;
            if (customButton != null)
            {
                customButton.onClick.RemoveAllListeners();
                customButton.onClick.AddListener(ToggleInGameMenu);
            }
        }
    }
} 