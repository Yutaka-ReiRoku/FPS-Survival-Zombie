using System;
using System.Collections;
using UnityEngine;
using cowsins;

/// <summary>
/// The SINGLE bridge between the Cowsins FPS engine and the custom AAA HUD.
/// This is the ONLY custom script that references cowsins.*. It reads
/// health/shield/ammo/weapon/reload/death from Cowsins (via engine events plus
/// authoritative property reads) and re-broadcasts engine-free C# events that
/// the HUD widgets consume. Widgets read the public properties on enable for
/// the current state, then subscribe to the events for live updates.
///
/// Design note: health/ammo handlers intentionally IGNORE the engine event's
/// numeric arguments and read the authoritative PlayerStats / WeaponIdentification
/// values directly, so we are immune to any argument-order assumptions.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CowsinsHUDAdapter : MonoBehaviour
{
    public static CowsinsHUDAdapter Instance { get; private set; }

    // ---- Health / Shield ----
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public float Shield { get; private set; }
    public float MaxShield { get; private set; }
    /// <summary>(currentHealth, maxHealth, tookDamageThisChange)</summary>
    public event Action<float, float, bool> OnHealthChanged;
    /// <summary>(currentShield, maxShield)</summary>
    public event Action<float, float> OnShieldChanged;

    // ---- Ammo ----
    public int Ammo { get; private set; }
    public int MagazineSize { get; private set; }
    public int Reserve { get; private set; }
    public bool LimitedReserve { get; private set; }
    public float Heat { get; private set; }
    /// <summary>(ammoInMagazine, reserveBullets)</summary>
    public event Action<int, int> OnAmmoChanged;
    public event Action<float> OnHeatChanged;
    public event Action OnFired;

    // ---- Weapon ----
    public string WeaponName { get; private set; } = string.Empty;
    public Sprite WeaponIcon { get; private set; }
    public bool HasWeapon { get; private set; }
    /// <summary>(weaponName, weaponIcon)</summary>
    public event Action<string, Sprite> OnWeaponChanged;

    // ---- Reload ----
    public bool IsReloading { get; private set; }
    public float ReloadTime { get; private set; }
    public event Action<bool> OnReloadChanged;

    // ---- Death ----
    public bool IsDead { get; private set; }
    public event Action OnDied;

    // ---- Progression (Coins / XP) ----
    public int Coins { get; private set; }
    public int PlayerLevel { get; private set; }
    public float XpFill { get; private set; }
    public event Action<int> OnCoinsChanged;
    /// <summary>(displayLevel, xpFill01)</summary>
    public event Action<int, float> OnXpChanged;

    // ---- Dash ----
    public int CurrentDashes { get; private set; }
    public int MaxDashes { get; private set; }
    public bool InfiniteDashes { get; private set; }
    /// <summary>(currentDashes, maxDashes)</summary>
    public event Action<int, int> OnDashChanged;

    private bool _staticBound;

    private PlayerStats _stats;
    private PlayerStatsEvents _statsEvents;
    private WeaponController _weapon;
    private PlayerDependencies _deps;
    private PlayerMovementEvents _moveEvents;
    private float _lastHealth = -1f;
    private bool _bound;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        StartCoroutine(AcquireAndBind());
    }

    private void OnDisable()
    {
        Unbind();
        UnsubscribeStatic();
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        Unbind();
        if (Instance == this) Instance = null;
    }

    private IEnumerator AcquireAndBind()
    {
        // Cowsins sets up PlayerStats / WeaponController in Start(); wait until present.
        float timeout = 12f;
        while (timeout > 0f && (_stats == null || _weapon == null))
        {
            if (_stats == null) _stats = FindObjectOfType<PlayerStats>();
            if (_weapon == null) _weapon = FindObjectOfType<WeaponController>();
            if (_deps == null) _deps = FindObjectOfType<PlayerDependencies>();
            if (_stats != null && _weapon != null) break;
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (_stats == null || _weapon == null)
        {
            Debug.LogWarning("[CowsinsHUDAdapter] PlayerStats/WeaponController not found; HUD will stay idle.");
            yield break;
        }

        Bind();

        // Pull authoritative current values (init events may have fired in Start before we bound).
        PullHealth(false);
        PullWeapon();
        PullAmmo();

        SubscribeStatic();
        PullCoins();
        PullXp();

        StartCoroutine(PollHeat());
        StartCoroutine(BindDashWhenReady());
    }

    // Provider events (PlayerDependencies.PlayerMovementEvents) are populated in the
    // player's Awake, which may run after our Bind(); wait for them, then subscribe dash.
    private IEnumerator BindDashWhenReady()
    {
        float timeout = 12f;
        while (timeout > 0f && (_deps == null || _deps.PlayerMovementEvents == null || _deps.PlayerMovementEvents.Events == null))
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (_deps == null || _deps.PlayerMovementEvents == null) yield break;
        _moveEvents = _deps.PlayerMovementEvents.Events;
        if (_moveEvents == null) yield break;
        _moveEvents.OnInitializeDash.AddListener(HandleInitDash);
        _moveEvents.OnDashUsed.AddListener(HandleDashUsed);
        _moveEvents.OnDashGained.AddListener(HandleDashGained);
    }

    private void Bind()
    {
        if (_bound) return;

        _statsEvents = (_stats as IPlayerStatsEventsProvider)?.Events;
        if (_statsEvents != null)
        {
            _statsEvents.OnHealthChanged.AddListener(HandleHealthChanged);
            _statsEvents.OnInitializeHealth.AddListener(HandleInitializeHealth);
        }
        _stats.AddOnDieListener(HandleDied);

        var e = _weapon.Events;
        if (e != null)
        {
            e.OnAmmoChanged.AddListener(HandleAmmoChanged);
            e.OnStartReload.AddListener(HandleStartReload);
            e.OnFinishReload.AddListener(HandleFinishReload);
            e.OnSelectWeapon.AddListener(HandleWeaponChanged);
            e.OnUnholster.AddListener(HandleUnholster);
            e.OnShoot.AddListener(HandleShoot);
        }

        _bound = true;
    }

    private void Unbind()
    {
        if (!_bound) return;

        if (_statsEvents != null)
        {
            _statsEvents.OnHealthChanged.RemoveListener(HandleHealthChanged);
            _statsEvents.OnInitializeHealth.RemoveListener(HandleInitializeHealth);
        }
        if (_stats != null) _stats.RemoveOnDieListener(HandleDied);

        if (_weapon != null && _weapon.Events != null)
        {
            var e = _weapon.Events;
            e.OnAmmoChanged.RemoveListener(HandleAmmoChanged);
            e.OnStartReload.RemoveListener(HandleStartReload);
            e.OnFinishReload.RemoveListener(HandleFinishReload);
            e.OnSelectWeapon.RemoveListener(HandleWeaponChanged);
            e.OnUnholster.RemoveListener(HandleUnholster);
            e.OnShoot.RemoveListener(HandleShoot);
        }

        if (_moveEvents != null)
        {
            _moveEvents.OnInitializeDash.RemoveListener(HandleInitDash);
            _moveEvents.OnDashUsed.RemoveListener(HandleDashUsed);
            _moveEvents.OnDashGained.RemoveListener(HandleDashGained);
        }

        _bound = false;
    }

    // ---- Health (read authoritative props; ignore event float args) ----
    private void HandleInitializeHealth(float a, float b, float c, float d) => PullHealth(false);
    private void HandleHealthChanged(float a, float b, bool damaged) => PullHealth(true);

    private void PullHealth(bool fromChange)
    {
        if (_stats == null) return;
        Health = _stats.Health;
        MaxHealth = _stats.MaxHealth;
        Shield = _stats.Shield;
        MaxShield = _stats.MaxShield;
        bool tookDamage = fromChange && _lastHealth >= 0f && Health < _lastHealth - 0.001f;
        _lastHealth = Health;
        OnHealthChanged?.Invoke(Health, MaxHealth, tookDamage);
        OnShieldChanged?.Invoke(Shield, MaxShield);
    }

    private void HandleDied()
    {
        IsDead = true;
        OnDied?.Invoke();
    }

    // ---- Ammo ----
    private void HandleAmmoChanged(bool auto) => PullAmmo();

    private void PullAmmo()
    {
        if (_weapon == null) return;
        var id = _weapon.Id;
        var w = _weapon.Weapon;
        if (id != null)
        {
            Ammo = id.bulletsLeftInMagazine;
            MagazineSize = id.magazineSize;
            Reserve = id.totalBullets;
            Heat = id.heatRatio;
        }
        LimitedReserve = w != null && w.limitedMagazines;
        OnAmmoChanged?.Invoke(Ammo, Reserve);
    }

    private IEnumerator PollHeat()
    {
        var wait = new WaitForSecondsRealtime(0.05f);
        while (true)
        {
            if (_weapon != null && _weapon.Id != null)
            {
                float h = _weapon.Id.heatRatio;
                if (!Mathf.Approximately(h, Heat))
                {
                    Heat = h;
                    OnHeatChanged?.Invoke(Heat);
                }
            }
            if (CoinManager.Instance != null && CoinManager.Instance.coins != Coins) PullCoins();
            var xpm = ExperienceManager.Instance;
            if (xpm != null && xpm.GetPlayerLevel() != PlayerLevel) PullXp();
            yield return wait;
        }
    }

    // ---- Weapon ----
    private void HandleWeaponChanged() => PullWeapon();
    private void HandleUnholster(bool a, bool b) => PullWeapon();

    private void PullWeapon()
    {
        if (_weapon == null) return;
        var w = _weapon.Weapon;
        HasWeapon = w != null;
        WeaponName = w != null ? w._name : string.Empty;
        WeaponIcon = w != null ? w.icon : null;
        ReloadTime = w != null ? w.reloadTime : 0f;
        OnWeaponChanged?.Invoke(WeaponName, WeaponIcon);
        PullAmmo();
    }

    // ---- Reload ----
    private void HandleStartReload()
    {
        IsReloading = true;
        OnReloadChanged?.Invoke(true);
    }

    private void HandleFinishReload()
    {
        IsReloading = false;
        OnReloadChanged?.Invoke(false);
        PullAmmo();
    }

    // ---- Shoot (for fire-punch animation; ammo number handled by OnAmmoChanged) ----
    private void HandleShoot() => OnFired?.Invoke();

    // ---- Dash ----
    private void HandleInitDash(int max) { MaxDashes = max; CurrentDashes = max; InfiniteDashes = false; OnDashChanged?.Invoke(CurrentDashes, MaxDashes); }
    private void HandleDashUsed(int cur) { CurrentDashes = cur; if (cur > MaxDashes) MaxDashes = cur; OnDashChanged?.Invoke(CurrentDashes, MaxDashes); }
    private void HandleDashGained(int cur) { CurrentDashes = cur; if (cur > MaxDashes) MaxDashes = cur; OnDashChanged?.Invoke(CurrentDashes, MaxDashes); }

    // ---- Progression: Coins / XP (Cowsins static UIEvents) ----
    private void SubscribeStatic()
    {
        if (_staticBound) return;
        UIEvents.onCoinsChange += HandleCoinsChanged;
        UIEvents.onExperienceCollected += HandleXpChanged;
        _staticBound = true;
    }

    private void UnsubscribeStatic()
    {
        if (!_staticBound) return;
        UIEvents.onCoinsChange -= HandleCoinsChanged;
        UIEvents.onExperienceCollected -= HandleXpChanged;
        _staticBound = false;
    }

    private void HandleCoinsChanged(int amount, bool updatePanel) => PullCoins();
    private void HandleXpChanged(bool updatePanel) => PullXp();

    private void PullCoins()
    {
        Coins = CoinManager.Instance != null ? CoinManager.Instance.coins : Coins;
        OnCoinsChanged?.Invoke(Coins);
    }

    private void PullXp()
    {
        var xp = ExperienceManager.Instance;
        if (xp == null) { OnXpChanged?.Invoke(PlayerLevel, XpFill); return; }
        PlayerLevel = xp.GetPlayerLevel();
        float fill = 0f;
        var reqs = xp.experienceRequirements;
        if (reqs != null && xp.playerLevel >= 0 && xp.playerLevel < reqs.Length && reqs[xp.playerLevel] > 0)
            fill = Mathf.Clamp01(xp.GetCurrentExperience() / (float)reqs[xp.playerLevel]);
        XpFill = fill;
        OnXpChanged?.Invoke(PlayerLevel, XpFill);
    }
}
