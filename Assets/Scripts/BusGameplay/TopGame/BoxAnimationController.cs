using UnityEngine;

public class BoxAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private static readonly int IsActive = Animator.StringToHash("IsActive");
    private static readonly int IsDeactive = Animator.StringToHash("IsDeactive");

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void PlayBorn()
    {
        if (animator == null) return;
        animator.SetBool(IsDeactive, false);
        animator.SetBool(IsActive, true);
    }

    public void PlayComplete()
    {
        if (animator == null) return;
        animator.SetBool(IsDeactive, true);
    }

    public void ResetToIdle()
    {
        if (animator == null) return;
        animator.SetBool(IsDeactive, false);
        animator.SetBool(IsActive, false);
    }
}
