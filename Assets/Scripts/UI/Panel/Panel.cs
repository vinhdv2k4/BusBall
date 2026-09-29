using System;
using DG.Tweening;
using UnityEngine;

namespace BusBallJam.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class Panel : MonoBehaviour
    {
        [Header("Panel Animation")]
        [SerializeField] protected CanvasGroup panelCanvasGroup;
        [SerializeField] protected Transform contentRoot;
        [SerializeField] protected float openDuration = 0.35f;
        [SerializeField] protected float closeDuration = 0.2f;

        public bool IsOpen { get; protected set; }
        public Action OnCloseCallback { get; set; }

        protected virtual void Awake()
        {
            if (panelCanvasGroup == null) panelCanvasGroup = GetComponent<CanvasGroup>();
            if (contentRoot == null) contentRoot = transform.Find("Content") ?? transform;
        }

        public virtual void OnSetup() { }

        public virtual void Open()
        {
            IsOpen = true;
            gameObject.SetActive(true);
            OnSetup();
            PlayOpenAnimation(OnOpenCompleted);
        }

        public virtual void OnOpenCompleted() { }

        public virtual void Close()
        {
            PlayCloseAnimation(() =>
            {
                OnCloseCompleted();
            });
        }

        protected virtual void OnCloseCompleted()
        {
            IsOpen = false;
            gameObject.SetActive(false);
            OnCloseCallback?.Invoke();
            PanelManager.Instance?.ReleasePanel(this);
        }

        protected virtual void PlayOpenAnimation(Action onComplete)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.DOFade(1f, openDuration).SetUpdate(true);
            }

            if (contentRoot != null)
            {
                contentRoot.DOKill();
                contentRoot.localScale = Vector3.one * 0.4f;
                contentRoot.DOScale(1f, openDuration).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        protected virtual void PlayCloseAnimation(Action onComplete)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.DOFade(0f, closeDuration).SetUpdate(true);
            }

            if (contentRoot != null)
            {
                contentRoot.DOKill();
                contentRoot.DOScale(0.7f, closeDuration).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }
    }
}
