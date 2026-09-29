using UnityEngine;

namespace BallDropParty.Gameplay
{
    /// Put this on the non-trigger collider at the conveyor hole.
    public sealed class ConveyorEntryCollider : MonoBehaviour
    {
        [SerializeField] private ConveyorManager _conveyor;
        [SerializeField] private Collider _entryCollider;
        [SerializeField] private LayerMask _ballLayers = ~0;
        [SerializeField] private bool _logBallDetection = true;

        private void Awake()
        {
            if (_conveyor == null) _conveyor = GetComponentInParent<ConveyorManager>();
            if (_entryCollider == null) _entryCollider = GetComponent<Collider>();
        }

        private void FixedUpdate()
        {
            if (_entryCollider == null || !_entryCollider.enabled) return;
            Bounds bounds = _entryCollider.bounds;
            Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents,
                _entryCollider.transform.rotation, _ballLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != _entryCollider) TryReceive(hits[i]);
            }
        }

        private void OnCollisionEnter(Collision collision) => TryReceive(collision.collider);
        private void OnCollisionStay(Collision collision) => TryReceive(collision.collider);

        private void TryReceive(Collider ballCollider)
        {
            if (_conveyor == null) _conveyor = GetComponentInParent<ConveyorManager>();
            if (_conveyor == null)
                _conveyor = FindAnyObjectByType<ConveyorManager>();

            BallItem ball = ballCollider != null ? ballCollider.GetComponentInParent<BallItem>() : null;
            if (ball == null) return;

            bool received = _conveyor != null && _conveyor.TryReceiveBallFromEntryCollider(ballCollider);
            if (_logBallDetection)
            {
                Debug.Log($"[ConveyorEntryCollider] Ball detected: {ball.name} | Received: {received}", this);
            }
        }

    }
}
