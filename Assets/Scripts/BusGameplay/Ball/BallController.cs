using System;
using System.Collections;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public const string POOL_KEY = "Ball";
    public static float SpeedUpMultiplier = 1f;

    [SerializeField] private ColorType colorType;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private CircleCollider2D ballCollider;
    [SerializeField] private SmoothOutlineApplier smoothOutlineApplier;

    private Vector3 startScale;
    private float defaultMass;
    private Coroutine movementRoutine;

    public ColorType ColorType => colorType;
    public ColorType Color => colorType;
    public DockSlot Slot { get; private set; }
    public bool IsDockAnimationCompleted { get; private set; }
    public bool IsDocked => Slot != null;
    public bool IsDockedOnBox => false;
    public bool IsCompacting { get; private set; }
    public Vector3 StartScale => startScale;

    private void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (ballCollider == null) ballCollider = GetComponent<CircleCollider2D>();
        if (smoothOutlineApplier == null) smoothOutlineApplier = GetComponent<SmoothOutlineApplier>();
        startScale = transform.localScale;
        defaultMass = body != null ? body.mass : 1f;
    }

    public void Initialize(ColorType newColor) { colorType = newColor; ResetData(); }

    public void Dock(DockSlot slot, bool turnOnCollider = true, Action onComplete = null)
    {
        if (slot == null || !slot.CanAccept(this)) return;
        KillAllMovementTweens();
        Slot = slot;
        IsDockAnimationCompleted = false;
        SetColliders(turnOnCollider);
        DisablePhysics();
        movementRoutine = StartCoroutine(MoveRoutine(slot.transform.position, 0.12f, false, () =>
        {
            transform.rotation = slot.transform.rotation;
            IsDockAnimationCompleted = true;
            onComplete?.Invoke();
        }));
    }

    public bool CanStartCompactStep() => !IsCompacting && IsDocked && IsDockAnimationCompleted;

    public void MoveToConveyorSlotLinear(DockSlot newSlot, float duration)
    {
        if (newSlot == null) return;
        KillAllMovementTweens();
        Slot = newSlot;
        DisablePhysics();
        movementRoutine = StartCoroutine(MoveRoutine(newSlot.transform.position, duration, true, CompleteConveyorCompactMove));
    }

    public void MoveTo(Vector3 worldPosition, float duration)
    {
        KillAllMovementTweens();
        DisablePhysics();
        movementRoutine = StartCoroutine(MoveRoutine(worldPosition, duration, false, null));
    }

    public void MoveToConveyorSlotAlongMovingSpline(DockSlot newSlot, float duration, Func<float, Vector3> getWorldPosition)
    {
        if (newSlot == null || getWorldPosition == null) return;
        KillAllMovementTweens();
        Slot = newSlot;
        DisablePhysics();
        movementRoutine = StartCoroutine(PathMoveRoutine(duration, getWorldPosition));
    }

    private IEnumerator MoveRoutine(Vector3 target, float duration, bool compacting, Action onComplete)
    {
        IsCompacting = compacting;
        Vector3 start = transform.position;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * SpeedUpMultiplier;
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        transform.position = target;
        movementRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator PathMoveRoutine(float duration, Func<float, Vector3> getWorldPosition)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * SpeedUpMultiplier;
            transform.position = getWorldPosition(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        transform.position = getWorldPosition(1f);
        movementRoutine = null;
        CompleteConveyorCompactMove();
    }

    private void CompleteConveyorCompactMove() { IsCompacting = false; IsDockAnimationCompleted = true; }
    private void KillCompactMoveTween() { if (movementRoutine != null) StopCoroutine(movementRoutine); movementRoutine = null; IsCompacting = false; }
    public void KillAllMovementTweens() => KillCompactMoveTween();

    public void Undock()
    {
        KillAllMovementTweens();
        Slot = null;
        IsDockAnimationCompleted = false;
        SetColliders(true);
        EnablePhysics();
    }

    public void SetColliders(bool enable) { if (ballCollider != null) ballCollider.enabled = enable; }
    public void OnSpawned() => ResetData();
    public void OnDespawned() => ResetData();
    public void Release() => OnDespawned();

    private void ResetData()
    {
        KillAllMovementTweens();
        Slot = null;
        IsDockAnimationCompleted = false;
        IsCompacting = false;
        transform.localScale = startScale == Vector3.zero ? Vector3.one : startScale;
        ResetMass();
        EnablePhysics();
        SetColliders(true);
        SetReviveOutline(false);
    }

    public void TripleMass() { if (body != null) body.mass = defaultMass * 3f; }
    public void ResetMass() { if (body != null) body.mass = defaultMass; }

    public void EnablePhysics()
    {
        if (body == null) return;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = true;
    }

    public void DisablePhysics()
    {
        if (body == null) return;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;
    }

    public void AddRandomVerticalForce(float fromRange, float toRange)
    {
        if (body != null) body.AddForce(Vector2.up * UnityEngine.Random.Range(fromRange, toRange), ForceMode2D.Impulse);
    }

    public void AddRandomHorizontalForce(float fromRange, float toRange)
    {
        if (body == null) return;
        Vector2 force = new(UnityEngine.Random.Range(fromRange, toRange), UnityEngine.Random.Range(fromRange, toRange));
        body.AddForce(force, ForceMode2D.Impulse);
    }

    public void SetReviveOutline(bool enabled)
    {
        smoothOutlineApplier?.SetReviveOutline(enabled);
    }
}
