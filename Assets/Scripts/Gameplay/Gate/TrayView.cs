using System.Collections.Generic;
using UnityEngine;

namespace BallDropParty.Gameplay
{
    public sealed class TrayView : MonoBehaviour
    {
        [SerializeField] private List<TrayLayerView> _layers = new();
        [SerializeField] private List<Renderer> _renderers = new();
        [SerializeField] private Transform _connectionAnchor;

        private Material[] _defaultMaterials;

        public Transform ConnectionAnchor => _connectionAnchor != null ? _connectionAnchor : transform;

        private void Awake()
        {
            EnsureRenderers();
            CacheDefaultMaterials();
        }

        public void ApplyMaterial(Material material)
        {
            EnsureRenderers();
            CacheDefaultMaterials();

            if (material == null)
            {
                ClearMaterial();
                return;
            }

            for (var i = 0; i < _renderers.Count; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null) continue;

                var sharedMaterials = renderer.sharedMaterials;
                if (sharedMaterials == null || sharedMaterials.Length == 0)
                {
                    renderer.sharedMaterials = new[] { material };
                    continue;
                }

                sharedMaterials[0] = material;
                renderer.sharedMaterials = sharedMaterials;
            }
        }

        public void ClearMaterial()
        {
            EnsureRenderers();
            CacheDefaultMaterials();

            for (var i = 0; i < _renderers.Count; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null || _defaultMaterials == null || i >= _defaultMaterials.Length) continue;

                var sharedMaterials = renderer.sharedMaterials;
                if (sharedMaterials == null || sharedMaterials.Length == 0)
                {
                    renderer.sharedMaterials = new[] { _defaultMaterials[i] };
                    continue;
                }

                sharedMaterials[0] = _defaultMaterials[i];
                renderer.sharedMaterials = sharedMaterials;
            }
        }

        private void EnsureRenderers()
        {
            if (_renderers.Count == 0)
            {
                _renderers.AddRange(GetComponentsInChildren<Renderer>());
            }
        }

        private void CacheDefaultMaterials()
        {
            if (_defaultMaterials != null && _defaultMaterials.Length == _renderers.Count) return;

            _defaultMaterials = new Material[_renderers.Count];
            for (var i = 0; i < _renderers.Count; i++)
            {
                _defaultMaterials[i] = _renderers[i] != null
                    ? _renderers[i].sharedMaterial
                    : null;
            }
        }

        public Transform GetFillSlot(int layerIndex, int slotIndex)
        {
            if (layerIndex >= 0 && layerIndex < _layers.Count && _layers[layerIndex] != null)
            {
                var slot = _layers[layerIndex].GetFillSlot(slotIndex);
                if (slot != null) return slot;
            }

            return transform;
        }

        public void FillSlot(int layerIndex, int slotIndex, BallController ball)
        {
            if (layerIndex < 0 || layerIndex >= _layers.Count || _layers[layerIndex] == null) return;
            _layers[layerIndex].FillSlot(slotIndex, ball);
        }

        public void FillSlot(int layerIndex, int slotIndex, BallItem ball)
        {
            if (layerIndex < 0 || layerIndex >= _layers.Count || _layers[layerIndex] == null) return;
            _layers[layerIndex].FillSlot(slotIndex, ball);
        }

        public void PlayLayerComplete(int layerIndex)
        {
            SetLayerCompleted(layerIndex, true, true);
        }

        public void SetLayerCompleted(int layerIndex, bool completed, bool animate)
        {
            if (layerIndex < 0 || layerIndex >= _layers.Count || _layers[layerIndex] == null) return;
            _layers[layerIndex].SetCompleted(completed, animate);
        }

        public void ResetLayerVisuals()
        {
            for (var i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] != null) _layers[i].ResetVisual();
            }
        }

        public void SetActiveVisual(bool active, bool animate = true)
        {
            for (var i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] != null) _layers[i].SetActiveVisual(active, animate);
            }
        }
    }
}
