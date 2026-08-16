using System;

public class LosePanel : BasePanel
{
    private Action onRetry;

    public void Configure(Action retryAction) => onRetry = retryAction;

    protected override void OnPrimaryButtonClicked() => onRetry?.Invoke();
}
