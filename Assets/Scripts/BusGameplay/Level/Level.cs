using UnityEngine;

public class Level : MonoBehaviour
{
    [SerializeField] private GameObject busAreaPrefab;
    [SerializeField] private Transform objectRoot;
    [SerializeField] private LevelJsonLoader jsonLoader;

    private GameObject busArea;

    public GameObject BusArea => busArea;
    public Transform ObjectRoot => objectRoot != null ? objectRoot : transform;

    private void Awake()
    {
        if (jsonLoader == null) jsonLoader = GetComponent<LevelJsonLoader>();
    }

    public bool LoadJsonLevel() => jsonLoader != null && jsonLoader.LoadLevel();

    public GameObject CreateBusArea()
    {
        if (busArea != null) return busArea;
        if (busAreaPrefab == null) return null;
        busArea = Instantiate(busAreaPrefab, ObjectRoot);
        busArea.name = busAreaPrefab.name;
        return busArea;
    }

    public void ResetData()
    {
        if (busArea != null)
        {
            Destroy(busArea);
            busArea = null;
        }

        for (int i = ObjectRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = ObjectRoot.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }
    }
}
