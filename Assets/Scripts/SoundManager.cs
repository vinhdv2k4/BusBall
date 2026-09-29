using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum SoundType
{
    ButtonClick,
    PopupOpen,
    PopupClose,
    CarClick,
    CarDrive,
    CarHit,
    CarDropBall,
    BallDrop,
    BallEnterConveyor,
    BallFeedTray,
    TrayComplete,
    GateComplete,
    LevelWin,
    LevelLose,
    StarAward,
    CoinCollect
}

public enum MusicType
{
    None,
    LobbyMusic,
    GameplayMusic,
    WinMusic,
    LoseMusic
}

[Serializable]
public class SoundData
{
    public SoundType type;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool randomizePitch = false;
    public float minPitch = 0.92f;
    public float maxPitch = 1.08f;
}

[Serializable]
public class MusicData
{
    public MusicType type;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.8f;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private const string PREF_SOUND = "KEY_SOUND_ENABLED";
    private const string PREF_MUSIC = "KEY_MUSIC_ENABLED";
    private const string PREF_VIBRATE = "KEY_VIBRATE_ENABLED";
    private const string PREF_SOUND_VOL = "KEY_SOUND_VOLUME";
    private const string PREF_MUSIC_VOL = "KEY_MUSIC_VOLUME";

    [Header("--- Audio Clip Libraries (Sound List) ---")]
    public List<SoundData> soundList = new()
    {
        new SoundData { type = SoundType.ButtonClick, volume = 1f, randomizePitch = true },
        new SoundData { type = SoundType.CarClick, volume = 1f },
        new SoundData { type = SoundType.CarDrive, volume = 1f },
        new SoundData { type = SoundType.CarHit, volume = 1f },
        new SoundData { type = SoundType.CarDropBall, volume = 1f },
        new SoundData { type = SoundType.BallDrop, volume = 1f, randomizePitch = true },
        new SoundData { type = SoundType.BallEnterConveyor, volume = 1f, randomizePitch = true },
        new SoundData { type = SoundType.BallFeedTray, volume = 1f, randomizePitch = true },
        new SoundData { type = SoundType.TrayComplete, volume = 1f },
        new SoundData { type = SoundType.GateComplete, volume = 1f },
        new SoundData { type = SoundType.LevelWin, volume = 1f },
        new SoundData { type = SoundType.LevelLose, volume = 1f }
    };

    public List<MusicData> musicList = new()
    {
        new MusicData { type = MusicType.GameplayMusic, volume = 0.8f },
        new MusicData { type = MusicType.LobbyMusic, volume = 0.8f },
        new MusicData { type = MusicType.WinMusic, volume = 0.8f },
        new MusicData { type = MusicType.LoseMusic, volume = 0.8f }
    };

