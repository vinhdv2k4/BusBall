using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace BusBallJam.Spline
{
    public class SplineAdapter
    {
        private const int MinBakeResolution = 8;

        private readonly SplineContainer _splineContainer;
        private readonly bool _reverseDirection;
        private readonly List<Vector3> _samplePositions = new();
        private readonly List<float> _sampleDistances = new();
        private readonly List<float> _sampleProgresses = new();
        private float _length;

        public SplineAdapter(SplineContainer splineContainer, bool reverseDirection = false, int bakeResolution = 256)
        {
            _splineContainer = splineContainer;
            _reverseDirection = reverseDirection;
            BakeSpline(Mathf.Max(MinBakeResolution, bakeResolution));
        }

        public float Length => _length;
        public float TotalLength => _length;
        public bool ReverseDirection => _reverseDirection;
        public bool IsValid => _splineContainer != null;
        public SplineContainer SplineContainer => _splineContainer;

        public Vector3 EvaluatePosition(float progress)
        {
            if (_splineContainer == null) return Vector3.zero;
            return _splineContainer.EvaluatePosition(Mathf.Clamp01(progress));
        }

        public Vector3 EvaluatePositionByDistance(float distance)
        {
            return EvaluateBakedPosition(distance, false);
        }

        public Vector3 EvaluatePositionByDistance(float distance, bool loop)
        {
            return EvaluateBakedPosition(distance, loop);
        }

        public Quaternion EvaluateRotation(float progress)
        {
            if (_splineContainer == null) return Quaternion.identity;

            var tangent = (Vector3)_splineContainer.EvaluateTangent(Mathf.Clamp01(progress));
            if (_reverseDirection) tangent = -tangent;
            return tangent.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(tangent.normalized, Vector3.up)
                : Quaternion.identity;
        }

        public Quaternion EvaluateRotationByDistance(float distance)
        {
            return EvaluateRotation(DistanceToProgress(distance));
        }

        public Quaternion EvaluateRotationByDistance(float distance, bool loop)
        {
            return EvaluateRotation(DistanceToProgress(distance, loop));
        }

        public Vector3 EvaluateTangent(float progress)
        {
            if (_splineContainer == null) return Vector3.forward;
            var tangent = (Vector3)_splineContainer.EvaluateTangent(Mathf.Clamp01(progress));
            return _reverseDirection ? -tangent.normalized : tangent.normalized;
        }

        public float ProgressToDistance(float progress)
        {
            if (_splineContainer == null || _samplePositions.Count == 0) return 0f;
            return FindClosestDistance(EvaluatePosition(Mathf.Repeat(progress, 1f)));
        }

        public float DistanceToProgress(float distance)
        {
            if (_length <= 0.001f) return 0f;
            return DistanceToSplineProgress(NormalizeDistance(distance, false));
        }

        public float DistanceToProgress(float distance, bool loop)
        {
            if (_length <= 0.001f) return 0f;
            return DistanceToSplineProgress(NormalizeDistance(distance, loop));
        }

        public float WrapDistance(float distance)
        {
            return _length <= 0.001f ? 0f : Mathf.Repeat(distance, _length);
        }

        public float ForwardDistance(float from, float to, bool loop)
        {
            if (!loop)
            {
                return to >= from ? to - from : float.PositiveInfinity;
            }

            if (Mathf.Approximately(from, to)) return 0f;
            return to >= from ? to - from : _length - from + to;
        }

        public bool DidCrossDistance(float previous, float current, float target, bool loop)
        {
            if (!loop)
            {
                return previous <= target && current >= target;
            }

            previous = WrapDistance(previous);
            current = WrapDistance(current);
            target = WrapDistance(target);

            if (current >= previous)
            {
                return previous <= target && target <= current;
            }

            return target >= previous || target <= current;
        }

        public bool IsDistanceNear(float a, float b, float radius, bool loop)
        {
            var distance = Mathf.Abs(a - b);
            if (loop) distance = Mathf.Min(distance, _length - distance);
            return distance <= Mathf.Max(0f, radius);
        }

        public float FindClosestDistance(Vector3 worldPosition)
        {
            if (_samplePositions.Count == 0) return 0f;
            if (_samplePositions.Count == 1) return 0f;

            var bestDistance = 0f;
            var bestSqrDistance = float.PositiveInfinity;

            for (var i = 0; i < _samplePositions.Count - 1; i++)
            {
                var a = _samplePositions[i];
                var b = _samplePositions[i + 1];
                var ab = b - a;
                var abSqrMagnitude = ab.sqrMagnitude;
                var segmentT = abSqrMagnitude > 0.000001f
                    ? Mathf.Clamp01(Vector3.Dot(worldPosition - a, ab) / abSqrMagnitude)
                    : 0f;
                var projected = Vector3.Lerp(a, b, segmentT);
                var sqrDistance = (worldPosition - projected).sqrMagnitude;

                if (sqrDistance >= bestSqrDistance) continue;

                bestSqrDistance = sqrDistance;
                bestDistance = Mathf.Lerp(_sampleDistances[i], _sampleDistances[i + 1], segmentT);
            }

            return Mathf.Clamp(bestDistance, 0f, _length);
        }

        public void ReBake(int bakeResolution = 256)
        {
            BakeSpline(Mathf.Max(MinBakeResolution, bakeResolution));
        }

        private void BakeSpline(int bakeResolution)
        {
            _samplePositions.Clear();
            _sampleDistances.Clear();
            _sampleProgresses.Clear();

            if (_splineContainer == null)
            {
                _length = 1f;
                _samplePositions.Add(Vector3.zero);
                _sampleDistances.Add(0f);
                _sampleProgresses.Add(0f);
                return;
            }

            var segmentCount = Mathf.Max(MinBakeResolution, bakeResolution);
            var cumulativeDistance = 0f;
            var previousPosition = EvaluateMovementProgress(0f);

            _samplePositions.Add(previousPosition);
            _sampleDistances.Add(0f);
            _sampleProgresses.Add(GetSplineProgressFromMovementProgress(0f));

            for (var i = 1; i <= segmentCount; i++)
            {
                var movementProgress = i / (float)segmentCount;
                var splineProgress = GetSplineProgressFromMovementProgress(movementProgress);
                var position = EvaluatePosition(splineProgress);
                cumulativeDistance += Vector3.Distance(previousPosition, position);

                _samplePositions.Add(position);
                _sampleDistances.Add(cumulativeDistance);
                _sampleProgresses.Add(splineProgress);

                previousPosition = position;
            }

            _length = Mathf.Max(0.001f, cumulativeDistance);
        }

        private Vector3 EvaluateBakedPosition(float distance, bool loop)
        {
            if (_samplePositions.Count == 0) return Vector3.zero;
            if (_samplePositions.Count == 1) return _samplePositions[0];

            distance = NormalizeDistance(distance, loop);

            var index = FindSegmentIndex(distance);
            var distanceA = _sampleDistances[index];
            var distanceB = _sampleDistances[index + 1];
            var localT = Mathf.Approximately(distanceA, distanceB)
                ? 0f
                : Mathf.InverseLerp(distanceA, distanceB, distance);

            return Vector3.Lerp(_samplePositions[index], _samplePositions[index + 1], localT);
        }

        private float DistanceToSplineProgress(float distance)
        {
            if (_sampleProgresses.Count == 0) return 0f;
            if (_sampleProgresses.Count == 1) return _sampleProgresses[0];

            var index = FindSegmentIndex(distance);
            var distanceA = _sampleDistances[index];
            var distanceB = _sampleDistances[index + 1];
            var localT = Mathf.Approximately(distanceA, distanceB)
                ? 0f
                : Mathf.InverseLerp(distanceA, distanceB, distance);

            return Mathf.Lerp(_sampleProgresses[index], _sampleProgresses[index + 1], localT);
        }

        private float NormalizeDistance(float distance, bool loop)
        {
            if (_length <= 0.001f) return 0f;
            return loop ? WrapDistance(distance) : Mathf.Clamp(distance, 0f, _length);
        }

        private int FindSegmentIndex(float distance)
        {
            for (var i = 0; i < _sampleDistances.Count - 1; i++)
            {
                if (distance <= _sampleDistances[i + 1]) return i;
            }

            return Mathf.Max(0, _sampleDistances.Count - 2);
        }

        private Vector3 EvaluateMovementProgress(float movementProgress)
        {
            return EvaluatePosition(GetSplineProgressFromMovementProgress(movementProgress));
        }

        private float GetSplineProgressFromMovementProgress(float movementProgress)
        {
            movementProgress = Mathf.Clamp01(movementProgress);
            return _reverseDirection ? 1f - movementProgress : movementProgress;
        }
    }
}
