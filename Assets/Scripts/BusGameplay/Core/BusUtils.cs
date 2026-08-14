using UnityEngine;

public static class BusUtils
{
    public static int GetBallCapacity(BusType type) => BusData.GetBallCapacity(type);

    public static Vector3 GetForward(Transform bus)
    {
        return bus == null ? Vector3.forward : bus.forward;
    }

    public static HitDirection GetHitDirection(Transform attacker, Collider victim)
    {
        if (attacker == null || victim == null) return HitDirection.Front;

        Vector3 localDirection = attacker.InverseTransformDirection(victim.bounds.center - attacker.position);
        if (Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.z))
            return localDirection.x < 0f ? HitDirection.Left : HitDirection.Right;

        return localDirection.z < 0f ? HitDirection.Back : HitDirection.Front;
    }
}
