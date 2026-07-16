using UnityEngine;
using cowsins;

public class GiftBox : Interactable
{
    public override void Interact(Transform player)
    {
        base.Interact(player);
        AdRewardManager.Instance?.ShowAd(player);
        Destroy(this);
        gameObject.layer = LayerMask.NameToLayer("Default");
    }
}
