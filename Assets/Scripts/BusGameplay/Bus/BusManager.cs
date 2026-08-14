using UnityEngine;

public class BusManager : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0f)] private float enterDistance = 0.5f;
    [SerializeField, Min(0f)] private float conveyorSpeedWhenNoBus = 1.75f;
    public float MoveSpeed => moveSpeed;
    public float EnterDistance => enterDistance;
    public float ConveyorSpeedWhenNoBus => conveyorSpeedWhenNoBus;

}
