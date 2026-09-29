using System.Collections.Generic;
using UnityEngine;

namespace BallDropParty.Gameplay
{
    public class GateManager : MonoBehaviour
    {
        [SerializeField] private List<GateLane> _gates = new();
        [SerializeField] private TrayFactory _trayFactory;

        public IReadOnlyList<GateLane> Gates => _gates;
        public int GateCount => _gates != null ? _gates.Count : 0;
        public System.Action OnAllGatesCompleted;

        private void Awake()
        {
            if (_gates == null || _gates.Count == 0)
            {
                var found = GetComponentsInChildren<GateLane>(true);
                _gates = new List<GateLane>(found);
            }

            if (_trayFactory == null)
            {
                _trayFactory = GetComponentInChildren<TrayFactory>(true) ?? FindAnyObjectByType<TrayFactory>();
            }
        }

        [ContextMenu("Find Gate Lanes")]
        public void FindGateLanes()
        {
            _gates.Clear();
            var found = GetComponentsInChildren<GateLane>(true);
            _gates.AddRange(found);
            for (int i = 0; i < _gates.Count; i++)
            {
                if (_gates[i] != null) _gates[i].GateIndex = i;
            }
        }

        public GateLane GetGate(int gateIndex)
        {
            if (_gates != null && gateIndex >= 0 && gateIndex < _gates.Count)
                return _gates[gateIndex];
            return null;
        }

        public void Build(List<List<ColorId>> laneTrayColors)
        {
            InitGates(laneTrayColors);
        }

        public void InitGates(List<List<ColorId>> laneTrayColors)
        {
            if (_gates == null || _gates.Count == 0)
            {
                FindGateLanes();
            }

            for (int i = 0; i < _gates.Count; i++)
            {
                var lane = _gates[i];
                if (lane == null) continue;

                lane.GateIndex = i;
                var colors = (laneTrayColors != null && i < laneTrayColors.Count) ? laneTrayColors[i] : null;
                lane.Init(i, colors, _trayFactory);
                lane.OnLaneCompleted = HandleLaneCompleted;
            }
        }

        private void HandleLaneCompleted(GateLane lane)
        {
            bool allDone = true;
            foreach (var g in _gates)
            {
                if (g != null && !g.IsCompleted)
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone)
            {
                Debug.Log("GateManager: All gates completed!");
                OnAllGatesCompleted?.Invoke();
            }
        }
    }
}
