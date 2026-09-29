using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BallDropParty.Gameplay
{
    public class Tray : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TrayView _view;
        [SerializeField] private TrayBallReceiver _ballReceiver;
        [SerializeField] private TrayModifierView _modifierView;

        [Header("Settings")]
        [SerializeField] private float _moveDuration = 0.1f;
        [SerializeField] private float _completeDuration = 0.18f;
        [SerializeField] private Vector3 _completeOffsetPosition = new(0f, 0.3f, -0.5f);
        [SerializeField] private string _ballPoolId = "ball";
        [SerializeField] private GameObject _ballFallbackPrefab;

        [Header("Runtime State")]
        [SerializeField] private ColorId _colorId = ColorId.Blue;
        [SerializeField] private int _capacity = 3;

        private readonly List<BallItem> _balls = new();
        private readonly Queue<(BallItem ball, float duration)> _incomingBalls = new();
        private bool _processingIncomingBalls;
        private bool _isCompleted = false;

        public TrayView View => _view;
        public TrayBallReceiver BallReceiver => _ballReceiver;
        public TrayModifierView ModifierView => _modifierView;
        public ColorId ColorId => _colorId;
        public int Capacity => Mathf.Max(1, _capacity);
        public int CurrentBallCount => _balls.Count;
        // Incoming balls already occupy capacity, even while their DOTween
        // animation is still running.
        public bool IsFull => _balls.Count + _incomingBalls.Count >= Capacity;
        public bool IsCompleted => _isCompleted;

        public Action<Tray> OnTrayFull;

        private void Awake()
        {
            if (_view == null) _view = GetComponentInChildren<TrayView>();
            if (_ballReceiver == null) _ballReceiver = GetComponentInChildren<TrayBallReceiver>();
            if (_modifierView == null) _modifierView = GetComponentInChildren<TrayModifierView>();
        }

        public void Init(ColorId colorId, int capacity = 3, GameColorConfig colorConfig = null)
        {
            _colorId = colorId;
            _capacity = capacity;
            _balls.Clear();
            _isCompleted = false;

            if (_view == null) _view = GetComponentInChildren<TrayView>();
            if (_view != null)
            {
                _view.ResetLayerVisuals();
                _view.SetActiveVisual(true, false);
            }

            ApplyColor(colorConfig);
        }

        public void ApplyColor(GameColorConfig colorConfig = null)
        {
            var entry = GameColorConfigProvider.Get(_colorId);
            if (entry != null && _view != null)
            {
                if (entry.trayMaterial != null)
                {
                    _view.ApplyMaterial(entry.trayMaterial);
                }
            }
        }

        public bool CanAcceptBall(BallItem ball)
        {
            if (ball == null || _balls.Count + _incomingBalls.Count >= Capacity || _isCompleted) return false;
            if (_colorId != ColorId.None && ball.colorId != _colorId && ball.colorId != ColorId.Wild)
                return false;

            return true;
        }

        public bool TryAcceptBall(BallItem ball, float moveDuration = 0.2f)
        {
            if (!CanAcceptBall(ball)) return false;

            // Claim ownership immediately, not after the receive animation.
            // This prevents the conveyor from continuing to treat this ball as
            // active during the DOTween delay.
            ball.isCollected = true;
            ball.isOnConveyor = false;
            _incomingBalls.Enqueue((ball, moveDuration));
            if (!_processingIncomingBalls)
                StartCoroutine(ProcessIncomingBalls());
            return true;
        }

        private IEnumerator ProcessIncomingBalls()
        {
            _processingIncomingBalls = true;
            while (_incomingBalls.Count > 0)
            {
                var incoming = _incomingBalls.Dequeue();
                BallItem ball = incoming.ball;
                if (ball == null) continue;

                int slotIdx = _balls.Count;
                _balls.Add(ball);
                ball.isCollected = true;
                Transform slot = GetSlotTransform(slotIdx);
                bool completed = false;
                Vector3 targetPos = slot != null ? slot.position : transform.position;

                if (_ballReceiver != null)
                    _ballReceiver.ReceiveBall(ball, slot, () => completed = true);
                else
                    ball.MoveToPosition(targetPos, incoming.duration, () => completed = true);

                yield return new WaitUntil(() => completed);
                OnBallReceived(ball, slotIdx);
            }
            _processingIncomingBalls = false;
        }

        private void OnBallReceived(BallItem ball, int slotIdx)
        {
            SoundManager.Instance?.PlayBallFeedTray();

            if (_view != null)
            {
                _view.FillSlot(0, slotIdx, ball);
            }
            else
            {
                Transform slot = GetSlotTransform(slotIdx);
                ball.transform.SetParent(slot != null ? slot : transform, true);
                ball.transform.localPosition = Vector3.zero;
                ball.transform.localRotation = Quaternion.identity;
                ball.transform.localScale = Vector3.one;
            }

            if (_balls.Count >= Capacity && !_isCompleted)
            {
                _isCompleted = true;
                _view?.PlayLayerComplete(0);
                SoundManager.Instance?.PlayTrayComplete();
                OnTrayFull?.Invoke(this);
            }
        }

        public Transform GetSlotTransform(int slotIndex)
        {
            if (_view != null)
            {
                return _view.GetFillSlot(0, slotIndex);
            }
            return transform;
        }

        public void AnimateCompleteAndExit(float duration = 0.4f, Action onComplete = null)
        {
            Vector3 exitPos = transform.position + _completeOffsetPosition + Vector3.back * 3.0f;
            transform.DOMove(exitPos, duration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    ReleaseBallsToPool();
                    onComplete?.Invoke();
                    Destroy(gameObject);
                });
        }

        public void ReleaseBallsToPool()
        {
            for (int i = _balls.Count - 1; i >= 0; i--)
            {
                BallItem ball = _balls[i];
                if (ball == null) continue;

                if (GameplayObjectPool.Instance != null)
                    GameplayObjectPool.Instance.Release(ball.gameObject);
                else
                    Destroy(ball.gameObject);
            }
            _balls.Clear();
        }
    }
}
