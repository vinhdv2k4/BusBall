using UnityEngine;

namespace BallDropParty.Gameplay
{
    public class GateLaneView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _completedVfx;

        public void PlayTrayCompleted(Vector3 position)
        {
            if (_completedVfx != null)
            {
                _completedVfx.transform.position = position;
                _completedVfx.Play();
            }
        }
    }
}
