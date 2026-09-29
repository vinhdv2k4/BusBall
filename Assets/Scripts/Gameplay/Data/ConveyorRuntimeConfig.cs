using System;
using System.Collections.Generic;
using UnityEngine;

namespace BallDropParty.Gameplay
{
    [Serializable]
    public class TrayReceiveZoneData
    {
        public int gateIndex;
        [Range(0f, 1f)]
        public float progress = 0.5f;
        public float radius = 0.55f;
        public int priority = 0;
    }

    [CreateAssetMenu(fileName = "ConveyorRuntimeConfig", menuName = "BusBallJam/Conveyor Runtime Config")]
    public class ConveyorRuntimeConfig : ScriptableObject
    {
        [SerializeField] private bool _reverseDirection = false;
        [SerializeField] private int _slotCapacity = 30;
        [SerializeField] private int _splineBakeResolution = 256;

        [Header("Speed Config")]
        [SerializeField, Tooltip("Toc do quay cua conveyor visual (m/s)")]
        private float _speedConveyor = 2.2f;

        [SerializeField, Tooltip("Toc do di chuyen cua bong theo conveyor (m/s)")]
        private float _speedBallFollow = 3.6f;

        [SerializeField, Tooltip("Toc do luot slot cua bong (m/s)")]
        private float _speedBallShift = 16f;

        [SerializeField, Tooltip("He so tang toc khi hoan thanh")]
        private float _speedBallEndMultiplier = 1.3f;

        [Header("Drop & Entry")]
        [Range(0f, 1f)]
        [SerializeField, Tooltip("Vi tri bong roi tu phễu xuong conveyor (0..1)")]
        private float _entryProgress = 0.173f;

        [SerializeField, Tooltip("Khoang cach toi thieu giua 2 bong tren conveyor")]
        private float _minBallSpacing = 0.1f;

        [Header("Receive Zones")]
        [SerializeField] private List<TrayReceiveZoneData> _receiveZones = new()
        {
            new TrayReceiveZoneData { gateIndex = 0, progress = 0.852f, radius = 0.55f, priority = 0 },
            new TrayReceiveZoneData { gateIndex = 1, progress = 0.741f, radius = 0.55f, priority = 1 },
            new TrayReceiveZoneData { gateIndex = 2, progress = 0.628f, radius = 0.55f, priority = 2 },
            new TrayReceiveZoneData { gateIndex = 3, progress = 0.518f, radius = 0.55f, priority = 3 }
        };

        public bool ReverseDirection => _reverseDirection;
        public int SlotCapacity => Mathf.Max(1, _slotCapacity);
        public int SplineBakeResolution => Mathf.Max(8, _splineBakeResolution);
        public float SpeedConveyor => Mathf.Max(0f, _speedConveyor);
        public float SpeedBallFollow => Mathf.Max(0f, _speedBallFollow);
        public float SpeedBallShift => Mathf.Max(0f, _speedBallShift);
        public float SpeedBallEndMultiplier => Mathf.Max(0f, _speedBallEndMultiplier);
        public float EntryProgress => Mathf.Repeat(_entryProgress, 1f);
        public float MinBallSpacing => Mathf.Max(0f, _minBallSpacing);
        public IReadOnlyList<TrayReceiveZoneData> ReceiveZones => _receiveZones;
    }
}
