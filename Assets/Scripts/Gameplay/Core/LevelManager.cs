using System.Collections.Generic;
using UnityEngine;
using BusBallJam.UI;

namespace BallDropParty.Gameplay
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Level Data")]
        [SerializeField] private int _levelId = 1;
        [SerializeField] private bool _loadOnStart = true;
        [SerializeField] private TextAsset _levelJson;

        [Header("Runtime Modules")]
        [SerializeField] private ParkingLotManager _parkingLotManager;
        [SerializeField] private ConveyorManager _conveyorManager;
        [SerializeField] private GateManager _gateManager;
        [SerializeField] private TargetDrop _targetDrop;
        [SerializeField] private GameColorConfig _colorConfig;
        [SerializeField] private LevelProgressTracker _progressTracker;

        private LevelData _currentLevelData;
        private bool _isLevelCompleted = false;

        public int CurrentLevelId => _levelId;
        public LevelData CurrentLevelData => _currentLevelData;
        public bool IsLevelCompleted => _isLevelCompleted;

        public System.Action<int> OnLevelLoaded;
        public System.Action<int> OnLevelWon;
        public System.Action<int> OnLevelLost;

        private void Awake()
        {
            Instance = this;
            EnsureModules();
        }

        private void Start()
        {
            if (_loadOnStart)
            {
                LoadLevel(_levelId);
            }
        }

        private void EnsureModules()
        {
            if (_parkingLotManager == null)
                _parkingLotManager = FindAnyObjectByType<ParkingLotManager>();

            if (_conveyorManager == null)
                _conveyorManager = FindAnyObjectByType<ConveyorManager>();

            if (_gateManager == null)
                _gateManager = FindAnyObjectByType<GateManager>();

            if (_targetDrop == null)
                _targetDrop = FindAnyObjectByType<TargetDrop>();

            if (_progressTracker == null)
                _progressTracker = GetComponent<LevelProgressTracker>() ?? FindAnyObjectByType<LevelProgressTracker>();

            if (PanelManager.Instance == null)
            {
                var existingPm = FindAnyObjectByType<PanelManager>(FindObjectsInactive.Include);
                if (existingPm == null)
                {
                    var pmGo = new GameObject("PanelManager");
                    pmGo.AddComponent<PanelManager>();
                }
            }

            if (_colorConfig == null)
                _colorConfig = Resources.Load<GameColorConfig>("GameColorConfig");
        }

        [ContextMenu("Load Current Level")]
        public void LoadCurrentLevel()
        {
            LoadLevel(_levelId);
        }

        public void LoadLevel(int levelId)
        {
            _levelId = levelId;
            _isLevelCompleted = false;
            EnsureModules();

            PanelManager.Instance?.CloseAllPanel();

            Debug.Log($"LevelManager: Loading Level {levelId}...");

            _currentLevelData = DataManager.LoadLevel(levelId);

            if ((_currentLevelData == null || _currentLevelData.cars == null || _currentLevelData.cars.Length == 0) && _levelJson != null)
            {
                Debug.Log($"LevelManager: Fallback to inspector _levelJson for Level {levelId}");
                _currentLevelData = JsonUtility.FromJson<LevelData>(_levelJson.text);
            }

            if (_currentLevelData == null)
            {
                Debug.LogWarning($"LevelManager: Failed to load Level {levelId} data!");
                return;
            }

            // 1. Setup Parking Lot Cars
            if (_parkingLotManager != null)
            {
                _parkingLotManager.InitCars(levelId, _currentLevelData);
            }

            // 2. Setup Conveyor & Clear all leftover balls
            if (_conveyorManager != null)
            {
                _conveyorManager.Initialize();
                _conveyorManager.ClearAllBalls();
            }

            var dropper = FindAnyObjectByType<BallDropper>(FindObjectsInactive.Include);
            if (dropper != null)
            {
                dropper.StopAllCoroutines();
            }

            // 3. Setup Gate Lanes and Trays
            if (_gateManager != null)
            {
                SetupGateTraysFromLevelData(_currentLevelData);
                _gateManager.OnAllGatesCompleted = OnLevelCompleted;
            }

            OnLevelLoaded?.Invoke(levelId);
        }

        private void SetupGateTraysFromLevelData(LevelData levelData)
        {
            if (levelData == null || _gateManager == null) return;

            var guests = levelData.GetGuests();
            var cars = levelData.GetCars();

            var trayColorQueue = new List<ColorId>();

            if (guests.Length > 0)
            {
                foreach (var g in guests)
                {
                    int trayCount = Mathf.CeilToInt((float)g.number / 3f);
                    for (int t = 0; t < trayCount; t++)
                    {
                        trayColorQueue.Add(g.eColor);
                    }
                }
            }
            else if (cars.Length > 0)
            {
                foreach (var car in cars)
                {
                    int ballCount = car.type switch
                    {
                        CarType.Small4Slots  => 4,
                        CarType.Medium6Slots => 6,
                        CarType.Big9Slots    => 9,
                        _                   => 4
                    };
                    int trayCount = Mathf.CeilToInt((float)ballCount / 3f);
                    for (int t = 0; t < trayCount; t++)
                    {
                        trayColorQueue.Add(car.eColor);
                    }
                }
            }

            int laneCount = _gateManager.GateCount > 0 ? _gateManager.GateCount : 4;
            var laneTrayColors = new List<List<ColorId>>();
            for (int i = 0; i < laneCount; i++)
            {
                laneTrayColors.Add(new List<ColorId>());
            }

            for (int i = 0; i < trayColorQueue.Count; i++)
            {
                int targetLane = i % laneCount;
                laneTrayColors[targetLane].Add(trayColorQueue[i]);
            }

            _gateManager.Build(laneTrayColors);
        }

        private void OnLevelCompleted()
        {
            if (_isLevelCompleted) return;

            _isLevelCompleted = true;
            Debug.Log($"LevelManager: LEVEL {_levelId} WON! CONGRATULATIONS!");
            SoundManager.Instance?.PlayWin();
            OnLevelWon?.Invoke(_levelId);

            var winPanel = PanelManager.Instance?.OpenPanel<WinPanel>();
            winPanel?.Setup(new WinPanel.Data
            {
                level = _levelId,
                nextLevel = NextLevel
            });
        }

        [ContextMenu("Test Trigger Win")]
        public void TriggerWin()
        {
            OnLevelCompleted();
        }

        [ContextMenu("Test Trigger Lose")]
        public void TriggerLose()
        {
            if (_isLevelCompleted) return;

            _isLevelCompleted = true;
            Debug.Log($"LevelManager: LEVEL {_levelId} FAILED!");
            SoundManager.Instance?.PlayLose();
            OnLevelLost?.Invoke(_levelId);

            var losePanel = PanelManager.Instance?.OpenPanel<LosePanel>();
            losePanel?.Setup(new LosePanel.Data
            {
                level = _levelId,
                retry = RestartLevel
            });
        }

        public void RestartLevel()
        {
            LoadLevel(_levelId);
        }

        public void NextLevel()
        {
            LoadLevel(_levelId + 1);
        }
    }
}
