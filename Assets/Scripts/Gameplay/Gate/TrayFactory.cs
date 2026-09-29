using UnityEngine;

namespace BallDropParty.Gameplay
{
    public class TrayFactory : MonoBehaviour
    {
        [SerializeField] private Tray _trayPrefab;

        public Tray CreateTray(ColorId colorId, Transform parent = null)
        {
            if (_trayPrefab == null)
            {
                Debug.LogWarning("TrayFactory: Missing tray prefab!");
                return null;
            }

            var tray = Instantiate(_trayPrefab, parent);
            tray.Init(colorId, 3);
            return tray;
        }
    }
}
