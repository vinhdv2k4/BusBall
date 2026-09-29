using System.Collections.Generic;
using UnityEngine;

public static class Helper
{
    public static Vector3[] GetFrontPoints(Collider collider, Vector3 direction)
    {
        if (collider == null)
            return System.Array.Empty<Vector3>();

        var bounds = collider.bounds;
        var extents = bounds.extents;
        var center = bounds.center;

        var corners = new List<Vector3>(8)
        {
            center + new Vector3(extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, -extents.y, -extents.z)
        };

        var forward = direction.normalized;
        var ranked = new List<(Vector3 point, float score)>();
        foreach (var corner in corners)
        {
            float score = Vector3.Dot((corner - center).normalized, forward);
            ranked.Add((corner, score));
        }

        ranked.Sort((a, b) => b.score.CompareTo(a.score));
        if (ranked.Count < 2)
            return new[] { center, center + forward * extents.magnitude, center + forward * extents.magnitude * 0.5f };

        var p0 = ranked[0].point;
        var p1 = ranked[1].point;
        var mid = (p0 + p1) / 2f;
        return new[] { p0, p1, mid };
    }
}
