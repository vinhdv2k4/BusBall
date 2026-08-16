using UnityEngine;

public class BusAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Animation topAnimation;
    [SerializeField] private GameObject releaseVfx;
    [SerializeField] private GameObject landingVfx;
    private const string IdleState = "Car_Idle";
    private const string HitState = "Car_Hit";
    private const string HitLeftState = "Car_Left";
    private const string HitRightState = "Car_Right";
    private const string HitBackState = "Car_Back";
    private const string StuckState = "Car_Stuck";
    private const string MoveState = "Car_Front";
    private const string SmallOpenState = "Car_S_Open";
    private const string MediumOpenState = "Car_M_Open";
    private const string LargeOpenState = "Car_L_Open";
    private const string ReleaseState = "Car_Release";
    private const string HurryState = "Hurry_Bus";
    private int playingStateHash;

    public void PlayIdle() { PlayIfExists(IdleState); }
    public void PlayBlocked() { PlayIfExists(HitState); }
    public void PlayStuck() { PlayIfExists(StuckState, true); }
    public void PlayMove() { PlayIfExists(MoveState); }
    public void PlayMoveLeft() { PlayIfExists(HitLeftState); }
    public void PlayMoveRight() { PlayIfExists(HitRightState); }
    public void PlayGarageOut() { PlayIfExists(SmallOpenState); }
    public void PlayRelease() { PlayIfExists(ReleaseState); }
    public void PlayHurry() { PlayIfExists(HurryState); }
    public void PlayOpen(BusType busType)
    {
        PlayIfExists(GetOpenState(busType));
    }

    public float GetOpenDuration(BusType busType)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        string state = GetOpenState(busType);
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip != null && clip.name == state) return clip.length;
        return 0f;
    }

    public float GetReleaseDuration()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip != null && clip.name == ReleaseState) return clip.length;
        return 0f;
    }

    private static string GetOpenState(BusType busType)
    {
        return busType switch
        {
            BusType.Medium => MediumOpenState,
            BusType.Large => LargeOpenState,
            _ => SmallOpenState
        };
    }

    public void PlayHit(HitDirection direction)
    {
        string state = direction switch
        {
            HitDirection.Left => HitLeftState,
            HitDirection.Right => HitRightState,
            HitDirection.Back => HitBackState,
            _ => HitState
        };
        PlayIfExists(state, true);
    }

    private void PlayIfExists(string stateName, bool restart = false)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;

        int stateHash = Animator.StringToHash(stateName);
        if (!restart && playingStateHash == stateHash) return;
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogWarning($"{name}: Animator state '{stateName}' was not found.");
            return;
        }

        animator.Play(stateHash, 0, 0f);
        playingStateHash = stateHash;
    }

    public void PlayReleaseVfx()
    {
        if (releaseVfx != null) releaseVfx.SetActive(true);
    }

    public void PlayLandingVfx()
    {
        if (landingVfx != null) landingVfx.SetActive(true);
    }
}
