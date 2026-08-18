using System;
using System.Collections.Generic;
using UnityEngine;

// 游戏内统一使用的音效键。玩法脚本只依赖这个枚举，不直接依赖 AudioClip 资源。
public enum SfxId
{
    Jump = 0,
    ShootPea = 1,
    ShootMachineGun = 2,
    ShootCorn = 3,
    ProjectileHit = 4,
    EnemyDeath = 5,
    WeaponPickup = 6,
    Explosion = 7,
    PlayerHurt = 8,
    GameOver = 9,
    UiClick = 10,
    Pause = 11,
    Resume = 12
}

[Serializable]
public sealed class SfxDefinition
{
    public SfxId Id;
    public string ResourcePath;
    [Range(0f, 1f)] public float Volume = 1f;
    [Range(0.5f, 2f)] public float PitchMin = 0.98f;
    [Range(0.5f, 2f)] public float PitchMax = 1.02f;
}

// 轻量 2D 音效管理器：自动创建、跨场景持久化，并通过音频池支持连续射击和爆炸重叠播放。
public sealed class AudioManager : MonoBehaviour
{
    public const string LibraryResourcePath = "Audio/AudioSfxLibrary";
    private const float UniformSfxVolume = 0.58f;

    public static AudioManager Instance { get; private set; }

    [SerializeField, Min(4)]
    private int _voiceCount = 16;

    [SerializeField, Range(0f, 1f)]
    private float _masterVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float _sfxVolume = 1f;

    private readonly Dictionary<SfxId, SfxDefinition> _definitions = new();
    private readonly Dictionary<SfxId, AudioClip> _clipCache = new();
    private readonly HashSet<SfxId> _missingClipWarnings = new();
    private AudioSource[] _voices = Array.Empty<AudioSource>();
    private int _nextVoice;
    private bool _initialized;

    public float MasterVolume
    {
        get => _masterVolume;
        set => _masterVolume = Mathf.Clamp01(value);
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = Mathf.Clamp01(value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static AudioManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        if (!Application.isPlaying)
            return null;

        var managerObject = new GameObject(nameof(AudioManager));
        var manager = managerObject.AddComponent<AudioManager>();
        return Instance != null ? Instance : manager;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _voiceCount = Mathf.Max(4, _voiceCount);
        _masterVolume = Mathf.Clamp01(_masterVolume);
        _sfxVolume = Mathf.Clamp01(_sfxVolume);
        BuildVoicePool();
        LoadDefinitions();
        _initialized = true;
        PreloadAll();
    }

    private void BuildVoicePool()
    {
        _voices = new AudioSource[_voiceCount];
        for (int i = 0; i < _voices.Length; i++)
        {
            var voiceObject = new GameObject($"SfxVoice_{i:00}");
            voiceObject.transform.SetParent(transform, false);

            var source = voiceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            _voices[i] = source;
        }
    }

    private void LoadDefinitions()
    {
        _definitions.Clear();
        _clipCache.Clear();
        _missingClipWarnings.Clear();

        foreach (var definition in CreateDefaultDefinitions())
            _definitions[definition.Id] = definition;

        var library = Resources.Load<AudioSfxLibrary>(LibraryResourcePath);
        if (library == null || library.Entries == null)
            return;

        foreach (var definition in library.Entries)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.ResourcePath))
                continue;

