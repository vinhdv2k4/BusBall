using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TopGameController : MonoBehaviour
{
    [Header("Top Game References")]
    [SerializeField] private Transform ballStart;
    [SerializeField] private Transform topAnchor;
    [SerializeField] private Transform ballVacuumPoint;
    [SerializeField] private BoxLaneHolder boxLaneHolder;
    [SerializeField] private TMP_Text conveyorSlotFillText;
    [SerializeField] private GameObject ballGate;
    [SerializeField] private FunnelController funnelController;
    private readonly Dictionary<ColorType, int> conveyorColorCounts = new();
    private int occupiedConveyorSlots;
    private int conveyorCapacity;

    public Transform BallStart => ballStart;
    public Transform TopAnchor => topAnchor;
    public Transform BallVacuumPoint => ballVacuumPoint;
    public BoxLaneHolder BoxLaneHolder => boxLaneHolder;
    public FunnelController FunnelController => funnelController;

    public void Init(TopGameConfigData topGameConfig, int conveyorSlotCapacity)
    {
        boxLaneHolder?.Init(topGameConfig);
        conveyorCapacity = Mathf.Max(0, conveyorSlotCapacity);
        conveyorColorCounts.Clear();
        occupiedConveyorSlots = 0;
        UpdateConveyorText();
    }

    public bool IsWin()
    {
        return boxLaneHolder != null && boxLaneHolder.GetTotalBoxLeft() == 0;
    }

    public void OnDockBall(BallController ball)
    {
        if (ball == null) return;
        occupiedConveyorSlots++;
        if (!conveyorColorCounts.ContainsKey(ball.ColorType)) conveyorColorCounts[ball.ColorType] = 0;
        conveyorColorCounts[ball.ColorType]++;
        boxLaneHolder?.TryConsumeBall(ball.ColorType);
        UpdateConveyorText();
    }

    public void OnUndockBall(BallController ball)
    {
        if (ball == null) return;
        occupiedConveyorSlots = Mathf.Max(0, occupiedConveyorSlots - 1);
        if (conveyorColorCounts.TryGetValue(ball.ColorType, out int count))
            conveyorColorCounts[ball.ColorType] = Mathf.Max(0, count - 1);
        UpdateConveyorText();
    }

    public bool IsFullSlotConvayor() => occupiedConveyorSlots >= conveyorCapacity;
    public bool HasBallInConvayor() => occupiedConveyorSlots > 0;
    public int GetOccupiedSlotConvayorCount() => occupiedConveyorSlots;
    public Dictionary<ColorType, int> GetConveyorColorCounts() => new(conveyorColorCounts);
    public bool IsStuckConvayor() => IsFullSlotConvayor() && !IsWin();

    private void UpdateConveyorText()
    {
        if (conveyorSlotFillText != null)
            conveyorSlotFillText.text = $"{occupiedConveyorSlots}/{conveyorCapacity}";
    }
}
