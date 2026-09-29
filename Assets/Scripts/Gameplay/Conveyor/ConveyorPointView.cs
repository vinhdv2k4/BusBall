using UnityEngine;

namespace BallDropParty.Gameplay
{
    public class ConveyorPointView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _color1 = new Color(0.38823533f, 0.4039216f, 0.5647059f, 1f);
        [SerializeField] private Color _color2 = new Color(0.31764707f, 0.33333334f, 0.45098042f, 1f);

        private int _index = -1;
        private bool _isVisible = true;
        private ConveyorManager _conveyor;

        public int Index => _index;
        public bool IsVisible => _isVisible;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            ApplyVisualState();
        }

        private void OnValidate()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            ApplyVisualState();
        }

        public void Setup(int index)
        {
            _index = index;
            gameObject.name = $"{nameof(ConveyorPointView)}_{index:00}";
            ApplyVisualState();
        }

        public void SetConveyor(ConveyorManager conveyor) => _conveyor = conveyor;

        public void SetColor(Color color)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = color;
        }

        public void SetColorGroup(bool isColor1)
        {
            var color = isColor1 ? _color1 : _color2;
            SetColor(color);
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        public void SetVisible(bool visible)
        {
            if (_isVisible == visible) return;

            _isVisible = visible;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = _isVisible;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_index >= 0)
            {
                Gizmos.color = Color.cyan;
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.15f, $"{_index}");
            }
        }
#endif
    }
}
