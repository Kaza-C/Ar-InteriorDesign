using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace ARInteriorDesign
{
    public enum GameState
    {
        MainMenu,
        ARMode,
        VRMode,
        InGame,
        Settings,
        Loading,
        Paused
    }
    
    public enum RoomMode
    {
        CustomRoom,
        BlankRoom
    }

    public class GameStateManager : MonoBehaviour
    {
        [Header("State Management")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        [SerializeField] private GameState previousState = GameState.MainMenu;
        [SerializeField] private RoomMode selectedRoomMode = RoomMode.CustomRoom;
        
        [Header("Managers")]
        [SerializeField] private MainMenuManager mainMenuManager;
        [SerializeField] private ARInteriorDesignManager arManager;
        [SerializeField] private ARUIManager uiManager;
        [SerializeField] private InGameMenuManager inGameMenuManager;
        
        [Header("Audio")]
        [SerializeField] private AudioSource backgroundMusicSource;
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;
        
        // Events
        public System.Action<GameState> OnStateChanged;
        public System.Action<RoomMode> OnRoomModeChanged;
        public System.Action OnGameStarted;
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;
        public System.Action OnReturnToMainMenu;
        
        // Singleton pattern
        public static GameStateManager Instance { get; private set; }
        
        // Properties
        public GameState CurrentState => currentState;
        public GameState PreviousState => previousState;
        public RoomMode SelectedRoomMode => selectedRoomMode;
        public bool IsInGame => currentState == GameState.ARMode || currentState == GameState.VRMode;
        public bool IsInMenu => currentState == GameState.MainMenu || currentState == GameState.Settings;
        
        private void Awake()
        {
            // Implement singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGameState();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            SetState(GameState.MainMenu);
        }
        
        private void InitializeGameState()
        {
            // Find managers if not assigned
            if (mainMenuManager == null)
                mainMenuManager = FindObjectOfType<MainMenuManager>();
            
            if (arManager == null)
                arManager = FindObjectOfType<ARInteriorDesignManager>();
            
            if (uiManager == null)
                uiManager = FindObjectOfType<ARUIManager>();
            
            if (inGameMenuManager == null)
                inGameMenuManager = FindObjectOfType<InGameMenuManager>();
        }
        
        public void SetState(GameState newState)
        {
            if (currentState == newState) return;
            
            previousState = currentState;
            currentState = newState;
            
            Debug.Log($"Game state changed: {previousState} -> {currentState}");
            
            OnStateChanged?.Invoke(currentState);
            HandleStateChange();
        }
        
        private void HandleStateChange()
        {
            switch (currentState)
            {
                case GameState.MainMenu:
                    ShowMainMenu();
                    break;
                    
                case GameState.ARMode:
                    StartARMode();
                    break;
                    
                case GameState.VRMode:
                    StartVRMode();
                    break;
                    
                case GameState.InGame:
                    EnterInGameState();
                    break;
                    
                case GameState.Settings:
                    ShowSettings();
                    break;
                    
                case GameState.Loading:
                    ShowLoadingScreen();
                    break;
                    
                case GameState.Paused:
                    PauseGame();
                    break;
            }
            
            UpdateAudio();
        }
        
        private void ShowMainMenu()
        {
            if (mainMenuManager != null)
            {
                mainMenuManager.ShowMainMenu();
            }
            
            // Hide other managers
            if (arManager != null && arManager.gameObject.activeInHierarchy)
            {
                arManager.gameObject.SetActive(false);
            }
            
            if (inGameMenuManager != null && inGameMenuManager.gameObject.activeInHierarchy)
            {
                inGameMenuManager.HideInGameMenu();
            }
            
            Time.timeScale = 1f;
        }
        
        private void StartARMode()
        {
            if (mainMenuManager != null)
            {
                mainMenuManager.HideMainMenu();
            }
            
            if (arManager != null)
            {
                arManager.gameObject.SetActive(true);
            }
            
            if (uiManager != null)
            {
                uiManager.gameObject.SetActive(true);
            }
            
            SetState(GameState.InGame);
            OnGameStarted?.Invoke();
        }
        
        private void StartVRMode()
        {
            if (mainMenuManager != null)
            {
                mainMenuManager.HideMainMenu();
            }
            
            // Configure for VR mode (blank room)
            if (arManager != null)
            {
                arManager.gameObject.SetActive(true);
                // Set up blank room configuration
            }
            
            if (uiManager != null)
            {
                uiManager.gameObject.SetActive(true);
            }
            
            SetState(GameState.InGame);
            OnGameStarted?.Invoke();
        }
        
        private void EnterInGameState()
        {
            Time.timeScale = 1f;
        }
        
        private void ShowSettings()
        {
            if (uiManager != null)
            {
                uiManager.OpenSettings();
            }
        }
        
        private void ShowLoadingScreen()
        {
            // Implement loading screen logic
            Debug.Log("Loading...");
        }
        
        private void PauseGame()
        {
            Time.timeScale = 0f;
            
            if (inGameMenuManager != null)
            {
                inGameMenuManager.ShowInGameMenu();
            }
            
            OnGamePaused?.Invoke();
        }
        
        private void UpdateAudio()
        {
            if (backgroundMusicSource == null) return;
            
            AudioClip targetClip = null;
            
            if (IsInMenu && menuMusic != null)
            {
                targetClip = menuMusic;
            }
            else if (IsInGame && gameplayMusic != null)
            {
                targetClip = gameplayMusic;
            }
            
            if (targetClip != null && backgroundMusicSource.clip != targetClip)
            {
                StartCoroutine(FadeMusicTransition(targetClip));
            }
        }
        
        private IEnumerator FadeMusicTransition(AudioClip newClip)
        {
            float fadeTime = 1f;
            float startVolume = backgroundMusicSource.volume;
            
            // Fade out
            for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                backgroundMusicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
                yield return null;
            }
            
            // Change clip
            backgroundMusicSource.clip = newClip;
            backgroundMusicSource.Play();
            
            // Fade in
            for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                backgroundMusicSource.volume = Mathf.Lerp(0f, startVolume, t / fadeTime);
                yield return null;
            }
        }
        
        public void SetRoomMode(RoomMode roomMode)
        {
            selectedRoomMode = roomMode;
            OnRoomModeChanged?.Invoke(roomMode);
            Debug.Log($"Room mode set to: {roomMode}");
        }
        
        public void StartGame(RoomMode roomMode)
        {
            SetRoomMode(roomMode);
            
            if (roomMode == RoomMode.CustomRoom)
            {
                SetState(GameState.ARMode);
            }
            else
            {
                SetState(GameState.VRMode);
            }
        }
        
        public void ResumeGame()
        {
            if (previousState == GameState.InGame || IsInGame)
            {
                SetState(GameState.InGame);
                Time.timeScale = 1f;
                OnGameResumed?.Invoke();
            }
        }
        
        public void PauseGameMenu()
        {
            if (IsInGame)
            {
                SetState(GameState.Paused);
            }
        }
        
        public void ReturnToMainMenu()
        {
            SetState(GameState.MainMenu);
            OnReturnToMainMenu?.Invoke();
        }
        
        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        public void LoadRoom(string roomName)
        {
            SetState(GameState.Loading);
            StartCoroutine(LoadRoomCoroutine(roomName));
        }
        
        private IEnumerator LoadRoomCoroutine(string roomName)
        {
            // Simulate loading time
            yield return new WaitForSeconds(1f);
            
            // Load the room using FurnitureManager
            if (uiManager != null)
            {
                uiManager.LoadRoom();
            }
            
            // Return to game state
            if (selectedRoomMode == RoomMode.CustomRoom)
            {
                SetState(GameState.ARMode);
            }
            else
            {
                SetState(GameState.VRMode);
            }
        }
        
        public void SaveAndExitToMenu()
        {
            // Save the current room
            if (uiManager != null)
            {
                uiManager.SaveRoom();
            }
            
            // Return to main menu
            ReturnToMainMenu();
        }
        
        public void ExitWithoutSaving()
        {
            // Just return to main menu without saving
            ReturnToMainMenu();
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && IsInGame)
            {
                PauseGameMenu();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && IsInGame)
            {
                PauseGameMenu();
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
} 