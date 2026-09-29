using System;
using DG.Tweening;
using UnityEngine;

namespace BallDropParty.Gameplay
{
    public sealed class TrayBallReceiver : MonoBehaviour
    {
        [SerializeField] private float _acceptBallDuration = 0.2f;
        [SerializeField] private Ease easeMove = Ease.InOutQuad;
        [SerializeField] private float _acceptBallJumpPower = 0.1f;

        public float AcceptBallDuration => _acceptBallDuration;
        public Ease EaseMove => easeMove;

        public void ReceiveBall(BallItem ball, Transform slot, Action onComplete = null)
        {
            if (ball == null)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 endPos = slot != null ? slot.position : transform.position;

            if (_acceptBallDuration <= 0f)
            {
                ball.transform.position = endPos;
                onComplete?.Invoke();
                return;
            }

            Vector3 startPos = ball.transform.position;
            Vector3 midPos = (startPos + endPos) * 0.5f;
            midPos.y += _acceptBallJumpPower;

            DOTween.Sequence()
                .Append(ball.transform.DOPath(new[] { midPos, endPos }, _acceptBallDuration, PathType.CatmullRom).SetEase(easeMove))
                .Join(ball.transform.DOScale(Vector3.one, _acceptBallDuration).SetEase(Ease.OutQuad))
                .OnComplete(() =>
                {
                    ball.transform.position = endPos;
                    onComplete?.Invoke();
                });
        }

        public void ReceiveBall(BallController ball, Transform slot, Action onComplete = null)
        {
            if (ball == null)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 endPos = slot != null ? slot.position : transform.position;

            if (_acceptBallDuration <= 0f)
            {
                ball.transform.position = endPos;
                onComplete?.Invoke();
                return;
            }

            Vector3 startPos = ball.transform.position;
            Vector3 midPos = (startPos + endPos) * 0.5f;
            midPos.y += _acceptBallJumpPower;

            DOTween.Sequence()
                .Append(ball.transform.DOPath(new[] { midPos, endPos }, _acceptBallDuration, PathType.CatmullRom).SetEase(easeMove))
                .Join(ball.transform.DOScale(Vector3.one, _acceptBallDuration).SetEase(Ease.OutQuad))
                .OnComplete(() =>
                {
                    ball.transform.position = endPos;
                    onComplete?.Invoke();
                });
        }
    }
}
