using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BallDropParty.Gameplay
{
    public sealed class TrayLayerView : MonoBehaviour
    {
        [SerializeField] private List<TrayFillSlot> _fillSlots = new();
        [SerializeField] private GameObject _completedVisual;
        [SerializeField] private float _lidAnimationDuration = 0.15f;
        [SerializeField] private Ease _lidEase = Ease.OutBack;

        private Vector3 _lidClosedScale;
        private bool _hasCachedLidScale;
        private bool _isActive;
        private bool _isCompleted;
        private Tween _lidTween;

        public IReadOnlyList<TrayFillSlot> FillSlots => _fillSlots;

        private void Awake()
        {
            CacheLidScale();
        }

        public Transform GetFillSlot(int index)
        {
            if (index >= 0 && index < _fillSlots.Count && _fillSlots[index] != null)
            {
                return _fillSlots[index].transform;
            }

            return transform;
        }

        public void FillSlot(int index, BallController ball)
        {
            if (index < 0 || index >= _fillSlots.Count || _fillSlots[index] == null) return;
            _fillSlots[index].Fill(ball);
        }

        public void FillSlot(int index, BallItem ball)
        {
            if (index < 0 || index >= _fillSlots.Count || _fillSlots[index] == null) return;
            _fillSlots[index].Fill(ball);
        }

        public void ResetVisual()
        {
            _isActive = false;
            _isCompleted = false;
            SetLidClosed(true, false);
        }

        public void SetCompleted(bool completed, bool animate)
        {
            _isCompleted = completed;
            SetLidClosed(ShouldCloseLid(), animate);
        }

        public void SetActiveVisual(bool active, bool animate)
        {
            _isActive = active;
            SetLidClosed(ShouldCloseLid(), animate);
        }

        private bool ShouldCloseLid()
        {
            return _isCompleted || !_isActive;
        }

        private void SetLidClosed(bool closed, bool animate)
        {
            if (_completedVisual == null) return;

            CacheLidScale();
            _lidTween?.Kill();

            if (!animate || _lidAnimationDuration <= 0f)
            {
                ApplyLidState(closed);
                return;
            }

            var lidTransform = _completedVisual.transform;
            if (closed)
            {
                _completedVisual.SetActive(true);
            }

            _lidTween = lidTransform.DOScale(closed ? _lidClosedScale : Vector3.zero, _lidAnimationDuration)
                .SetEase(_lidEase)
                .OnComplete(() =>
                {
                    if (!closed && _completedVisual != null)
                        _completedVisual.SetActive(false);
                });
        }

        private void ApplyLidState(bool closed)
        {
            if (_completedVisual == null) return;
            _completedVisual.transform.localScale = closed ? _lidClosedScale : Vector3.zero;
            _completedVisual.SetActive(closed);
        }

        private void CacheLidScale()
        {
            if (_hasCachedLidScale || _completedVisual == null) return;

            _lidClosedScale = _completedVisual.transform.localScale;
            if (_lidClosedScale == Vector3.zero) _lidClosedScale = Vector3.one;
            _hasCachedLidScale = true;
        }
    }
}
