using UnityEngine;

namespace BallDropParty.Gameplay
{
    public class BallView : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        private Material _defaultMaterial;

        public Renderer Renderer => _renderer;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null) _defaultMaterial = _renderer.sharedMaterial;
        }

        public void ApplyMaterial(Material material)
        {
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null) return;
            _renderer.sharedMaterial = material != null ? material : _defaultMaterial;
        }

        public void SetColor(Color color)
        {
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null) return;

            var block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            _renderer.SetPropertyBlock(block);

            if (_renderer.material != null)
            {
                _renderer.material.color = color;
            }
        }
    }
}
