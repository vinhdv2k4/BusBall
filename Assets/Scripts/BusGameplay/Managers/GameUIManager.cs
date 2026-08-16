using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private WinPanel winPanel;
    [SerializeField] private LosePanel losePanel;

    private GameplayManager gameplayManager;
    private LevelJsonLoader levelJsonLoader;
    private bool isTransitioning;

    private void Awake()
    {
        ResolveManagers();
        EnsurePopups();
        HideAll();
    }

    private void OnEnable()
    {
        ResolveManagers();
        if (gameplayManager != null)
            gameplayManager.StateChanged += HandleGameplayStateChanged;
    }

    private void OnDisable()
    {
        if (gameplayManager != null)
            gameplayManager.StateChanged -= HandleGameplayStateChanged;
    }

    public void ShowWin()
    {
        PreparePanelForDisplay(winPanel);
        losePanel?.Hide();
        winPanel?.Show();
    }

    public void ShowLose()
    {
        PreparePanelForDisplay(losePanel);
        winPanel?.Hide();
        losePanel?.Show();
    }

    public void HideAll()
    {
        winPanel?.Hide();
        losePanel?.Hide();
    }

    public void RestartCurrentLevel()
    {
        StartCoroutine(TransitionToLevel(false));
    }

    public void LoadNextLevel()
    {
        StartCoroutine(TransitionToLevel(true));
    }

    private void HandleGameplayStateChanged(GameplayManager.GameplayState state)
    {
        if (state == GameplayManager.GameplayState.Won) ShowWin();
        else if (state == GameplayManager.GameplayState.Lost) ShowLose();
    }

    private IEnumerator TransitionToLevel(bool loadNextLevel)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        GameSoundManager.Instance?.StopResultSound();
        GameSoundManager.Instance?.PlayButtonClick();
        yield return new WaitForSecondsRealtime(0.08f);

        if (loadNextLevel)
        {
            ResolveManagers();
            levelJsonLoader?.SelectNextLevel();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ResolveManagers()
    {
        if (gameplayManager == null)
            gameplayManager = FindFirstObjectByType<GameplayManager>();
        if (levelJsonLoader == null)
            levelJsonLoader = FindFirstObjectByType<LevelJsonLoader>();
    }

    private void EnsurePopups()
    {
        Canvas canvas = GetOrCreateCanvas(transform);

        if (winPanel == null)
            winPanel = CreatePopup<WinPanel>(canvas.transform, "WinPopup", "YOU WIN", "NEXT");
        else
            winPanel = EnsureScenePanel(winPanel, canvas.transform);

        if (losePanel == null)
            losePanel = CreatePopup<LosePanel>(canvas.transform, "LosePopup", "TRY AGAIN", "RETRY");
        else
            losePanel = EnsureScenePanel(losePanel, canvas.transform);

        winPanel.Configure(LoadNextLevel);
        losePanel.Configure(RestartCurrentLevel);
    }

    private static Canvas GetOrCreateCanvas(Transform preferredRoot)
    {
        Canvas canvas = preferredRoot != null ? preferredRoot.GetComponentInChildren<Canvas>(true) : null;
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (preferredRoot != null)
                canvasObject.transform.SetParent(preferredRoot, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        canvas.transform.localScale = Vector3.one;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
        }

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private static T EnsureScenePanel<T>(T panel, Transform canvasTransform) where T : BasePanel
    {
        if (panel == null) return null;

        if (!panel.gameObject.scene.IsValid())
            panel = Instantiate(panel, canvasTransform);
        else if (!panel.transform.IsChildOf(canvasTransform))
            panel.transform.SetParent(canvasTransform, false);

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        return panel;
    }

    private static void PreparePanelForDisplay(BasePanel panel)
    {
        if (panel == null) return;

        Transform current = panel.transform.parent;
        while (current != null)
        {
            current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    private static T CreatePopup<T>(Transform parent, string name, string title, string buttonLabel)
        where T : BasePanel
    {
        GameObject overlay = new(name, typeof(RectTransform), typeof(Image), typeof(T));
        overlay.transform.SetParent(parent, false);
        T popup = overlay.GetComponent<T>();
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.14f, 0.84f);

        GameObject panel = new("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520f, 300f);
        panel.GetComponent<Image>().color = new Color(0.11f, 0.2f, 0.3f, 1f);

        GameObject titleObject = new("Title", typeof(RectTransform), typeof(Text));
        titleObject.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.55f);
        titleRect.anchorMax = new Vector2(1f, 0.9f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        Text titleText = titleObject.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 56;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.text = title;

        GameObject buttonObject = new("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.18f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.18f);
        buttonRect.sizeDelta = new Vector2(280f, 84f);
        buttonObject.GetComponent<Image>().color = new Color(0.98f, 0.68f, 0.2f, 1f);
        popup.SetPrimaryButton(buttonObject.GetComponent<Button>());

        GameObject buttonTextObject = new("Text", typeof(RectTransform), typeof(Text));
        buttonTextObject.transform.SetParent(buttonObject.transform, false);
        RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        Text buttonText = buttonTextObject.GetComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 34;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = new Color(0.1f, 0.14f, 0.18f, 1f);
        buttonText.text = buttonLabel;

        return popup;
    }
}
