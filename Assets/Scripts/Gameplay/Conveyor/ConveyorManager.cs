using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Splines;
using BusBallJam.Spline;

namespace BallDropParty.Gameplay
{
    public class ConveyorManager : MonoBehaviour
    {
        [Header("Spline")]
        [SerializeField] private SplineContainer _splineContainer;
        [SerializeField] private ConveyorRuntimeConfig _runtimeConfig;

        [Header("Managers")]
        [SerializeField] private GateManager _gateManager;
        [SerializeField] private Transform _ballMoveParent;

        [Header("Point Views")]
        [SerializeField] private ConveyorPointView _pointViewPrefab;
        [SerializeField] private Transform _pointViewRoot;
        [SerializeField] private bool _showPointViews = true;
        [SerializeField, Min(0.01f)] private float _minimumBallSpacing = 0.35f;
        [SerializeField, Min(0.01f)] private float _entryReceiveRadius = 1.2f;
        [SerializeField, Min(0f)] private float _entrySnapDuration = 0.4f;

        private SplineAdapter _splineAdapter;
        private readonly List<ConveyorPointView> _pointViews = new();
        private readonly List<BallItem> _activeBalls = new();
        private readonly Dictionary<BallItem, ConveyorPointView> _ballPointAnchors = new();
        private readonly Dictionary<BallItem, int> _reservedPointByBall = new();
        private readonly Dictionary<int, BallItem> _reservedPointOwners = new();
        private BallItem _chainLeader;
        private readonly HashSet<int> _entryClaimedPoints = new();
        private readonly HashSet<int> _entryReservedPoints = new();
        private float _beltDistanceOffset = 0f;
        private bool _isInitialized = false;
        private ParkingLotManager _parkingLotManager;

        public SplineAdapter SplineAdapter => _splineAdapter;
        public ConveyorRuntimeConfig Config => _runtimeConfig;
        public IReadOnlyList<BallItem> ActiveBalls => _activeBalls;
        public float EntryProgress => _runtimeConfig != null ? _runtimeConfig.EntryProgress : 0f;

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            if (Application.isPlaying && _pointViews.Count == 0)
            {
                BuildPointViews();
            }
        }

        public void Initialize()
        {
            // Spline da duoc bake va reference khong thay doi thi khong bake lai.
            if (_isInitialized && _splineAdapter != null && _splineAdapter.IsValid)
                return;

            if (_splineContainer == null)
                _splineContainer = GetComponent<SplineContainer>() ?? GetComponentInChildren<SplineContainer>();

            if (_pointViewRoot == null)
            {
                var receivePos = transform.Find("ReceiveBallPos");
                if (receivePos != null) _pointViewRoot = receivePos;
            }

            if (_ballMoveParent == null)
            {
                var ballParent = transform.Find("BallMoveParent");
                if (ballParent != null) _ballMoveParent = ballParent;
                else _ballMoveParent = transform;
            }

            if (_gateManager == null)
                _gateManager = FindAnyObjectByType<GateManager>();

            bool reverse = _runtimeConfig != null && _runtimeConfig.ReverseDirection;
            int resolution = _runtimeConfig != null ? _runtimeConfig.SplineBakeResolution : 256;

            if (_splineContainer != null)
            {
                _splineAdapter = new SplineAdapter(_splineContainer, reverse, resolution);
                _isInitialized = true;
            }
        }