    [Header("--- Audio Sources ---")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private int _sfxPoolSize = 12;

    private readonly Dictionary<SoundType, SoundData> _soundDict = new();
    private readonly Dictionary<MusicType, MusicData> _musicDict = new();
    private readonly List<AudioSource> _sfxPool = new();

    private bool _isSoundOn = true;
    private bool _isMusicOn = true;
    private bool _isVibrateOn = true;
    private float _soundVolume = 1f;
    private float _musicVolume = 0.8f;
    private MusicType _currentMusic = MusicType.None;
    private Tween _musicFadeTween;

    public bool IsSoundOn
    {
        get => _isSoundOn;
        set
        {
            _isSoundOn = value;
            PlayerPrefs.SetInt(PREF_SOUND, _isSoundOn ? 1 : 0);
            PlayerPrefs.Save();
            UpdateAudioSourceSettings();
            OnSoundToggled?.Invoke(_isSoundOn);
        }
    }

    public bool IsMusicOn
    {
        get => _isMusicOn;
        set
        {
            _isMusicOn = value;
            PlayerPrefs.SetInt(PREF_MUSIC, _isMusicOn ? 1 : 0);
            PlayerPrefs.Save();
            UpdateAudioSourceSettings();
            if (!_isMusicOn && _musicSource != null)
            {
                _musicSource.Stop();
            }
            else if (_isMusicOn && _currentMusic != MusicType.None)
            {
                PlayMusic(_currentMusic, true, 0.3f);
            }
            OnMusicToggled?.Invoke(_isMusicOn);
        }
    }

    public bool IsVibrateOn
    {
        get => _isVibrateOn;
        set
        {
            _isVibrateOn = value;
            PlayerPrefs.SetInt(PREF_VIBRATE, _isVibrateOn ? 1 : 0);
            PlayerPrefs.Save();
            OnVibrateToggled?.Invoke(_isVibrateOn);
        }
    }

    public float SoundVolume
    {
        get => _soundVolume;
        set
        {
            _soundVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREF_SOUND_VOL, _soundVolume);
            PlayerPrefs.Save();
            UpdateAudioSourceSettings();
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREF_MUSIC_VOL, _musicVolume);
            PlayerPrefs.Save();
            UpdateAudioSourceSettings();
        }
    }

    public event Action<bool> OnSoundToggled;
    public event Action<bool> OnMusicToggled;
    public event Action<bool> OnVibrateToggled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadPreferences();
        BuildDictionaries();
        EnsureAudioSources();
    }

    private void LoadPreferences()
    {
        _isSoundOn = PlayerPrefs.GetInt(PREF_SOUND, 1) == 1;
        _isMusicOn = PlayerPrefs.GetInt(PREF_MUSIC, 1) == 1;
        _isVibrateOn = PlayerPrefs.GetInt(PREF_VIBRATE, 1) == 1;
        _soundVolume = PlayerPrefs.GetFloat(PREF_SOUND_VOL, 1f);
        _musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.8f);
    }

    private void BuildDictionaries()
    {
        _soundDict.Clear();
        foreach (var item in soundList)
        {
            if (item != null && !_soundDict.ContainsKey(item.type))
            {
                _soundDict.Add(item.type, item);
            }
        }

        _musicDict.Clear();
        foreach (var item in musicList)
        {
            if (item != null && !_musicDict.ContainsKey(item.type))
            {
                _musicDict.Add(item.type, item);
            }
        }
    }

    private void EnsureAudioSources()
    {
        if (_musicSource == null)
        {
            var musicGo = new GameObject("MusicSource");
            musicGo.transform.SetParent(transform);
            _musicSource = musicGo.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        }

        if (_sfxSource == null)
        {
            var sfxGo = new GameObject("SFXSource");
            sfxGo.transform.SetParent(transform);
            _sfxSource = sfxGo.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
        }

        _sfxPool.Clear();
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            var poolGo = new GameObject($"SFX_Voice_{i}");
            poolGo.transform.SetParent(transform);
            var src = poolGo.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            _sfxPool.Add(src);
        }

        UpdateAudioSourceSettings();
    }

    private void UpdateAudioSourceSettings()
    {
        if (_musicSource != null)
        {
            _musicSource.mute = !_isMusicOn;
            _musicSource.volume = _musicVolume;
        }

        if (_sfxSource != null)
        {
            _sfxSource.mute = !_isSoundOn;
            _sfxSource.volume = _soundVolume;
        }

        for (int i = 0; i < _sfxPool.Count; i++)
        {
            if (_sfxPool[i] != null)
            {
                _sfxPool[i].mute = !_isSoundOn;
                _sfxPool[i].volume = _soundVolume;
            }
        }
    }

    #region Sound (SFX) Playback

    public void PlaySound(SoundType type, float volumeMultiplier = 1f)
    {
        if (!_isSoundOn) return;

        if (!_soundDict.TryGetValue(type, out var soundData) || soundData.clip == null)
        {
            return;
        }

        float finalVolume = soundData.volume * volumeMultiplier * _soundVolume;
        float pitch = soundData.pitch;
        if (soundData.randomizePitch)
        {
            pitch = UnityEngine.Random.Range(soundData.minPitch, soundData.maxPitch);
        }

        AudioSource source = GetAvailableSFXSource();
        if (source != null)
        {
            source.pitch = pitch;
            source.PlayOneShot(soundData.clip, finalVolume);
        }
        else if (_sfxSource != null)
        {
            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(soundData.clip, finalVolume);
        }
    }

    public void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f, bool randomPitch = false)
    {
        if (!_isSoundOn || clip == null) return;

        if (randomPitch)
        {
            pitch = UnityEngine.Random.Range(0.93f, 1.07f);
        }

        AudioSource source = GetAvailableSFXSource();
        if (source != null)
        {
            source.pitch = pitch;
            source.PlayOneShot(clip, volume * _soundVolume);
        }
        else if (_sfxSource != null)
        {
            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, volume * _soundVolume);
        }
    }

    public void PlayButtonClick() => PlaySound(SoundType.ButtonClick);
    public void PlayCarHit() => PlaySound(SoundType.CarHit);
    public void PlayCarDropBall() => PlaySound(SoundType.CarDropBall);
    public void PlayTrayComplete() => PlaySound(SoundType.TrayComplete);
    public void PlayBallDrop() => PlaySound(SoundType.BallDrop);
    public void PlayBallEnterConveyor() => PlaySound(SoundType.BallEnterConveyor);
    public void PlayBallFeedTray() => PlaySound(SoundType.BallFeedTray);
    public void PlayWin() => PlaySound(SoundType.LevelWin);
    public void PlayLose() => PlaySound(SoundType.LevelLose);

    private AudioSource GetAvailableSFXSource()
    {
        for (int i = 0; i < _sfxPool.Count; i++)
        {
            if (_sfxPool[i] != null && !_sfxPool[i].isPlaying)
            {
                return _sfxPool[i];
            }
        }
        return _sfxPool.Count > 0 ? _sfxPool[0] : null;
    }

    #endregion

    #region Music Playback

    public void PlayMusic(MusicType type, bool loop = true, float fadeDuration = 0.5f)
    {
        _currentMusic = type;
        if (!_isMusicOn || type == MusicType.None)
        {
            StopMusic(fadeDuration);
            return;
        }

        if (!_musicDict.TryGetValue(type, out var musicData) || musicData.clip == null)
        {
            return;
        }

        if (_musicSource == null) return;

        float targetVolume = musicData.volume * _musicVolume;

        _musicFadeTween?.Kill();
        if (_musicSource.isPlaying && _musicSource.clip == musicData.clip)
        {
            _musicFadeTween = _musicSource.DOFade(targetVolume, fadeDuration);
            return;
        }

        if (fadeDuration > 0f && _musicSource.isPlaying)
        {
            _musicFadeTween = _musicSource.DOFade(0f, fadeDuration * 0.5f).OnComplete(() =>
            {
                _musicSource.clip = musicData.clip;
                _musicSource.loop = loop;
                _musicSource.Play();
                _musicFadeTween = _musicSource.DOFade(targetVolume, fadeDuration * 0.5f);
            });
        }
        else
        {
            _musicSource.clip = musicData.clip;
            _musicSource.loop = loop;
            _musicSource.volume = targetVolume;
            _musicSource.Play();
        }
    }

    public void PlayBGM(AudioClip clip, bool loop = true, float fadeDuration = 0.5f)
    {
        if (clip == null) return;
        if (!_isMusicOn)
        {
            _musicSource.clip = clip;
            return;
        }

        if (_musicSource == null) return;

        _musicFadeTween?.Kill();
        if (_musicSource.isPlaying && _musicSource.clip == clip)
        {
            _musicFadeTween = _musicSource.DOFade(_musicVolume, fadeDuration);
            return;
        }

        if (fadeDuration > 0f && _musicSource.isPlaying)
        {
            _musicFadeTween = _musicSource.DOFade(0f, fadeDuration * 0.5f).OnComplete(() =>
            {
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.Play();
                _musicFadeTween = _musicSource.DOFade(_musicVolume, fadeDuration * 0.5f);
            });
        }
        else
        {
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = _musicVolume;
            _musicSource.Play();
        }
    }

    public void StopMusic(float fadeDuration = 0.5f)
    {
        if (_musicSource == null || !_musicSource.isPlaying) return;

        _musicFadeTween?.Kill();
        if (fadeDuration > 0f)
        {
            _musicFadeTween = _musicSource.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                _musicSource.Stop();
            });
        }
        else
        {
            _musicSource.Stop();
        }
    }

    #endregion

    #region Haptics & Helpers

    public void PlayVibrate(long milliseconds = 50)
    {
        if (!_isVibrateOn) return;
#if UNITY_ANDROID || UNITY_IOS
        try { Handheld.Vibrate(); } catch {}
#endif
    }

    public void ToggleSound() => IsSoundOn = !IsSoundOn;
    public void ToggleMusic() => IsMusicOn = !IsMusicOn;
    public void ToggleVibrate() => IsVibrateOn = !IsVibrateOn;

    public static void Play(SoundType type, float volume = 1f)
    {
        if (Instance != null) Instance.PlaySound(type, volume);
    }

    public static void PlayBGM(MusicType type, float fade = 0.5f)
    {
        if (Instance != null) Instance.PlayMusic(type, true, fade);
    }

    public static void Vibrate(long milliseconds = 50)
    {
        if (Instance != null) Instance.PlayVibrate(milliseconds);
    }

    #endregion
}
