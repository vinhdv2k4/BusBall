using UnityEngine;

namespace BallDropParty.Gameplay
{
    public sealed class GateSlot : MonoBehaviour
    {
        [SerializeField] private Transform _trayPoint;
        [SerializeField] private Transform _spawnPoint;

        public Transform TrayPoint => _trayPoint != null ? _trayPoint : transform;
        public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : TrayPoint;

        public Vector3 Position => TrayPoint.position;
        public Quaternion Rotation => TrayPoint.rotation;
    }
}