        [ContextMenu("Build Point Views")]
        public void BuildPointViews()
        {
            ClearPointViews();
            Initialize();

            if (_splineAdapter == null || !_splineAdapter.IsValid) return;

            int capacity = _runtimeConfig != null ? _runtimeConfig.SlotCapacity : 24;
            if (capacity <= 0) return;

            if (_pointViewRoot == null)
            {
                var rootObj = new GameObject("ReceiveBallPos");
                rootObj.transform.SetParent(transform, false);
                _pointViewRoot = rootObj.transform;
            }

            float distancePerSlot = _splineAdapter.Length / capacity;

            for (int i = 0; i < capacity; i++)
            {
                float distance = i * distancePerSlot;
                Vector3 pos = _splineAdapter.EvaluatePositionByDistance(distance, true);
                Quaternion rot = _splineAdapter.EvaluateRotationByDistance(distance, true);

                ConveyorPointView view;
                if (_pointViewPrefab != null)
                {
                    view = Instantiate(_pointViewPrefab, pos, rot, _pointViewRoot);
                }
                else
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
                    go.transform.SetPositionAndRotation(pos, rot);
                    go.transform.SetParent(_pointViewRoot, true);
                    var col = go.GetComponent<Collider>();
                    if (col != null) DestroyImmediate(col);
                    view = go.AddComponent<ConveyorPointView>();
                }

                view.Setup(i);
                view.SetConveyor(this);
                view.SetColorGroup(i % 2 == 0);
                view.SetVisible(_showPointViews);
                _pointViews.Add(view);
            }
        }

        public void ClearPointViews()
        {
            for (int i = _pointViews.Count - 1; i >= 0; i--)
            {
                if (_pointViews[i] != null)
                {
                    if (Application.isPlaying) Destroy(_pointViews[i].gameObject);
                    else DestroyImmediate(_pointViews[i].gameObject);
                }
            }
            _pointViews.Clear();
        }

        [ContextMenu("Clear All Balls")]
        public void ClearAllBalls()
        {
            for (int i = _activeBalls.Count - 1; i >= 0; i--)
            {
                BallItem ball = _activeBalls[i];
                if (ball != null)
                {
                    ball.transform.DOKill();
                    ball.isOnConveyor = false;
                    ball.isCollected = false;
                    ball.isEnteringConveyor = false;
                    if (GameplayObjectPool.Instance != null)
                        GameplayObjectPool.Instance.Release(ball.gameObject);
                    else
                        Destroy(ball.gameObject);
                }
            }
            _activeBalls.Clear();
            _ballPointAnchors.Clear();
            _chainLeader = null;

            if (_ballMoveParent != null)
            {
                var childBalls = _ballMoveParent.GetComponentsInChildren<BallItem>(true);
                for (int i = childBalls.Length - 1; i >= 0; i--)
                {
                    if (childBalls[i] != null)
                    {
                        childBalls[i].transform.DOKill();
                        if (GameplayObjectPool.Instance != null)
                            GameplayObjectPool.Instance.Release(childBalls[i].gameObject);
                        else
                            Destroy(childBalls[i].gameObject);
                    }
                }
            }

            _beltDistanceOffset = 0f;
            _reservedPointByBall.Clear();
            _reservedPointOwners.Clear();
        }

        public bool TryAddBallAtEntry(BallItem ball)
        {
            if (ball == null) return false;
            return AddBall(ball, EntryProgress);
        }

        public bool IsSlotFreeAtDistance(float checkDistance, float thresholdRatio = 0.0f)
        {
            if (_splineAdapter == null || !_splineAdapter.IsValid) return false;

            float slotDist = GetDistancePerSlot();
            float requiredClearance = slotDist * thresholdRatio;

            for (int i = 0; i < _activeBalls.Count; i++)
            {
                var existing = _activeBalls[i];
                if (existing == null || !existing.isOnConveyor || existing.isCollected)
                    continue;

                float fDist = _splineAdapter.ForwardDistance(checkDistance, existing.conveyorDistance, true);
                float bDist = _splineAdapter.ForwardDistance(existing.conveyorDistance, checkDistance, true);

                if (fDist < requiredClearance || bDist < requiredClearance)
                {
                    return false; // Slot is currently occupied by a passing ball!
                }
            }

            return true; // Slot is free!
        }

        private void DisablePhysicsOnConveyor(BallItem ball)
        {
            if (ball == null) return;
            var rb = ball.GetComponent<Rigidbody>();
            if (rb == null) rb = ball.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        public bool TryReceiveBallAtPoint(BallItem ball, ConveyorPointView point)
        {
            if (ball == null || point == null) return false;
            if (_splineAdapter == null || !_splineAdapter.IsValid) Initialize();
            if (_splineAdapter == null || !_splineAdapter.IsValid) return false;

            float distance = _splineAdapter.FindClosestDistance(point.transform.position);

            // A chasing ball reserves its destination point before reaching
            // it. Never let a newly dropped ball steal that point.
            if (IsPointReservedByAnotherBall(point.Index, ball))
                return false;

            // Require 0.6f clearance to prevent cutting in line when compact!
            if (!IsSlotFreeAtDistance(distance, 0.6f))
            {
                return false;
            }

            DisablePhysicsOnConveyor(ball);

            float progress = _splineAdapter.DistanceToProgress(distance, true);
            bool added = AddBall(ball, progress); // Instantly pull/snap into conveyor slot simultaneously!
            if (added)
            {
                _ballPointAnchors[ball] = point;
                TryReservePoint(ball, point);
            }
            if (added && _chainLeader == null) _chainLeader = ball;
            return added;
        }

        public bool IsEntrySlotFree(float progress)
        {
            if (_splineAdapter == null || !_splineAdapter.IsValid) Initialize();
            if (_splineAdapter == null || !_splineAdapter.IsValid) return false;

            float distance = _splineAdapter.ProgressToDistance(Mathf.Repeat(progress, 1f));
            return IsSlotFreeAtDistance(distance, 0.6f);
        }

        public bool TryClaimPointAtEntry(Vector3 worldPosition, out float progress)
        {
            progress = EntryProgress;
            if (_splineAdapter == null || !_splineAdapter.IsValid) Initialize();
            if (_splineAdapter == null || !_splineAdapter.IsValid || _pointViews.Count == 0) return false;

            int index = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < _pointViews.Count; i++)
            {
                if (_pointViews[i] == null) continue;
                if (_entryClaimedPoints.Contains(i)) continue;
                float distance = (_pointViews[i].transform.position - worldPosition).sqrMagnitude;
                if (distance < best) { best = distance; index = i; }
            }
            if (index < 0) return false;

            if (index < 0) return false;

            float pointDistance = _splineAdapter.FindClosestDistance(_pointViews[index].transform.position);
            float radius = GetDistancePerSlot() * 0.55f;
            for (int i = 0; i < _activeBalls.Count; i++)
            {
                var ball = _activeBalls[i];
                if (ball == null || ball.isCollected || !ball.isOnConveyor) continue;
                // Check immediately, but wait until this point itself passes
                // the entry. Never reserve a farther point early.
                if (_splineAdapter.IsDistanceNear(ball.conveyorDistance, pointDistance, radius, true))
                    return false;
            }

            _entryClaimedPoints.Add(index);
            progress = _splineAdapter.DistanceToProgress(pointDistance, true);
            return true;
        }

        public bool TryReserveNextFreePoint(Vector3 worldPosition, out int pointIndex)
        {
            pointIndex = -1;
            if (_splineAdapter == null || !_splineAdapter.IsValid) Initialize();
            if (_splineAdapter == null || !_splineAdapter.IsValid || _pointViews.Count == 0) return false;

            int entryIndex = -1;
            float nearestEntryDistance = float.PositiveInfinity;
            for (int i = 0; i < _pointViews.Count; i++)
            {
                if (_pointViews[i] == null) continue;
                float distance = (_pointViews[i].transform.position - worldPosition).sqrMagnitude;
                if (distance < nearestEntryDistance)
                {
                    nearestEntryDistance = distance;
                    entryIndex = i;
                }
            }
            if (entryIndex < 0) return false;

            // Only inspect the entry point and the next three points behind it.
            // Do not reserve an arbitrary free point far away on the conveyor.
            for (int offset = 0; offset <= 3; offset++)
            {
                int i = (entryIndex - offset + _pointViews.Count) % _pointViews.Count;
                if (_pointViews[i] == null || _entryReservedPoints.Contains(i)) continue;
                float pointDistance = _splineAdapter.FindClosestDistance(_pointViews[i].transform.position);
                bool occupied = false;
                for (int b = 0; b < _activeBalls.Count; b++)
                {
                    var ball = _activeBalls[b];
                    if (ball == null || ball.isCollected || !ball.isOnConveyor) continue;
                    if (_splineAdapter.IsDistanceNear(ball.conveyorDistance, pointDistance, GetDistancePerSlot() * 0.55f, true))
                    { occupied = true; break; }
                }
                if (occupied) continue;

                pointIndex = i;
                break;
            }

            if (pointIndex < 0) return false;
            _entryReservedPoints.Add(pointIndex);
            return true;
        }

        public bool TryConsumeReservedPointAtEntry(Vector3 worldPosition, int pointIndex, out float progress)
        {
            progress = EntryProgress;
            if (pointIndex < 0 || pointIndex >= _pointViews.Count || _pointViews[pointIndex] == null)
                return false;
            if (!_entryReservedPoints.Contains(pointIndex)) return false;

            int nearest = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < _pointViews.Count; i++)
            {
                if (_pointViews[i] == null) continue;
                float distance = (_pointViews[i].transform.position - worldPosition).sqrMagnitude;
                if (distance < best) { best = distance; nearest = i; }
            }
            if (nearest != pointIndex) return false;

            progress = _splineAdapter.DistanceToProgress(
                _splineAdapter.FindClosestDistance(_pointViews[pointIndex].transform.position), true);
            _entryReservedPoints.Remove(pointIndex);
            _entryClaimedPoints.Add(pointIndex);
            return true;
        }

        public bool AddBall(BallItem ball, float startProgress = 0f)
        {
            if (ball == null) return false;

            if (_splineAdapter == null || !_splineAdapter.IsValid)
                Initialize();

            if (_splineAdapter == null || !_splineAdapter.IsValid) return false;

            DisablePhysicsOnConveyor(ball);

            float baseProgress = Mathf.Repeat(startProgress, 1f);
            float entryDistance = _splineAdapter.ProgressToDistance(baseProgress);

            if (!_activeBalls.Contains(ball))
            {
                _activeBalls.Add(ball);
            }

            Vector3 startPos = ball.transform.position;

            ball.isOnConveyor = true;
            ball.isCollected = false;
            ball.isEnteringConveyor = true;
            ball.conveyorDistance = entryDistance;
            ball.conveyorProgress = baseProgress;
            ball.chaseUnlockTime = Time.time + 0.3f;

            if (_ballMoveParent != null)
            {
                ball.transform.SetParent(_ballMoveParent, true);
            }

            Vector3 targetPos = _splineAdapter.EvaluatePositionByDistance(entryDistance, true);
            Quaternion targetRot = _splineAdapter.EvaluateRotationByDistance(entryDistance, true);

            // Animate visualOffset over 0.10s to bypass late landing delay
            ball.transform.DOKill();
            Vector3 startOffset = startPos - targetPos;
            ball.visualOffset = startOffset;

            DOTween.To(() => ball.visualOffset, x => ball.visualOffset = x, Vector3.zero, _entrySnapDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (ball != null)
                    {
                        ball.visualOffset = Vector3.zero;
                        ball.isEnteringConveyor = false;
                    }
                });

            // Fast visual jump scale animation
            ball.transform.localScale = Vector3.one;
            Sequence scaleSeq = DOTween.Sequence();
            scaleSeq.Append(ball.transform.DOScale(new Vector3(1.1f, 0.9f, 1.1f), 0.05f).SetEase(Ease.OutQuad));
            scaleSeq.Append(ball.transform.DOScale(Vector3.one, 0.05f).SetEase(Ease.OutBack));

            ball.transform.SetPositionAndRotation(startPos, targetRot);

            return true;
        }

        public bool AddBallWithTween(BallItem ball, float startProgress, float duration, System.Action onComplete = null)
        {
            bool result = AddBall(ball, startProgress);
            onComplete?.Invoke();
            return result;
        }

        public bool AddBallWithArc(BallItem ball, float startProgress, float duration, float arcHeight = 0.35f, System.Action onComplete = null)
        {
            bool result = AddBall(ball, startProgress);
            onComplete?.Invoke();
            return result;
        }

        public void RemoveBall(BallItem ball)
        {
            if (ball == null) return;

            BallItem behindBall = FindBallDirectlyBehind(ball);
            if (behindBall != null)
            {
                behindBall.chaseUnlockTime = Time.time + 0.2f;
            }

            // Stop any conveyor visual tween before another system takes ownership
            // of the ball (for example, the tray receive animation).
            ball.transform.DOKill();
            ball.isOnConveyor = false;
            ball.isEnteringConveyor = false;
            ball.conveyorSlotIndex = -1;
            _activeBalls.Remove(ball);
            _ballPointAnchors.Remove(ball);
            ReleasePointReservation(ball);
            if (ball == _chainLeader)
                _chainLeader = _activeBalls.Count > 0 ? _activeBalls[0] : null;

            // Invalidate anchors of remaining trailing balls so they immediately close gaps
            for (int i = 0; i < _activeBalls.Count; i++)
            {
                if (_activeBalls[i] != null && _activeBalls[i] != _chainLeader)
                {
                    _ballPointAnchors.Remove(_activeBalls[i]);
                }
            }
            RefreshBallSlotIndices();
        }

        private void Update()
        {
            if (_splineAdapter == null || !_splineAdapter.IsValid) return;

            _entryClaimedPoints.Clear();

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (_parkingLotManager == null)
            {
                _parkingLotManager = FindAnyObjectByType<ParkingLotManager>(FindObjectsInactive.Include);
            }

            float speedMultiplier = 1.0f;
            if (_parkingLotManager != null && _parkingLotManager.CarCount == 0)
            {
                // Only double speed if all active balls have entered the conveyor
                bool anyBallNotOnConveyor = false;
                var allBalls = FindObjectsByType<BallItem>(FindObjectsSortMode.None);
                for (int i = 0; i < allBalls.Length; i++)
                {
                    if (allBalls[i] != null && !allBalls[i].isOnConveyor && !allBalls[i].isCollected)
                    {
                        anyBallNotOnConveyor = true;
                        break;
                    }
                }

                if (!anyBallNotOnConveyor)
                {
                    speedMultiplier = 2.0f;
                }
            }

            float speedConveyor = (_runtimeConfig != null ? _runtimeConfig.SpeedConveyor : 1.5f) * speedMultiplier;
            float speedBallFollow = (_runtimeConfig != null ? _runtimeConfig.SpeedBallFollow : 5.0f) * 0.70f;
            float distancePerSlot = GetDistancePerSlot();
            float entryDistance = _splineAdapter.ProgressToDistance(EntryProgress);

            // 1. Advance Belt Offset
            _beltDistanceOffset = _splineAdapter.WrapDistance(_beltDistanceOffset + speedConveyor * dt);

            // 2. Update Point Views along the Spline
            if (_pointViews.Count > 0)
            {
                // Point va ball dung cung mot nhip slot cua conveyor.
                float pointDistancePerSlot = GetDistancePerSlot();
                for (int i = 0; i < _pointViews.Count; i++)
                {
                    if (_pointViews[i] == null) continue;
                    float slotDist = _splineAdapter.WrapDistance(_beltDistanceOffset - i * pointDistancePerSlot);
                    Vector3 pos = _splineAdapter.EvaluatePositionByDistance(slotDist, true);
                    Quaternion rot = _splineAdapter.EvaluateRotationByDistance(slotDist, true);
                    _pointViews[i].SetPositionAndRotation(pos, rot);
                    _pointViews[i].SetVisible(_showPointViews);
                }
            }

            // 3. Update Balls along the Spline
            _chainLeader = FindChainLeader();

            if (_chainLeader != null && _activeBalls.Count > 1)
            {
                _activeBalls.Sort((a, b) =>
                {
                    if (a == b) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    
                    bool aOn = a.isOnConveyor && !a.isCollected;
                    bool bOn = b.isOnConveyor && !b.isCollected;
                    
                    if (aOn != bOn)
                    {
                        return aOn ? -1 : 1;
                    }
                    
                    if (!aOn) return 0;
                    
                    if (a == _chainLeader) return -1;
                    if (b == _chainLeader) return 1;

                    float distA = _splineAdapter.ForwardDistance(a.conveyorDistance, _chainLeader.conveyorDistance, true);
                    float distB = _splineAdapter.ForwardDistance(b.conveyorDistance, _chainLeader.conveyorDistance, true);
                    return distA.CompareTo(distB);
                });
            }

            for (int i = 0; i < _activeBalls.Count; i++)
            {
                var ball = _activeBalls[i];
                if (ball == null || ball.isCollected)
                {
                    if (ball == _chainLeader) _chainLeader = null;
                    ReleasePointReservation(ball);
                    _activeBalls.RemoveAt(i);
                    i--;
                    continue;
                }

                if (ball.isOnConveyor)
                {
                    if (ball.isEnteringConveyor)
                    {
                        if (_ballPointAnchors.TryGetValue(ball, out var anchor) && anchor != null)
                        {
                            float slotDist = _splineAdapter.WrapDistance(_beltDistanceOffset - anchor.Index * GetDistancePerSlot());
                            ball.conveyorDistance = slotDist;
                            ball.conveyorProgress = _splineAdapter.DistanceToProgress(slotDist, true);
                        }

                        Vector3 entryBallPos = _splineAdapter.EvaluatePositionByDistance(ball.conveyorDistance, true);
                        Quaternion entryBallRot = _splineAdapter.EvaluateRotationByDistance(ball.conveyorDistance, true);
                        ball.transform.SetPositionAndRotation(entryBallPos + ball.visualOffset, entryBallRot);
                        continue;
                    }

                    // The leader is the stable head of the loop. It follows
                    // its conveyor point but never chases another ball, so a
                    // closed loop always has a real target to merge into.
                    if (ball == _chainLeader)
                    {
                        UpdateLeaderBall(ball, speedConveyor);
                        continue;
                    }

                    float extraSpeed = 0f;
                    BallItem frontBall = null;
                    float minDistAhead = float.PositiveInfinity;

                    // Find the REAL physical ball directly in front of this ball along the 3D spline
                    for (int j = 0; j < _activeBalls.Count; j++)
                    {
                        var candidate = _activeBalls[j];
                        if (candidate == null || candidate == ball || !candidate.isOnConveyor || candidate.isEnteringConveyor || candidate.isCollected)
                            continue;

                        float distAhead = _splineAdapter.ForwardDistance(ball.conveyorDistance, candidate.conveyorDistance, true);
                        if (distAhead > 0.001f && distAhead < minDistAhead)
                        {
                            minDistAhead = distAhead;
                            frontBall = candidate;
                        }
                    }

                    // When the head of a compact chain sees a gap, every ball
                    // behind it receives the same chase speed. This preserves
                    // the chain spacing instead of making balls chase one by
                    // one.
                    if (frontBall != null)
                    {
                        extraSpeed = GetChainChaseExtraSpeed(ball, distancePerSlot, speedConveyor);
                    }

                    // Advance continuously with base conveyor speed + gentle extra chase speed.
                    // Never move an existing ball backward to make room for a
                    // newly entered ball.
                    float moveSpeed = speedConveyor + extraSpeed;
                    float nextDistance = _splineAdapter.WrapDistance(ball.conveyorDistance + moveSpeed * dt);
                    bool canAdvance = true;
                    if (frontBall != null)
                    {
                        float nextGap = _splineAdapter.ForwardDistance(nextDistance, frontBall.conveyorDistance, true);
                        canAdvance = nextGap >= distancePerSlot - 0.002f;
                    }

                    if (canAdvance)
                        ball.conveyorDistance = nextDistance;

                    ball.conveyorProgress = _splineAdapter.DistanceToProgress(ball.conveyorDistance, true);

                    // Set visual transform (with visualOffset tracking)
                    Vector3 ballPos = _splineAdapter.EvaluatePositionByDistance(ball.conveyorDistance, true);
                    Quaternion ballRot = _splineAdapter.EvaluateRotationByDistance(ball.conveyorDistance, true);
                    ball.transform.SetPositionAndRotation(ballPos + ball.visualOffset, ballRot);
                }
            }

            FeedBallsToTrays();
        }

        private bool IsPointAvailableForEntry(ConveyorPointView point, BallItem ball)
        {
            if (point == null || _splineAdapter == null || !_splineAdapter.IsValid) return false;
            if (IsPointReservedByAnotherBall(point.Index, ball)) return false;
            float distance = _splineAdapter.FindClosestDistance(point.transform.position);
            return IsSlotFreeAtDistance(distance, 0.6f);
        }

        public bool TryReceiveBallFromEntryCollider(Collider ballCollider)
        {
            if (ballCollider == null) return false;
            if (_pointViews.Count == 0) BuildPointViews();
            BallItem ball = ballCollider.GetComponentInParent<BallItem>();
            if (ball == null || ball.isOnConveyor || ball.isEnteringConveyor || ball.isCollected) return false;

            float receiveRadius = _entryReceiveRadius;
            float receiveRadiusSqr = receiveRadius * receiveRadius;

            List<ConveyorPointView> candidates = new List<ConveyorPointView>();

            for (int i = 0; i < _pointViews.Count; i++)
            {
                ConveyorPointView point = _pointViews[i];
                if (point == null) continue;

                float sqrDistance = (point.transform.position - ball.transform.position).sqrMagnitude;
                if (sqrDistance <= receiveRadiusSqr)
                {
                    if (IsPointAvailableForEntry(point, ball))
                    {
                        candidates.Add(point);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                // Sort candidates by index ascending to prioritize front-most slots sequentially
                candidates.Sort((a, b) => a.Index.CompareTo(b.Index));
                return TryReceiveBallAtPoint(ball, candidates[0]);
            }

            return false;
        }

        private void RefreshBallSlotIndices()
        {
            for (int i = 0; i < _activeBalls.Count; i++)
            {
                if (_activeBalls[i] != null)
                {
                    _activeBalls[i].conveyorSlotIndex = i;
                }
            }
        }

        private bool IsPointReservedByAnotherBall(int pointIndex, BallItem ball)
        {
            if (pointIndex < 0) return false;
            if (!_reservedPointOwners.TryGetValue(pointIndex, out BallItem owner)) return false;
            if (owner != null && owner.isOnConveyor && !owner.isCollected)
                return owner != ball;

            _reservedPointOwners.Remove(pointIndex);
            return false;
        }

        private bool TryReservePoint(BallItem ball, ConveyorPointView point)
        {
            if (ball == null || point == null || point.Index < 0) return false;
            int targetIndex = point.Index;
            if (IsPointReservedByAnotherBall(targetIndex, ball)) return false;

            ReleasePointReservation(ball);
            _reservedPointByBall[ball] = targetIndex;
            _reservedPointOwners[targetIndex] = ball;
            return true;
        }

        private void ReleasePointReservation(BallItem ball)
        {
            if (ball == null || !_reservedPointByBall.TryGetValue(ball, out int pointIndex)) return;
            if (_reservedPointOwners.TryGetValue(pointIndex, out BallItem owner) && owner == ball)
                _reservedPointOwners.Remove(pointIndex);
            _reservedPointByBall.Remove(ball);
        }

        private float GetChainChaseExtraSpeed(BallItem ball, float distancePerSlot, float speedConveyor)
        {
            BallItem chainHead = ball;
            var visited = new HashSet<BallItem>();

            while (chainHead != null && visited.Add(chainHead))
            {
                BallItem front = FindNearestFrontBall(chainHead);
                if (front == null) return 0f;

                float gap = _splineAdapter.ForwardDistance(
                    chainHead.conveyorDistance, front.conveyorDistance, true);
                if (gap > distancePerSlot + 0.001f)
                {
                    if (Time.time < chainHead.chaseUnlockTime)
                        return 0f;

                    float targetDistance = _splineAdapter.WrapDistance(
                        front.conveyorDistance - distancePerSlot);
                    ConveyorPointView targetPoint = FindNearestPointView(targetDistance);
                    if (targetPoint == null || !TryReservePoint(chainHead, targetPoint))
                        return 0f;

                    float maxExtraSpeed = Mathf.Min(7.2f, speedConveyor * 4f);
                    return Mathf.Min((gap - distancePerSlot) * 12f, maxExtraSpeed);
                }

                // The next ball is still part of this compact chain, so look
                // ahead until reaching the ball that owns the actual gap.
                chainHead = front;
            }

            return 0f;
        }

        private void UpdateLeaderBall(BallItem ball, float speedConveyor)
        {
            if (!_ballPointAnchors.TryGetValue(ball, out var anchor) || anchor == null)
            {
                anchor = FindNearestPointView(ball.conveyorDistance);
                if (anchor != null)
                {
                    _ballPointAnchors[ball] = anchor;
                }
            }

            if (anchor != null)
            {
                ball.conveyorDistance = _splineAdapter.FindClosestDistance(anchor.transform.position);
                ball.conveyorProgress = _splineAdapter.DistanceToProgress(ball.conveyorDistance, true);
                ball.transform.SetPositionAndRotation(anchor.transform.position + ball.visualOffset, anchor.transform.rotation);
            }
            else
            {
                float current = _splineAdapter.WrapDistance(ball.conveyorDistance + speedConveyor * Time.deltaTime);
                ball.conveyorDistance = current;
                ball.conveyorProgress = _splineAdapter.DistanceToProgress(current, true);
                Vector3 pos = _splineAdapter.EvaluatePositionByDistance(current, true);
                Quaternion rot = _splineAdapter.EvaluateRotationByDistance(current, true);
                ball.transform.SetPositionAndRotation(pos + ball.visualOffset, rot);
            }
        }

        private ConveyorPointView FindNearestPointView(float distance)
        {
            if (_pointViews.Count == 0) return null;
            Vector3 target = _splineAdapter.EvaluatePositionByDistance(distance, true);
            ConveyorPointView nearest = null;
            float nearestSqrDistance = float.PositiveInfinity;
            for (int i = 0; i < _pointViews.Count; i++)
            {
                var point = _pointViews[i];
                if (point == null) continue;
                float sqrDistance = (point.transform.position - target).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = point;
                }
            }
            return nearest;
        }

        private float GetDistancePerSlot()
        {
            int capacity = _runtimeConfig != null ? _runtimeConfig.SlotCapacity : 24;
            capacity = Mathf.Max(1, capacity);
            float configuredSpacing = _runtimeConfig != null ? _runtimeConfig.MinBallSpacing : 0f;
            // Luon giu khoang cach lon hon ban kinh/duong kinh ball de chuoi
            // khong bi dinh khi co nhieu ball cung vao conveyor.
            return Mathf.Max(_splineAdapter.Length / capacity, configuredSpacing, _minimumBallSpacing);
        }

        private BallItem FindNearestFrontBall(BallItem source, Dictionary<BallItem, float> distanceSnapshot = null)
        {
            BallItem nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < _activeBalls.Count; i++)
            {
                BallItem candidate = _activeBalls[i];
                if (candidate == null || candidate == source || candidate.isCollected || !candidate.isOnConveyor || candidate.isEnteringConveyor)
                    continue;

                float sourceDistance = distanceSnapshot != null && distanceSnapshot.TryGetValue(source, out var sourceSnapshot)
                    ? sourceSnapshot : source.conveyorDistance;
                float candidateDistance = distanceSnapshot != null && distanceSnapshot.TryGetValue(candidate, out var candidateSnapshot)
                    ? candidateSnapshot : candidate.conveyorDistance;
                float distance = _splineAdapter.ForwardDistance(sourceDistance, candidateDistance, true);
                if (distance > 0.001f && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }
            return nearest;
        }

        private BallItem FindBallDirectlyBehind(BallItem frontBall)
        {
            if (frontBall == null || _splineAdapter == null) return null;
            BallItem behindBall = null;
            float minDistance = float.PositiveInfinity;

            for (int i = 0; i < _activeBalls.Count; i++)
            {
                var candidate = _activeBalls[i];
                if (candidate == null || candidate == frontBall || !candidate.isOnConveyor || candidate.isEnteringConveyor || candidate.isCollected)
                    continue;

                float distance = _splineAdapter.ForwardDistance(candidate.conveyorDistance, frontBall.conveyorDistance, true);
                if (distance > 0.001f && distance < minDistance)
                {
                    minDistance = distance;
                    behindBall = candidate;
                }
            }

            return behindBall;
        }

        private void FeedBallsToTrays()
        {
            if (_gateManager == null || _runtimeConfig == null)
                return;

            var receiveZones = _runtimeConfig.ReceiveZones;
            if (receiveZones == null || receiveZones.Count == 0)
                return;

            for (int z = 0; z < receiveZones.Count; z++)
            {
                var zone = receiveZones[z];
                var gate = _gateManager.GetGate(zone.gateIndex);
                var tray = gate != null ? gate.CurrentFrontTray : null;
                if (tray == null)
                    continue;

                List<(BallItem ball, float linearDelta)> candidates = new();
                float zoneDistance = _splineAdapter.ProgressToDistance(Mathf.Repeat(zone.progress, 1f));

                for (int i = 0; i < _activeBalls.Count; i++)
                {
                    var ball = _activeBalls[i];
                    if (ball == null || !ball.isOnConveyor || ball.isEnteringConveyor || ball.isCollected)
                        continue;

                    float ballDistance = ball.conveyorDistance;
                    float linearDelta = _splineAdapter.ForwardDistance(ballDistance, zoneDistance, true);

                    if (linearDelta <= zone.radius)
                    {
                        if (tray.CanAcceptBall(ball))
                        {
                            candidates.Add((ball, linearDelta));
                        }
                    }
                }

                if (candidates.Count == 0)
                    continue;

                // Sort by linearDelta ascending (front-most ball first)
                candidates.Sort((a, b) => a.linearDelta.CompareTo(b.linearDelta));

                for (int i = 0; i < candidates.Count; i++)
                {
                    var ball = candidates[i].ball;
                    if (ball == null || !ball.isOnConveyor || ball.isCollected)
                        continue;

                    if (!tray.CanAcceptBall(ball))
                        continue;

                    if (tray.TryAcceptBall(ball, 0.2f))
                    {
                        RemoveBall(ball);
                    }
                }
            }
        }

        private BallItem FindChainLeader()
        {
            if (_activeBalls == null || _activeBalls.Count == 0 || _splineAdapter == null || !_splineAdapter.IsValid)
                return null;

            BallItem bestLeader = null;
            float maxGap = -1f;

            for (int i = 0; i < _activeBalls.Count; i++)
            {
                var ball = _activeBalls[i];
                if (ball == null || !ball.isOnConveyor || ball.isCollected)
                    continue;

                float minDistAhead = float.PositiveInfinity;
                for (int j = 0; j < _activeBalls.Count; j++)
                {
                    var candidate = _activeBalls[j];
                    if (candidate == null || candidate == ball || !candidate.isOnConveyor || candidate.isCollected)
                        continue;

                    float distAhead = _splineAdapter.ForwardDistance(ball.conveyorDistance, candidate.conveyorDistance, true);
                    if (distAhead > 0.001f && distAhead < minDistAhead)
                    {
                        minDistAhead = distAhead;
                    }
                }

                if (float.IsPositiveInfinity(minDistAhead))
                {
                    minDistAhead = _splineAdapter.Length;
                }

                if (minDistAhead > maxGap)
                {
                    maxGap = minDistAhead;
                    bestLeader = ball;
                }
            }

            return bestLeader;
        }

        private bool IsBallInsideReceiveZone(BallItem ball, TrayReceiveZoneData zone)
        {
            if (ball == null || zone == null)
                return false;

            float zoneDistance = _splineAdapter.ProgressToDistance(Mathf.Repeat(zone.progress, 1f));
            float delta = Mathf.Abs(Mathf.DeltaAngle(
                (ball.conveyorDistance / _splineAdapter.Length) * 360f,
                (zoneDistance / _splineAdapter.Length) * 360f));

            float linearDelta = (delta / 360f) * _splineAdapter.Length;
            return linearDelta <= Mathf.Max(0.01f, zone.radius);
        }

        private void OnDrawGizmos()
        {
            if (_splineContainer == null)
            {
                _splineContainer = GetComponent<UnityEngine.Splines.SplineContainer>() ?? GetComponentInChildren<UnityEngine.Splines.SplineContainer>();
            }

            if (_splineContainer == null) return;

            Vector3 entryPosition = transform.position;
            if (_splineAdapter != null && _splineAdapter.IsValid)
            {
                float entryDistance = _splineAdapter.ProgressToDistance(EntryProgress);
                entryPosition = _splineAdapter.EvaluatePositionByDistance(entryDistance, true);
            }
            else
            {
                var spline = _splineContainer.Spline;
                if (spline != null && spline.Count > 0)
                {
                    float progress = _runtimeConfig != null ? _runtimeConfig.EntryProgress : 0f;
                    entryPosition = transform.TransformPoint(spline.EvaluatePosition(progress));
                }
            }

            float receiveRadius = _entryReceiveRadius;

            // Draw entry receive radius wire sphere
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(entryPosition, receiveRadius);
            
            // Draw a solid center sphere
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawSphere(entryPosition, 0.15f);
        }


    }
}
