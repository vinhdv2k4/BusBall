using UnityEngine;

namespace BallDropParty.Gameplay
{
    public class BallPhysicsController : MonoBehaviour
    {
        [SerializeField] private BallController _ball;

        public BallController Ball => _ball;

        private void Awake()
        {
            if (_ball == null) _ball = GetComponent<BallController>();
        }

        public void EnablePhysics()
        {
            _ball?.EnablePhysics();
        }

        public void DisablePhysics()
        {
            _ball?.DisablePhysics();
        }
    }
}
