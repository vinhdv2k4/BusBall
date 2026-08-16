using System;

public class WinPanel : BasePanel
{
    private Action onNextLevel;

    public void Configure(Action nextLevelAction) => onNextLevel = nextLevelAction;

    protected override void OnPrimaryButtonClicked() => onNextLevel?.Invoke();
}
