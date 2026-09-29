using UnityEngine;

namespace BallDropParty.Gameplay
{
    public sealed class TrayFillSlot : MonoBehaviour
    {
        public void Fill(BallController ball)
        {
            if (ball == null) return;

            ball.transform.SetParent(transform, true);
            ball.transform.localPosition = Vector3.zero;
            ball.transform.localRotation = Quaternion.identity;
            ball.transform.localScale = Vector3.one;
        }

        public void Fill(BallItem ball)
        {
            if (ball == null) return;

            ball.transform.SetParent(transform, true);
            ball.transform.localPosition = Vector3.zero;
            ball.transform.localRotation = Quaternion.identity;
            ball.transform.localScale = Vector3.one;
        }

        public void Fill(Transform ballTransform)
        {
            if (ballTransform == null) return;

            ballTransform.SetParent(transform, true);
            ballTransform.localPosition = Vector3.zero;
            ballTransform.localRotation = Quaternion.identity;
            ballTransform.localScale = Vector3.one;
        }
    }
}
