using UnityEngine;

public class BoxAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private const string CompleteState = "Box_Complete";
    private const string IdleState = "Idle";

    public void PlayComplete() { if (animator != null) animator.Play(CompleteState); }
    public void ResetToIdle() { if (animator != null) animator.Play(IdleState); }
}
