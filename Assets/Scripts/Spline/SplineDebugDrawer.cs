using UnityEngine;
using UnityEngine.Splines;

namespace BusBallJam.Spline
{
    [ExecuteAlways]
    public class SplineDebugDrawer : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool reverseDirection = false;
        [SerializeField] private int sampleCount = 64;
        [SerializeField] private float sphereSize = 0.08f;
        [SerializeField] private Color splineColor = Color.cyan;
        [SerializeField] private Color arrowColor = Color.yellow;

        private SplineAdapter _adapter;

        private void Reset()
        {
            splineContainer = GetComponent<SplineContainer>();
        }

        private void OnValidate()
        {
            if (splineContainer == null)
                splineContainer = GetComponent<SplineContainer>();

            _adapter = null;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            if (splineContainer == null)
                splineContainer = GetComponent<SplineContainer>();

            if (splineContainer == null) return;

            if (_adapter == null || !_adapter.IsValid)
            {
                _adapter = new SplineAdapter(splineContainer, reverseDirection, Mathf.Max(32, sampleCount));
            }

            var samples = Mathf.Max(2, sampleCount);
            var previous = _adapter.EvaluatePosition(0f);

            Gizmos.color = splineColor;
            for (var i = 1; i <= samples; i++)
            {
                var progress = i / (float)samples;
                var next = _adapter.EvaluatePosition(progress);
                Gizmos.DrawLine(previous, next);

                if (i % 8 == 0)
                {
                    Gizmos.color = arrowColor;
                    var tangent = _adapter.EvaluateTangent(progress);
                    Gizmos.DrawRay(next, tangent * (sphereSize * 2.5f));
                    Gizmos.color = splineColor;
                }

                previous = next;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_adapter.EvaluatePosition(0f), sphereSize * 1.2f);
        }
    }
}
