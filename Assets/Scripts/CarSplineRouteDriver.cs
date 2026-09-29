using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using BusBallJam.Spline;
using BallDropParty.Gameplay;

[DisallowMultipleComponent]
public class CarSplineRouteDriver : MonoBehaviour
{
    [Header("Route")]
    [Tooltip("Spline 1: đường thẳng đi sang trái. Rotation X = 0°.")]
    [SerializeField] private SplineContainer spline1;
    [Tooltip("Spline 2: đường thẳng đi từ dưới lên. Rotation X = 90°.")]
    [SerializeField] private SplineContainer spline2;
    [Tooltip("Spline 3: đường thẳng đi từ trái sang phải. Rotation X = 180°.")]
    [SerializeField] private SplineContainer spline3;
    [Tooltip("Spline đơn dự phòng (routeSpline cũ).")]
    [SerializeField] private SplineContainer routeSpline;
    [SerializeField] private float spline1RotationX = 0f;
    [SerializeField] private float spline2RotationX = 90f;
    [SerializeField] private float spline3RotationX = 180f;

    [Tooltip("Field cũ, chỉ dùng nếu chưa gán 3 spline mới.")]
    [SerializeField] private bool reverseDirection = false;
    [SerializeField] private int bakeResolution = 256;

    [Header("Boundary")]
    [SerializeField] private LayerMask carLayer;
    [SerializeField] private float targetFollowDistance = 2f;
    [SerializeField] private LayerMask boundaryLayer;
    [SerializeField] private float boundaryMoveSpeed = 6f;
    [SerializeField] private float boundaryCheckRadius = 0.1f;

    [Header("Start Angle")]
    [Tooltip("Optional override for the car's starting heading before entering the route.")]
    [SerializeField] private float startAngle = 0f;
    [SerializeField] private bool useStartAngleOverride = false;

    [Header("Speeds")]
    [SerializeField] private float approachSpeed = 6f;
    [SerializeField] private float splineSpeed = 6f;

    [Header("Priority Yield Settings")]
    [Tooltip("Bán kính cast vật lý để check xe nhường đường (1.0m)")]
    [SerializeField] private float priorityYieldRadius = 1.0f;

    [Header("Spline Rotation")]
    [SerializeField] private float rotationTweenDuration = 0.25f;

    private SplineAdapter _routeAdapter;
    private readonly Dictionary<Car, Coroutine> _activeRouteCoroutines = new();
    private bool _targetOccupied;

    public SplineContainer RouteSpline => spline1 != null ? spline1 : routeSpline;
    public bool HasRoute => (HasSpline(spline1) && HasSpline(spline2) && HasSpline(spline3)) || HasSpline(RouteSpline);
    public bool TargetOccupied => _targetOccupied;
    public float RouteLength => _routeAdapter != null ? _routeAdapter.Length : 0f;
    public float StartAngle => startAngle;

    private void Awake()
    {
        InitializeAdapter();
    }

    private void OnValidate()
    {
        InitializeAdapter();
    }

    public void InitializeAdapter()
    {
        var selectedSpline = RouteSpline;
        if (selectedSpline == null)
        {
            _routeAdapter = null;
            return;
        }

        if (selectedSpline.Spline == null || selectedSpline.Spline.Count == 0)
        {
            _routeAdapter = null;
            return;
        }

        _routeAdapter = new SplineAdapter(selectedSpline, reverseDirection, bakeResolution);
    }

    private bool HasSpline(SplineContainer spline)
    {
        return spline != null && spline.Spline != null && spline.Spline.Count > 0;
    }

    private SplineAdapter GetAdapter(SplineContainer spline)
    {
        return new SplineAdapter(spline, reverseDirection, bakeResolution);
    }

    public bool StartRoute(Car car, Action onComplete = null)
    {
        if (car == null)
            return false;

        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        InitializeAdapter();
        if (!HasRoute)
            return false;

        StopRoute(car);

        _activeRouteCoroutines[car] = car.StartCoroutine(RouteRoutine(car, onComplete));
        return true;
    }

