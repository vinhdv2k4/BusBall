using UnityEngine;

namespace BallDropParty.Gameplay
{
    /// <summary>
    /// Receiver for Animation Events used by the box Animator.
    /// Keep this component on the same GameObject as the Animator.
    /// </summary>
    public class BoxAnimationEvents : MonoBehaviour
    {
        private static readonly int IsDeactive = Animator.StringToHash("IsDeactive");

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        // Called by the Animation Event in Box_Die.
        public void OnStartDeactive()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_animator != null && _animator.isActiveAndEnabled)
                _animator.SetBool(IsDeactive, true);
        }
    }
}
