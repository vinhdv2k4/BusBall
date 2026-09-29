using UnityEngine;
using BallDropParty.Gameplay;

public class TargetDrop : MonoBehaviour
{
    public static TargetDrop Instance { get; private set; }

    [Header("Target Location")]
    public Transform targetTransform;
    [Tooltip("Góc xoay X mong muốn của xe khi đến điểm Target")]
    public float targetAngleX = 180f;
    public bool useTargetTransformRotation = true;

    [Header("Conveyor")]
    public ConveyorManager conveyor;
    public Transform conveyorTransform;

    [Header("Exit")]
    public Transform exitTransform;

    public Transform ConveyorPoint => conveyor != null ? conveyor.transform : conveyorTransform;
    public Transform TargetPoint => targetTransform != null ? targetTransform : transform;
    public Vector3 TargetPosition => targetTransform != null ? targetTransform.position : transform.position;
    public Quaternion TargetRotation => targetTransform != null ? targetTransform.rotation : transform.rotation;
    public float TargetAngleX => useTargetTransformRotation && targetTransform != null
        ? targetTransform.eulerAngles.x
        : targetAngleX;
    public Vector3 ConveyorPosition => ConveyorPoint != null ? ConveyorPoint.position : transform.position;
    public Vector3 ExitPosition => exitTransform != null ? exitTransform.position : transform.position + transform.right * 5f;

    public bool HasTarget => targetTransform != null;
    public bool HasConveyor => conveyor != null || conveyorTransform != null;
    public bool HasExit => exitTransform != null;
    public ConveyorManager Conveyor => conveyor != null ? conveyor : conveyorTransform != null ? conveyorTransform.GetComponentInParent<ConveyorManager>() : null;

    private void Awake()
    {
        Instance = this;

        if (conveyor == null)
            conveyor = FindAnyObjectByType<ConveyorManager>();
    }

    public void OnCarArrived(Car car)
    {
        if (car == null) return;
        Debug.Log($"TargetDrop: Car {car.name} arrived at drop point.");
    }
}