    public void StopRoute(Car car = null)
    {
        if (car != null)
        {
            if (_activeRouteCoroutines.TryGetValue(car, out var coroutine))
            {
                car.StopCoroutine(coroutine);
                _activeRouteCoroutines.Remove(car);
            }
            return;
        }

        foreach (var pair in _activeRouteCoroutines)
        {
            if (pair.Key != null && pair.Value != null)
                pair.Key.StopCoroutine(pair.Value);
        }

        _activeRouteCoroutines.Clear();
    }

    private IEnumerator RouteRoutine(Car car, Action onComplete)
    {
        car.IsInParkingLot = false;
        Vector3 currentDirection = GetInitialDirection(car);
        RaycastHit boundaryHitInfo = default;

        if (boundaryLayer != 0)
        {
            yield return MoveAlongDirectionUntilBoundary(car, currentDirection, hitInfo => boundaryHitInfo = hitInfo);

            if (boundaryHitInfo.collider != null)
            {
                var side = GetBoundarySide(boundaryHitInfo.collider);
                car.SmoothSetPlanarRotationX(GetBoundaryRotationX(side), rotationTweenDuration);
                switch (side)
                {
                    case BoundarySide.Right:
                    case BoundarySide.Left:
                    case BoundarySide.Bottom:
                        currentDirection = Vector3.up;
                        yield return MoveAlongDirectionUntilBoundary(car, currentDirection, hitInfo => boundaryHitInfo = hitInfo, BoundarySide.Top);
                        break;
                    case BoundarySide.Top:
                        break;
                    default:
                        break;
                }

            }
        }

        // Đã ra khỏi bãi đỗ xe và lên đường chạy
        car.IsInParkingLot = false;

        List<(SplineContainer spline, float rotX)> segments = new();
        if (HasSpline(spline1) && HasSpline(spline2) && HasSpline(spline3))
        {
            segments.Add((spline1, spline1RotationX));
            segments.Add((spline2, spline2RotationX));
            segments.Add((spline3, spline3RotationX));
        }
        else if (HasSpline(RouteSpline))
        {
            segments.Add((RouteSpline, spline1RotationX));
        }

        for (int segment = 0; segment < segments.Count; segment++)
        {
            // Mỗi xe phải có adapter riêng. Không dùng _routeAdapter dùng chung ở đây,
            // nếu không xe thứ hai bắt đầu sẽ làm reset tiến độ của xe thứ nhất.
            var (splineContainer, rotX) = segments[segment];
            SplineAdapter routeAdapter = GetAdapter(splineContainer);
            float startDistance = segment == 0 ? routeAdapter.FindClosestDistance(car.transform.position) : 0f;
            Vector3 routeEntryPoint = routeAdapter.EvaluatePositionByDistance(startDistance, false);
            yield return MoveToPoint(car, routeEntryPoint, approachSpeed, true);
            car.SmoothSetPlanarRotationX(rotX, rotationTweenDuration);

            // Bắt đầu vào segment Spline
            car.SplineIndex = segment;
            car.SplineDistance = startDistance;

            float currentDistance = startDistance;
            float yieldWaitTimer = 0f;
            bool wasBlockedLastFrame = false;

            while (currentDistance < routeAdapter.Length)
            {
                if (yieldWaitTimer > 0f)
                {
                    yieldWaitTimer -= Time.deltaTime;
                    Vector3 pos = routeAdapter.EvaluatePositionByDistance(currentDistance, false);
                    car.SetPlanarPosition(pos);
                    yield return null;
                    continue;
                }

                float nextDistance = Mathf.Clamp(currentDistance + splineSpeed * car.SpeedMultiplier * Time.deltaTime, 0f, routeAdapter.Length);
                Vector3 nextPosition = routeAdapter.EvaluatePositionByDistance(nextDistance, false);
                Vector3 stepDirection = nextPosition - car.transform.position;

                RaycastHit blockedHit;
                bool isBlocked = IsTargetBlocked(car, nextPosition, out _)
                    || (stepDirection.sqrMagnitude > 0.0001f && car.IsPathBlocked(stepDirection, stepDirection.magnitude + 0.1f, out blockedHit));

                if (isBlocked)
                {
                    wasBlockedLastFrame = true;
                    float backupDist = 0.2f;
                    Vector3 reverseDirection = -stepDirection.normalized;
                    if (reverseDirection.sqrMagnitude < 0.001f)
                    {
                        reverseDirection = -car.transform.up;
                    }

                    int mask = carLayer.value != 0 ? carLayer.value : Physics.DefaultRaycastLayers;
                    if (Physics.Raycast(car.transform.position, reverseDirection, out RaycastHit backHit, backupDist + 0.5f, mask, QueryTriggerInteraction.Ignore))
                    {
                        backupDist = Mathf.Max(0f, backHit.distance - 0.5f);
                    }

                    if (backupDist > 0.05f)
                    {
                        Debug.Log($"Car {car.name} backing up on spline by {backupDist}m to resolve conflict");
                        float targetBackupDistance = Mathf.Max(0f, currentDistance - backupDist);
                        float backupStartDistance = currentDistance;
                        
                        float backupElapsed = 0f;
                        float backupDuration = 0.75f;
                        while (backupElapsed < backupDuration)
                        {
                            float t = backupElapsed / backupDuration;
                            currentDistance = Mathf.Lerp(backupStartDistance, targetBackupDistance, t);
                            car.SplineDistance = currentDistance;
                            Vector3 backupPos = routeAdapter.EvaluatePositionByDistance(currentDistance, false);
                            car.SetPlanarPosition(backupPos);
                            backupElapsed += Time.deltaTime;
                            yield return null;
                        }
                        currentDistance = targetBackupDistance;
                        car.SplineDistance = currentDistance;
                        Vector3 finalBackupPos = routeAdapter.EvaluatePositionByDistance(currentDistance, false);
                        car.SetPlanarPosition(finalBackupPos);

                        yieldWaitTimer = 0f; // Proceed immediately after backing up
                    }
                    else
                    {
                        car.SetPlanarPosition(car.transform.position);
                        yieldWaitTimer = 0.2f;
                    }
                    yield return null;
                    continue;
                }

                if (wasBlockedLastFrame)
                {
                    wasBlockedLastFrame = false;
                    car.TriggerCornerSpeedUp();
                }

                currentDistance = nextDistance;
                car.SplineDistance = currentDistance;
                car.SetPlanarPosition(nextPosition);
                yield return null;
            }
        }

        car.SplineIndex = 999;
        _activeRouteCoroutines.Remove(car);
        onComplete?.Invoke();
    }

