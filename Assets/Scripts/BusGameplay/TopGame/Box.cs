using System.Collections.Generic;
using UnityEngine;

public class Box : MonoBehaviour
{
    [SerializeField] private ColorType colorType;
    [SerializeField] private List<DockSlot> slots = new();
    [SerializeField] private List<MeshRenderer> mrBoxes = new();
    [SerializeField] private List<MeshRenderer> mrBoxSlots = new();
    [SerializeField] private Transform tfBoxDieVfx;
    [SerializeField] private Transform boxMechanicHolder;
    [SerializeField] private BoxAnimationController boxAnimationController;

    public ColorType ColorType => colorType;
    public IReadOnlyList<DockSlot> Slots => slots;
    public bool IsFull { get; private set; }

    private void Awake()
    {
        if (boxAnimationController == null) boxAnimationController = GetComponent<BoxAnimationController>();
    }

    public void Initialize(ColorType color, int slotCount)
    {
        colorType = color;
        IsFull = false;
        UpdateMechanic();
    }

    public bool TryReceiveBall(BallController ball)
    {
        if (ball == null || ball.ColorType != colorType || IsFull) return false;
        foreach (DockSlot slot in slots)
        {
            if (slot == null || slot.HasBall) continue;
            if (!slot.TryDockBall(ball)) continue;
            IsFull = true;
            foreach (DockSlot candidate in slots) if (candidate != null && !candidate.HasBall) IsFull = false;
            return true;
        }
        return false;
    }

    public void PlayDieVfx()
    {
        if (tfBoxDieVfx != null) tfBoxDieVfx.gameObject.SetActive(true);
        boxAnimationController?.PlayComplete();
    }

    public void ResetData()
    {
        IsFull = false;
        foreach (DockSlot slot in slots) slot?.ReleaseBall();
        if (tfBoxDieVfx != null) tfBoxDieVfx.gameObject.SetActive(false);
        boxAnimationController?.ResetToIdle();
        ClearMechanicInstances();
    }

    public bool HasHiddenMechanic() => GetComponentInChildren<BoxHidden>(true) != null;

    public void UpdateMechanic()
    {
        if (boxMechanicHolder == null) return;
        foreach (BoxHidden hidden in boxMechanicHolder.GetComponentsInChildren<BoxHidden>(true)) hidden.Activate();
    }

    public void RevealBox()
    {
        if (boxMechanicHolder == null) return;
        foreach (BoxHidden hidden in boxMechanicHolder.GetComponentsInChildren<BoxHidden>(true)) hidden.DeActivate();
    }

    private void ClearMechanicInstances()
    {
        if (boxMechanicHolder == null) return;
        for (int i = boxMechanicHolder.childCount - 1; i >= 0; i--)
            Destroy(boxMechanicHolder.GetChild(i).gameObject);
    }
}
