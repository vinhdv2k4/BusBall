using UnityEngine;
using UnityEngine.Splines;

namespace BusBallJam.Spline
{
    public class SplinePathFollower : MonoBehaviour
    {
        [Header("Spline Target")]
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private bool reverseDirection = false;
        [SerializeField] private int bakeResolution = 256;

        [Header("Movement")]
        [SerializeField] private float speed = 3f;
        [SerializeField] private bool autoPlay = true;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool updateRotation = true;
        [SerializeField] private float startDistance = 0f;

        private SplineAdapter _adapter;
        private float _currentDistance;
        private bool _isPlaying;

        public SplineAdapter Adapter => _adapter;
        public float CurrentDistance => _currentDistance;
        public float CurrentProgress => _adapter != null ? _adapter.DistanceToProgress(_currentDistance, loop) : 0f;
        public float Speed { get => speed; set => speed = value; }
        public bool IsPlaying => _isPlaying;

        private void Start()
        {
            Initialize();
            if (autoPlay)
            {
                Play();
            }
        }

        public void Initialize()
        {
            if (splineContainer == null)
                splineContainer = GetComponentInParent<SplineContainer>();

            if (splineContainer != null)
            {
                _adapter = new SplineAdapter(splineContainer, reverseDirection, bakeResolution);
                _currentDistance = startDistance;
                UpdateTransform();
            }
        }

        public void SetSplineContainer(SplineContainer container, bool reverse = false)
        {
            splineContainer = container;
            reverseDirection = reverse;
            Initialize();
        }

        public void Play()
        {
            if (_adapter == null) Initialize();
            _isPlaying = true;
        }

        public void Pause()
        {
            _isPlaying = false;
        }

        public void Stop()
        {
            _isPlaying = false;
            _currentDistance = startDistance;
            UpdateTransform();
        }

        public void SetDistance(float distance)
        {
            _currentDistance = distance;
            UpdateTransform();
        }

        public void SetProgress(float progress)
        {
            if (_adapter == null) return;
            _currentDistance = _adapter.ProgressToDistance(progress);
            UpdateTransform();
        }

        private void Update()
        {
            if (!_isPlaying || _adapter == null || !_adapter.IsValid) return;

            _currentDistance += speed * Time.deltaTime;

            if (loop)
            {
                _currentDistance = _adapter.WrapDistance(_currentDistance);
            }
            else
            {
                if (_currentDistance >= _adapter.Length)
                {
                    _currentDistance = _adapter.Length;
                    _isPlaying = false;
                }
                else if (_currentDistance < 0f)
                {
                    _currentDistance = 0f;
                    _isPlaying = false;
                }
            }

            UpdateTransform();
        }

        private void UpdateTransform()
        {
            if (_adapter == null || !_adapter.IsValid) return;

            transform.position = _adapter.EvaluatePositionByDistance(_currentDistance, loop);

            if (updateRotation)
            {
                transform.rotation = _adapter.EvaluateRotationByDistance(_currentDistance, loop);
            }
        }
    }
}
