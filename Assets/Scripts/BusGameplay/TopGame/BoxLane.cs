using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using DG.Tweening;

public class BoxLane : MonoBehaviour
{
    [SerializeField] private Transform boxHolder, boxStart, boxEnd;
    [SerializeField] private MeshRenderer meshTunnel;
    [SerializeField] private BoxLaneDetector boxLaneDetector;
    [SerializeField] private Animator animator;
    [SerializeField, Min(1)] private int maxBoxCount = 5;
    // With the default speed multiplier 1.5 this produces a 0.1 second move.
    [SerializeField, Min(0.01f)] private float boxMoveDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float boxMoveSpeedMultiplier = 1.5f;
    private readonly List<Box> boxes = new();
    private readonly List<BoxDataConfig> configs = new();
    private readonly HashSet<DockSlot> reservedSlots = new();
    private readonly HashSet<BallController> receivingBalls = new();
    private readonly HashSet<Box> completingBoxes = new();
    private int nextConfigIndex;
    private GameObject runtimeBoxPrefab;
    public int Count => boxes.Count;
    public int ConfigCount { get; private set; }
    public IReadOnlyList<Box> Boxes => boxes;

    public void Init(BoxLaneConfigData config, GameObject boxPrefab)
    {
        runtimeBoxPrefab = boxPrefab;
        ClearBoxes(); configs.Clear();
        reservedSlots.Clear();
        receivingBalls.Clear();
        completingBoxes.Clear();
        PlayGateIdle();
        if (config?.boxDataConfigs != null) configs.AddRange(config.boxDataConfigs);
        ConfigCount = configs.Count;
        nextConfigIndex = 0;
        boxLaneDetector?.Initialize(this);
        if (animator == null) animator = GetComponent<Animator>();
        for (int i = 0; i < maxBoxCount; i++)
            if (!SpawnNextBox(boxPrefab)) break;
        if (boxes.Count > 0 && boxes[0] != null)
            boxes[0].PlayAsFirstBox();
        ReflowStackLayout();
        RefreshBoxVisibility();
    }

    private bool SpawnNextBox(GameObject boxPrefab)
    {
        if (boxPrefab == null || nextConfigIndex >= configs.Count) return false;
        GameObject go = Instantiate(boxPrefab, boxHolder != null ? boxHolder : transform);
        Box box = go.GetComponent<Box>();
        if (box == null) { Destroy(go); return false; }
        box.Initialize(configs[nextConfigIndex++].colorType, box.Slots.Count);
        boxes.Add(box);
        return true;
    }

    public Box GetFirstBox() => boxes.Count > 0 ? boxes[0] : null;

    public void ProcessBallInDetector(Collider2D other)
    {
        BallController ball = other != null ? other.GetComponentInParent<BallController>() : null;
        Box first = GetFirstBox();
        if (ball == null || first == null) return;
        if (receivingBalls.Contains(ball)) return;
        Debug.Log(
            $"[BALL ROUTE] ball={ball.name} -> lane={name}, " +
            $"boxColor={first.ColorType}, sourceSlot={(ball.Slot != null ? ball.Slot.SlotIndex.ToString() : "none")}",
            this);
        if (!first.CanReceiveBall(ball)) return;
        DockSlot targetSlot = null;
        foreach (DockSlot slot in first.Slots)
            if (slot != null && !slot.HasBall && !reservedSlots.Contains(slot))
            {
                targetSlot = slot;
                break;
            }
        if (targetSlot == null) return;

        DockSlot sourceSlot = ball.Slot;
        Debug.Log(
            $"[BALL ROUTE] ball={ball.name} reserved lane={name}, " +
            $"targetSlot={targetSlot.name}, sourceSlot={(sourceSlot != null ? sourceSlot.name : "none")}",
            this);
        reservedSlots.Add(targetSlot);
        receivingBalls.Add(ball);
        // Let the box/gate move first, then send the ball into the box on an arc.
        PlayGateIdle();
        StartCoroutine(ReceiveBallAfterGateRoutine(ball, first, targetSlot, sourceSlot));
    }

