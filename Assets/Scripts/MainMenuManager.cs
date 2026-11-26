using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    GameObject menuCanvas;
    GameObject panel;
    Text notice;

    const float SaveDelay = 0.4f;
    float saveAt;
    float nextPreview;

    void Start()
    {
        Time.timeScale = 1f;
        menuCanvas = new GameObject(
            "ProgressMenu", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster)
        );

        Canvas canvas = menuCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 1f;

        MakeButton(menuCanvas.transform, "SETTINGS", new Vector2(490f, 290f),
            new Vector2(220f, 65f), ShowSettings);

        // The AudioSystem (MysticAudio + MusicManager) is created once before the first scene loads.
        SaveData saved = SaveSystem.Data;
        notice = MakeText(menuCanvas.transform, "", new Vector2(0f, -300f), 20);
        notice.text = SaveSystem.LoadNotice;
    }

    public void PlayGame()
    {
        ShowLevels();
    }

    void ShowLevels()
    {
        OpenPanel("SELECT LEVEL");
        MakeText(panel.transform, "BEST LEVEL SCORE: " + SaveSystem.Data.bestScore,
            new Vector2(0f, 190f), 24);

        LevelData[] levels = GameData.Instance.levels;
        for (int i = 0; i < levels.Length; i++)
        {
            int level = i + 1;
            bool unlocked = SaveSystem.Data.unlockedLevels.Contains(level);
            string label = levels[i].title;
            if (!unlocked)
                label += " - LOCKED";
            else if (SaveSystem.Data.completedLevels.Contains(level))
                label += " - COMPLETE";

            Button button = MakeButton(panel.transform, label,
                new Vector2(0f, 100f - i * 90f), new Vector2(430f, 70f),
                () => StartLevel(level));

            button.interactable = unlocked;
        }

        MakeText(panel.transform, "Continue from the start of level " + SaveSystem.Data.currentLevel,
            new Vector2(0f, -170f), 21);
        MakeButton(panel.transform, "BACK", new Vector2(0f, -260f),
            new Vector2(220f, 65f), ClosePanel);
    }

    void StartLevel(int level)
    {
        if (!SaveSystem.Data.unlockedLevels.Contains(level))
            return;

        SaveSystem.SelectedLevel = level;
        SaveSystem.Data.currentLevel = level;
        SaveSystem.Save();
        SceneManager.LoadScene("SampleScene");
    }

    void ShowSettings()
    {
        OpenPanel("SETTINGS");
        VolumeRow(true, 130f);
        VolumeRow(false, 15f);
        MakeButton(panel.transform, "RESET PROGRESS", new Vector2(0f, -120f),
            new Vector2(330f, 65f), AskReset);
        MakeButton(panel.transform, "BACK", new Vector2(0f, -260f),
            new Vector2(220f, 65f), ClosePanel);
    }

    // One audio row: label with percent and decibels, a slider and the old -/+ buttons for fine steps.
    void VolumeRow(bool music, float y)
    {
        float volume = music ? SaveSystem.Data.musicVolume : SaveSystem.Data.effectsVolume;
        Text label = MakeText(panel.transform, VolumeLabel(music, volume),
            new Vector2(0f, y + 32f), 26);
        Slider slider = MakeSlider(panel.transform, new Vector2(0f, y - 22f),
            new Vector2(400f, 40f), volume);

        slider.onValueChanged.AddListener(value =>
        {
            SaveSystem.ChangeVolume(music, value, false);
            label.text = VolumeLabel(music, value);
            saveAt = Time.unscaledTime + SaveDelay;

            // Let the player hear the new effects level while dragging.
            if (!music && Time.unscaledTime >= nextPreview)
            {
                nextPreview = Time.unscaledTime + 0.25f;
                MysticAudio.Play(Sfx.Pickup);
            }
        });

        MakeButton(panel.transform, "-", new Vector2(-260f, y - 22f), new Vector2(75f, 65f),
            () => slider.value -= 0.05f);
        MakeButton(panel.transform, "+", new Vector2(260f, y - 22f), new Vector2(75f, 65f),
            () => slider.value += 0.05f);
    }

    static string VolumeLabel(bool music, float volume)
    {
        float db = MysticAudio.ToDecibels(volume);
        string level = db <= MysticAudio.MinDecibels ? "OFF" : db.ToString("0.0") + " dB";
        return (music ? "MUSIC " : "EFFECTS ") + Mathf.RoundToInt(volume * 100f) + "%   (" + level + ")";
    }

    Slider MakeSlider(Transform parent, Vector2 position, Vector2 size, float value)
    {
        GameObject root = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image track = SliderPart("Background", root.transform, new Color(0.08f, 0.2f, 0.22f));
        Stretch(track.rectTransform, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f));

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(0f, 0.3f), new Vector2(1f, 0.7f));
        Image fill = SliderPart("Fill", fillArea.transform, new Color(0.33f, 0.78f, 0.27f));
        fill.rectTransform.sizeDelta = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRect, Vector2.zero, Vector2.one);
        handleAreaRect.offsetMin = new Vector2(14f, 0f);
        handleAreaRect.offsetMax = new Vector2(-14f, 0f);
        Image handle = SliderPart("Handle", handleArea.transform, new Color(1f, 0.82f, 0.08f));
        handle.rectTransform.sizeDelta = new Vector2(28f, 0f);

        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        return slider;
    }

    static Image SliderPart(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (saveAt > 0f && Time.unscaledTime >= saveAt)
            SavePendingVolume();
    }

    void SavePendingVolume()
    {
        saveAt = 0f;
        if (!SaveSystem.Save() && notice != null)
            notice.text = SaveSystem.LastError;
    }

    void AskReset()
    {
        OpenPanel("RESET PROGRESS?");
        MakeText(panel.transform,
            "Delete completed levels, unlocks and best score " + SaveSystem.Data.bestScore +
            "?\nMusic and effects will return to their defaults.",
            new Vector2(0f, 90f), 25);
        MakeButton(panel.transform, "CANCEL", new Vector2(-160f, -100f),
            new Vector2(260f, 70f), ShowSettings);
        Button delete = MakeButton(panel.transform, "DELETE", new Vector2(160f, -100f),
            new Vector2(260f, 70f), ConfirmReset);
        delete.GetComponent<Image>().color = new Color(0.65f, 0.15f, 0.15f);
    }

    void ConfirmReset()
    {
        if (SaveSystem.ResetProgress())
            ShowLevels();
        else
            notice.text = SaveSystem.LastError;
    }

    void OpenPanel(string title)
    {
        ClosePanel();
        panel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(menuCanvas.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.09f, 0.97f);
        MakeText(panel.transform, title, new Vector2(0f, 280f), 36);
        notice = MakeText(panel.transform, SaveSystem.LastError,
            new Vector2(0f, -325f), 18);
    }

    void ClosePanel()
    {
        if (saveAt > 0f)
            SavePendingVolume();

        if (panel != null)
        {
            panel.SetActive(false);
            Destroy(panel);
            panel = null;
        }
    }

    Text MakeText(Transform parent, string value, Vector2 position, int fontSize)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(1000f, 95f);
        Text text = obj.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    Button MakeButton(Transform parent, string label, Vector2 position,
        Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject(label,
            typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        obj.GetComponent<Image>().color = new Color(0.08f, 0.2f, 0.22f);
        Button button = obj.GetComponent<Button>();
        button.onClick.AddListener(action);
        MysticAudio.RegisterButton(button);
        Text text = MakeText(obj.transform, label, Vector2.zero, 23);
        text.rectTransform.sizeDelta = size;
        return button;
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveSystem.Save();
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused)
            SaveSystem.Save();
    }

    public void QuitGame()
    {
        SaveSystem.Save();
        Application.Quit();
    }
}
