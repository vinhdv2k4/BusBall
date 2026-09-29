using UnityEngine;

public class CarTargetTrigger : MonoBehaviour
{
    [Header("Target")]
    public Transform targetTransform;

    public Vector3 TargetPosition => targetTransform != null ? targetTransform.position : transform.position;

    public bool HasTarget => targetTransform != null;
}
