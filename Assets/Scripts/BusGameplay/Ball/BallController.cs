using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

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
    public Rigidbody2D Rb => body;
    public CircleCollider2D CirCollider => ballCollider;
    public DockSlot Slot { get; private set; }
    public bool IsDockAnimationCompleted { get; private set; }
    public bool IsDocked => Slot != null;
    public bool IsDockedOnBox => false;
    public bool IsCompacting { get; private set; }
    public bool IsMoving => movementRoutine != null;
    public Vector3 StartScale => startScale;

    private void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (ballCollider == null) ballCollider = GetComponent<CircleCollider2D>();
        if (smoothOutlineApplier == null) smoothOutlineApplier = GetComponent<SmoothOutlineApplier>();
        startScale = transform.localScale;
        defaultMass = body != null ? body.mass : 1f;
    }

    public void Initialize(ColorType newColor, bool activatePhysics = true)
    {
        colorType = newColor;
        ResetData(activatePhysics);

        string matName = GetMaterialNameSuffix(newColor);
        if (!string.IsNullOrEmpty(matName))
        {
            string path = $"Materials/Color/M_Color_{matName}";
            Material mat = Resources.Load<Material>(path);
            if (mat != null)
            {
                if (smoothOutlineApplier != null)
                {
                    smoothOutlineApplier.SetDefaultOutline(mat);
                    Debug.Log($"Ball {name}: Loaded and applied ball material {path}");
                }
                else
                {
                    Debug.LogWarning($"Ball {name}: smoothOutlineApplier is null!");
                }
            }
            else
            {
                Debug.LogWarning($"Ball {name}: Failed to load ball material {path}!");
            }
        }
        else
        {
            Debug.Log($"Ball {name}: matName suffix is null/empty for colorType={newColor}");
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

    public void Dock(DockSlot slot, bool turnOnCollider = true, Action onComplete = null)
    {
        if (slot == null || !slot.CanAccept(this)) return;
        KillAllMovementTweens();
        Slot = slot;
        transform.SetParent(slot.transform, true);
        IsDockAnimationCompleted = false;
        // A docked ball must stay in the 2D physics simulation so conveyor
        // detectors can receive trigger callbacks while it follows its slot.
        SetColliders(true);
        EnableKinematicPhysics();
        movementRoutine = StartCoroutine(MoveRoutine(slot.transform.position, 0.12f, false, () =>
        {
            // Reset the ball to the slot's local origin so it stays centered
            // while following the conveyor spline.
            transform.localPosition = Vector3.zero;
            // The ball follows both the position and rotation of the conveyor slot.
            // Keep its local rotation neutral so the slot's spline rotation drives it.
            transform.localRotation = Quaternion.identity;
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
        EnableKinematicPhysics();
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
        IsDockAnimationCompleted = false;
        SetColliders(true);
        EnableKinematicPhysics();
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
        DockSlot targetSlot = Slot;
        transform.SetParent(null, true);
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * SpeedUpMultiplier;
            transform.position = getWorldPosition(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        transform.position = getWorldPosition(1f);
        if (targetSlot != null)
        {
            transform.SetParent(targetSlot.transform, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        movementRoutine = null;
        CompleteConveyorCompactMove();
    }

    private void CompleteConveyorCompactMove() { IsCompacting = false; IsDockAnimationCompleted = true; }
    private void KillCompactMoveTween()
    {
        if (movementRoutine != null) StopCoroutine(movementRoutine);
        movementRoutine = null;
        IsCompacting = false;
    }
    public void KillAllMovementTweens() => KillCompactMoveTween();

    public void Undock()
    {
        KillAllMovementTweens();
        transform.SetParent(null, true);
        Slot = null;
        IsDockAnimationCompleted = false;
        SetColliders(true);
        EnablePhysics();
    }

    public void SetColliders(bool enable) { if (ballCollider != null) ballCollider.enabled = enable; }
    public void OnSpawned() => ResetData();
    public void OnDespawned() => ResetData();
    public void Release() => OnDespawned();

    private void ResetData(bool activatePhysics = true)
    {
        KillAllMovementTweens();
        Slot = null;
        IsDockAnimationCompleted = false;
        IsCompacting = false;
        transform.localScale = startScale == Vector3.zero ? Vector3.one : startScale;
        ResetMass();
        if (activatePhysics)
        {
            EnablePhysics();
            SetColliders(true);
        }
        else
        {
            DisablePhysics();
            SetColliders(false);
        }
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

    public void MoveToBoxArc(DockSlot targetSlot, float duration, float arcHeight, Action onComplete = null)
    {
        if (targetSlot == null) return;
        KillAllMovementTweens();
        SetColliders(false);
        EnableKinematicPhysics();
        Vector3 target = targetSlot.transform.position;
        transform.DOKill();
        transform.DOJump(target, Mathf.Max(0f, arcHeight), 1, Mathf.Max(0.01f, duration))
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                targetSlot.SetBall(this);
                onComplete?.Invoke();
            });
    }

    private void EnableKinematicPhysics()
    {
        if (body == null) return;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
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
