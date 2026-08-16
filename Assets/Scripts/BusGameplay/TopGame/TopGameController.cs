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
    private int expectedBusReleaseCount;
    private int releasedBusCount;
    private bool conveyorSpeedIncreased;
    private DockSlotConveyor subscribedConveyor;

    public Transform BallStart => ballStart;
    public Transform TopAnchor => topAnchor;
    public Transform BallVacuumPoint => ballVacuumPoint;
    public BoxLaneHolder BoxLaneHolder => boxLaneHolder;
    public FunnelController FunnelController => funnelController;

    private void Start()
    {
        RefreshConveyorReference();
        RefreshConveyorState();
    }

    private void Update()
    {
        // The conveyor owns the source of truth; polling also covers runtime
        // hierarchy initialization order and direct slot changes.
        RefreshConveyorReference();
        RefreshConveyorState();
    }

    public void Init(TopGameConfigData topGameConfig, PrefabSO prefabSO = null)
    {
        boxLaneHolder?.Init(topGameConfig, prefabSO);
        RefreshConveyorReference();
        conveyorColorCounts.Clear();
        occupiedConveyorSlots = 0;
        expectedBusReleaseCount = 0;
        releasedBusCount = 0;
        conveyorSpeedIncreased = false;
        funnelController?.SetConveyorSpeedMultiplier(1f);
        RefreshConveyorState();
    }

    private void OnDestroy()
    {
        if (subscribedConveyor != null)
            subscribedConveyor.BallCountChanged -= RefreshConveyorState;
    }

    public void SetExpectedBusReleaseCount(int count)
    {
        expectedBusReleaseCount = Mathf.Max(0, count);
    }

    public void NotifyBusReleasedBalls()
    {
        releasedBusCount++;
        if (conveyorSpeedIncreased || expectedBusReleaseCount <= 0 ||
            releasedBusCount < expectedBusReleaseCount)
            return;

        conveyorSpeedIncreased = true;
        funnelController?.SetConveyorSpeedMultiplier(2f);
    }

    public bool IsWin()
    {
        return boxLaneHolder != null && boxLaneHolder.GetTotalBoxLeft() == 0;
    }

    public void OnDockBall(BallController ball)
    {
        RefreshConveyorState();
    }

    public void OnUndockBall(BallController ball)
    {
        RefreshConveyorState();
    }

    public bool IsFullSlotConvayor() => conveyorCapacity > 0 && occupiedConveyorSlots >= conveyorCapacity;
    public bool HasBallInConvayor() => occupiedConveyorSlots > 0;
    public int GetOccupiedSlotConvayorCount() => occupiedConveyorSlots;
    public Dictionary<ColorType, int> GetConveyorColorCounts() => new(conveyorColorCounts);
    public bool IsStuckConvayor() => IsFullSlotConvayor() && !IsWin();

    private void UpdateConveyorText()
    {
        if (conveyorSlotFillText != null)
            conveyorSlotFillText.text = $"{occupiedConveyorSlots}/{conveyorCapacity}";
    }

    private void SubscribeToConveyor()
    {
        DockSlotConveyor conveyor = funnelController != null ? funnelController.Conveyor : null;
        if (conveyor == null)
            conveyor = FindFirstObjectByType<DockSlotConveyor>();
        if (subscribedConveyor == conveyor) return;

        if (subscribedConveyor != null)
            subscribedConveyor.BallCountChanged -= RefreshConveyorState;
        subscribedConveyor = conveyor;
        if (subscribedConveyor != null)
            subscribedConveyor.BallCountChanged += RefreshConveyorState;
    }

    private void RefreshConveyorReference()
    {
        SubscribeToConveyor();
        conveyorCapacity = subscribedConveyor != null ? subscribedConveyor.Count : 0;
    }

    private void RefreshConveyorState()
    {
        occupiedConveyorSlots = subscribedConveyor != null ? subscribedConveyor.BallCount : 0;
        conveyorColorCounts.Clear();
        if (subscribedConveyor != null)
        {
            foreach (DockSlot slot in subscribedConveyor.Slots)
            {
                BallController ball = slot != null ? slot.Ball : null;
                if (ball == null) continue;
                conveyorColorCounts.TryGetValue(ball.ColorType, out int count);
                conveyorColorCounts[ball.ColorType] = count + 1;
            }
        }
        UpdateConveyorText();
    }
}
