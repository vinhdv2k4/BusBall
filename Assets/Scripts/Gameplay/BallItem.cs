using System;
using DG.Tweening;
using UnityEngine;

namespace BallDropParty.Gameplay
{
    public class BallItem : MonoBehaviour
    {
        [Header("State")]
        public ColorId colorId = ColorId.Blue;
        public bool isOnConveyor = false;
        public bool isCollected = false;
        public float conveyorDistance = 0f;
        public float conveyorProgress = 0f;
        public int conveyorSlotIndex = -1;
        [HideInInspector] public bool isEnteringConveyor;
        [HideInInspector] public bool isChasingConveyorPoint;
        [HideInInspector] public float chaseUnlockTime;
        [HideInInspector] public Vector3 visualOffset = Vector3.zero;

        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            ApplyColor();
        }

        public void Init(ColorId newColorId)
        {
            colorId = newColorId;
            isOnConveyor = false;
            isCollected = false;
            conveyorDistance = 0f;
            conveyorProgress = 0f;
            conveyorSlotIndex = -1;
            isEnteringConveyor = false;
            isChasingConveyorPoint = false;
            chaseUnlockTime = 0f;
            visualOffset = Vector3.zero;
            ApplyColor();
        }

        public void ApplyColor()
        {
            if (_renderer == null)
                _renderer = GetComponentInChildren<Renderer>();

            if (_renderer == null) return;

            var entry = GameColorConfigProvider.Get(colorId);
            if (entry != null)
            {
                if (entry.ballMaterial != null)
                {
                    _renderer.material = entry.ballMaterial;
                }
                else
                {
                    var block = new MaterialPropertyBlock();
                    _renderer.GetPropertyBlock(block);
                    block.SetColor("_Color", entry.color);
                    block.SetColor("_BaseColor", entry.color);
                    _renderer.SetPropertyBlock(block);

                    if (_renderer.material != null)
                    {
                        _renderer.material.color = entry.color;
                    }
                }
            }
        }

        public void MoveToPosition(Vector3 targetPosition, Action onComplete = null)
        {
            MoveToPosition(targetPosition, 0.3f, 0.4f, onComplete);
        }

        public void MoveToPosition(Vector3 targetPosition, float duration, Action onComplete = null)
        {
            MoveToPosition(targetPosition, duration, 0.4f, onComplete);
        }

        public void MoveToPosition(Vector3 targetPosition, float duration, float jumpHeight, Action onComplete = null)
        {
            isOnConveyor = false;
            transform.DOKill();

            Vector3 startPos = transform.position;
            Vector3 midPoint = (startPos + targetPosition) * 0.5f + Vector3.up * jumpHeight;

            transform.DOPath(new[] { midPoint, targetPosition }, duration, PathType.CatmullRom)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    transform.position = targetPosition;
                    onComplete?.Invoke();
                });
        }
    }
}
