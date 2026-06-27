using UnityEngine;
#if INVENTORY_PRO_ADD_ON
using cowsins.Inventory;
#endif
namespace cowsins
{
    public partial class BulletsPickeable : Pickeable
    {
        [Tooltip("How many bullets you will get"), SerializeField, SaveField] private int amountOfBullets;

        [Tooltip("If true, the pickup amount for hitscan/custom weapons is scaled by the current weapon's magazine size (magazinesPerPickup). If false, amountOfBullets is used as a flat amount for every weapon."), SerializeField]
        private bool scaleByMagazineSize = true;

        [Tooltip("How many magazine-worth of bullets a hitscan/custom weapon receives from this pickup (only used when scaleByMagazineSize is true)."), SerializeField, Min(1)]
        private int magazinesPerPickup = 2;

        [Tooltip("Fixed amount given to projectile weapons (e.g. rocket launchers) since their magazine size is tiny. Kept separate so rockets do not receive dozens of rounds."), SerializeField, Min(1)]
        private int projectilePickupAmount = 3;

        [SerializeField] private BulletsItem_SO bulletsSO;

        [SerializeField] private Sprite bulletsIcon;

        [SerializeField] private GameObject bulletsGraphics;

        public int AmountOfBullets => amountOfBullets;

        /// <summary>
        /// Returns the amount of ammo this pickup should grant for the currently equipped weapon.
        /// Projectile weapons (rocket launchers, etc.) receive a small fixed amount, while
        /// hitscan/custom weapons receive an amount proportional to their magazine size so
        /// each weapon type gets a sensible number of rounds.
        /// </summary>
        private int GetPickupAmount(Weapon_SO weapon, WeaponIdentification id)
        {
            if (weapon == null) return amountOfBullets;

            switch (weapon.shootStyle)
            {
                case ShootStyle.Projectile:
                    return projectilePickupAmount;
                case ShootStyle.Melee:
                    return 0;
                case ShootStyle.Hitscan:
                case ShootStyle.Custom:
                default:
                    if (scaleByMagazineSize && id != null)
                        return Mathf.Max(1, id.magazineSize * magazinesPerPickup);
                    return amountOfBullets;
            }
        }

        public override void Awake()
        {
            base.Awake();
            image.sprite = bulletsIcon;
            Destroy(graphics.transform.GetChild(0).gameObject);
            Instantiate(bulletsGraphics, graphics);
        }
        public override void Interact(Transform player)
        {
            if (bulletsSO == null)
            {
                CowsinsUtilities.LogError("<b><color=yellow>Bullet_SO</color></b> " +
                "not found! Skipping Interaction.", this);
                return;
            }
#if INVENTORY_PRO_ADD_ON
            if (InventoryProManager.instance)
            {
                (bool success, int remainingAmount) = InventoryProManager.instance._GridGenerator.AddItemToInventory(bulletsSO, amountOfBullets);
                if (success)
                {
                    alreadyInteracted = true;
                    interactableEvents.OnInteract?.Invoke();
                    StoreData();
                    ToastManager.Instance?.ShowToast($"x{amountOfBullets - remainingAmount} {ToastManager.Instance.CollectedMsg}");
                    amountOfBullets = remainingAmount;
                    if(amountOfBullets <= 0) Destroy(this.gameObject);
                }
                else
                    ToastManager.Instance?.ShowToast(ToastManager.Instance.InventoryIsFullMsg);
                return;
            }
#else
            if (player.GetComponent<IWeaponReferenceProvider>().Weapon == null) return;
#endif
            alreadyInteracted = true;
            base.Interact(player);
            var wRef = player.GetComponent<IWeaponReferenceProvider>();
            int granted = GetPickupAmount(wRef.Weapon, wRef.Id);
            wRef.Id.totalBullets += granted;
            // Fire OnAmmoChanged so HUD updates reserve ammo immediately on pickup.
            var wEvents = player.GetComponent<IWeaponEventsProvider>();
            if (wEvents != null && wEvents.Events != null)
                wEvents.Events.OnAmmoChanged?.Invoke(false);
            Destroy(this.gameObject);
        }
        public void SetBullets(BulletsItem_SO bulletsSO, int amountOfBullets)
        {
            this.amountOfBullets = amountOfBullets;
            this.bulletsSO = bulletsSO;
        }

        public override bool IsForbiddenInteraction(IWeaponReferenceProvider weaponController)
        {
            return AddonManager.instance.isInventoryAddonAvailable
                ? false
                : weaponController.Weapon != null && !weaponController.Weapon.limitedMagazines || weaponController.Weapon == null;
        }

#if SAVE_LOAD_ADD_ON
        // Destroy if picked up.
        // Interacted State is called after loading.
        public override void LoadedState()
        {
            if (this.alreadyInteracted) Destroy(this.gameObject);
        }
#endif
    }
}