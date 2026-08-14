using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class RoadPath : MonoBehaviour
{
    [SerializeField] private SplineContainer[] splineContainers;
    [SerializeField] private bool loop;

    public int RoadCount => splineContainers == null ? 0 : splineContainers.Length;

    public bool TryEvaluate(int roadIndex, float normalizedT, out Vector3 position, out Vector3 tangent)
    {
        position = default;
        tangent = transform.forward;
        if (!TryGetRoad(roadIndex, out SplineContainer road)) return false;

        road.Evaluate(Mathf.Clamp01(normalizedT), out float3 positionNative,
            out float3 tangentNative, out float3 _);
        position = new Vector3(positionNative.x, positionNative.y, positionNative.z);
        Vector3 tangentVector = new Vector3(tangentNative.x, tangentNative.y, tangentNative.z);
        tangent = tangentVector.sqrMagnitude < 0.001f ? transform.forward : tangentVector.normalized;
        return true;
    }

    public bool TryGetRoad(int roadIndex, out SplineContainer road)
    {
        road = null;
        if (splineContainers == null || roadIndex < 0 || roadIndex >= splineContainers.Length) return false;
        road = splineContainers[roadIndex];
        return road != null;
    }

    public Vector3 GetPosition(int roadIndex, float normalizedT)
    {
        return TryEvaluate(roadIndex, normalizedT, out Vector3 position, out _) ? position : transform.position;
    }

    public Vector3 GetTangent(int roadIndex, float normalizedT)
    {
        return TryEvaluate(roadIndex, normalizedT, out _, out Vector3 tangent) ? tangent : transform.forward;
    }

    public void SetTopAnchorPosition(Vector3 position)
    {
        transform.position = position;
    }

    private void OnDrawGizmosSelected()
    {
        if (splineContainers == null) return;
        Gizmos.color = Color.yellow;
        foreach (SplineContainer road in splineContainers)
        {
            if (road == null) continue;
            Vector3 previous = road.EvaluatePosition(0f);
            for (int i = 1; i <= 32; i++)
            {
                Vector3 current = road.EvaluatePosition(i / 32f);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}
