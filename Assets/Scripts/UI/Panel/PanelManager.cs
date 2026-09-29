using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace BusBallJam.UI
{
    public class PanelManager : MonoBehaviour
    {
        public static PanelManager Instance { get; private set; }

        [Header("Canvas & Root")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasScaler _canvasScaler;
        [SerializeField] private GraphicRaycaster _raycaster;
        [SerializeField] private CanvasGroup _dimBackground;

        [Header("Pre-registered Panels (Optional)")]
        [SerializeField] private List<Panel> _registeredPanels = new();

        private readonly List<Panel> _openStack = new();

        public Canvas Canvas => _canvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureCanvasHierarchy();
        }

        private void EnsureCanvasHierarchy()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInChildren<Canvas>(true);
                if (_canvas == null)
                {
                    _canvas = GetComponent<Canvas>();
                }
                if (_canvas == null)
                {
                    var canvasGo = new GameObject("UICanvas");
                    canvasGo.transform.SetParent(transform, false);
                    _canvas = canvasGo.AddComponent<Canvas>();
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _canvas.sortingOrder = 999;

                    _canvasScaler = canvasGo.AddComponent<CanvasScaler>();
                    _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    _canvasScaler.referenceResolution = new Vector2(1080, 1920);
                    _canvasScaler.matchWidthOrHeight = 0.5f;

                    _raycaster = canvasGo.AddComponent<GraphicRaycaster>();
                }
            }

            // Ensure EventSystem
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Ensure Dim Background
            if (_dimBackground == null)
            {
                var dimGo = transform.Find("DimBackground")?.gameObject;
                if (dimGo == null && _canvas != null)
                {
                    dimGo = _canvas.transform.Find("DimBackground")?.gameObject;
                }

                if (dimGo == null)
                {
                    dimGo = new GameObject("DimBackground");
                    dimGo.transform.SetParent(_canvas.transform, false);
                    dimGo.transform.SetAsFirstSibling();

                    var img = dimGo.AddComponent<Image>();
                    img.color = new Color(0f, 0f, 0f, 0.75f);
                    var rt = dimGo.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    _dimBackground = dimGo.AddComponent<CanvasGroup>();
                }
                else
                {
                    _dimBackground = dimGo.GetComponent<CanvasGroup>() ?? dimGo.AddComponent<CanvasGroup>();
                }
                _dimBackground.alpha = 0f;
                dimGo.SetActive(false);
            }
        }

        public T OpenPanel<T>(Action onClose = null) where T : Panel
        {
            EnsureCanvasHierarchy();

            T panel = GetOrCreatePanel<T>();
            if (panel == null)
            {
                Debug.LogError($"[PanelManager] Could not get or create panel of type {typeof(T).Name}");
                return null;
            }

            panel.transform.SetAsLastSibling();
            panel.OnCloseCallback = onClose;

            if (!_openStack.Contains(panel))
                _openStack.Add(panel);

            UpdateDimBackground();
            panel.Open();
            return panel;
        }

        public void ClosePanel<T>() where T : Panel
        {
            T panel = FindOpenPanel<T>();
            if (panel != null)
            {
                panel.Close();
            }
        }

        public void CloseCurrentPanel()
        {
            if (_openStack.Count > 0)
            {
                var top = _openStack[_openStack.Count - 1];
                top.Close();
            }
        }

        public void CloseAllPanel()
        {
            for (int i = _openStack.Count - 1; i >= 0; i--)
            {
                if (_openStack[i] != null)
                    _openStack[i].Close();
            }
            _openStack.Clear();
            UpdateDimBackground();
        }

        public void ReleasePanel(Panel panel)
        {
            if (panel != null)
            {
                _openStack.Remove(panel);
            }
            UpdateDimBackground();
        }

        private T FindOpenPanel<T>() where T : Panel
        {
            foreach (var p in _openStack)
            {
                if (p is T typed) return typed;
            }
            return null;
        }

        private T GetOrCreatePanel<T>() where T : Panel
        {
            // 1. Look in registered panels
            foreach (var p in _registeredPanels)
            {
                if (p is T typed) return typed;
            }

            // 2. Look in children of Canvas
            var existing = _canvas.GetComponentInChildren<T>(true);
            if (existing != null)
            {
                if (!_registeredPanels.Contains(existing))
                    _registeredPanels.Add(existing);
                return existing;
            }

            // 3. Load from Resources
            var prefab = Resources.Load<T>($"UI/Panel/{typeof(T).Name}") ?? Resources.Load<T>($"{typeof(T).Name}");
            if (prefab != null)
            {
                var instance = Instantiate(prefab, _canvas.transform, false);
                _registeredPanels.Add(instance);
                return instance;
            }

            // 4. Create procedural fallback
            var newGo = new GameObject(typeof(T).Name);
            newGo.transform.SetParent(_canvas.transform, false);
            var newPanel = newGo.AddComponent<T>();
            _registeredPanels.Add(newPanel);
            return newPanel;
        }

        private void UpdateDimBackground()
        {
            if (_dimBackground == null) return;

            bool shouldShow = _openStack.Count > 0;
            _dimBackground.DOKill();
            if (shouldShow)
            {
                _dimBackground.gameObject.SetActive(true);
                _dimBackground.DOFade(1f, 0.25f).SetUpdate(true);
            }
            else
            {
                _dimBackground.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() => _dimBackground.gameObject.SetActive(false));
            }
        }
    }
}
