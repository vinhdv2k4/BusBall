using System.Collections;
using UnityEngine;

namespace BusBallJam.Spline
{
    [RequireComponent(typeof(Collider))]
    public class ColliderBlock : MonoBehaviour
    {
        private Collider _blockCollider;

        private void Awake()
        {
            _blockCollider = GetComponent<Collider>();
        }

        public bool CanPass() => true;

        public IEnumerator WaitUntilCanPass()
        {
            yield break;
        }
    }
}
