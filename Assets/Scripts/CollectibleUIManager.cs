using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CollectibleUIManager : MonoBehaviour
{
    public GameObject entryPrefab;
    public Transform panelContainer;

    private Dictionary<CollectibleType, TextMeshProUGUI> entries = new();

    public void UpdateCollectibleUI(CollectibleType type, int count)
    {
        if (!entries.ContainsKey(type))
        {
            GameObject entryGO = Instantiate(entryPrefab, panelContainer);
            TextMeshProUGUI text = entryGO.GetComponent<TextMeshProUGUI>();
            entries[type] = text;
        }

        entries[type].text = $"{type}: {count}";
    }
}
