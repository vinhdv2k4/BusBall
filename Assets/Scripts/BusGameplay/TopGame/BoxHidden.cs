using UnityEngine;

public class BoxHidden : MonoBehaviour
{
    [SerializeField] private GameObject hiddenModel;

    public void SetupMechanic(BoxMechanicConfig config)
    {
        if (config != null) hiddenModel?.SetActive(true);
    }

    public void Activate()
    {
        if (hiddenModel != null) hiddenModel.SetActive(true);
    }

    public void DoAction() { Activate(); }

    public void DeActivate()
    {
        if (hiddenModel != null) hiddenModel.SetActive(false);
    }
}
