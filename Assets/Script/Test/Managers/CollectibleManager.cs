using System.Collections.Generic;
using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance;

    private List<JournalData> collected = new();

    public int Count => collected.Count;

    private void Awake()
    {
        Instance = this;
    }

    public void Collect(JournalData journal)
    {
        if (collected.Contains(journal))
            return;

        collected.Add(journal);

        if (JournalUI.Instance != null)
            JournalUI.Instance.Show(journal);
        else
            Debug.LogWarning("[CollectibleManager] No JournalUI in scene; collected without display.");

        Debug.Log($"Collected {Count}/6");
    }
}