using UnityEngine;
using UnityEngine.Rendering;
using BallDropParty.Gameplay;
using BusBallJam.UI;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] private CarSplineRouteDriver _roadMap;
    [SerializeField] private TargetDrop _targetDrop;
    [SerializeField] private BallDropper _ballDropper;
    [SerializeField] private ParkingLotManager _parkingLotManager;
    [SerializeField] private ConveyorManager _conveyorManager;
    [SerializeField] private GateManager _gateManager;
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private PanelManager _panelManager;

    public CarSplineRouteDriver RoadMap => _roadMap != null ? _roadMap : (_roadMap = FindAnyObjectByType<CarSplineRouteDriver>(FindObjectsInactive.Include));
    public TargetDrop TargetDrop => _targetDrop != null ? _targetDrop : (_targetDrop = FindAnyObjectByType<TargetDrop>(FindObjectsInactive.Include));
    public BallDropper BallDropper => _ballDropper != null ? _ballDropper : (_ballDropper = FindAnyObjectByType<BallDropper>(FindObjectsInactive.Include));
    public ParkingLotManager ParkingLotManager => _parkingLotManager != null ? _parkingLotManager : (_parkingLotManager = FindAnyObjectByType<ParkingLotManager>(FindObjectsInactive.Include));
    public ConveyorManager ConveyorManager => _conveyorManager != null ? _conveyorManager : (_conveyorManager = FindAnyObjectByType<ConveyorManager>(FindObjectsInactive.Include));
    public GateManager GateManager => _gateManager != null ? _gateManager : (_gateManager = FindAnyObjectByType<GateManager>(FindObjectsInactive.Include));
    public LevelManager LevelManager => _levelManager != null ? _levelManager : (_levelManager = FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include));
    public PanelManager PanelManager => _panelManager != null ? _panelManager : (_panelManager = FindAnyObjectByType<PanelManager>(FindObjectsInactive.Include));

    private void Awake()
    {
        // Tối ưu hóa FPS mượt mà cho thiết bị di động (mặc định Unity giới hạn ở 30 FPS)
        Application.targetFrameRate = 60;

        DebugManager.instance.displayRuntimeUI = false;
        DebugManager.instance.displayPersistentRuntimeUI = false;
        DebugManager.instance.enableRuntimeUI = false;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureReferences();
    }

    public void EnsureReferences()
    {
        if (_roadMap == null) _roadMap = FindAnyObjectByType<CarSplineRouteDriver>(FindObjectsInactive.Include);
        if (_targetDrop == null) _targetDrop = FindAnyObjectByType<TargetDrop>(FindObjectsInactive.Include);
        if (_ballDropper == null) _ballDropper = FindAnyObjectByType<BallDropper>(FindObjectsInactive.Include);
        if (_parkingLotManager == null) _parkingLotManager = FindAnyObjectByType<ParkingLotManager>(FindObjectsInactive.Include);
        if (_conveyorManager == null) _conveyorManager = FindAnyObjectByType<ConveyorManager>(FindObjectsInactive.Include);
        if (_gateManager == null) _gateManager = FindAnyObjectByType<GateManager>(FindObjectsInactive.Include);
        if (_levelManager == null) _levelManager = FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (_panelManager == null) _panelManager = FindAnyObjectByType<PanelManager>(FindObjectsInactive.Include);

        // Đảm bảo RoadMap luôn active khi chạy
        if (_roadMap != null && !_roadMap.gameObject.activeSelf)
        {
            _roadMap.gameObject.SetActive(true);
        }
    }
}
