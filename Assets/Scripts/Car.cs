using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Car : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public Renderer bodyRenderer;

    [Header("Raycast")]
    public LayerMask carLayer;
    public LayerMask obstacleLayer;
    public LayerMask boundaryLayer;
    public float rayDistance = 8f;
    public float hitPushDistance = 0.25f;
    public float driveSpeed = 6f;
    public float returnDuration = 0.2f;

    [Header("Ball Drop")]
    public Transform ballReleasePoint;

    [Header("Orientation")]
    public float fixedRotationY = 90f;
    public float rotationOffsetX = 0f;

    private CarData _carData;
    private Vector3 _vectorDir;
    private Vector3 _initPosition;
    private Quaternion _initRotation;
    private Collider _carCollider;
    private Rigidbody _rigidbody;
    private bool _isMoving;
    private bool _movementWasBlocked;
    private bool _processingPhysicalBlock;
    private Coroutine _movementCoroutine;
    private float _cornerSpeedMultiplier = 1.0f;
    private float _cornerSpeedMultiplierTimer = 0f;
    private bool _previouslyBlockedInParkingLot = false;

    [HideInInspector] public bool canClick = true;
    [HideInInspector] public bool isBlocking;
    [HideInInspector] public bool isMovingToWall;
    [HideInInspector] public GameObject closestBlockObj;

    public string CarType => _carData?.carType;
    public Vector3 MoveDirection => _vectorDir;
    public float LockedZ => 0.5f;
    public bool IsMoving => _isMoving;
    public bool IsInParkingLot { get; set; } = true;
    public int SplineIndex { get; set; } = -1;
    public float SplineDistance { get; set; } = 0f;
    public float SpeedMultiplier => _cornerSpeedMultiplier;

    public void TriggerCornerSpeedUp()
    {
        _cornerSpeedMultiplier = 1.2f;
        _cornerSpeedMultiplierTimer = 0.3f;
    }

    public void Init(CarData carData, CarSO carSO)
    {
        StopAllCoroutines();
        transform.DOKill();

        _carData = carData;
        _carCollider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        _isMoving = false;
        IsInParkingLot = true;
        SplineIndex = -1;
        SplineDistance = 0f;
        _movementWasBlocked = false;
        _processingPhysicalBlock = false;
        _movementCoroutine = null;
        canClick = true;
        isBlocking = false;
        isMovingToWall = false;
        closestBlockObj = null;

        _initPosition = new Vector3(transform.position.x, transform.position.y, 0.5f);
        transform.position = _initPosition;

        float headingAngle = GetAngleFromCarData(carData);
        _vectorDir = GetVectorFromAngle(headingAngle);
        if (_vectorDir.sqrMagnitude > 0.0001f)
        {
            SetPlanarRotationX(headingAngle);
        }
        _initRotation = transform.rotation;

        if (bodyRenderer != null)
        {
            var color = GameColorConfigProvider.Get(carData.eColor).color;
            Debug.Log($"Car.Init: carType={carData.carType}, eColor={carData.eColor}, resolvedColor={color}, renderer={bodyRenderer.name}");
            var block = new MaterialPropertyBlock();
            bodyRenderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            bodyRenderer.SetPropertyBlock(block);

            // MaterialPropertyBlock khong tao material instance moi khi xe duoc lay lai tu pool.
        }
        else
        {
            Debug.LogWarning($"Car.Init: missing bodyRenderer for carType={carData.carType}");
        }
    }

    public float GetAngleFromCarData(CarData carData)
    {
        if (carData == null) return 90f;
        return carData.CarAngle;
    }

    public Vector3 GetVectorFromAngle(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(-Mathf.Cos(rad), Mathf.Sin(rad), 0f).normalized;
    }

    public Vector3 GetPlanarPosition(Vector3 position)
    {
        position.z = 0.5f;
        return position;
    }

    public void SetPlanarPosition(Vector3 position)
    {
        transform.position = GetPlanarPosition(position);

        Collider blockingCollider = null;
        if (closestBlockObj != null)
        {
            blockingCollider = closestBlockObj.GetComponent<Collider>();
            if (blockingCollider == null)
                blockingCollider = closestBlockObj.GetComponentInChildren<Collider>();
        }

        if (!_processingPhysicalBlock && isMovingToWall && blockingCollider != null
            && IsActuallyTouching(_carCollider, blockingCollider))
        {
            _processingPhysicalBlock = true;
            ProcessPhysicalBlock(blockingCollider.transform);
            _processingPhysicalBlock = false;
        }
    }

    private bool IsActuallyTouching(Collider ownCollider, Collider otherCollider)
    {
        if (ownCollider == null || otherCollider == null || !ownCollider.enabled || !otherCollider.enabled)
            return false;

        return Physics.ComputePenetration(
            ownCollider,
            ownCollider.transform.position,
            ownCollider.transform.rotation,
            otherCollider,
            otherCollider.transform.position,
            otherCollider.transform.rotation,
            out _,
            out float penetrationDistance)
            && penetrationDistance > 0.0001f;
    }

    public void SetPlanarRotation(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        float angleX = Mathf.Atan2(direction.y, -direction.x) * Mathf.Rad2Deg;
        SetPlanarRotationX(angleX);
    }

    public void SmoothSetPlanarRotation(Vector3 direction, float duration = 0.25f)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        float angleX = Mathf.Atan2(direction.y, -direction.x) * Mathf.Rad2Deg;
        SmoothSetPlanarRotationX(angleX, duration);
    }

    public void SetPlanarRotationX(float angleX)
    {
        transform.rotation = Quaternion.Euler(angleX + rotationOffsetX, fixedRotationY, 0f);
    }

    public void SmoothSetPlanarRotationX(float angleX, float duration = 0.25f)
    {
        transform.DOKill();
        transform.DORotate(
            new Vector3(angleX + rotationOffsetX, fixedRotationY, 0f),
            Mathf.Max(0f, duration))
            .SetEase(Ease.OutSine);
        TriggerCornerSpeedUp();
    }

    private void Update()
    {
        if (_cornerSpeedMultiplierTimer > 0f)
        {
            _cornerSpeedMultiplierTimer -= Time.deltaTime;
            if (_cornerSpeedMultiplierTimer <= 0f)
            {
                _cornerSpeedMultiplier = 1.0f;
            }
        }

        if (_isMoving) return;

        bool isPressed = false;
        Vector2 screenPosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
        {
            isPressed = true;
            screenPosition = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            isPressed = true;
            screenPosition = Input.mousePosition;
        }
#endif

        if (isPressed)
        {
            // Ignore click if clicking on UI elements like buttons
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var camera = Camera.main;
            if (camera != null)
            {
                var ray = camera.ScreenPointToRay(screenPosition);
                if (Physics.Raycast(ray, out var hit, 100f))
                {
                    if (hit.collider == _carCollider || (hit.collider != null && hit.collider.transform.IsChildOf(transform)))
                    {
                        Debug.Log($"Car.UpdateRaycast: Click detected on {name}");
                        TryDrive();
                    }
                }
            }
        }
    }

    private void OnMouseDown()
    {
        Debug.Log($"Car.OnMouseDown: Fired on {name}");
        TryDrive();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Car.OnPointerClick: Fired on {name}");
        TryDrive();
    }

    public void TryDrive()
    {
        if (!canClick || _isMoving || isBlocking || isMovingToWall || closestBlockObj != null || _carCollider == null)
        {
            Debug.Log($"Car.TryDrive: Ignored (isMoving={_isMoving}, hasCollider={_carCollider != null})");
            return;
        }

        canClick = false;

        // Nếu bị chặn ngay trong bãi parking: tiến lên chạm collider, va chạm rung lắc và giật về chỗ cũ
        if (IsPathBlocked(out var startBlockedHit))
        {
            Debug.Log($"Car.TryDrive: Blocked immediately in parking lot by {startBlockedHit.collider.name}");
            float dist = Mathf.Max(0.15f, startBlockedHit.distance * 0.75f);
            TriggerBlockBumpAndBounceBack(startBlockedHit.collider != null ? startBlockedHit.collider.gameObject : null, dist);
            return;
        }

        // Đã bắt đầu xuất phát: xe chuyển trạng thái không còn ở ô đỗ (không giật lùi về bãi nữa)
        IsInParkingLot = false;
        _isMoving = true;

        var targetDrop = GameController.Instance != null && GameController.Instance.TargetDrop != null
            ? GameController.Instance.TargetDrop
            : FindAnyObjectByType<TargetDrop>(FindObjectsInactive.Include);

        var routeDriver = GameController.Instance != null && GameController.Instance.RoadMap != null
            ? GameController.Instance.RoadMap
            : FindAnyObjectByType<CarSplineRouteDriver>(FindObjectsInactive.Include);

        if (_movementCoroutine != null)
        {
            StopCoroutine(_movementCoroutine);
            _movementCoroutine = null;
        }

        // Ưu tiên chạy theo Spline Route Driver (RoadMap)
        _movementWasBlocked = false;
        isBlocking = false;
        isMovingToWall = false;
        closestBlockObj = null;

        if (routeDriver != null && routeDriver.HasRoute)
        {
            _isMoving = true;
            if (_previouslyBlockedInParkingLot)
            {
                _previouslyBlockedInParkingLot = false;
                TriggerCornerSpeedUp();
            }
            bool accepted = routeDriver.StartRoute(this, () =>
            {
                Debug.Log("Car.TryDrive: Reached route end, dropping balls.");
                targetDrop?.OnCarArrived(this);
                DropBallsAndExit(targetDrop);
            });

            if (accepted)
                return;
        }

        // Fallback: Chạy thẳng đến TargetDrop
        if (targetDrop != null && targetDrop.HasTarget)
        {
            _isMoving = true;
            if (_previouslyBlockedInParkingLot)
            {
                _previouslyBlockedInParkingLot = false;
                TriggerCornerSpeedUp();
            }
            Vector3 targetPosition = targetDrop.TargetPosition;
            Debug.Log($"Car.TryDrive: Driving to target at {targetPosition}");
            _movementCoroutine = StartCoroutine(MoveToTargetRoutine(targetPosition, () =>
            {
                Debug.Log("Car.TryDrive: Reached target, dropping balls.");
                targetDrop?.OnCarArrived(this);
                DropBallsAndExit(targetDrop);
            }));
        }
        else
        {
            Debug.LogWarning("Car.TryDrive: No RoadMap route or TargetDrop found in scene.");
            _isMoving = false;
            canClick = true;
        }
    }

    private bool IsPathBlocked(out RaycastHit blockedHit)
    {
        // Chỉ check khoảng cách ngắn (0.5m) trước đầu xe để xem có xe nào đỗ chắn ngay cửa slot không.
        // Tránh quét quá xa (8m) xuyên qua làn đường chạm vào các xe đỗ ở phía đối diện bãi.
        return IsPathBlocked(_vectorDir, 0.5f, out blockedHit);
    }

    public bool IsPathBlocked(Vector3 direction, float checkDistance, out RaycastHit blockedHit)
    {
        blockedHit = default;

        if (_carCollider == null || direction.sqrMagnitude <= 0.0001f)
            return false;

        LayerMask blockMask = carLayer | obstacleLayer;
        if (blockMask == 0)
            return false;

        Vector3 moveDirection = direction.normalized;
        float maxDistance = Mathf.Max(0.01f, checkDistance);

        // Lấy tâm và kích thước chuẩn của BoxCollider (không dùng bounds vì bounds là AABB xoay theo trục thế giới)
        Vector3 center;
        Vector3 halfExtents;
        if (_carCollider is BoxCollider box)
        {
            center = transform.TransformPoint(box.center);
            Vector3 worldSize = Vector3.Scale(box.size, transform.lossyScale);
            halfExtents = new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z)) * 0.5f;
        }
        else
        {
            Bounds bounds = _carCollider.bounds;
            center = bounds.center;
            halfExtents = bounds.extents;
        }

        // Dùng BoxCast với kích thước chuẩn đã xoay theo transform.rotation
        if (Physics.BoxCast(center, halfExtents * 0.9f, moveDirection, out blockedHit, transform.rotation, maxDistance, blockMask))
        {
            if (blockedHit.collider != null && blockedHit.collider != _carCollider && !blockedHit.collider.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        Vector3[] frontPoints = Helper.GetFrontPoints(_carCollider, moveDirection);
        for (int i = 0; i < frontPoints.Length; i++)
        {
            Vector3 origin = GetPlanarPosition(frontPoints[i]);
            if (!Physics.Raycast(origin, moveDirection, out blockedHit, maxDistance, blockMask))
                continue;

            if (blockedHit.collider == null)
                continue;

            if (blockedHit.collider == _carCollider || blockedHit.collider.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }

    public void HandleMovementBlocked(Vector3 hitPoint, GameObject blockObject = null)
    {
        // Khi đã ở trên đường đi thì không kích hoạt cơ chế giật lùi về bãi đỗ
        if (!IsInParkingLot || isBlocking || isMovingToWall)
            return;

        _movementWasBlocked = true;
        canClick = false;
        isBlocking = true;
        isMovingToWall = true;

        if (blockObject != null)
        {
            Car blockingCarFromObject = blockObject.GetComponentInParent<Car>();
            closestBlockObj = blockingCarFromObject != null ? blockingCarFromObject.gameObject : blockObject;
        }
        else if (Physics.Raycast(transform.position, _vectorDir.normalized, out var hit, rayDistance, carLayer | obstacleLayer)
            && hit.collider != null && hit.collider.transform != transform)
        {
            Car blockingCarFromRay = hit.collider.GetComponentInParent<Car>();
            closestBlockObj = blockingCarFromRay != null ? blockingCarFromRay.gameObject : hit.collider.gameObject;
        }

        if (closestBlockObj != null && closestBlockObj.TryGetComponent(out Car blockingCar))
            blockingCar.canClick = false;
    }

    public bool MovementWasBlocked => _movementWasBlocked;

    private void OnTriggerEnter(Collider other)
    {
        ProcessPhysicalBlock(other != null ? other.transform : null);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessPhysicalBlock(collision != null ? collision.transform : null);
    }

    private void ProcessPhysicalBlock(Transform otherTransform)
    {
        if (!IsInParkingLot || otherTransform == null)
            return;

        if (!isMovingToWall || closestBlockObj == null)
            return;

        Car contactedCar = otherTransform.GetComponentInParent<Car>();
        if (contactedCar != null)
        {
            Car expectedCar = closestBlockObj.GetComponentInParent<Car>();
            if (expectedCar != contactedCar) return;
        }
        else if (otherTransform.gameObject != closestBlockObj
            && !otherTransform.IsChildOf(closestBlockObj.transform))
        {
            return;
        }

        TriggerBlockBumpAndBounceBack(otherTransform.gameObject, 0.3f);
    }

    public void TriggerBlockBumpAndBounceBack(GameObject blockObject, float bumpDistance = 0.35f)
    {
        if (!IsInParkingLot)
            return;

        _previouslyBlockedInParkingLot = true;
        isMovingToWall = false;
        isBlocking = false;
        _isMoving = true;
        canClick = false;

        if (_movementCoroutine != null)
        {
            StopCoroutine(_movementCoroutine);
            _movementCoroutine = null;
        }

        transform.DOKill();

        var routeDriver = GameController.Instance != null && GameController.Instance.RoadMap != null
            ? GameController.Instance.RoadMap
            : FindAnyObjectByType<CarSplineRouteDriver>(FindObjectsInactive.Include);
        routeDriver?.StopRoute(this);

        Vector3 moveDir = _vectorDir.sqrMagnitude > 0.0001f ? _vectorDir.normalized : transform.up;
        float actualBumpDist = Mathf.Clamp(bumpDistance, 0.15f, 0.5f);
        Vector3 bumpTargetPos = GetPlanarPosition(transform.position + moveDir * actualBumpDist);

        GameObject targetObj = blockObject != null ? blockObject : closestBlockObj;

        // Sequence: 1. Tiến lên chạm va chạm -> 2. Đập trúng & rung xe bị đâm -> 3. Giật lùi về vị trí ban đầu
        Sequence bumpSeq = DOTween.Sequence();
        bumpSeq.Append(transform.DOMove(bumpTargetPos, 0.09f).SetEase(Ease.OutQuad));
        bumpSeq.AppendCallback(() =>
        {
            SoundManager.Instance?.PlayCarHit();

            if (targetObj != null)
            {
                Car blockingCar = targetObj.GetComponentInParent<Car>();
                if (blockingCar != null)
                {
                    blockingCar.canClick = false;
                    Vector3 blockedEuler = blockingCar.transform.localEulerAngles;
                    DOTween.Sequence()
                        .Append(blockingCar.transform.DOLocalRotate(
                            new Vector3(blockedEuler.x + 3f, blockedEuler.y, blockedEuler.z), 0.05f))
                        .Append(blockingCar.transform.DOLocalRotate(
                            new Vector3(blockedEuler.x - 3f, blockedEuler.y, blockedEuler.z), 0.06f))
                        .Append(blockingCar.transform.DOLocalRotate(blockedEuler, 0.05f))
                        .OnComplete(() =>
                        {
                            blockingCar.transform.localRotation = Quaternion.Euler(blockedEuler);
                            blockingCar.canClick = true;
                        });
                }
            }
        });
        bumpSeq.Append(transform.DOMove(GetPlanarPosition(_initPosition), returnDuration).SetEase(Ease.OutQuad));
        bumpSeq.OnComplete(() =>
        {
            SetPlanarPosition(_initPosition);
            SetPlanarRotationX(GetAngleFromCarData(_carData));
            _isMoving = false;
            canClick = true;
            closestBlockObj = null;
            _movementWasBlocked = false;
            IsInParkingLot = true;
        });
    }

    private void ResetToInitPos()
    {
        StopAllCoroutines();
        _movementCoroutine = null;
        _isMoving = true;
        IsInParkingLot = true;
        canClick = false;
        transform.DOKill();
        transform.DOMove(GetPlanarPosition(_initPosition), returnDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                SetPlanarPosition(_initPosition);
                SetPlanarRotationX(GetAngleFromCarData(_carData));
                _isMoving = false;
                canClick = true;
                closestBlockObj = null;
                _movementWasBlocked = false;
            });
    }

    public bool ShouldYieldForHigherYCar(float radius = 1.5f)
    {
        return ShouldYieldTrafficPriority(_vectorDir, radius);
    }

    public bool ShouldYieldTrafficPriority(Vector3 moveDirection, float radius = 1.5f)
    {
        // Khi vẫn đang ở bãi đỗ xe thì không kích hoạt cơ chế đứng nhường đường
        if (IsInParkingLot) return false;

        int mask = carLayer.value != 0 ? carLayer.value : Physics.DefaultRaycastLayers;
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, mask, QueryTriggerInteraction.Ignore);

        float currentY = transform.position.y;
        float currentX = transform.position.x;

        for (int i = 0; i < colliders.Length; i++)
        {
            var otherCar = colliders[i].GetComponentInParent<Car>();
            if (otherCar == null || otherCar == this || !otherCar.gameObject.activeInHierarchy || otherCar.IsInParkingLot)
                continue;

            if (!otherCar.canClick || otherCar.IsMoving)
            {
                float otherY = otherCar.transform.position.y;
                float otherX = otherCar.transform.position.x;

                float diffY = otherY - currentY;
                float diffX = otherX - currentX;

                // Nếu cả X và Y đều nằm trong vùng gần nhau (< 1.0m): cần xét thứ tự ưu tiên
                if (Mathf.Abs(diffY) < 1.0f && Mathf.Abs(diffX) < 1.0f)
                {
                    // 1. Nếu xe kia ĐÃ Ở TRÊN SPLINE còn xe này CHƯA Ở TRÊN SPLINE:
                    // Xe chưa ở Spline bắt buộc dừng lại nhường cho xe trên Spline chạy trước
                    if (otherCar.SplineIndex >= 0 && SplineIndex < 0)
                    {
                        return true;
                    }

                    // Nếu xe này đã ở Spline còn xe kia chưa lên: xe này được quyền đi tiếp
                    if (SplineIndex >= 0 && otherCar.SplineIndex < 0)
                    {
                        continue;
                    }

                    // 2. Nếu CẢ 2 XE ĐỀU Ở TRÊN SPLINE:
                    if (SplineIndex >= 0 && otherCar.SplineIndex >= 0)
                    {
                        // Xe nào ở Spline có số thứ tự lớn hơn (Spline 3 > Spline 2 > Spline 1) được ưu tiên chạy trước
                        if (otherCar.SplineIndex > SplineIndex)
                        {
                            return true;
                        }
                        if (otherCar.SplineIndex < SplineIndex)
                        {
                            continue;
                        }

                        // Cùng 1 Spline: xe nào ở vị trí xa hơn trên spline (phía trước) được ưu tiên
                        if (otherCar.SplineDistance > SplineDistance + 0.05f)
                        {
                            return true;
                        }
                        if (SplineDistance > otherCar.SplineDistance + 0.05f)
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
                        if (moveDirection.x > 0.01f)
                        {
                            if (diffX < -0.05f)
                                return true;
                        }
                        else if (moveDirection.x < -0.01f)
                        {
                            if (diffX > 0.05f)
                                return true;
                        }
                        else if (otherCar.GetInstanceID() > GetInstanceID())
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private IEnumerator MoveToTargetRoutine(Vector3 targetPosition, System.Action onComplete)
    {
        targetPosition = GetPlanarPosition(targetPosition);
        Vector3 initialDirection = targetPosition - transform.position;
        initialDirection.z = 0f;
        if (initialDirection.sqrMagnitude > 0.0001f)
        {
            SetPlanarRotation(initialDirection.normalized);
        }

        float yieldWaitTimer = 0f;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            if (yieldWaitTimer > 0f)
            {
                yieldWaitTimer -= Time.deltaTime;
                SetPlanarPosition(transform.position);
                yield return null;
                continue;
            }

            Vector3 stepDir = targetPosition - transform.position;
            if (ShouldYieldTrafficPriority(stepDir, 1.0f) || IsPathBlocked(stepDir, 0.2f, out _))
            {
                Vector3 reverseDirection = -_vectorDir.normalized;
                if (reverseDirection.sqrMagnitude < 0.001f)
                {
                    reverseDirection = -transform.up;
                }
                float backupDist = 0.2f;

                int mask = carLayer.value != 0 ? carLayer.value : Physics.DefaultRaycastLayers;
                if (Physics.Raycast(transform.position, reverseDirection, out RaycastHit backHit, backupDist + 0.5f, mask, QueryTriggerInteraction.Ignore))
                {
                    backupDist = Mathf.Max(0f, backHit.distance - 0.5f);
                }

                if (backupDist > 0.05f)
                {
                    Debug.Log($"Car {name} backing up in MoveToTargetRoutine by {backupDist}m");
                    Vector3 backupStart = transform.position;
                    Vector3 backupTarget = GetPlanarPosition(backupStart + reverseDirection * backupDist);

                    float backupElapsed = 0f;
                    float backupDuration = 0.75f;
                    while (backupElapsed < backupDuration)
                    {
                        Vector3 currentBackupPos = Vector3.Lerp(backupStart, backupTarget, backupElapsed / backupDuration);
                        SetPlanarPosition(currentBackupPos);
                        backupElapsed += Time.deltaTime;
                        yield return null;
                    }
                    SetPlanarPosition(backupTarget);
                    yieldWaitTimer = 0f; // Proceed immediately after backing up
                }
                else
                {
                    SetPlanarPosition(transform.position);
                    yieldWaitTimer = 0.2f;
                }
                yield return null;
                continue;
            }

            Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, driveSpeed * SpeedMultiplier * Time.deltaTime);
            Vector3 moveDirection = nextPosition - transform.position;
            moveDirection.z = 0f;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                if (IsPathBlocked(moveDirection, moveDirection.magnitude + 0.05f, out var blockedHit))
                {
                    HandleMovementBlocked(blockedHit.point, blockedHit.collider.gameObject);
                    yield break;
                }
                SetPlanarRotation(moveDirection.normalized);
            }

            SetPlanarPosition(nextPosition);
            yield return null;
        }

        SetPlanarPosition(targetPosition);
        _movementCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator PlayHitFeedback(Vector3 hitPoint)
    {
        _isMoving = true;
        Vector3 blockedPosition = transform.position;
        Vector3 pushedPosition = blockedPosition - (_vectorDir.normalized * hitPushDistance);
        SoundManager.Instance?.PlayCarHit();

        float elapsed = 0f;
        while (elapsed < returnDuration)
        {
            SetPlanarPosition(Vector3.Lerp(blockedPosition, pushedPosition, elapsed / returnDuration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetPlanarPosition(pushedPosition);
        yield return new WaitForSeconds(0.1f);

        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            SetPlanarPosition(Vector3.Lerp(pushedPosition, blockedPosition, elapsed / returnDuration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetPlanarPosition(blockedPosition);
        SetPlanarRotationX(GetAngleFromCarData(_carData));
        _isMoving = false;
        _movementCoroutine = null;
    }

    public CarData CarData => _carData;

    private void DropBallsAndExit(TargetDrop targetDrop)
    {
        _isMoving = false;
        if (_movementCoroutine != null)
        {
            StopCoroutine(_movementCoroutine);
            _movementCoroutine = null;
        }

        // Cập nhật rotation theo TargetDrop khi đến đích
        if (targetDrop != null)
        {
            SetPlanarRotationX(targetDrop.TargetAngleX);
        }

        // Rung lắc nhẹ rotation X ±2 độ ngay khoảnh khắc vừa dừng xe
        PlayBrakeShake();

        var ballDropper = GameController.Instance != null && GameController.Instance.BallDropper != null
            ? GameController.Instance.BallDropper
            : FindAnyObjectByType<BallDropper>(FindObjectsInactive.Include);

        var releaseTransform = ballReleasePoint != null ? ballReleasePoint : transform;

        if (ballDropper == null)
        {
            Debug.LogWarning("Car.DropBallsAndExit: ballDropper is not found.");
            MoveOutRight(targetDrop);
            return;
        }

        if (targetDrop != null && targetDrop.HasConveyor && targetDrop.Conveyor != null)
        {
            var conveyor = targetDrop.Conveyor;
            ballDropper.SpawnBallsOnConveyor(releaseTransform, targetDrop.TargetPoint, conveyor, _carData.type, _carData.eColor, () => MoveOutRight(targetDrop));
            return;
        }

        ballDropper.SpawnBallsAtTransform(releaseTransform, _carData.type, _carData.eColor, () => MoveOutRight(targetDrop));
    }

    private void PlayBrakeShake()
    {
        transform.DOKill();
        transform.DOPunchRotation(new Vector3(2f, 0f, 0f), 0.35f, vibrato: 10, elasticity: 0.5f);
    }

    private void MoveOutRight(TargetDrop targetDrop)
    {
        transform.DOKill();

        Vector3 exitPosition;
        if (targetDrop != null && targetDrop.HasExit)
        {
            exitPosition = targetDrop.ExitPosition;
            Debug.Log($"Car.MoveOutRight: exiting to assigned exit position {exitPosition}");
        }
        else
        {
            Vector3 exitDirection = _vectorDir.sqrMagnitude > 0.0001f ? _vectorDir.normalized : Vector3.right;
            exitPosition = transform.position + exitDirection * 5f;
            Debug.Log($"Car.MoveOutRight: exiting to local right {exitPosition}");
        }

        exitPosition = new Vector3(exitPosition.x, exitPosition.y, _initPosition.z);

        // Xoay xe theo hướng di chuyển ra exit
        Vector3 exitDir = exitPosition - transform.position;
        exitDir.z = 0f;
        if (exitDir.sqrMagnitude > 0.0001f)
        {
            SetPlanarRotation(exitDir.normalized);
        }

        float distance = Vector3.Distance(transform.position, exitPosition);
        float duration = Mathf.Max(0.01f, distance / driveSpeed);

        transform.DOMove(exitPosition, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                SetPlanarPosition(transform.position);
                _isMoving = false;
                var routeDriver = GameController.Instance != null && GameController.Instance.RoadMap != null
                    ? GameController.Instance.RoadMap
                    : FindAnyObjectByType<CarSplineRouteDriver>(FindObjectsInactive.Include);
                routeDriver?.ReleaseTarget(this);
                var parkingManager = FindAnyObjectByType<ParkingLotManager>(FindObjectsInactive.Include);
                parkingManager?.ReleaseCarToPool(this);
                Debug.Log("Car.MoveOutRight: exit complete.");
            });
    }

    private void OnDrawGizmosSelected()
    {
        if (_carCollider == null)
            _carCollider = GetComponent<Collider>();

        if (_carCollider == null)
            return;

        Vector3[] points = Helper.GetFrontPoints(_carCollider, _vectorDir == Vector3.zero ? Vector3.forward : _vectorDir);
        Gizmos.color = Color.yellow;
        foreach (var point in points)
        {
            Gizmos.DrawSphere(point, 0.05f);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.2f, transform.position + Vector3.up * 0.2f + _vectorDir * rayDistance);
    }
}
