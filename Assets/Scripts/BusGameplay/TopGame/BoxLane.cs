using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class BoxLane : MonoBehaviour
{
    [SerializeField] private Transform boxHolder, boxStart, boxEnd;
    [SerializeField] private MeshRenderer meshTunnel;
    [SerializeField] private BoxLaneDetector boxLaneDetector;
    [SerializeField] private Animator animator;
    [SerializeField, Min(1)] private int maxBoxCount = 5;
    private readonly List<Box> boxes = new();
    private readonly List<BoxDataConfig> configs = new();
    private int nextConfigIndex;
    private GameObject runtimeBoxPrefab;
    public int Count => boxes.Count;
    public int ConfigCount { get; private set; }
    public IReadOnlyList<Box> Boxes => boxes;

    public void Init(BoxLaneConfigData config, GameObject boxPrefab)
    {
        runtimeBoxPrefab = boxPrefab;
        ClearBoxes(); configs.Clear();
        PlayGateIdle();
        if (config?.boxDataConfigs != null) configs.AddRange(config.boxDataConfigs);
        ConfigCount = configs.Count;
        nextConfigIndex = 0;
        boxLaneDetector?.Initialize(this);
        if (animator == null) animator = GetComponent<Animator>();
        for (int i = 0; i < maxBoxCount; i++)
            if (!SpawnNextBox(boxPrefab)) break;
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
        if (ball.IsDocked && boxLaneDetector != null && !boxLaneDetector.HasReachedSplineProgress(ball))
            return;
        if (ball.IsDocked && ball.Slot != null)
            ball.Slot.ReleaseBall();

        if (first.TryReceiveBall(ball) && first.IsFull)
        {
            PlayGateClose();
            StartCoroutine(CompleteBoxRoutine(first));
        }
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
        if (box == null) yield break;
        box.PlayDieVfx();
        yield return new WaitForSeconds(0.75f);
        RemoveBoxAndReflow(box);
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
        for (int i = 0; i < boxes.Count; i++)
            if (boxes[i] != null) boxes[i].transform.position = GetPosition(i);
    }

    private void RefreshBoxVisibility()
    {
        for (int i = 0; i < boxes.Count; i++)
            if (boxes[i] != null) boxes[i].gameObject.SetActive(i < boxes.Count - 1);
    }

    private void ClearBoxes()
    {
        foreach (Box box in boxes) if (box != null) Destroy(box.gameObject);
        boxes.Clear();
    }
}
