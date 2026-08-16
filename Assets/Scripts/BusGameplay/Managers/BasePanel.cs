using UnityEngine;
using UnityEngine.UI;

public abstract class BasePanel : MonoBehaviour
{
    [SerializeField] private Button primaryButton;

    protected virtual void Awake()
    {
        if (primaryButton != null)
            primaryButton.onClick.AddListener(HandlePrimaryButtonClicked);
    }

    protected virtual void OnDestroy()
    {
        if (primaryButton != null)
            primaryButton.onClick.RemoveListener(HandlePrimaryButtonClicked);
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public virtual void Hide() => gameObject.SetActive(false);

    public void SetPrimaryButton(Button button)
    {
        if (primaryButton != null)
            primaryButton.onClick.RemoveListener(HandlePrimaryButtonClicked);

        primaryButton = button;
        if (primaryButton != null)
            primaryButton.onClick.AddListener(HandlePrimaryButtonClicked);
    }

    private void HandlePrimaryButtonClicked() => OnPrimaryButtonClicked();

    protected abstract void OnPrimaryButtonClicked();
}
