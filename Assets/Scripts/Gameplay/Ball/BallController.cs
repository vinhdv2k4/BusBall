using System.Collections;
using UnityEngine;

namespace BallDropParty.Gameplay
{
    public enum BallState
    {
        None = 0,
        SpawnedInFunnel = 1,
        WaitingAtFunnelBottom = 2,
        MovingToConveyor = 3,
        OnConveyor = 4,
        Reviving = 5,
        Collecting = 6,
        Collected = 7
    }

    [SelectionBase]
    public class BallController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Rigidbody _rigidbody3D;
        [SerializeField] private Collider _collider3D;
        [SerializeField] private BallView _view;
        [SerializeField] private float _ballScale = 1.25f;

        [Header("State")]
        [SerializeField] private ColorId _colorId = ColorId.Blue;
        [SerializeField] private BallState _state = BallState.None;

        public string BallId { get; private set; }
        public ColorId ColorId => _colorId;
        public BallState State => _state;
        public Rigidbody Rigidbody3D => _rigidbody3D;
        public Collider Collider3D => _collider3D;
        public BallView View => _view;
        public float BallScale => _ballScale;

        public float ConveyorProgress { get; set; }
        public float PreviousConveyorDistance { get; set; }
        public float ConveyorDistance { get; set; }
        public int TargetPointIndex { get; set; } = -1;
        public bool IsOnConveyor => _state == BallState.OnConveyor;
        public bool IsCollected => _state == BallState.Collected || _state == BallState.Collecting;

        private void Awake()
        {
            if (_rigidbody3D == null) _rigidbody3D = GetComponent<Rigidbody>();
            if (_collider3D == null) _collider3D = GetComponent<Collider>();
            if (_view == null) _view = GetComponentInChildren<BallView>();

            transform.localScale = Vector3.one * _ballScale;
            ApplyColor();
        }

        public void Init(ColorId colorId, GameColorConfig colorConfig = null)
        {
            BallId = System.Guid.NewGuid().ToString("N");
            _colorId = colorId;
            _state = BallState.OnConveyor;
            ConveyorProgress = 0f;
            ConveyorDistance = 0f;
            TargetPointIndex = -1;

            transform.localScale = Vector3.one * _ballScale;
            ApplyColor(colorConfig);

            EnablePhysics();
        }

        public void ApplyColor(GameColorConfig colorConfig = null)
        {
            if (_view == null) _view = GetComponentInChildren<BallView>();
            if (_view == null) return;

            var entry = GameColorConfigProvider.Get(_colorId);
            if (entry != null)
            {
                if (entry.ballMaterial != null)
                {
                    _view.ApplyMaterial(entry.ballMaterial);
                }
                else
                {
                    _view.SetColor(entry.color);
                }
            }
        }

        public void SetState(BallState state)
        {
            _state = state;
        }

        public void EnablePhysics()
        {
            if (_rigidbody3D == null) _rigidbody3D = GetComponent<Rigidbody>();
            if (_rigidbody3D != null)
            {
                _rigidbody3D.isKinematic = false;
                _rigidbody3D.useGravity = true;
            }

            if (_collider3D == null) _collider3D = GetComponent<Collider>();
            if (_collider3D != null)
            {
                _collider3D.enabled = true;
                _collider3D.isTrigger = false;
            }
        }

        public void DisablePhysics()
        {
            if (_rigidbody3D == null) _rigidbody3D = GetComponent<Rigidbody>();
            if (_rigidbody3D != null)
            {
                _rigidbody3D.isKinematic = true;
                _rigidbody3D.useGravity = false;
            }

            if (_collider3D == null) _collider3D = GetComponent<Collider>();
            if (_collider3D != null)
            {
                _collider3D.enabled = false;
            }
        }

        public void MoveToPosition(Vector3 targetPosition, float duration, System.Action onComplete = null)
        {
            DisablePhysics();
            _state = BallState.Collecting;

            StartCoroutine(AnimateMove(targetPosition, duration, () =>
            {
                _state = BallState.Collected;
                onComplete?.Invoke();
            }));
        }

        private IEnumerator AnimateMove(Vector3 target, float duration, System.Action onComplete)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Vector3 current = Vector3.Lerp(start, target, smoothT);
                current.y += Mathf.Sin(smoothT * Mathf.PI) * 0.4f;
                transform.position = current;

                yield return null;
            }

            transform.position = target;
            onComplete?.Invoke();
        }
    }
}