    public bool ShouldYieldForHigherYCar(Car car, float radius = 1.5f)
    {
        return ShouldYieldTrafficPriority(car, car.MoveDirection, radius);
    }

    public bool ShouldYieldTrafficPriority(Car car, Vector3 moveDirection, float radius = 1.5f)
    {
        if (car == null || car.IsInParkingLot) return false;

        int mask = carLayer.value != 0 ? carLayer.value : Physics.DefaultRaycastLayers;
        Collider[] colliders = Physics.OverlapSphere(car.transform.position, radius, mask, QueryTriggerInteraction.Ignore);

        float currentY = car.transform.position.y;
        float currentX = car.transform.position.x;

        for (int i = 0; i < colliders.Length; i++)
        {
            var otherCar = colliders[i].GetComponentInParent<Car>();
            if (otherCar == null || otherCar == car || !otherCar.gameObject.activeInHierarchy || otherCar.IsInParkingLot)
                continue;

            // Nhường đường cho xe đang di chuyển hoặc đã lên route/target
            if (!otherCar.canClick || otherCar.IsMoving)
            {
                float otherY = otherCar.transform.position.y;
                float otherX = otherCar.transform.position.x;

                float diffY = otherY - currentY;
                float diffX = otherX - currentX;

                // Nếu cả X và Y đều nằm trong vùng gần nhau (< 1.0m): cần xét thứ tự ưu tiên
                if (Mathf.Abs(diffY) < 1.0f && Mathf.Abs(diffX) < 1.0f)
                {
                    // 1. Nếu xe kia ĐÃ Ở TRÊN SPLINE (SplineIndex >= 0) còn xe này CHƯA Ở TRÊN SPLINE (SplineIndex < 0):
                    // Xe chưa ở Spline bắt buộc dừng lại nhường cho xe trên Spline chạy trước
                    if (otherCar.SplineIndex >= 0 && car.SplineIndex < 0)
                    {
                        return true;
                    }

                    // Nếu xe này đã ở Spline còn xe kia chưa lên: xe này được quyền đi tiếp
                    if (car.SplineIndex >= 0 && otherCar.SplineIndex < 0)
                    {
                        continue;
                    }

                    // 2. Nếu CẢ 2 XE ĐỀU Ở TRÊN SPLINE:
                    if (car.SplineIndex >= 0 && otherCar.SplineIndex >= 0)
                    {
                        // Xe nào ở Spline có số thứ tự lớn hơn (Spline 3 > Spline 2 > Spline 1) được ưu tiên chạy trước
                        if (otherCar.SplineIndex > car.SplineIndex)
                        {
                            return true;
                        }
                        if (otherCar.SplineIndex < car.SplineIndex)
                        {
                            continue;
                        }

                        // Cùng 1 Spline: xe nào ở khoảng cách xa hơn trên spline (phía trước) được ưu tiên
                        if (otherCar.SplineDistance > car.SplineDistance + 0.05f)
                        {
                            return true;
                        }
                        if (car.SplineDistance > otherCar.SplineDistance + 0.05f)
                        {
                            continue;
                        }
                    }

                    // 3. Nếu cả 2 xe đều chưa lên Spline (hoặc cùng vị trí): xét theo tọa độ
                    if (diffY > 0.05f)
                    {
                        return true;
                    }

                    if (Mathf.Abs(diffY) <= 0.05f)
                    {
                        // Đi từ Trái sang Phải (X tăng dần): xe có X nhỏ hơn được ưu tiên đi tiếp
                        if (moveDirection.x > 0.01f)
                        {
                            if (diffX < -0.05f)
                                return true;
                        }
                        // Đi từ Phải sang Trái (X giảm dần): xe có X lớn hơn được ưu tiên đi tiếp
                        else if (moveDirection.x < -0.01f)
                        {
                            if (diffX > 0.05f)
                                return true;
                        }
                        else if (otherCar.GetInstanceID() > car.GetInstanceID())
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private bool IsTargetBlocked(Car car, Vector3 targetPosition, out Vector3 stopPosition)
    {
        stopPosition = car.transform.position;
        Vector3 moveDirection = targetPosition - car.transform.position;
        if (ShouldYieldTrafficPriority(car, moveDirection, priorityYieldRadius))
        {
            return true;
        }

        return false;
    }

    public void ReleaseTarget(Car car)
    {
        _targetOccupied = false;
    }

    private float GetBoundaryRotationX(BoundarySide side)
    {
        return side switch
        {
            BoundarySide.Left => 90f,
            BoundarySide.Top => 90f,
            BoundarySide.Right => 90f,
            BoundarySide.Bottom => 90f,
            _ => transform.eulerAngles.x
        };
    }

    private IEnumerator MoveAlongDirectionUntilBoundary(Car car, Vector3 direction, Action<RaycastHit> onBoundaryHit, BoundarySide requiredSide = BoundarySide.Unknown)
    {
        RaycastHit hitInfo = default;
        float yieldWaitTimer = 0f;
        bool wasBlocked = false;

        while (true)
        {
            if (yieldWaitTimer > 0f)
            {
                yieldWaitTimer -= Time.deltaTime;
                car.SetPlanarPosition(car.transform.position);
                yield return null;
                continue;
            }

            float stepDistance = boundaryMoveSpeed * car.SpeedMultiplier * Time.deltaTime;
            float rayDistance = stepDistance + boundaryCheckRadius;

            // Nếu trên đường di chuyển có xe phía trước: dừng lại đợi 0.2s, KHÔNG giật lùi về bãi đỗ
            if (car.IsPathBlocked(direction, stepDistance + 0.1f, out var blockedByCar))
            {
                car.SetPlanarPosition(car.transform.position);
                yieldWaitTimer = 0.2f;
                wasBlocked = true;
                yield return null;
                continue;
            }

            if (wasBlocked)
            {
                wasBlocked = false;
                car.TriggerCornerSpeedUp();
            }

            if (Physics.Raycast(car.transform.position, direction, out hitInfo, rayDistance, boundaryLayer))
            {
                onBoundaryHit?.Invoke(hitInfo);

                if (requiredSide == BoundarySide.Unknown || GetBoundarySide(hitInfo.collider) == requiredSide)
                {
                    car.SetPlanarPosition(hitInfo.point);
                    yield break;
                }

                car.SetPlanarPosition(hitInfo.point);
                yield break;
            }

            car.SetPlanarPosition(car.transform.position + direction * stepDistance);
            yield return null;
        }
    }

    private IEnumerator MoveToPoint(Car car, Vector3 destination, float speed, bool updateRotation = false)
    {
        destination = car.GetPlanarPosition(destination);
        float yieldWaitTimer = 0f;
        bool wasBlocked = false;

        while (Vector3.Distance(car.transform.position, destination) > 0.01f)
        {
            if (yieldWaitTimer > 0f)
            {
                yieldWaitTimer -= Time.deltaTime;
                car.SetPlanarPosition(car.transform.position);
                yield return null;
                continue;
            }

            Vector3 nextPosition = Vector3.MoveTowards(car.transform.position, destination, speed * car.SpeedMultiplier * Time.deltaTime);
            Vector3 moveDirection = nextPosition - car.transform.position;

            bool isBlocked = IsTargetBlocked(car, destination, out _)
                || (moveDirection.sqrMagnitude > 0.0001f && car.IsPathBlocked(moveDirection, moveDirection.magnitude + 0.1f, out _));

            if (isBlocked)
            {
                wasBlocked = true;
                Vector3 reverseDirection = -car.MoveDirection.normalized;
                if (reverseDirection.sqrMagnitude < 0.001f)
                {
                    reverseDirection = -car.transform.up;
                }
                float backupDist = 0.2f;

                int mask = carLayer.value != 0 ? carLayer.value : Physics.DefaultRaycastLayers;
                if (Physics.Raycast(car.transform.position, reverseDirection, out RaycastHit backHit, backupDist + 0.5f, mask, QueryTriggerInteraction.Ignore))
                {
                    backupDist = Mathf.Max(0f, backHit.distance - 0.5f);
                }

                if (backupDist > 0.05f)
                {
                    Debug.Log($"Car {car.name} backing up in MoveToPoint by {backupDist}m");
                    Vector3 backupStart = car.transform.position;
                    Vector3 backupTarget = car.GetPlanarPosition(backupStart + reverseDirection * backupDist);

                    float backupElapsed = 0f;
                    float backupDuration = 0.75f;
                    while (backupElapsed < backupDuration)
                    {
                        Vector3 currentBackupPos = Vector3.Lerp(backupStart, backupTarget, backupElapsed / backupDuration);
                        car.SetPlanarPosition(currentBackupPos);
                        backupElapsed += Time.deltaTime;
                        yield return null;
                    }
                    car.SetPlanarPosition(backupTarget);
                    yieldWaitTimer = 0f; // Proceed immediately after backing up
                }
                else
                {
                    car.SetPlanarPosition(car.transform.position);
                    yieldWaitTimer = 0.2f;
                }
                yield return null;
                continue;
            }

            if (wasBlocked)
            {
                wasBlocked = false;
                car.TriggerCornerSpeedUp();
            }

            if (moveDirection.sqrMagnitude > 0.0001f && updateRotation)
            {
                car.SetPlanarRotation(moveDirection.normalized);
            }

            car.SetPlanarPosition(nextPosition);
            yield return null;
        }

        car.SetPlanarPosition(destination);
    }

    private Vector3 GetInitialDirection(Car car)
    {
        if (useStartAngleOverride)
        {
            float startRadians = startAngle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(startRadians), Mathf.Sin(startRadians), 0f).normalized;
        }

        if (car != null && car.MoveDirection.sqrMagnitude > 0.0001f)
            return car.MoveDirection.normalized;

        Vector3 forward = car != null ? car.transform.up : Vector3.up;
        forward.z = 0f;

        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.up;
    }

    private BoundarySide GetBoundarySide(Collider collider)
    {
        if (collider == null) return BoundarySide.Unknown;

        string nameLower = collider.name.ToLowerInvariant();
        if (nameLower.Contains("top"))
            return BoundarySide.Top;
        if (nameLower.Contains("right"))
            return BoundarySide.Right;
        if (nameLower.Contains("bottom"))
            return BoundarySide.Bottom;
        if (nameLower.Contains("left"))
            return BoundarySide.Left;

        return BoundarySide.Unknown;
    }

    private enum BoundarySide
    {
        Unknown,
        Top,
        Right,
        Bottom,
        Left
    }

}
