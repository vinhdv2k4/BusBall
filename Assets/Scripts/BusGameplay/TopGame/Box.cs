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
    public int EmptySlotCount
    {
        get
        {
            int count = 0;
            foreach (DockSlot slot in slots)
                if (slot != null && !slot.HasBall)
                    count++;
            return count;
        }
    }

    private void Awake()
    {
        if (boxAnimationController == null) boxAnimationController = GetComponent<BoxAnimationController>();
    }

    public void Initialize(ColorType color, int slotCount)
    {
        colorType = color;
        IsFull = false;
        ApplyColorMaterial();
        boxAnimationController?.PlayBorn();
        UpdateMechanic();
    }

    private void ApplyColorMaterial()
    {
        string suffix = GetMaterialNameSuffix(colorType);
        if (string.IsNullOrEmpty(suffix))
        {
            Debug.LogWarning($"Box {name}: invalid color type {colorType}.", this);
            return;
        }

        string colorPath = $"Materials/Color/M_Color_{suffix}";
        string glassPath = $"Materials/Glass/M_Glass_{suffix}";
        Material colorMaterial = Resources.Load<Material>(colorPath);
        Material glassMaterial = Resources.Load<Material>(glassPath);
        if (colorMaterial == null)
        {
            Debug.LogWarning($"Box {name}: failed to load color material. Color={colorPath}.", this);
            return;
        }

        glassMaterial ??= Resources.Load<Material>("Materials/Glass/M_Glass_White");

        foreach (MeshRenderer renderer in mrBoxes)
        {
            if (renderer == null) continue;
            ApplyBoxRendererMaterials(renderer, colorMaterial, glassMaterial);
        }

        // Slot meshes use the body color unless they have their own material setup.
        foreach (MeshRenderer renderer in mrBoxSlots)
            if (renderer != null) renderer.sharedMaterial = colorMaterial;
    }

    private static void ApplyBoxRendererMaterials(
        MeshRenderer renderer,
        Material colorMaterial,
        Material glassMaterial)
    {
        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
        {
            renderer.sharedMaterial = colorMaterial;
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material current = materials[i];
            string materialName = current != null ? current.name : string.Empty;
            if (materialName.StartsWith("M_Glass_"))
                materials[i] = glassMaterial;
            else
                materials[i] = colorMaterial;
        }

        renderer.sharedMaterials = materials;
    }

    private static string GetMaterialNameSuffix(ColorType color)
    {
        return color switch
        {
            ColorType.Red => "Red",
            ColorType.Green => "Green",
            ColorType.Blue => "Blue",
            ColorType.Yellow => "Yellow",
            ColorType.Purple => "Purple",
            ColorType.Orange => "Orange",
            ColorType.Pink => "Pink",
            ColorType.Cyan => "BlueLight",
            ColorType.Forest => "GreenLight",
            ColorType.Brown => "Brown",
            ColorType.Black => "Black",
            _ => null
        };
    }

    public bool TryReceiveBall(BallController ball)
    {
        if (!CanReceiveBall(ball)) return false;
        foreach (DockSlot slot in slots)
        {
            if (slot == null || slot.HasBall) continue;
            if (!slot.TryDockBall(ball)) continue;
            RefreshFullState();
            return true;
        }
        return false;
    }

    public bool CanReceiveBall(BallController ball)
    {
        if (ball == null || ball.ColorType != colorType || IsFull) return false;
        foreach (DockSlot slot in slots)
            if (slot != null && !slot.HasBall) return true;
        return false;
    }

    public bool RefreshFullState()
    {
        IsFull = true;
        foreach (DockSlot slot in slots)
        {
            if (slot != null && !slot.HasBall)
            {
                IsFull = false;
                break;
            }
        }
        return IsFull;
    }

    public void PlayDieVfx()
    {
        if (tfBoxDieVfx != null) tfBoxDieVfx.gameObject.SetActive(true);
        VfxManager.Play(VfxType.BoxDie, transform.position);
        boxAnimationController?.PlayDie();
    }

    public void PlayAsFirstBox() => boxAnimationController?.PlayOpen();

    public void ResetData()
    {
        IsFull = false;
        foreach (DockSlot slot in slots)
        {
            BallController ball = slot?.RemoveBallForConsumption();
            if (ball != null)
            {
                ball.KillAllMovementTweens();
                ball.Release();
                ObjectPool.Recycle(ball.gameObject);
            }
            slot?.Clear();
        }
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
