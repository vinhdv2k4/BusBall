using UnityEngine;

[CreateAssetMenu(fileName = "PrefabSO", menuName = "Configs/PrefabSO")]
public class PrefabSO : ScriptableObject
{
    [Header("Gameplay prefabs")]
    [SerializeField] private BallController prfBall;
    [SerializeField] private Bus smallBus;
    [SerializeField] private Bus mediumBus;
    [SerializeField] private Bus largeBus;

    [Header("Future gameplay prefabs")]
    [SerializeField] private GameObject prfBox;
    [SerializeField] private GameObject prfBoxLane;
    [SerializeField] private GameObject garage;
    [SerializeField] private GameObject iceCube;

    public Bus GetBus(BusType type)
    {
        return type switch
        {
            BusType.Small => smallBus,
            BusType.Medium => mediumBus,
            BusType.Large => largeBus,
            _ => mediumBus
        };
    }

    public GameObject GetGarage() => garage;
    public BallController GetBall() => prfBall;
    public GameObject GetBox() => prfBox;
    public GameObject GetBoxLane() => prfBoxLane;
    public GameObject GetIceCube() => iceCube;
}