    private IEnumerator ReceiveBallAfterGateRoutine(
        BallController ball,
        Box box,
        DockSlot targetSlot,
        DockSlot sourceSlot)
    {
        yield return new WaitForSeconds(0.15f);
        // Keep the ball on the conveyor while the box/gate is moving. Only
        // release it once the same box is still active and ready to receive.
        if (ball == null || box == null || box != GetFirstBox() ||
            !box.CanReceiveBall(ball) || targetSlot == null || targetSlot.HasBall ||
            ball.Slot != sourceSlot ||
            (boxLaneDetector != null && !boxLaneDetector.IsDetecting(ball)))
        {
            Debug.LogWarning(
                $"[BALL ROUTE CANCELLED] ball={(ball != null ? ball.name : "null")} " +
                $"lane={name}, targetSlot={(targetSlot != null ? targetSlot.name : "none")}, " +
                $"currentSlot={(ball != null && ball.Slot != null ? ball.Slot.name : "none")}",
                this);
            if (targetSlot != null) reservedSlots.Remove(targetSlot);
            if (ball != null) receivingBalls.Remove(ball);
            yield break;
        }

        if (ball.IsDocked && ball.Slot != null)
            ball.Slot.ReleaseBall(false);

        Debug.Log(
            $"[BALL ROUTE ENTER] ball={ball.name} -> lane={name}, " +
            $"boxColor={box.ColorType}, targetSlot={targetSlot.name}",
            this);
        ball.MoveToBoxArc(targetSlot, 0.2f, 0.6f, () =>
        {
            reservedSlots.Remove(targetSlot);
            receivingBalls.Remove(ball);
            if (boxes.Contains(box) && box == GetFirstBox() &&
                !completingBoxes.Contains(box) && box.RefreshFullState())
            {
                completingBoxes.Add(box);
                Debug.Log(
                    $"[BOX COMPLETE START] lane={name}, box={box.name}, color={box.ColorType}",
                    box);
                PlayGateClose();
                StartCoroutine(CompleteBoxRoutine(box));
            }
            else if (!boxes.Contains(box) || box != GetFirstBox())
            {
                Debug.LogWarning(
                    $"[BOX COMPLETE BLOCKED] lane={name}, box={(box != null ? box.name : "null")}",
                    this);
            }
        });
    }

    public void PlayGateClose()
    {
        if (animator != null)
            animator.Play("Gate_Close", 0, 0f);
    }

    public void PlayGateIdle()
    {
        if (animator != null)
            animator.Play("Gate_Idle", 0, 0f);
    }

    private IEnumerator CompleteBoxRoutine(Box box)
    {
        if (box == null || !boxes.Contains(box) || box != GetFirstBox()) yield break;
        box.PlayDieVfx();
        yield return new WaitForSeconds(0.75f);
        if (box != null && boxes.Contains(box) && box == GetFirstBox())
        {
            Debug.Log(
                $"[BOX COMPLETE REMOVE] lane={name}, box={box.name}, color={box.ColorType}",
                this);
            RemoveBoxAndReflow(box);
        }
    }

    public bool TryGetFirstColor(out ColorType color)
    {
        Box box = GetFirstBox();
        if (box == null) { color = ColorType.None; return false; }
        color = box.ColorType; return true;
    }

    public List<ColorType> GetAllColors()
    {
        List<ColorType> result = new();
        foreach (Box box in boxes) if (box != null) result.Add(box.ColorType);
        return result;
    }

    public bool RemoveFirstBox(ColorType color)
    {
        Box first = GetFirstBox();
        if (first == null || first.ColorType != color) return false;
        RemoveBoxAndReflow(first); return true;
    }

    public bool RemoveBox(Box box)
    {
        if (box == null || boxes.Count == 0 || boxes[0] != box) return false;
        RemoveBoxAndReflow(box); return true;
    }

    private void RemoveBoxAndReflow(Box box)
    {
        if (!boxes.Remove(box)) return;
        reservedSlots.Clear();
        receivingBalls.Clear();
        completingBoxes.Remove(box);
        box.ResetData(); Destroy(box.gameObject);
        SpawnNextBox(runtimeBoxPrefab);
        ReflowStackLayout();
        RefreshBoxVisibility();
    }

    private Vector3 GetPosition(int index)
    {
        if (boxStart == null || boxEnd == null) return transform.position;
        int denominator = Mathf.Max(1, maxBoxCount - 1);
        // Config[0] is the first box processed at EndPoint.
        // The last queued box stays hidden at StartPoint.
        float normalized = 1f - index / (float)denominator;
        return Vector3.Lerp(boxStart.position, boxEnd.position, normalized);
    }

    private void ReflowStackLayout()
    {
        float duration = boxMoveDuration / Mathf.Max(0.01f, boxMoveSpeedMultiplier);
        for (int i = 0; i < boxes.Count; i++)
        {
            if (boxes[i] == null) continue;
            Box movingBox = boxes[i];
            bool isHiddenHolderBox = configs.Count >= 5 && i == boxes.Count - 1;
            movingBox.gameObject.SetActive(!isHiddenHolderBox);
            movingBox.transform.DOKill();
            Tween moveTween = movingBox.transform.DOMove(GetPosition(i), duration)
                .SetEase(Ease.OutQuad);
            if (i == 0)
                moveTween.OnComplete(movingBox.PlayAsFirstBox);
        }
    }

    private void RefreshBoxVisibility()
    {
        // Only lanes with five or more configured boxes keep a hidden queue box
        // at the holder/start point. Four-box lanes show every box.
        bool hideHolderBox = configs.Count >= 5;
        for (int i = 0; i < boxes.Count; i++)
            if (boxes[i] != null)
                boxes[i].gameObject.SetActive(!hideHolderBox || i < boxes.Count - 1);
    }

    private void ClearBoxes()
    {
        foreach (Box box in boxes) if (box != null) Destroy(box.gameObject);
        boxes.Clear();
    }
}
