using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuHandler : MonoBehaviour
{
    [Header("Sub-Menus")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject storyModesPanel;
    [SerializeField] private GameObject mapMenuPanel;
    [SerializeField] private GameObject loadoutMenuPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button playBtn;

    [Header("Story Mode Buttons")]
    [SerializeField] private Button lightBtn;
    [SerializeField] private Button darkBtn;
    [SerializeField] private Button chaosBtn;
    [SerializeField] private Button endlessBtn;

    [Header("Map Menu Buttons")]
    [SerializeField] private Button mapBackBtn;
    [SerializeField] private Button mapContinueBtn;

    [Header("Loadout Menu Buttons")]
    [SerializeField] private Button loadoutBackBtn;
    [SerializeField] private Button startGameBtn;

    [Header("Map Menu Reference")]
    [SerializeField] private MapMenu mapMenu;

    void Start()
    {
        // Start on main menu
        ShowOnly(mainMenuPanel);

        // Main menu
        playBtn.onClick.AddListener(() => ShowOnly(storyModesPanel));

        // Story mode selection → sets alignment → map menu
        lightBtn.onClick.AddListener(() => SelectStoryMode(StoryMode.Light));
        darkBtn.onClick.AddListener(() => SelectStoryMode(StoryMode.Dark));
        chaosBtn.onClick.AddListener(() => SelectStoryMode(StoryMode.Chaos));
        endlessBtn.onClick.AddListener(() => SelectStoryMode(StoryMode.Endless));
        // storyBackBtn.onClick.AddListener(() => ShowOnly(mainMenuPanel));

        // Map menu
        mapBackBtn.onClick.AddListener(() => ShowOnly(storyModesPanel));
        mapContinueBtn.onClick.AddListener(() => ShowOnly(loadoutMenuPanel));

        // Loadout menu
        loadoutBackBtn.onClick.AddListener(() => ShowOnly(mapMenuPanel));
        startGameBtn.onClick.AddListener(StartGame);
    }

    void SelectStoryMode(StoryMode mode)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StoryMode = mode;

        Debug.Log($"[MainMenuHandler] Story mode set to {mode}");
        ShowOnly(mapMenuPanel);
    }

    void StartGame()
    {
        if (GameManager.Instance.GetEquippedWeaponData(0) == null || GameManager.Instance.GetEquippedWeaponData(1) == null) return;
        // Use level selected in MapMenu, fallback to first unlocked
        int buildIndex = -1;

        if (mapMenu != null && mapMenu.HasSelection)
        {
            buildIndex = mapMenu.SelectedBuildIndex;
        }

        // Reject invalid indices (0 = Bootstrap, 1 = MainMenu)
        if (buildIndex < 1 && GameManager.Instance != null)
        {
            var unlocked = GameManager.Instance.GetUnlockedLevels();
            if (unlocked.Count > 0)
                buildIndex = unlocked[0];
            else
                buildIndex = 1; // Default first level
        }

        if (buildIndex >= 1)
        {
            Debug.Log($"[MainMenuHandler] Starting game at build index {buildIndex}");
            SceneManager.LoadScene(buildIndex);
            return;
        }

        // Fallback — should not happen after migration
        Debug.LogWarning("[MainMenuHandler] No valid level found, loading scene 1");
        SceneManager.LoadScene(1);
    }

    void ShowOnly(GameObject activePanel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (storyModesPanel != null) storyModesPanel.SetActive(false);
        if (mapMenuPanel != null) mapMenuPanel.SetActive(false);
        if (loadoutMenuPanel != null) loadoutMenuPanel.SetActive(false);

        if (activePanel != null)
            activePanel.SetActive(true);
    }
}
