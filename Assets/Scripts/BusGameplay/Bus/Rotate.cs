using UnityEngine;

public class Rotate : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private bool rotateOnY = true;

    public void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;
        if (rotateOnY) direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    public void FaceHitDirection(HitDirection direction)
    {
        Vector3 localDirection = direction switch
        {
            HitDirection.Left => Vector3.left,
            HitDirection.Right => Vector3.right,
            HitDirection.Back => Vector3.back,
            _ => Vector3.forward
        };
        FaceDirection(transform.TransformDirection(localDirection));
    }
}
