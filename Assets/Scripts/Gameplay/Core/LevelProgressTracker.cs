using UnityEngine;

namespace BallDropParty.Gameplay
{
    public class LevelProgressTracker : MonoBehaviour
    {
        private int _pendingActionCount;

        public bool HasPendingGameplayAction => _pendingActionCount > 0;
        public int PendingActionCount => _pendingActionCount;

        public void BeginAction()
        {
            _pendingActionCount++;
        }

        public void EndAction()
        {
            _pendingActionCount = Mathf.Max(0, _pendingActionCount - 1);
        }

        public void ResetTracker()
        {
            _pendingActionCount = 0;
        }
    }
}
