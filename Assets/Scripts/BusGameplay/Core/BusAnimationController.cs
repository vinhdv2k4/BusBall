using UnityEngine;

public class BusAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Animation topAnimation;
    [SerializeField] private GameObject releaseVfx;
    [SerializeField] private GameObject landingVfx;
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string blockedState = "Blocked";
    [SerializeField] private string hitLeftState = "Hit_Left";
    [SerializeField] private string hitRightState = "Hit_Right";
    [SerializeField] private string hitFrontState = "Hit_Front";
    [SerializeField] private string hitBackState = "Hit_Back";

    public void PlayIdle() { if (animator != null) animator.Play(idleState); }
    public void PlayBlocked() { if (animator != null) animator.Play(blockedState); }

    public void PlayHit(HitDirection direction)
    {
        if (animator == null) return;
        string state = direction switch
        {
            HitDirection.Left => hitLeftState,
            HitDirection.Right => hitRightState,
            HitDirection.Back => hitBackState,
            _ => hitFrontState
        };
        animator.Play(state);
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
