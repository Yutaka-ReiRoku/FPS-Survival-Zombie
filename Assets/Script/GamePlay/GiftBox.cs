using UnityEngine;
using cowsins;

public class GiftBox : Interactable
{
    [Header("Gift Box Rewards")]
    [SerializeField] private string[] rewardDescriptions = { "100 Coins", "Health Pack" };
    [SerializeField] private int coinReward = 100;
    [SerializeField] private int healthReward = 50;

    public override void Interact(Transform player)
    {
        base.Interact(player);

        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(coinReward, false);

        var stats = player.GetComponent<PlayerStats>();
        if (stats != null && healthReward > 0)
            stats.Heal(healthReward);

        GiftBoxManager.Instance?.Show(rewardDescriptions, coinReward, healthReward);

        Destroy(this);
        gameObject.layer = LayerMask.NameToLayer("Default");
    }
}
