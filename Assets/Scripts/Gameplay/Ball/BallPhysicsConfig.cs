using UnityEngine;

[CreateAssetMenu(fileName = "BallPhysicsConfig", menuName = "BusBallJam/Ball Physics Config")]
public class BallPhysicsConfig : ScriptableObject
{
    [Header("Rigidbody")]
    [Min(0.01f)] public float mass = 1f;
    [Min(0f)] public float linearDamping = 0.08f;
    [Min(0f)] public float angularDamping = 0.05f;
    [Min(0f)] public float maxDepenetrationVelocity = 8f;
    public bool useGravity = true;
    [Min(0.1f)] public float maxLinearVelocity = 25f;
    [Min(0.1f)] public float maxAngularVelocity = 25f;

    [Header("Constraints")]
    public RigidbodyConstraints constraints = RigidbodyConstraints.FreezePositionZ;

    [Header("Collider")]
    public PhysicsMaterial physicMaterial;

    public void ApplyTo(Rigidbody body)
    {
        if (body == null) return;
        body.isKinematic = false;
        body.useGravity = useGravity;
        body.mass = mass;
        body.linearDamping = linearDamping;
        body.angularDamping = angularDamping;
        body.maxDepenetrationVelocity = maxDepenetrationVelocity;
        body.maxLinearVelocity = maxLinearVelocity;
        body.maxAngularVelocity = maxAngularVelocity;
        body.constraints = constraints;

        var collider = body.GetComponent<Collider>();
        if (collider != null && physicMaterial != null)
            collider.material = physicMaterial;
    }
}
