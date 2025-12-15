using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameMainMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string firstLevelSceneName = "Room1_Orientation";

    [Header("Main Panels")]
    [SerializeField] private GameObject panelMain;
    [SerializeField] private GameObject panelSettings;
    [SerializeField] private GameObject panelAudio;
    [SerializeField] private GameObject panelVideo;
    [SerializeField] private GameObject panelCredits;
    [SerializeField] private GameObject panelLoading;

    [Header("Loading UI")]
    [SerializeField] private Slider loadingBar;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;   // Slider_MasterVolume

    [Header("Control Settings")]
    [SerializeField] private Slider mouseSensitivitySlider; // şimdilik zorunlu değil

    [Header("Optional Labels (UI Text)")]
    [SerializeField] private TextMeshProUGUI fullscreenModeLabel;
    [SerializeField] private TextMeshProUGUI resolutionLabel;
    [SerializeField] private TextMeshProUGUI qualityLabel;

    [Header("Camera")]
    [SerializeField] private Transform menuCamera;       // Camera
    [SerializeField] private Transform camPosMain;       // Cam_Pos_Main
    [SerializeField] private Transform camPosSettings;   // Cam_Pos_Settings (Settings + Credits)
    [SerializeField] private float camMoveDuration = 0.6f;
    [SerializeField] private AnimationCurve camMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSwoosh;
    [SerializeField] private AudioSource sfxClick;
    [SerializeField] private AudioSource sfxHover;   // hover için

    [Header("Highlight Button Groups")]
    [Tooltip("0 = Audio, 1 = Video")]
    [SerializeField] private Button[] tabButtons;

    [Tooltip("0 = Fullscreen, 1 = Windowed, 2 = Borderless")]
    [SerializeField] private Button[] fullscreenButtons;

    [Tooltip("0 = 1920x1080, 1 = 1600x900, 2 = 1280x720")]
    [SerializeField] private Button[] resolutionButtons;

    [Tooltip("0 = Low, 1 = Medium, 2 = High")]
    [SerializeField] private Button[] qualityButtons;

    // PlayerPrefs keyleri
    private const string PREF_VOLUME = "MasterVolume";
    private const string PREF_SENS   = "MouseSensitivity";
    private const string PREF_FS     = "FullscreenModeIndex";
    private const string PREF_RES    = "ResolutionIndex";
    private const string PREF_QUAL   = "QualityLevelIndex";

    // 0: Fullscreen, 1: Windowed, 2: Borderless
    private int fullscreenModeIndex = 0;
    // 0: 1920x1080, 1: 1600x900, 2: 1280x720
    private int resolutionIndex = 0;
    // 0: Low, 1: Medium, 2: High  -> default Yüksek
    private int qualityIndex = 2;

    private Coroutine camMoveRoutine;

    // highlight için normal ve hover sprite
    private Sprite baseButtonSprite;
    private Sprite hoverButtonSprite;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        // Panellerin başlangıç hali
        if (panelMain)      panelMain.SetActive(true);
        if (panelSettings)  panelSettings.SetActive(false);
        if (panelAudio)     panelAudio.SetActive(false);
        if (panelVideo)     panelVideo.SetActive(false);
        if (panelCredits)   panelCredits.SetActive(false);
        if (panelLoading)   panelLoading.SetActive(false);

        // Kamera başlangıç pozisyonu
        if (menuCamera != null && camPosMain != null)
        {
            menuCamera.position = camPosMain.position;
            menuCamera.rotation = camPosMain.rotation;
        }

        // Ses slider’ı 0–1 arası; default 0.5
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
        }

        // Başlık label'larını sabitle
        if (fullscreenModeLabel)
            fullscreenModeLabel.text = "Ekran Modu";
        if (resolutionLabel)
            resolutionLabel.text = "Çözünürlük";
        if (qualityLabel)
            qualityLabel.text = "Görüntü Kalitesi";

        InitButtonSprites();

        LoadSettingsFromPrefs();
        ApplyAllSettingsToGame();

        // Ayarlar ilk açıldığında Audio sekmesi selected gözükecek şekilde hazırlayalım
        SetGroupHighlight(tabButtons, 0);
        RefreshVideoGroupsHighlight();
    }

    // =========================================================
    //  BUTON SPRITELARI (HIGHLIGHT SİSTEMİ)
    // =========================================================

    private void InitButtonSprites()
    {
        // Asset’te tüm butonlar aynı frame’i kullandığı için
        // herhangi bir butondan normal + highlight sprite’ı okuyup
        // hepsine uygulayacağız.
        Button sample = null;

        if (tabButtons != null && tabButtons.Length > 0 && tabButtons[0] != null)
            sample = tabButtons[0];
        else if (fullscreenButtons != null && fullscreenButtons.Length > 0 && fullscreenButtons[0] != null)
            sample = fullscreenButtons[0];
        else if (resolutionButtons != null && resolutionButtons.Length > 0 && resolutionButtons[0] != null)
            sample = resolutionButtons[0];
        else if (qualityButtons != null && qualityButtons.Length > 0 && qualityButtons[0] != null)
            sample = qualityButtons[0];

        if (sample == null) return;

        var img = sample.targetGraphic as Image;
        if (img == null) return;

        baseButtonSprite = img.sprite;
        hoverButtonSprite = sample.spriteState.highlightedSprite;
    }

    private void SetGroupHighlight(Button[] group, int activeIndex)
    {
        if (group == null || group.Length == 0) return;
        if (baseButtonSprite == null || hoverButtonSprite == null) return;

        for (int i = 0; i < group.Length; i++)
        {
            var btn = group[i];
            if (btn == null) continue;

            var img = btn.targetGraphic as Image;
            if (img == null) continue;

            img.sprite = (i == activeIndex) ? hoverButtonSprite : baseButtonSprite;
        }
    }

    private void RefreshVideoGroupsHighlight()
    {
        SetGroupHighlight(fullscreenButtons, fullscreenModeIndex);
        SetGroupHighlight(resolutionButtons,  resolutionIndex);
        SetGroupHighlight(qualityButtons,     qualityIndex);
    }

    // =========================================================
    //  OYUN BAŞLAT / ÇIKIŞ
    // =========================================================

    public void StartGame()
    {
        if (sfxClick) sfxClick.Play();

        if (panelLoading != null && loadingBar != null)
        {
            panelLoading.SetActive(true);
            StartCoroutine(LoadLevelAsync());
        }
        else
        {
            SceneManager.LoadScene(firstLevelSceneName);
        }
    }

    public void QuitGame()
    {
        if (sfxClick) sfxClick.Play();

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator LoadLevelAsync()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(firstLevelSceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (loadingBar != null)
                loadingBar.value = progress;

            if (op.progress >= 0.9f)
            {
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // =========================================================
    //  MENÜLER ARASI GEÇİŞ
    // =========================================================

    public void OpenSettings()
    {
        if (sfxClick) sfxClick.Play();
        if (sfxSwoosh) sfxSwoosh.Play();

        if (panelMain)     panelMain.SetActive(false);
        if (panelCredits)  panelCredits.SetActive(false);
        if (panelSettings) panelSettings.SetActive(true);

        // Settings açıldığında varsayılan olarak Audio paneli gelsin
        ShowAudioPanel();

        MoveCameraTo(camPosSettings);
    }

    public void OpenCredits()
    {
        if (sfxClick) sfxClick.Play();
        if (sfxSwoosh) sfxSwoosh.Play();

        if (panelMain)     panelMain.SetActive(false);
        if (panelSettings) panelSettings.SetActive(false);
        if (panelCredits)  panelCredits.SetActive(true);

        MoveCameraTo(camPosSettings);
    }

    public void BackToMain()
    {
        if (sfxClick) sfxClick.Play();
        if (sfxSwoosh) sfxSwoosh.Play();

        if (panelMain)     panelMain.SetActive(true);
        if (panelSettings) panelSettings.SetActive(false);
        if (panelCredits)  panelCredits.SetActive(false);

        MoveCameraTo(camPosMain);
    }

    public void ShowAudioPanel()
    {
        if (sfxClick) sfxClick.Play();

        if (panelAudio) panelAudio.SetActive(true);
        if (panelVideo) panelVideo.SetActive(false);

        // Tab highlight: Audio seçili
        SetGroupHighlight(tabButtons, 0);
    }

    public void ShowVideoPanel()
    {
        if (sfxClick) sfxClick.Play();

        if (panelAudio) panelAudio.SetActive(false);
        if (panelVideo) panelVideo.SetActive(true);

        // Tab highlight: Video seçili
        SetGroupHighlight(tabButtons, 1);

        // Video paneli açıldığında mevcut ayarlara göre highlightları tazele
        RefreshVideoGroupsHighlight();
    }

    // =========================================================
    //  KAMERA GEÇİŞİ
    // =========================================================

    private void MoveCameraTo(Transform target)
    {
        if (menuCamera == null || target == null) return;

        if (camMoveRoutine != null)
            StopCoroutine(camMoveRoutine);

        camMoveRoutine = StartCoroutine(MoveCameraRoutine(target));
    }

    private IEnumerator MoveCameraRoutine(Transform target)
    {
        Vector3 startPos = menuCamera.position;
        Quaternion startRot = menuCamera.rotation;

        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;

        float duration = Mathf.Max(0.01f, camMoveDuration);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float eval = camMoveCurve != null
                ? camMoveCurve.Evaluate(Mathf.Clamp01(t))
                : Mathf.Clamp01(t);

            menuCamera.position = Vector3.Lerp(startPos, endPos, eval);
            menuCamera.rotation = Quaternion.Slerp(startRot, endRot, eval);

            yield return null;
        }

        menuCamera.position = endPos;
        menuCamera.rotation = endRot;
    }

    // =========================================================
    //  SES AYARLARI
    // =========================================================

    // Slider_MasterVolume -> On Value Changed (float) buraya bağlı
    public void OnMasterVolumeChanged(float value)
    {
        // Slider 0–1 arası
        float clamped = Mathf.Clamp01(value);
        AudioListener.volume = clamped;

        PlayerPrefs.SetFloat(PREF_VOLUME, clamped);
    }

    public void PlayHoverSfx()
    {
        if (sfxHover) sfxHover.Play();
    }

    // =========================================================
    //  KONTROL AYARLARI
    // =========================================================

    public void OnMouseSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat(PREF_SENS, value);
        // Oyundaki player controller bu değeri PlayerPrefs’ten okuyacak
    }

    // =========================================================
    //  GÖRÜNTÜ AYARLARI
    // =========================================================

    // Ekran modu butonları
    public void SetFullscreen_Exclusive()
    {
        if (sfxClick) sfxClick.Play();

        fullscreenModeIndex = 0;
        PlayerPrefs.SetInt(PREF_FS, fullscreenModeIndex);
        ApplyFullscreenMode();
        RefreshVideoGroupsHighlight();
    }

    public void SetFullscreen_Windowed()
    {
        if (sfxClick) sfxClick.Play();

        fullscreenModeIndex = 1;
        PlayerPrefs.SetInt(PREF_FS, fullscreenModeIndex);
        ApplyFullscreenMode();
        RefreshVideoGroupsHighlight();
    }

    public void SetFullscreen_Borderless()
    {
        if (sfxClick) sfxClick.Play();

        fullscreenModeIndex = 2;
        PlayerPrefs.SetInt(PREF_FS, fullscreenModeIndex);
        ApplyFullscreenMode();
        RefreshVideoGroupsHighlight();
    }

    private void ApplyFullscreenMode()
    {
        switch (fullscreenModeIndex)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
        }
        // fullscreenModeLabel hep "Ekran Modu" olarak kalıyor
    }

    // Çözünürlük butonları
    public void SetResolution_1920x1080()
    {
        if (sfxClick) sfxClick.Play();

        resolutionIndex = 0;
        PlayerPrefs.SetInt(PREF_RES, resolutionIndex);
        Screen.SetResolution(1920, 1080, Screen.fullScreenMode);

        RefreshVideoGroupsHighlight();
    }

    public void SetResolution_1600x900()
    {
        if (sfxClick) sfxClick.Play();

        resolutionIndex = 1;
        PlayerPrefs.SetInt(PREF_RES, resolutionIndex);
        Screen.SetResolution(1600, 900, Screen.fullScreenMode);

        RefreshVideoGroupsHighlight();
    }

    public void SetResolution_1280x720()
    {
        if (sfxClick) sfxClick.Play();

        resolutionIndex = 2;
        PlayerPrefs.SetInt(PREF_RES, resolutionIndex);
        Screen.SetResolution(1280, 720, Screen.fullScreenMode);

        RefreshVideoGroupsHighlight();
    }

    // Kalite butonları
    public void SetQualityLow()
    {
        if (sfxClick) sfxClick.Play();

        qualityIndex = 0;
        PlayerPrefs.SetInt(PREF_QUAL, qualityIndex);
        ApplyQuality();
        RefreshVideoGroupsHighlight();
    }

    public void SetQualityMedium()
    {
        if (sfxClick) sfxClick.Play();

        qualityIndex = 1;
        PlayerPrefs.SetInt(PREF_QUAL, qualityIndex);
        ApplyQuality();
        RefreshVideoGroupsHighlight();
    }

    public void SetQualityHigh()
    {
        if (sfxClick) sfxClick.Play();

        qualityIndex = 2;
        PlayerPrefs.SetInt(PREF_QUAL, qualityIndex);
        ApplyQuality();
        RefreshVideoGroupsHighlight();
    }

    private void ApplyQuality()
    {
        QualitySettings.SetQualityLevel(qualityIndex, true);
        // qualityLabel hep "Görüntü Kalitesi" olarak kalıyor
    }

    // =========================================================
    //  PREFS YÜKLE / UYGULA
    // =========================================================

    private void LoadSettingsFromPrefs()
    {
        float volumeSaved = PlayerPrefs.GetFloat(PREF_VOLUME, 0.5f);
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = volumeSaved;

        AudioListener.volume = Mathf.Clamp01(volumeSaved);

        float sens = PlayerPrefs.GetFloat(PREF_SENS, 1f);
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.value = sens;

        fullscreenModeIndex = PlayerPrefs.GetInt(PREF_FS, 0);
        resolutionIndex     = PlayerPrefs.GetInt(PREF_RES, 0);
        qualityIndex        = PlayerPrefs.GetInt(PREF_QUAL, 2);
    }

    private void ApplyAllSettingsToGame()
    {
        ApplyFullscreenMode();

        switch (resolutionIndex)
        {
            case 0:
                Screen.SetResolution(1920, 1080, Screen.fullScreenMode);
                break;
            case 1:
                Screen.SetResolution(1600, 900, Screen.fullScreenMode);
                break;
            case 2:
                Screen.SetResolution(1280, 720, Screen.fullScreenMode);
                break;
        }

        ApplyQuality();
        RefreshVideoGroupsHighlight();
    }
}
