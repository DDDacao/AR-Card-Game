using UnityEngine;

/// <summary>
/// 音效管理器（单例）：统一控制游戏内所有的 BGM 与 SFX 播放。
/// 强制要求在主场景手动挂载 AudioManager 以方便 Inspector 调试与音量调校。
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<AudioManager>();
                if (instance == null)
                {
                    Debug.LogError("[AudioManager] 场景中缺少 AudioManager 实例！为了便于调试，请确保主场景中已手动挂载了 AudioManager 组件。");
                }
            }
            return instance;
        }
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip drawCardClip;
    [SerializeField] private AudioClip monsterHitClip;
    [SerializeField] private AudioClip playerHitClip;
    [SerializeField] private AudioClip bellClickClip;
    [SerializeField] private AudioClip victoryClip;

    [Header("音量单独调节 (0.0 至 1.0)")]
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [Range(0f, 1f)] public float drawCardVolume = 0.9f;
    [Range(0f, 1f)] public float monsterHitVolume = 0.9f;
    [Range(0f, 1f)] public float playerHitVolume = 0.9f;
    [Range(0f, 1f)] public float bellClickVolume = 0.9f;
    [Range(0f, 1f)] public float victoryVolume = 0.9f;

    private float lastDrawCardPlayTime = -1f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeAudioSources();
        LoadResourcesIfNull();
    }

    private void InitializeAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    private void LoadResourcesIfNull()
    {
        if (bgmClip == null) bgmClip = Resources.Load<AudioClip>("audioManager/BGM");
        if (drawCardClip == null) drawCardClip = Resources.Load<AudioClip>("audioManager/DrawCard");
        if (monsterHitClip == null) monsterHitClip = Resources.Load<AudioClip>("audioManager/MonsterHit");
        if (playerHitClip == null) playerHitClip = Resources.Load<AudioClip>("audioManager/PlayerHit");
        if (bellClickClip == null) bellClickClip = Resources.Load<AudioClip>("audioManager/BellClick");
        if (victoryClip == null) victoryClip = Resources.Load<AudioClip>("audioManager/Victory");
    }

    public void PlayBGM()
    {
        if (bgmClip == null)
        {
            bgmClip = Resources.Load<AudioClip>("audioManager/BGM");
        }

        if (bgmClip == null)
        {
            Debug.LogWarning("[AudioManager] 播放 BGM 失败：未找到 BGM clip 资源。");
            return;
        }

        bgmSource.volume = bgmVolume;

        if (bgmSource.clip == bgmClip && bgmSource.isPlaying) return;

        bgmSource.clip = bgmClip;
        bgmSource.Play();
        Debug.Log("[AudioManager] 开始播放背景音乐 BGM。");
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
            Debug.Log("[AudioManager] 停止背景音乐 BGM。");
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }

    public void PlayDrawCard()
    {
        if (Time.time - lastDrawCardPlayTime > 0.05f)
        {
            PlaySFX(drawCardClip, drawCardVolume);
            lastDrawCardPlayTime = Time.time;
        }
    }
    public void PlayMonsterHit() => PlaySFX(monsterHitClip, monsterHitVolume);
    public void PlayPlayerHit() => PlaySFX(playerHitClip, playerHitVolume);
    public void PlayBellClick() => PlaySFX(bellClickClip, bellClickVolume);
    public void PlayCampaignVictory() => PlaySFX(victoryClip, victoryVolume);
}
