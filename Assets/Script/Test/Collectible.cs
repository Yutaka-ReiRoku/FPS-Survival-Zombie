using UnityEngine;

public class Collectible : MonoBehaviour
{
    public JournalData journal;

    bool picked = false;

    public void Collect()
    {
        if (picked) return;

        picked = true;

        CollectibleManager.Instance.Collect(journal);

        gameObject.SetActive(false);
    }
}