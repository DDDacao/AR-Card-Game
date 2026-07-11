using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class StartIntroController : MonoBehaviour
{
    /// <summary>开场动画完全结束、准备显示 AR 战斗时触发。</summary>
    public event Action IntroFinished;

    [Header("Portable content")]
    [SerializeField] private VideoClip introVideo;
    [SerializeField] private Texture firstFrame;
    [SerializeField] private Texture tailFrame;

    [Header("Reliable frame-sequence playback")]
    [Tooltip("Resources subfolder containing JPG intro frames. When populated, this avoids device VideoPlayer decoding entirely.")]
    [SerializeField] private string framesResourcePath = "StartIntroFrames";
    [Min(1f)]
    [SerializeField] private float frameSequenceFps = 12f;

    [Header("Timing")]
    [Min(0.1f)]
    [SerializeField] private float playbackSpeed = 1f;
    [Tooltip("Source-video seconds reserved for the matching tail frame. The handoff begins at length minus this value.")]
    [Min(0.1f)]
    [SerializeField] private float sourceSecondsReservedForTail = 0.1f;
    [Min(0f)]
    [SerializeField] private float tailHoldBeforeReveal = 0.1f;
    [Min(0.1f)]
    [SerializeField] private float tailRevealDuration = 0.9f;
    [Tooltip("Safety floor that prevents a stale VideoPlayer.time value from skipping the intro on some devices.")]
    [Min(0.5f)]
    [SerializeField] private float minimumVideoPlaybackSeconds = 3f;

    [Header("Audio")]
    [Tooltip("Disabled by default because accelerated AI-video audio is usually distorted. Use a separate Unity audio cue for final production.")]
    [SerializeField] private bool playVideoAudio;

    [Header("Touch target")]
    [SerializeField] private Vector2 yinYangButtonPosition = new Vector2(0f, -210f);
    [SerializeField] private Vector2 yinYangButtonSize = new Vector2(420f, 420f);
    [SerializeField] private int sortingOrder = 1000;

    [Header("Integration")]
    [SerializeField] private UnityEvent onIntroFinished;

    private VideoPlayer videoPlayer;
    private RawImage firstFrameGraphic;
    private RawImage videoGraphic;
    private RawImage tailGraphic;
    private Button startButton;
    private Material opaqueVideoMaterial;
    private RenderTexture videoTexture;
    private bool videoPrepared;
    private bool playing;
    private bool handingOff;
    private bool videoFrameVisible;
    private float introStartedAt = -1f;
    private Texture2D[] introFrames = Array.Empty<Texture2D>();
    private bool useFrameSequence;

    private void Awake()
    {
        BuildOverlay();
        LoadFrameSequence();
        if (useFrameSequence)
        {
            startButton.interactable = true;
        }
        else
        {
            ConfigureVideo();
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
        }

        if (opaqueVideoMaterial != null)
        {
            Destroy(opaqueVideoMaterial);
        }

        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }

    }

    private void Update()
    {
        if (!playing || handingOff || videoPlayer == null || !videoPlayer.isPrepared)
        {
            return;
        }

        if (!videoFrameVisible || videoPlayer.length <= 0d)
        {
            return;
        }

        double playableSourceSeconds = System.Math.Max(0d, videoPlayer.length - sourceSecondsReservedForTail);
        float scheduledPlaybackSeconds = (float)(playableSourceSeconds / Mathf.Max(0.1f, playbackSpeed));
        float handoffDelay = Mathf.Max(minimumVideoPlaybackSeconds, scheduledPlaybackSeconds);

        if (Time.unscaledTime - introStartedAt >= handoffDelay)
        {
            BeginTailHandoff();
        }
    }

    /// <summary>Can be called by a scene button if a project needs a custom touch target.</summary>
    public void PlayIntro()
    {
        if (playing || handingOff || (!useFrameSequence && !videoPrepared))
        {
            return;
        }

        startButton.interactable = false;
        videoFrameVisible = false;
        introStartedAt = -1f;
        firstFrameGraphic.gameObject.SetActive(true);
        videoGraphic.gameObject.SetActive(false);
        playing = true;
        if (useFrameSequence)
        {
            StartCoroutine(PlayFrameSequence());
            Debug.Log("[StartIntro] Click accepted. Playing pre-rendered frame sequence.", this);
        }
        else
        {
            videoPlayer.playbackSpeed = playbackSpeed;
            videoPlayer.Play();
            StartCoroutine(ShowVideoAfterPlaybackStarts());
            Debug.Log("[StartIntro] Click accepted. Waiting for VideoPlayer playback to start.", this);
        }
    }

    private void BuildOverlay()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        firstFrameGraphic = CreateFullScreenRawImage("FirstFrame", firstFrame, true);
        videoGraphic = CreateFullScreenRawImage("VideoFrame", null, true);
        Shader opaqueVideoShader = Shader.Find("UI/StartIntroVideoOpaque");
        if (opaqueVideoShader == null)
        {
            Debug.LogError("Start intro video shader was not found.", this);
        }
        else
        {
            opaqueVideoMaterial = new Material(opaqueVideoShader);
            videoGraphic.material = opaqueVideoMaterial;
        }
        videoGraphic.gameObject.SetActive(false);
        tailGraphic = CreateFullScreenRawImage("InkTailFrame", tailFrame, true);
        tailGraphic.gameObject.SetActive(false);

        GameObject buttonObject = new GameObject("YinYangTouchButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);

        RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
        buttonTransform.anchorMin = new Vector2(0.5f, 0.5f);
        buttonTransform.anchorMax = new Vector2(0.5f, 0.5f);
        buttonTransform.pivot = new Vector2(0.5f, 0.5f);
        buttonTransform.anchoredPosition = yinYangButtonPosition;
        buttonTransform.sizeDelta = yinYangButtonSize;

        Image buttonGraphic = buttonObject.GetComponent<Image>();
        buttonGraphic.color = new Color(0f, 0f, 0f, 0f);
        buttonGraphic.raycastTarget = true;

        startButton = buttonObject.GetComponent<Button>();
        startButton.targetGraphic = buttonGraphic;
        startButton.transition = Selectable.Transition.None;
        startButton.interactable = false;
        startButton.onClick.AddListener(PlayIntro);
    }

    private void ConfigureVideo()
    {
        if (introVideo == null)
        {
            Debug.LogError("StartIntroController needs an intro video clip.", this);
            return;
        }

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = introVideo;
        videoTexture = new RenderTexture(1080, 608, 0, RenderTextureFormat.Default)
        {
            name = "StartIntroVideoTexture",
            useMipMap = false,
            autoGenerateMips = false
        };
        videoTexture.Create();
        videoGraphic.texture = videoTexture;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoTexture;
        videoPlayer.audioOutputMode = playVideoAudio ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.Prepare();
    }

    private void LoadFrameSequence()
    {
        if (string.IsNullOrWhiteSpace(framesResourcePath))
        {
            return;
        }

        introFrames = Resources.LoadAll<Texture2D>(framesResourcePath);
        Array.Sort(introFrames, (left, right) => string.CompareOrdinal(left.name, right.name));
        useFrameSequence = introFrames.Length > 1;

        if (useFrameSequence)
        {
            // Keep the idle screen and the first animated frame pixel-identical.
            // This removes the visible jump caused by using a separately exported PNG.
            firstFrameGraphic.texture = introFrames[0];
            Debug.Log("[StartIntro] Loaded " + introFrames.Length + " pre-rendered intro frames.", this);
        }
    }

    private void OnVideoPrepared(VideoPlayer preparedPlayer)
    {
        videoPrepared = true;
        startButton.interactable = true;
    }

    private IEnumerator ShowVideoAfterPlaybackStarts()
    {
        const float startTimeout = 2f;
        float elapsed = 0f;
        while (playing && videoPlayer != null && !videoPlayer.isPlaying && elapsed < startTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!playing || videoPlayer == null || !videoPlayer.isPlaying)
        {
            playing = false;
            startButton.interactable = true;
            Debug.LogError("[StartIntro] VideoPlayer did not start playback within two seconds.", this);
            yield break;
        }

        // waitForFirstFrame keeps the source from advancing before this render pass.
        // Waiting until the end of that pass avoids exposing an empty RenderTexture.
        yield return new WaitForEndOfFrame();
        TryShowFirstVideoFrame();
    }

    private IEnumerator PlayFrameSequence()
    {
        float frameDuration = 1f / Mathf.Max(1f, frameSequenceFps * playbackSpeed);
        videoGraphic.gameObject.SetActive(true);
        firstFrameGraphic.gameObject.SetActive(false);
        videoFrameVisible = true;
        introStartedAt = Time.unscaledTime;

        for (int index = 0; index < introFrames.Length; index++)
        {
            videoGraphic.texture = introFrames[index];
            yield return new WaitForSecondsRealtime(frameDuration);
        }

        BeginTailHandoff();
    }

    private void TryShowFirstVideoFrame()
    {
        if (!playing || videoFrameVisible || videoTexture == null)
        {
            return;
        }

        videoGraphic.texture = videoTexture;
        videoGraphic.gameObject.SetActive(true);
        firstFrameGraphic.gameObject.SetActive(false);
        videoFrameVisible = true;
        introStartedAt = Time.unscaledTime;
        Debug.Log("[StartIntro] First video frame displayed; transition timer started.", this);
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError("Start intro video failed to prepare: " + message, this);
    }

    private void BeginTailHandoff()
    {
        Debug.Log("[StartIntro] Tail handoff started after " + (Time.unscaledTime - introStartedAt).ToString("F2") + " seconds.", this);
        handingOff = true;
        playing = false;
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        videoGraphic.gameObject.SetActive(false);

        tailGraphic.color = Color.white;
        tailGraphic.gameObject.SetActive(true);
        StartCoroutine(RevealArAfterTailHold());
    }

    private IEnumerator RevealArAfterTailHold()
    {
        if (tailHoldBeforeReveal > 0f)
        {
            yield return new WaitForSecondsRealtime(tailHoldBeforeReveal);
        }

        float elapsed = 0f;
        while (elapsed < tailRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / tailRevealDuration);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            tailGraphic.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        IntroFinished?.Invoke();
        onIntroFinished?.Invoke();
        gameObject.SetActive(false);
    }

    private RawImage CreateFullScreenRawImage(string objectName, Texture texture, bool raycastTarget)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(transform, false);

        RectTransform imageTransform = imageObject.GetComponent<RectTransform>();
        imageTransform.anchorMin = Vector2.zero;
        imageTransform.anchorMax = Vector2.one;
        imageTransform.offsetMin = Vector2.zero;
        imageTransform.offsetMax = Vector2.zero;

        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = raycastTarget;
        return image;
    }
}
