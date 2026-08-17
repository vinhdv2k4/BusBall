using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(SplineAnimate))]
public class Bus : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BusData data = new();
    [SerializeField] private BusAnalysisData analysisData = new();
    [SerializeField] private BusPathData pathData = new();
    [Header("Renderers")]
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Renderer topRenderer;
    [SerializeField] private Renderer glassRenderer;
    [SerializeField] private SpriteRenderer iconTop;
    [SerializeField] private DecoyBus decoyBus;
    [Header("Spline")]
    [SerializeField] private SplineAnimate splineAnimate;
    [SerializeField] private RoadPath roadPath;
    [Header("Animation")]
    [SerializeField] private BusAnimationController busAnimationController;
    [Header("Vfx Position")]
    [SerializeField] private Transform tfReleaseBall;
    [SerializeField] private Transform tfLanding;
    [SerializeField] private Transform tfSmoke;
    [SerializeField] private Transform tfHurrySmoke;
    [Header("Ball Transforms")]
    [SerializeField] private List<Transform> ballTransforms = new();
    private BallController ballPrefab;
    [Header("Hurry Bus Light")]
    [SerializeField] private GameObject[] hurryBusLight;
    [SerializeField] private Transform dropParent;
    [SerializeField] private FunnelController funnel;
    [SerializeField] private TopGameController topGameController;
    [Header("Movement")]
    [SerializeField] private BusManager busManager;
    [SerializeField] private LayerMask blockMask = ~0;
    [SerializeField] private bool enableClickInput = true;
    [Header("Di chuyển cơ bản - sẽ nối spline sau")]
    private readonly List<BallController> balls = new();
    private Collider busCollider;
    private bool isBlocked;
    private Coroutine movementRoutine;
    private readonly List<Bus> blockingBuses = new();
    private readonly List<Bus> busesBlockedByThis = new();
    private readonly RaycastHit[] movementCastHits = new RaycastHit[8];
    private int levelIndex = -1;
    private bool releasedBlockedBuses;
    private bool hasFinishedRoad;
    private bool hasOpenedOnRoad;
    private bool isDespawning;
    private int lastReachedKnotIndex = -1;
    private float activeKnotZRotation;
    private Vector3 movementStartPosition;
    private Tween collisionReturnTween;
    private Tween roadEntryRotationTween;
    private VfxPoolable hurrySmokeVfx;
    private const float ExitDistance = 10f;
    private const float ExitSpeed = 10f;
    private const float RoadRotationDuration = 0.05f;
    private const float RoadEntryRotationDuration = 0.1f;

    public IReadOnlyList<BallController> Balls => balls;
    public int BallCount => balls.Count;
    public BusState State { get; private set; } = BusState.Idle;
    public bool IsBlocked => isBlocked;
    public event Action<bool> BlockedChanged;
    public event Action<BusState> StateChanged;
    public event Action<HitDirection> Hit;

    public BusData Data => data;
    public BusAnalysisData AnalysisData => analysisData;
    public BusPathData PathData => pathData;
    public IReadOnlyList<Transform> BallTransforms => ballTransforms;
    public int LevelIndex => levelIndex;

    private void Awake()
    {
        if (busManager == null)
            busManager = FindFirstObjectByType<BusManager>();
        if (roadPath == null)
            roadPath = FindFirstObjectByType<RoadPath>();
        if (funnel == null)
            funnel = FindFirstObjectByType<FunnelController>();
        if (topGameController == null)
            topGameController = FindFirstObjectByType<TopGameController>();

        busCollider = GetComponent<Collider>();
        SetHurryBusLights(false);
        if (splineAnimate != null)
            splineAnimate.Completed += OnSplineCompleted;
        foreach (Transform slot in ballTransforms)
        {
            BallController ball = slot != null ? slot.GetComponentInChildren<BallController>() : null;
            if (ball != null && !balls.Contains(ball)) balls.Add(ball);
        }
    }

    private void OnDestroy()
    {
        if (splineAnimate != null)
            splineAnimate.Completed -= OnSplineCompleted;
        StopHurrySmoke();
    }

    private void OnSplineCompleted()
    {
        if (State != BusState.FollowRoad || hasFinishedRoad) return;

        hasFinishedRoad = true;
        StartCoroutine(FinishRoad());
    }

    private System.Collections.IEnumerator FinishRoad()
    {
        ChangeState(BusState.Finished);

        // The ordered Ball Transforms list is authored in the Inspector.
        // Scale the balls one by one as soon as Car_Release begins.
        yield return ScaleBallsForReleaseStaggered(0.2f, 0.02f);

        DropBall();
        topGameController?.NotifyBusReleasedBalls();
        busAnimationController?.PlayMove();
        yield return ExitAndRecycle();
    }

    private void PrepareBallsForRelease()
    {
        foreach (BallController ball in balls)
        {
            if (ball != null)
                ball.transform.localScale = Vector3.one * 0.45f;
        }
    }

    private System.Collections.IEnumerator ScaleBallsForReleaseStaggered(float duration, float delayBetweenBalls)
    {
        foreach (Transform slot in ballTransforms)
        {
            if (slot == null) continue;
            BallController ball = slot.GetComponentInChildren<BallController>();
            if (ball == null) continue;

            StartCoroutine(ScaleBallForRelease(ball.transform, duration));
            yield return new WaitForSeconds(delayBetweenBalls);
        }

        // Let the last ball finish its own scale animation before releasing.
        yield return new WaitForSeconds(duration);
    }

    private System.Collections.IEnumerator ScaleBallForRelease(Transform ballTransform, float duration)
    {
        if (ballTransform == null) yield break;

        Vector3 startScale = ballTransform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ballTransform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.45f, t);
            yield return null;
        }
    }

    private System.Collections.IEnumerator ExitAndRecycle()
    {
        if (isDespawning) yield break;
        isDespawning = true;
        ChangeState(BusState.Despawn);

        Vector3 exitDirection = Vector3.right;
        Vector3 exitTarget = transform.position + exitDirection * ExitDistance;
        float speed = ExitSpeed;

        while (speed > 0f && (transform.position - exitTarget).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, exitTarget, speed * Time.deltaTime);
            yield return null;
        }

        RecycleToPool();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == null) return;
        Bus blockingBus = collision.collider.GetComponentInParent<Bus>();
        if (blockingBus == null || blockingBus == this || !blockingBuses.Contains(blockingBus)) return;
        HandleBusCollision(blockingBus, collision.collider);
    }

    private void HandleBusCollision(Bus blockingBus, Collider blockingCollider)
    {
        roadEntryRotationTween?.Kill();
        roadEntryRotationTween = null;
        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
            movementRoutine = null;
        }
        ChangeState(BusState.GotHit);
        StopHurrySmoke();
        blockingBus.PlayStuckFeedback();
        VfxManager.Play(VfxType.BusHit, blockingBus.transform.position);
        GameSoundManager.Instance?.PlayCarImpact();
        RestoreBlockedBusesIfReturned();
        SetBlocked(true);

        // The moving bus bounces back to where this attempt started.
        busAnimationController?.PlayHit(HitDirection.Back);
        collisionReturnTween?.Kill();
        collisionReturnTween = transform.DOMove(movementStartPosition, RoadRotationDuration)
            .SetEase(Ease.OutQuad);
        Hit?.Invoke(HitDirection.Back);
    }

    public void StartMoveStraight()
    {
        collisionReturnTween?.Kill();
        roadEntryRotationTween?.Kill();
        roadEntryRotationTween = null;
        movementStartPosition = transform.position;
        ChangeState(BusState.MoveStraight);
        StartHurrySmoke();
        if (blockingBuses.Count > 0 || (data != null && data.IsHurryBusMechanic()))
        {
            busAnimationController?.PlayHurry();
        }
        if (movementRoutine != null) StopCoroutine(movementRoutine);
        movementRoutine = StartCoroutine(MoveToRoadPoint());
    }

    private void Start()
    {
        ApplyColor();
        busAnimationController?.PlayIdle();
    }

    private void Update()
    {
        if (State == BusState.FollowRoad && !hasOpenedOnRoad && splineAnimate != null &&
            splineAnimate.NormalizedTime >= 0.8f)
        {
            hasOpenedOnRoad = true;
            if (iconTop != null) iconTop.enabled = false;
            busAnimationController?.PlayOpen(data.busType);
        }

        if (!TryGetPointerPress(out Vector2 screenPosition))
            return;

        TrySelect(screenPosition);
    }

    private static bool TryGetPointerPress(out Vector2 screenPosition)
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    private void LateUpdate()
    {
        if (State != BusState.FollowRoad || splineAnimate == null || splineAnimate.Container == null)
            return;

        Spline spline = splineAnimate.Container.Spline;
        if (spline == null || spline.Count == 0) return;

        int segmentCount = spline.Closed ? spline.Count : spline.Count - 1;
        if (segmentCount <= 0) return;

        float scaledT = Mathf.Clamp01(splineAnimate.NormalizedTime) * segmentCount;
        int currentKnotIndex = Mathf.Min(Mathf.FloorToInt(scaledT), spline.Count - 1);
        if (currentKnotIndex != lastReachedKnotIndex)
        {
            float newKnotZRotation = (splineAnimate.Container.transform.rotation *
                ToUnityQuaternion(spline[currentKnotIndex].Rotation)).eulerAngles.z;
            if (!hasOpenedOnRoad)
                PlayTurnAnimation(activeKnotZRotation, newKnotZRotation);
            activeKnotZRotation = newKnotZRotation;
            lastReachedKnotIndex = currentKnotIndex;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, activeKnotZRotation);
    }

    private static Quaternion ToUnityQuaternion(Unity.Mathematics.quaternion rotation)
    {
        return new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
    }

    private void PlayTurnAnimation(float currentZRotation, float nextZRotation)
    {
        Vector3 currentDirection = Quaternion.Euler(0f, 0f, currentZRotation) * Vector3.right;
        Vector3 nextDirection = Quaternion.Euler(0f, 0f, nextZRotation) * Vector3.right;
        float directionDot = Vector3.Dot(currentDirection, nextDirection);
        float directionCrossZ = Vector3.Cross(currentDirection, nextDirection).z;

        if (directionDot > 0.999f)
            busAnimationController?.PlayMove();
        else if (directionCrossZ > 0f)
            busAnimationController?.PlayMoveLeft();
        else
            busAnimationController?.PlayMoveRight();
    }

    private void TrySelect(Vector2 screenPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;

        Bus selectedBus = hit.collider.GetComponentInParent<Bus>();
        if (selectedBus != this) return;

        if (!enableClickInput || !CanInteract()) return;

        GameSoundManager.Instance?.PlayCarFill();
        SetBlocked(false);
        StartMoveStraight();
    }

    private bool CanInteract()
    {
        // Cho phép click lại khi bus đang bị chặn để phát lại Car_Hit.
        return State == BusState.Idle || State == BusState.Blocked;
    }

    private bool HasBlockingObject(Vector3 direction, float distance)
    {
        Vector3 origin = transform.position + direction.normalized * distance * 0.5f;
        Vector3 halfExtents = new(0.4f, 0.4f, Mathf.Max(0.1f, distance * 0.5f));
        Collider[] hits = Physics.OverlapBox(origin, halfExtents, transform.rotation, blockMask);
        foreach (Collider hit in hits)
            if (hit != null && hit.transform != transform && !hit.transform.IsChildOf(transform)) return true;
        return false;
    }

    private bool TryGetBlockingBus(Vector3 direction, float distance, out Bus blockingBus)
    {
        blockingBus = null;
        Vector3 origin = transform.position + direction.normalized * distance * 0.5f;
        Vector3 halfExtents = new(0.4f, 0.4f, Mathf.Max(0.1f, distance * 0.5f));
        Collider[] hits = Physics.OverlapBox(origin, halfExtents, transform.rotation, blockMask);

        foreach (Collider hit in hits)
        {
            if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            blockingBus = hit.GetComponentInParent<Bus>();
            if (blockingBus != null && blockingBus != this)
                return true;
        }

        return false;
    }

    private System.Collections.IEnumerator MoveToRoadPoint()
    {
        Vector3 parkingPosition = transform.position;
        Quaternion parkingRotation = transform.rotation;
        float enterDistance = busManager != null ? busManager.EnterDistance : 0.05f;
        float speed = busManager != null ? busManager.MoveSpeed : 0f;

        if (speed <= 0f)
        {
            Debug.LogWarning($"Bus {name}: BusManager or MoveSpeed is not configured.");
            StopHurrySmoke();
            yield break;
        }

        if (roadPath == null || !roadPath.TryGetRoad(pathData.splineContainerIndex, out SplineContainer road))
        {
            Debug.LogWarning($"Bus {name}: Road index {pathData.splineContainerIndex} is not configured.");
            StopHurrySmoke();
            yield break;
        }

        // nearestPoint is authored in the selected spline's local space.
        Vector3 targetPosition = road.transform.TransformPoint(pathData.nearestPoint);
        while ((transform.position - targetPosition).sqrMagnitude > 0.0001f)
        {
            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position, targetPosition, speed * Time.deltaTime);
            if (MoveOrHandleBusCollision(nextPosition)) yield break;
            transform.rotation = parkingRotation;
            if (!releasedBlockedBuses &&
                (transform.position - parkingPosition).sqrMagnitude >= enterDistance * enterDistance)
                ReleaseBlockedBuses();
            yield return null;
        }

        transform.position = targetPosition;
        ReleaseBlockedBuses();

        if (!roadPath.TryEvaluate(pathData.splineContainerIndex, pathData.nearestT,
                out Vector3 splineEntryPosition, out _))
        {
            StopHurrySmoke();
            yield break;
        }

        if (splineAnimate == null)
        {
            Debug.LogWarning($"Bus {name}: SplineAnimate is not assigned.");
            StopHurrySmoke();
            yield break;
        }

        // At the near point, begin turning toward the knot that contains nearestT.
        // The turn intentionally runs while the bus keeps moving straight to the spline.
        ChangeState(BusState.EnterRoad);
        splineAnimate.Container = road;
        splineAnimate.Alignment = SplineAnimate.AlignmentMode.None;
        splineAnimate.NormalizedTime = pathData.nearestT;
        hasFinishedRoad = false;
        hasOpenedOnRoad = false;
        lastReachedKnotIndex = GetReachedKnotIndex(road.Spline, pathData.nearestT);
        activeKnotZRotation = transform.eulerAngles.z;
        float splineEntryZRotation = GetKnotZRotation(road.Spline, lastReachedKnotIndex);
        PlayTurnAnimation(activeKnotZRotation, splineEntryZRotation);
        roadEntryRotationTween = transform.DORotate(new Vector3(0f, 0f, splineEntryZRotation),
                RoadEntryRotationDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => roadEntryRotationTween = null);

        while ((transform.position - splineEntryPosition).sqrMagnitude > 0.0001f)
        {
            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position, splineEntryPosition, speed * Time.deltaTime);
            if (MoveOrHandleBusCollision(nextPosition)) yield break;
            yield return null;
        }

        transform.position = splineEntryPosition;
        activeKnotZRotation = splineEntryZRotation;
        ChangeState(BusState.FollowRoad);
        splineAnimate.Play();
    }

    private bool MoveOrHandleBusCollision(Vector3 nextPosition)
    {
        Vector3 movement = nextPosition - transform.position;
        float movementDistance = movement.magnitude;
        if (movementDistance <= 0.0001f) return false;

        if (busCollider != null)
        {
            Bounds bounds = busCollider.bounds;
            int hitCount = Physics.BoxCastNonAlloc(
                bounds.center,
                bounds.extents * 0.98f,
                movement / movementDistance,
                movementCastHits,
                Quaternion.identity,
                movementDistance,
                blockMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = movementCastHits[i].collider;
                Bus blockingBus = hitCollider != null ? hitCollider.GetComponentInParent<Bus>() : null;
                if (blockingBus == null || blockingBus == this || !blockingBuses.Contains(blockingBus))
                    continue;

                float safeDistance = Mathf.Max(0f, movementCastHits[i].distance - 0.001f);
                transform.position += movement.normalized * safeDistance;
                HandleBusCollision(blockingBus, hitCollider);
                return true;
            }
        }

        transform.position = nextPosition;
        return false;
    }

    private static int GetReachedKnotIndex(Spline spline, float normalizedTime)
    {
        int segmentCount = spline.Closed ? spline.Count : spline.Count - 1;
        if (segmentCount <= 0) return -1;
        float scaledT = Mathf.Clamp01(normalizedTime) * segmentCount;
        return Mathf.Min(Mathf.FloorToInt(scaledT), spline.Count - 1);
    }

    private float GetKnotZRotation(Spline spline, int knotIndex)
    {
        if (knotIndex < 0 || knotIndex >= spline.Count || splineAnimate.Container == null)
            return transform.eulerAngles.z;

        return (splineAnimate.Container.transform.rotation *
            ToUnityQuaternion(spline[knotIndex].Rotation)).eulerAngles.z;
    }

    public void Configure(BusData data, Vector3 position, float zRotation)
    {
        ResetForPoolSpawn();
        this.data = data?.Clone() ?? new BusData();
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, zRotation));
        ApplyColor();
    }

    public void Configure(BusData data, BusAnalysisData analysisData, BusPathData pathData,
        Vector3 position, float zRotation)
    {
        this.analysisData = analysisData?.Clone() ?? new BusAnalysisData();
        this.pathData = pathData?.Clone() ?? new BusPathData();
        Configure(data, position, zRotation);
    }

    public void SetBallPrefab(BallController prefab)
    {
        ballPrefab = prefab;
        SpawnStaticBall();
    }

    public void RecycleToPool()
    {
        ResetForPoolSpawn();
        ObjectPool.Recycle(gameObject);
    }

    private void ResetForPoolSpawn()
    {
        StopAllCoroutines();
        movementRoutine = null;
        collisionReturnTween?.Kill();
        collisionReturnTween = null;
        roadEntryRotationTween?.Kill();
        roadEntryRotationTween = null;
        splineAnimate?.Pause();
        StopHurrySmoke();
        SetHurryBusLights(false);
        RecycleStoredBalls();
        blockingBuses.Clear();
        busesBlockedByThis.Clear();
        isBlocked = false;
        releasedBlockedBuses = false;
        hasFinishedRoad = false;
        hasOpenedOnRoad = false;
        isDespawning = false;
        State = BusState.Idle;
        if (iconTop != null) iconTop.enabled = true;
        busAnimationController?.PlayIdle();
    }

    private void RecycleStoredBalls()
    {
        foreach (BallController ball in balls)
        {
            if (ball == null || !ObjectPool.IsSpawned(ball.gameObject)) continue;
            ball.Release();
            ObjectPool.Recycle(ball.gameObject);
        }
        balls.Clear();
    }

    public void SpawnStaticBall()
    {
        SpawnBalls();
    }

    private void SpawnBalls()
    {
        if (ballPrefab == null || balls.Count > 0 || data == null) return;

        int ballCount = Mathf.Min(data.GetBallCapacity(), ballTransforms.Count);
        for (int i = 0; i < ballCount; i++)
        {
            Transform slot = ballTransforms[i];
            if (slot == null) continue;

            BallController ball = ObjectPool.Spawn(ballPrefab, slot, slot.position, slot.rotation);
            if (ball == null) continue;

            ball.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            ball.Initialize(data.colorType, false);
            balls.Add(ball);
        }
    }

    public void ClearBlockingBuses()
    {
        blockingBuses.Clear();
    }

    public void SetLevelIndex(int index)
    {
        levelIndex = index;
    }

    private Bus GetNearestBlockingBus(Vector3 direction, float distance)
    {
        Vector3 origin = transform.position + direction.normalized * distance * 0.5f;
        Vector3 halfExtents = new(0.4f, 0.4f, Mathf.Max(0.1f, distance * 0.5f));
        Collider[] overlaps = Physics.OverlapBox(origin, halfExtents, transform.rotation, blockMask);

        Bus nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Collider overlap in overlaps)
        {
            if (overlap == null) continue;

            Bus bus = overlap.GetComponentInParent<Bus>();
            if (bus == null || !blockingBuses.Contains(bus)) continue;

            float currentDistance = (bus.transform.position - transform.position).sqrMagnitude;
            if (currentDistance < nearestDistance ||
                (Mathf.Approximately(currentDistance, nearestDistance) &&
                 (nearest == null || bus.LevelIndex < nearest.LevelIndex)))
            {
                nearest = bus;
                nearestDistance = currentDistance;
            }
        }

        if (nearest != null) return nearest;

        // Fallback when the JSON relation exists but colliders do not overlap yet.
        foreach (Bus bus in blockingBuses)
        {
            if (bus == null) continue;
            if (nearest == null || bus.LevelIndex < nearest.LevelIndex)
                nearest = bus;
        }
        return nearest;
    }

    public void AddBlockingBus(Bus bus)
    {
        if (bus != null && bus != this && !blockingBuses.Contains(bus))
            blockingBuses.Add(bus);
    }

    private void PlayStuckFeedback()
    {
        busAnimationController?.PlayStuck();
        GameSoundManager.Instance?.PlayCarStuck();
    }

    private void StartHurrySmoke()
    {
        if (hurrySmokeVfx == null)
            hurrySmokeVfx = VfxManager.PlayAttached(VfxType.SmokeHurryBus, tfHurrySmoke);
    }

    private void StopHurrySmoke()
    {
        if (hurrySmokeVfx == null) return;
        hurrySmokeVfx.Release();
        hurrySmokeVfx = null;
    }

    private void SetHurryBusLights(bool active)
    {
        foreach (GameObject light in hurryBusLight)
        {
            if (light != null && light.activeSelf != active)
                light.SetActive(active);
        }
    }

    public void AddBlockedBus(Bus bus)
    {
        if (bus != null && bus != this && !busesBlockedByThis.Contains(bus))
            busesBlockedByThis.Add(bus);
    }

    private void ReleaseBlockedBuses()
    {
        if (releasedBlockedBuses) return;
        releasedBlockedBuses = true;

        foreach (Bus blockedBus in busesBlockedByThis)
            blockedBus?.RemoveBlockingBus(this);
    }

    private void RestoreBlockedBusesIfReturned()
    {
        if (!releasedBlockedBuses) return;
        releasedBlockedBuses = false;

        foreach (Bus blockedBus in busesBlockedByThis)
            blockedBus?.RestoreBlockingBus(this);
    }

    private void RestoreBlockingBus(Bus blockingBus)
    {
        if (blockingBus == null || blockingBus == this || blockingBuses.Contains(blockingBus)) return;

        blockingBuses.Add(blockingBus);
        analysisData.blockedBusCount++;
    }

    private void RemoveBlockingBus(Bus blockingBus)
    {
        if (!blockingBuses.Remove(blockingBus)) return;

        analysisData.blockedBusCount = Mathf.Max(0, analysisData.blockedBusCount - 1);
        if (analysisData.IsBlocked()) return;

        if (isBlocked)
        {
            isBlocked = false;
            BlockedChanged?.Invoke(false);
        }

        if (State == BusState.Blocked)
            ChangeState(BusState.Idle);
    }

    private void ApplyColor()
    {
        if (data == null) return;
        ColorType colorType = data.colorType;

        string matName = GetMaterialNameSuffix(colorType);
        if (!string.IsNullOrEmpty(matName))
        {
            string path = $"Materials/Color/M_Color_{matName}";
            Material mat = Resources.Load<Material>(path);
            if (mat != null)
            {
                if (bodyRenderer != null) bodyRenderer.sharedMaterial = mat;
                if (topRenderer != null) topRenderer.sharedMaterial = mat;
            }

            if (glassRenderer != null)
            {
                string glassPath = $"Materials/Glass/M_Glass_{matName}";
                Material glassMaterial = Resources.Load<Material>(glassPath);
                if (glassMaterial != null)
                {
                    Material[] materials = glassRenderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                        glassRenderer.sharedMaterial = glassMaterial;
                    else
                    {
                        for (int i = 0; i < materials.Length; i++)
                            materials[i] = glassMaterial;
                        glassRenderer.sharedMaterials = materials;
                    }
                }
            }
        }

        foreach (BallController ball in balls)
        {
            if (ball != null)
            {
                // Balls stored in the Bus must stay attached and non-physical
                // until Car_Release has completed.
                ball.Initialize(colorType, false);
            }
        }
    }

    private string GetMaterialNameSuffix(ColorType colorType)
    {
        return colorType switch
        {
            ColorType.Red => "Red",
            ColorType.Green => "Green",
            ColorType.Blue => "Blue",
            ColorType.Yellow => "Yellow",
            ColorType.Purple => "Purple",
            ColorType.Orange => "Orange",
            ColorType.Pink => "Pink",
            ColorType.Cyan => "BlueLight",
            ColorType.Forest => "GreenLight",
            ColorType.Brown => "Brown",
            ColorType.Black => "Black",
            _ => null
        };
    }

    public void Stop()
    {
        ChangeState(BusState.Finished);
    }

    private void SetBlocked(bool value)
    {
        if (isBlocked == value) return;
        isBlocked = value;
        ChangeState(value ? BusState.Blocked : BusState.MoveStraight);
        BlockedChanged?.Invoke(value);
    }

    private void ChangeState(BusState nextState)
    {
        if (State == nextState) return;
        State = nextState;
        SetHurryBusLights(nextState == BusState.MoveStraight ||
                          nextState == BusState.EnterRoad ||
                          nextState == BusState.FollowRoad);
        switch (nextState)
        {
            case BusState.Idle: busAnimationController?.PlayIdle(); break;
            case BusState.GarageOut: busAnimationController?.PlayGarageOut(); break;
            case BusState.MoveStraight:
            case BusState.EnterRoad:
            case BusState.FollowRoad: busAnimationController?.PlayMove(); break;
            case BusState.Blocked: busAnimationController?.PlayBlocked(); break;
            case BusState.Finished: busAnimationController?.PlayRelease(); break;
        }
        StateChanged?.Invoke(nextState);
    }

    public List<BallController> DropBall()
    {
        // Released balls belong to BallStart, which is the gameplay parent for
        // balls after they leave a bus. Their world-space formation is retained.
        Transform parent = topGameController != null && topGameController.BallStart != null
            ? topGameController.BallStart
            : dropParent != null ? dropParent : funnel?.BallHolder;
        return DropBallRow(parent);
    }

    public List<BallController> DropBall(Transform parent)
    {
        return DropBallRow(parent);
    }

    public List<BallController> DropBallRow(Transform parent)
    {
        List<BallController> dropped = new(balls);
        balls.Clear();
        if (dropped.Count > 0)
            GameSoundManager.Instance?.PlayFillSequence();
        for (int i = 0; i < dropped.Count; i++)
            ReleaseBall(dropped[i], parent, i, dropped.Count);
        return dropped;
    }

    public void DropBallRowIndividually(Transform parent)
    {
        StartCoroutine(DropBallRowIndividuallyCoroutine(parent));
    }

    private System.Collections.IEnumerator DropBallRowIndividuallyCoroutine(Transform parent)
    {
        List<BallController> dropped = new(balls);
        balls.Clear();
        for (int i = 0; i < dropped.Count; i++)
        {
            ReleaseBall(dropped[i], parent, i, dropped.Count);
            yield return new WaitForSeconds(0.08f);
        }
    }

    private void ReleaseBall(BallController ball, Transform parent, int index, int count)
    {
        if (ball == null) return;

        // Keep the exact formation authored under BallPosition. Re-parenting with
        // worldPositionStays preserves each ball's current world position.
        ball.transform.SetParent(parent, true);
        Vector3 releasePosition = ball.transform.position;
        releasePosition.z = -0.46f;
        ball.transform.position = releasePosition;
        ball.transform.localScale = Vector3.one * 0.45f;
        ball.DisablePhysics();
        ball.SetColliders(false);
        ball.Undock();
        VfxManager.Play(VfxType.BusReleaseBall, releasePosition);
        funnel?.ReceiveBall(ball);
    }

    public bool AddBall(BallController ball)
    {
        if (ball == null || balls.Contains(ball)) return false;
        balls.Add(ball);
        ball.transform.SetParent(transform, true);
        return true;
    }
}