            _definitions[definition.Id] = definition;
        }
    }

    // 资源尚未导入或配置表被移除时仍能提供一套可工作的默认映射。
    private static IEnumerable<SfxDefinition> CreateDefaultDefinitions()
    {
        yield return CreateDefinition(SfxId.Jump, "Audio/SFX/sfx_jump", UniformSfxVolume, 0.96f, 1.04f);
        yield return CreateDefinition(SfxId.ShootPea, "Audio/SFX/sfx_shoot_pea", UniformSfxVolume, 0.98f, 1.04f);
        yield return CreateDefinition(SfxId.ShootMachineGun, "Audio/SFX/sfx_shoot_machinegun", UniformSfxVolume, 0.90f, 0.98f);
        yield return CreateDefinition(SfxId.ShootCorn, "Audio/SFX/sfx_shoot_corn", UniformSfxVolume, 0.94f, 1.00f);
        yield return CreateDefinition(SfxId.ProjectileHit, "Audio/SFX/sfx_projectile_hit", UniformSfxVolume, 0.95f, 1.05f);
        yield return CreateDefinition(SfxId.EnemyDeath, "Audio/SFX/sfx_enemy_death", UniformSfxVolume, 0.92f, 1.00f);
        yield return CreateDefinition(SfxId.WeaponPickup, "Audio/SFX/sfx_weapon_pickup", UniformSfxVolume, 0.98f, 1.02f);
        yield return CreateDefinition(SfxId.Explosion, "Audio/SFX/sfx_explosion", UniformSfxVolume, 0.94f, 1.00f);
        yield return CreateDefinition(SfxId.PlayerHurt, "Audio/SFX/sfx_player_hurt", UniformSfxVolume, 0.94f, 1.02f);
        yield return CreateDefinition(SfxId.GameOver, "Audio/SFX/sfx_game_over", UniformSfxVolume, 0.98f, 1.02f);
        yield return CreateDefinition(SfxId.UiClick, "Audio/SFX/sfx_ui_click", UniformSfxVolume, 0.98f, 1.02f);
        yield return CreateDefinition(SfxId.Pause, "Audio/SFX/sfx_pause", UniformSfxVolume, 0.98f, 1.02f);
        yield return CreateDefinition(SfxId.Resume, "Audio/SFX/sfx_resume", UniformSfxVolume, 0.98f, 1.02f);
    }

    private static SfxDefinition CreateDefinition(
        SfxId id,
        string resourcePath,
        float volume,
        float pitchMin,
        float pitchMax)
    {
        return new SfxDefinition
        {
            Id = id,
            ResourcePath = resourcePath,
            Volume = volume,
            PitchMin = pitchMin,
            PitchMax = pitchMax
        };
    }

    // 统一播放入口。音效未接入玩法前也可以由调试按钮直接调用验证。
    public static void PlaySfx(SfxId id, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        var instance = EnsureInstance();
        if (instance == null)
            return;

        instance.PlayInternal(id, volumeMultiplier, pitchMultiplier);
    }

    private void PlayInternal(SfxId id, float volumeMultiplier, float pitchMultiplier)
    {
        Initialize();

        if (!_definitions.TryGetValue(id, out var definition))
        {
            Debug.LogWarning($"AudioManager has no definition for SfxId.{id}.", this);
            return;
        }

        var clip = ResolveClip(definition);
        if (clip == null)
            return;

        var voice = GetAvailableVoice();
        float pitchMin = Mathf.Min(definition.PitchMin, definition.PitchMax);
        float pitchMax = Mathf.Max(definition.PitchMin, definition.PitchMax);
        voice.pitch = Mathf.Clamp(UnityEngine.Random.Range(pitchMin, pitchMax) * pitchMultiplier, 0.1f, 3f);
        voice.volume = Mathf.Clamp01(_masterVolume * _sfxVolume * definition.Volume * volumeMultiplier);
        voice.clip = clip;
        voice.Play();
    }

    private AudioClip ResolveClip(SfxDefinition definition)
    {
        if (_clipCache.TryGetValue(definition.Id, out var cachedClip))
            return cachedClip;

        string resourcePath = NormalizeResourcePath(definition.ResourcePath);
        var clip = Resources.Load<AudioClip>(resourcePath);
        if (clip != null)
        {
            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
            _clipCache[definition.Id] = clip;
            return clip;
        }

        if (_missingClipWarnings.Add(definition.Id))
        {
            Debug.LogWarning(
                $"AudioManager could not load SfxId.{definition.Id} at Resources/{resourcePath}.",
                this);
        }

        return null;
    }

    private static string NormalizeResourcePath(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return string.Empty;

        string normalized = resourcePath.Trim().Replace('\\', '/');
        int extensionIndex = normalized.LastIndexOf('.');
        if (extensionIndex > normalized.LastIndexOf('/'))
            normalized = normalized.Substring(0, extensionIndex);
        return normalized;
    }

    private AudioSource GetAvailableVoice()
    {
        AudioSource fallback = _voices[_nextVoice];
        for (int i = 0; i < _voices.Length; i++)
        {
            int index = (_nextVoice + i) % _voices.Length;
            var voice = _voices[index];
            if (!voice.isPlaying)
            {
                _nextVoice = (index + 1) % _voices.Length;
                return voice;
            }

            fallback = voice;
        }

        fallback.Stop();
        _nextVoice = (_nextVoice + 1) % _voices.Length;
        return fallback;
    }

    // 可在资源刷新后由调试代码或 Inspector 按钮调用，用于一次性检查所有配置路径。
    public void PreloadAll()
    {
        Initialize();
        int loadedCount = 0;
        foreach (var definition in _definitions.Values)
        {
            if (ResolveClip(definition) != null)
                loadedCount++;
        }

        Debug.Log($"AudioManager preloaded {loadedCount}/{_definitions.Count} SFX clips.", this);
    }

    public static void StopAllSfx()
    {
        if (Instance == null || Instance._voices == null)
            return;

        foreach (var voice in Instance._voices)
        {
            if (voice != null)
                voice.Stop();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
