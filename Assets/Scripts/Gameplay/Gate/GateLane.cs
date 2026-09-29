using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace BallDropParty.Gameplay
{
    public class GateLane : MonoBehaviour
    {
        [SerializeField] private List<GateSlot> _slots = new();
        [SerializeField] private float _shiftStaggerDelay = 0.05f;
        [SerializeField] private GateLaneView _view;
        [SerializeField] private Transform _boosterAimPoint;
        [SerializeField] private Transform _topAnchorPoint;

        private readonly Queue<ColorId> _pendingTrayColors = new();
        private readonly List<Tray> _spawnedTrays = new();
        private Tray _currentFrontTray;
        private TrayFactory _factory;
        private int _gateIndex = 0;
        private bool _isCompleted = false;

        public int GateIndex { get => _gateIndex; set => _gateIndex = value; }
        public Tray CurrentFrontTray => _currentFrontTray;
        public IReadOnlyList<GateSlot> Slots => _slots;
        public IReadOnlyList<Tray> SpawnedTrays => _spawnedTrays;
        public bool IsCompleted => _isCompleted;

        public System.Action<GateLane> OnLaneCompleted;

        private void Awake()
        {
            if (_view == null) _view = GetComponentInChildren<GateLaneView>(true);

            if (_slots == null || _slots.Count == 0)
            {
                var found = GetComponentsInChildren<GateSlot>(true);
                _slots = new List<GateSlot>(found);
            }

            if (_boosterAimPoint == null)
            {
                var aim = transform.Find("SpawnPoint");
                if (aim != null) _boosterAimPoint = aim;
            }

            if (_topAnchorPoint == null)
            {
                var top = transform.Find("TopAnchorPoint");
                if (top != null) _topAnchorPoint = top;
            }
        }

        [ContextMenu("Find Gate Slots")]
        public void FindGateSlots()
        {
            _slots.Clear();
            var found = GetComponentsInChildren<GateSlot>(true);
            _slots.AddRange(found);
        }

        public void Init(int gateIndex, List<ColorId> trayColors, TrayFactory factory)
        {
            _gateIndex = gateIndex;
            _factory = factory;
            _pendingTrayColors.Clear();
            _isCompleted = false;

            ClearTrays();

            if (trayColors != null)
            {
                foreach (var c in trayColors)
                {
                    _pendingTrayColors.Enqueue(c);
                }
            }

            SpawnVisibleTrays(_slots.Count > 0 ? _slots.Count : 4);
        }

        public void ClearTrays()
        {
            foreach (var tray in _spawnedTrays)
            {
                if (tray != null)
                {
                    tray.ReleaseBallsToPool();
                    Destroy(tray.gameObject);
                }
            }
            _spawnedTrays.Clear();
            _currentFrontTray = null;
        }

        private void SpawnVisibleTrays(int maxVisible = 4)
        {
            while (_spawnedTrays.Count < maxVisible && _pendingTrayColors.Count > 0)
            {
                var color = _pendingTrayColors.Dequeue();
                SpawnTray(color);
            }

            UpdateTrayPositions(false);
        }

        private Tray SpawnTray(ColorId colorId)
        {
            Tray tray = null;
            if (_factory != null)
            {
                tray = _factory.CreateTray(colorId, transform);
            }
            else
            {
                var trayObj = new GameObject($"Tray_{colorId}");
                trayObj.transform.SetParent(transform);
                tray = trayObj.AddComponent<Tray>();
                tray.Init(colorId);
            }

            if (tray != null)
            {
                tray.OnTrayFull = HandleTrayFull;
                _spawnedTrays.Add(tray);
            }

            return tray;
        }

        private void HandleTrayFull(Tray tray)
        {
            if (tray != _currentFrontTray) return;

            tray.AnimateCompleteAndExit(0.35f, () =>
            {
                _spawnedTrays.Remove(tray);
                _view?.PlayTrayCompleted(tray.transform.position);

                if (_pendingTrayColors.Count > 0)
                {
                    var nextColor = _pendingTrayColors.Dequeue();
                    SpawnTray(nextColor);
                }

                UpdateTrayPositions(true);

                if (_spawnedTrays.Count == 0 && _pendingTrayColors.Count == 0)
                {
                    _isCompleted = true;
                    OnLaneCompleted?.Invoke(this);
                }
            });
        }

        private void UpdateTrayPositions(bool animate = true)
        {
            for (int i = 0; i < _spawnedTrays.Count; i++)
            {
                var tray = _spawnedTrays[i];
                if (tray == null) continue;

                Vector3 targetPos;
                Quaternion targetRot;

                if (i < _slots.Count && _slots[i] != null)
                {
                    targetPos = _slots[i].Position;
                    targetRot = _slots[i].Rotation;
                }
                else
                {
                    targetPos = transform.position + transform.forward * (i * 1.5f);
                    targetRot = transform.rotation;
                }

                if (animate)
                {
                    tray.transform.DOKill();
                    tray.transform.DOMove(targetPos, 0.25f).SetEase(Ease.OutQuad).SetDelay(i * _shiftStaggerDelay);
                    tray.transform.DORotateQuaternion(targetRot, 0.25f).SetEase(Ease.OutQuad);
                }
                else
                {
                    tray.transform.position = targetPos;
                    tray.transform.rotation = targetRot;
                }
            }

            _currentFrontTray = _spawnedTrays.Count > 0 ? _spawnedTrays[0] : null;
        }
    }
}
