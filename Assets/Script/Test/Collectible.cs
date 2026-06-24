using UnityEngine;

public class Collectible : MonoBehaviour
{
    public JournalData journal;

    bool picked = false;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Collect();
    }

    public void Collect()
    {
        if (picked) return;

        picked = true;

        CollectibleManager.Instance.Collect(journal);

        gameObject.SetActive(false);
    }
}