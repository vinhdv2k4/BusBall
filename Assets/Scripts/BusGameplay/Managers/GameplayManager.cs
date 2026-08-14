using System;
using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    public enum GameplayState { Loading, Playing, Won, Lost }

    [SerializeField] private LevelJsonLoader levelLoader;
    [SerializeField] private DockSlotConveyor dockSlotConveyor;
    [SerializeField] private TopGameController topGameController;
    [SerializeField] private Transform roadPathRoot;
    [SerializeField] private AnimationCurve ballJumpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public GameplayState State { get; private set; } = GameplayState.Loading;
    public AnimationCurve BallJumpCurve => ballJumpCurve;
    public event Action<GameplayState> StateChanged;

    private void Start() { LoadLevel(); }

    public void LoadLevel()
    {
        State = GameplayState.Loading;
        StateChanged?.Invoke(State);
        if (levelLoader != null && !levelLoader.LoadLevel()) { ChangeState(GameplayState.Lost); return; }
        ChangeState(GameplayState.Playing);
    }

    public void CheckWin()
    {
        if (topGameController != null && topGameController.IsWin()) ChangeState(GameplayState.Won);
    }

    public void CheckLoseConveyor()
    {
        if (topGameController != null && topGameController.IsStuckConvayor()) ChangeState(GameplayState.Lost);
    }

    public void ChangeState(GameplayState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(state);
    }

    public Transform GetRoadPathRoot() => roadPathRoot;
}
