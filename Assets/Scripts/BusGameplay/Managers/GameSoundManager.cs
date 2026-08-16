using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameSoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [SerializeField] private AudioClip blockPutClip;
    [SerializeField] private AudioClip boxCompleteClip;
    [SerializeField] private AudioClip fillBox1Clip;
    [SerializeField] private AudioClip fillBox2Clip;
    [SerializeField] private AudioClip fillBox3Clip;
    [SerializeField] private AudioClip carImpactClip;
    [SerializeField] private AudioClip carFillClip;
    [SerializeField] private AudioClip carStuckClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private AudioSource sfxSource;
    private AudioSource resultSource;
    private AudioSource uiSource;
    private GameplayManager gameplayManager;

    public static GameSoundManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        resultSource = CreateSource();
        uiSource = CreateSource();
        gameplayManager = GetComponent<GameplayManager>();
    }

    private void OnEnable()
    {
        if (gameplayManager == null)
            gameplayManager = GetComponent<GameplayManager>();
        if (gameplayManager != null)
            gameplayManager.StateChanged += HandleGameplayStateChanged;
    }

    private void OnDisable()
    {
        if (gameplayManager != null)
            gameplayManager.StateChanged -= HandleGameplayStateChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayWin() => PlayResult(winClip);

    public void PlayLose() => PlayResult(loseClip);

    public void PlayBlockPut() => Play(blockPutClip);

    public void PlayBoxComplete() => Play(boxCompleteClip);

    public void PlayCarImpact() => Play(carImpactClip);

    public void PlayCarFill() => Play(carFillClip);

    public void PlayCarStuck() => Play(carStuckClip);

    public void PlayButtonClick() => uiSource?.PlayOneShot(buttonClickClip, volume);

    public void StopResultSound()
    {
        if (resultSource == null) return;
        resultSource.Stop();
        resultSource.clip = null;
    }

    public void PlayFillSequence() => StartCoroutine(PlayFillSequenceRoutine());

    public void Play(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, volume);
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private void PlayResult(AudioClip clip)
    {
        if (resultSource == null || clip == null) return;
        resultSource.Stop();
        resultSource.clip = clip;
        resultSource.volume = volume;
        resultSource.Play();
    }

    private void HandleGameplayStateChanged(GameplayManager.GameplayState state)
    {
        if (state == GameplayManager.GameplayState.Won) PlayWin();
        else if (state == GameplayManager.GameplayState.Lost) PlayLose();
    }

    private IEnumerator PlayFillSequenceRoutine()
    {
        Play(fillBox1Clip);
        yield return new WaitForSeconds(0.02f);
        Play(fillBox2Clip);
        yield return new WaitForSeconds(0.02f);
        Play(fillBox3Clip);
    }
}
