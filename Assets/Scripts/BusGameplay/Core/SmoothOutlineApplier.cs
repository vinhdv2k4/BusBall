using UnityEngine;

public class SmoothOutlineApplier : MonoBehaviour
{
    [SerializeField] private Material defaultOutline;
    [SerializeField] private Material reviveOutline;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private bool applyOnAwake = true;

    public Material DefaultOutline => defaultOutline;
    public Material ReviveOutline => reviveOutline;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        if (applyOnAwake) SetReviveOutline(false);
    }

    public void SetReviveOutline(bool enabled)
    {
        Material material = enabled && reviveOutline != null ? reviveOutline : defaultOutline;
        if (material == null) return;
        foreach (Renderer target in targetRenderers)
            if (target != null) target.sharedMaterial = material;
    }

    public void SetDefaultOutline(Material material)
    {
        defaultOutline = material;
        SetReviveOutline(false);
    }
}
