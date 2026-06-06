using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MapMenu : MonoBehaviour
{
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject levelButtonPrefab;

    private List<Button> levelButtons = new List<Button>();
    private List<int> levelBuildIndices = new List<int>();

    private int selectedBuildIndex = -1;
    private Button selectedButton;
    private Color defaultColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private Color selectedColor = new Color(0.3f, 0.6f, 0.3f, 1f);

    public int SelectedBuildIndex => selectedBuildIndex;
    public bool HasSelection => selectedBuildIndex >= 0;

    void OnEnable()
    {
        PopulateLevels();
    }

    void OnDisable()
    {
        ClearEntries();
    }

    public void PopulateLevels()
    {
        ClearEntries();

        if (GameManager.Instance == null) return;

        var unlocked = GameManager.Instance.GetUnlockedLevels();
        if (unlocked == null || unlocked.Count == 0)
            unlocked = new List<int> { 2 }; // First level is at build index 2 (Bootstrap=0, MainMenu=1)

        int levelCount = SceneManager.sceneCountInBuildSettings;

        // Start from build index 2 to skip Bootstrap (0) and MainMenu (1)
        for (int i = 2; i < levelCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(scenePath)) continue;

            // Skip non-level scenes
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName.Contains("Bootstrap") || sceneName.Contains("MainMenu")) continue;
            int buildIndex = i;

            GameObject entry = Instantiate(levelButtonPrefab, contentContainer);
            entry.SetActive(true);
            entry.name = $"Level_{sceneName}";

            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = sceneName;

            var img = entry.GetComponent<Image>();
            bool isUnlocked = unlocked.Contains(buildIndex) || buildIndex == 2;

            if (entry.TryGetComponent<Button>(out var btn))
            {
                btn.interactable = isUnlocked;

                if (!isUnlocked)
                {
                    if (img != null) img.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                    if (label != null) label.color = Color.gray;
                }
                else
                {
                    if (img != null) img.color = defaultColor;
                    int capturedIndex = buildIndex;
                    btn.onClick.AddListener(() => SelectLevel(capturedIndex, btn, img));
                    levelButtons.Add(btn);
                    levelBuildIndices.Add(capturedIndex);
                }
            }
        }
    }

    void SelectLevel(int buildIndex, Button btn, Image img)
    {
        // Deselect previous
        if (selectedButton != null)
        {
            var prevImg = selectedButton.GetComponent<Image>();
            if (prevImg != null) prevImg.color = defaultColor;
        }

        // Select new
        selectedBuildIndex = buildIndex;
        selectedButton = btn;
        if (img != null) img.color = selectedColor;

        Debug.Log($"[MapMenu] Selected level build index: {buildIndex}");
    }

    void ClearEntries()
    {
        foreach (var btn in levelButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        levelButtons.Clear();
        levelBuildIndices.Clear();
        selectedBuildIndex = -1;
        selectedButton = null;
    }
}
