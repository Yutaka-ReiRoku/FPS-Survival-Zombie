using UnityEngine;
using cowsins;

/// <summary>
/// Singleton that manages the companion NPC lifecycle across chapters.
///
/// Responsibilities:
///   - Spawn the companion after Chapter 2 is completed (at the specified world position).
///   - Track whether the player accepted/refused at each dialogue stage.
///   - Teleport the companion near the player on chapter changes (so it doesn't
///     get blocked by ChapterBoundary walls).
///   - Trigger dialogue stage 2 when the player reaches Chapter 4's save room.
///   - Apply the "skip" logic when the player accepts stage 2 (skip Q9-Q10,
///     spawn Tank boss with reduced HP).
///
/// The companion prefab is loaded from Resources. If not found, the manager
/// logs a warning and does nothing (the game proceeds without a companion).
/// </summary>
public class CompanionManager : MonoBehaviour
{
    public static CompanionManager Instance { get; private set; }

    [Header("Companion Prefab")]
    [Tooltip("Prefab name in Resources (without extension). Must have CompanionAI + DialogueBubble + CompanionDialogueTrigger + CompanionHealthBar.")]
    public string companionPrefabResource = "Companion/BikerSurvivor";

    [Header("Spawn Position (after Chapter 2)")]
    public Vector3 spawnPosition = new Vector3(116.11f, 0f, -25.22f);

    [Header("Walk-Away Position (if refused at stage 1)")]
    public Vector3 deadEndPoint = new Vector3(60.62f, 0f, -21.49f);

    [Header("Ammo Cost (stage 1 accept)")]
    [Tooltip("Number of reserve bullets (totalBullets) the player must give the companion " +
             "for it to follow. If the current weapon's reserve ammo is below this, " +
             "the companion refuses to follow and the player can retry later.")]
    public int requiredAmmoToFollow = 40;

    [Header("Boss Skip")]
    [Tooltip("Tank boss max HP when the player accepts stage 2 (skip path).")]
    public int skippedTankMaxHealth = 250;

    [Header("Skip Cutscene (played between Ch4 and Ch5 skip)")]
    [Tooltip("Title of the cutscene shown when the player accepts the skip path.")]
    public string skipCutsceneTitle = "Tìm thấy kíp nổ";
    [Tooltip("Body text of the skip cutscene.")]
    [TextArea(3, 8)]
    public string skipCutsceneBody = "Sau nhiều giờ tìm kiếm, cuối cùng người chơi cũng đã tìm thấy kíp nổ... Đã đến lúc kết thúc mọi thứ.";
    [Tooltip("How long (seconds) the skip cutscene holds after typing completes.")]
    public float skipCutsceneHold = 3f;

    [Header("Skip Teleport")]
    [Tooltip("World position the player + companion are teleported to after the skip cutscene. " +
             "Defaults to near Q11_BombObjective so the player lands right at the Tank boss fight.")]
    public Vector3 skipTeleportPosition = new Vector3(1.71f, 0f, 14.99f);

    // ---- Runtime state ----
    public GameObject CompanionInstance { get; private set; }
    public CompanionAI CompanionAI { get; private set; }
    public CompanionDialogueTrigger DialogueTrigger { get; private set; }

    public bool AcceptedStage1 { get; private set; }
    public bool AcceptedStage2 { get; private set; }
    public bool Spawned { get; private set; }

    private bool _stage2Armed;
    private bool _subscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        // Fallback: if OnEnable ran before StoryManager.Awake.
        Subscribe();
    }

    private void OnDisable()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            StoryManager.Instance.OnChapterChanged -= HandleChapterChanged;
        }
        _subscribed = false;
    }

    private void Subscribe()
    {
        if (_subscribed) return;
        if (StoryManager.Instance == null) return;
        StoryManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        StoryManager.Instance.OnChapterChanged += HandleChapterChanged;
        _subscribed = true;
    }

    private void Update()
    {
        // Polling fallback: ensure we're subscribed and spawn if past Ch2.
        if (!_subscribed) Subscribe();

        var sm = StoryManager.Instance;
        if (sm == null) return;

        // If we're at Chapter 3+ and haven't spawned yet, spawn now.
        // This catches cases where the event was missed or an exception
        // in another handler broke the delegate chain.
        if (!Spawned && sm.CurrentChapter >= 3)
        {
            Debug.Log("[CompanionManager] Update fallback: CurrentChapter >= 3, spawning companion.");
            SpawnCompanion();
        }

        // Fallback: arm stage 2 dialogue when already at Chapter 4 with stage 1
        // accepted but stage 2 not armed. HandleChapterChanged only fires on
        // chapter transitions, so if the game starts directly at Ch4 (e.g. for
        // testing), the event is missed and stage 2 never arms.
        if (Spawned && AcceptedStage1 && sm.CurrentChapter >= 4 && !_stage2Armed && DialogueTrigger != null)
        {
            _stage2Armed = true;
            DialogueTrigger.ResetForStage(2);
            Debug.Log("[CompanionManager] Update fallback: armed stage 2 dialogue (already at Chapter 4).");
        }
    }

    // ---- Quest / Chapter hooks ----

    private void HandleQuestCompleted(QuestData quest)
    {
        try
        {
            var sm = StoryManager.Instance;
            if (sm == null) return;

            if (quest != null && quest.chapter == 2 && !Spawned)
            {
                // Use questId comparison instead of reference equality,
                // since the quest instance might differ.
                var ch2 = sm.chapter2Quests;
                if (ch2 != null && ch2.Length > 0)
                {
                    var lastQuest = ch2[ch2.Length - 1];
                    if (lastQuest != null && quest.questId == lastQuest.questId)
                    {
                        Debug.Log("[CompanionManager] Last Ch2 quest completed. Spawning companion.");
                        SpawnCompanion();
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CompanionManager] HandleQuestCompleted exception: {e}");
        }
    }

    private void HandleChapterChanged(int oldChapter, int newChapter)
    {
        try
        {
            if (newChapter <= 0) return; // -1 = story complete

            // Fallback: if we just entered Chapter 3 (or later) and the companion
            // hasn't spawned yet, spawn now.
            if (newChapter >= 3 && !Spawned)
            {
                Debug.Log("[CompanionManager] Chapter changed to " + newChapter + ", spawning companion (fallback).");
                SpawnCompanion();
            }

            // Teleport companion near the player on every chapter change.
            if (Spawned && CompanionAI != null && AcceptedStage1)
            {
                CompanionAI.TeleportNearPlayer();
            }

            // Arm stage 2 dialogue when entering Chapter 4.
            if (newChapter == 4 && Spawned && AcceptedStage1 && !_stage2Armed)
            {
                _stage2Armed = true;
                if (DialogueTrigger != null)
                    DialogueTrigger.ResetForStage(2);
                Debug.Log("[CompanionManager] Stage 2 dialogue armed (Chapter 4).");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CompanionManager] HandleChapterChanged exception: {e}");
        }
    }

    // ---- Spawn ----

    private void SpawnCompanion()
    {
        if (Spawned) return;
        var prefab = Resources.Load<GameObject>(companionPrefabResource);
        if (prefab == null)
        {
            Debug.LogWarning($"[CompanionManager] Companion prefab '{companionPrefabResource}' not found in Resources. Companion will not spawn.");
            return;
        }

        CompanionInstance = Instantiate(prefab, spawnPosition, Quaternion.identity);
        CompanionInstance.name = "BikerSurvivor_Companion";
        Spawned = true;

        CompanionAI = CompanionInstance.GetComponent<CompanionAI>();
        DialogueTrigger = CompanionInstance.GetComponent<CompanionDialogueTrigger>();

        if (DialogueTrigger != null)
        {
            DialogueTrigger.ActiveStage = 1;
            DialogueTrigger.interactable = true;
        }

        Debug.Log($"[CompanionManager] Companion spawned at {spawnPosition}. Waiting for player interaction (stage 1).");
    }

    // ---- Dialogue choice handler (called by CompanionDialogueTrigger) ----

    public void HandleDialogueChoice(int stage, bool accepted)
    {
        if (stage == 1)
        {
            if (accepted)
            {
                // Check if the player has enough reserve ammo to "pay" the
                // companion. If not, the companion stays put and the player
                // can retry after finding more ammo.
                if (TryConsumeAmmo(requiredAmmoToFollow))
                {
                    AcceptedStage1 = true;
                    if (CompanionAI != null) CompanionAI.StartFollowing();
                    Debug.Log($"[CompanionManager] Player ACCEPTED stage 1 and paid {requiredAmmoToFollow} bullets. Companion now follows.");
                }
                else
                {
                    // Not enough ammo — re-arm the trigger so the player can
                    // come back later with more ammo and try again.
                    if (DialogueTrigger != null) DialogueTrigger.ResetForStage(1);
                    SimpleNotification.Show($"Bạn không đủ {requiredAmmoToFollow} viên đạn dự trữ. Follower không thể đi theo.");
                    Debug.Log($"[CompanionManager] Player ACCEPTED stage 1 but lacks {requiredAmmoToFollow} reserve bullets. Companion stays. Trigger reset for retry.");
                }
            }
            else
            {
                AcceptedStage1 = false;
                if (CompanionAI != null) CompanionAI.WalkAway(deadEndPoint);
                Debug.Log("[CompanionManager] Player REFUSED stage 1. Companion walks away to " + deadEndPoint);
            }
        }
        else if (stage == 2)
        {
            AcceptedStage2 = accepted;
            if (accepted)
            {
                Debug.Log("[CompanionManager] Player ACCEPTED stage 2. Applying skip logic.");
                StartCoroutine(SkipToQuest11WithReducedBoss());
            }
            else
            {
                Debug.Log("[CompanionManager] Player REFUSED stage 2. Companion keeps following; player must complete all quests normally.");
            }
        }
    }

    /// <summary>
    /// Attempts to consume <paramref name="amount"/> reserve bullets from the
    /// player's currently-equipped weapon. Only <c>totalBullets</c> (reserve
    /// ammo) is considered — bullets already in the magazine are NOT touched.
    /// Returns true and deducts the ammo if the current weapon has enough
    /// reserve bullets; returns false and leaves ammo unchanged otherwise.
    /// </summary>
    private bool TryConsumeAmmo(int amount)
    {
        if (amount <= 0) return true;
        var wc = FindAnyObjectByType<cowsins.WeaponController>();
        if (wc == null || wc.Id == null)
        {
            Debug.LogWarning("[CompanionManager] WeaponController not found; cannot consume ammo.");
            return false;
        }
        int reserve = wc.Id.totalBullets;
        if (reserve < amount)
        {
            Debug.Log($"[CompanionManager] Not enough reserve ammo: have {reserve}, need {amount}.");
            return false;
        }
        wc.Id.totalBullets = reserve - amount;
        Debug.Log($"[CompanionManager] Consumed {amount} reserve bullets. Remaining: {wc.Id.totalBullets}.");
        return true;
    }

    // ---- Skip logic ----

    private System.Collections.IEnumerator SkipToQuest11WithReducedBoss()
    {
        var sm = StoryManager.Instance;
        if (sm == null) yield break;

        // 1) Skip remaining Chapter 4 quests (Quest 9) silently.
        while (sm.CurrentChapter == 4 && sm.ActiveQuest != null)
        {
            sm.CompleteActiveQuest();
            yield return null;
        }

        // 2) Side quests Ch4: unlock + auto-complete (grant rewards).
        //    The skip path assumes the player "already explored" Ch4, so all
        //    side quests are considered done with their rewards granted.
        AutoCompleteChapter4SideQuests();

        // 3) Collect (and hide) all Ch4 collectibles (journals).
        //    This both removes them from the world and registers them in
        //    CollectibleManager so the journal gallery reflects them.
        CollectAllChapter4Collectibles();

        // 4) Mark SaveRoom_Ch4 cutscene as played (so re-entering doesn't
        //    replay the "CHƯƠNG 4" banner). The checkpoint itself is set in
        //    step 7 to the teleport position (near Q11) so a death respawns
        //    the player near the boss fight, not back in Ch4.
        MarkSaveRoomCh4CutscenePlayed();

        // 5) Play the skip cutscene as a narrative bridge between Ch4 and Ch5.
        //    This explains how the player found the detonator without collecting
        //    all the journals, so the skip path doesn't feel abrupt.
        yield return PlaySkipCutscene();

        // 6) After Ch4 completes, the chapter advances to 5 with PendingChapterEntry.
        //    Force-activate the pending chapter quest so we can skip Ch5 quests.
        if (sm.PendingChapterEntry)
        {
            sm.ActivatePendingChapterQuest();
            yield return null;
        }

        // 7) Skip Chapter 5 quests until Quest 11 is active.
        //    Q11 is the boss-fight quest — the second-to-last quest in Ch5
        //    (index ch5.Length - 2 in the chapter5Quests array).
        //    We skip Q10 (collect documents) but STOP at Q11 so the player still
        //    fights the Tank boss (with reduced HP via CompanionBossSkipHook).
        while (sm.CurrentChapter == 5 && sm.ActiveQuest != null)
        {
            var ch5 = sm.chapter5Quests;
            // Stop when we reach Q11 (second-to-last quest).
            if (ch5 != null && sm.CurrentQuestIndex < ch5.Length - 2)
            {
                sm.CompleteActiveQuest();
                yield return null;
            }
            else
            {
                break;
            }
        }

        // 8) Set the reduced boss HP so the next Tank spawned by
        //    WaveQuestInteractable has reduced health (consumed on spawn).
        CompanionBossSkipHook.PendingReducedBossHP = skippedTankMaxHealth;

        // 9) Teleport the player + companion to the skip teleport position
        //    (near Q11_BombObjective). The save checkpoint is set to SaveRoom_Ch5
        //    (not the teleport position) so a death during the boss fight respawns
        //    the player at the Chapter 5 save zone — the player can heal and
        //    re-prepare before retrying the Tank boss, instead of respawning
        //    right on top of the fight.
        //    TeleportPlayerAndCompanion also restores timeScale if it's 0.

        // Temporarily disable chapter-transition cutscenes so SaveRoom_Ch5
        // doesn't play a "CHƯƠNG 5" banner (the skip cutscene already covers
        // the narrative bridge) AND so it doesn't capture prevTimeScale=0 and
        // leave the game paused after the transition cutscene ends.
        bool prevChapterCutsceneFlag = sm.playChapterTransitionCutscene;
        sm.playChapterTransitionCutscene = false;

        TeleportPlayerAndCompanion(skipTeleportPosition);

        // Set checkpoint to SaveRoom_Ch5's position so death respawns the
        // player at the save zone (heal + re-prepare) instead of at the
        // boss-fight teleport spot.
        var ch5Checkpoint = ResolveChapter5SaveRoomCheckpoint();
        SaveRoom.LastCheckpoint = ch5Checkpoint;
        SaveRoom.LastCheckpointRotation = Quaternion.identity;

        // 10) Safety net: after teleport, ChapterBoundary / SaveRoom triggers
        //     may fire (OnTriggerEnter from Physics.SyncTransforms). Wait one
        //     frame for those triggers to settle, then force-restore timeScale.
        yield return null;
        if (!PauseManager.Instance.IsPaused &&
            !(GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver))
            Time.timeScale = 1f;

        // Re-enable chapter-transition cutscenes for future chapters.
        sm.playChapterTransitionCutscene = prevChapterCutsceneFlag;

        Debug.Log($"[CompanionManager] Skip logic complete. Player teleported to {skipTeleportPosition}, checkpoint set to SaveRoom_Ch5 ({ch5Checkpoint}), should now fight Tank boss with reduced HP ({skippedTankMaxHealth}).");
    }

    /// <summary>
    /// Unlocks and auto-completes every Chapter 4 side quest, granting their
    /// rewards (EXP + journal). Used by the skip path so the player doesn't
    /// miss out on side quest rewards when skipping Ch4.
    /// </summary>
    private void AutoCompleteChapter4SideQuests()
    {
        var sqm = SideQuestManager.Instance;
        if (sqm == null)
        {
            Debug.LogWarning("[CompanionManager] SideQuestManager not found; cannot auto-complete Ch4 side quests.");
            return;
        }
        var quests = sqm.GetChapterSideQuests(4);
        if (quests == null) return;
        foreach (var q in quests)
        {
            if (q == null) continue;
            if (sqm.IsCompleted(q)) continue;
            // Unlock first (so CompleteSideQuest sees it as active), then complete.
            if (!sqm.IsActive(q)) sqm.UnlockChapterPublic(4);
            sqm.CompleteSideQuest(q);
        }
        Debug.Log($"[CompanionManager] Auto-completed {quests.Length} Chapter 4 side quests (rewards granted).");
    }

    /// <summary>
    /// Collects and hides every Collectible under Ch4_Residential so the skip
    /// path doesn't leave uncollected journals visible in the world. Each
    /// collectible is registered in CollectibleManager (so the journal gallery
    /// reflects it) and its GameObject is deactivated.
    /// </summary>
    private void CollectAllChapter4Collectibles()
    {
        var ch4 = GameObject.Find("=== WORLD ===/StoryZones/Ch4_Residential");
        if (ch4 == null)
        {
            Debug.LogWarning("[CompanionManager] Ch4_Residential not found; cannot collect Ch4 collectibles.");
            return;
        }
        int count = 0;
        for (int i = 0; i < ch4.transform.childCount; i++)
        {
            var col = ch4.transform.GetChild(i).GetComponent<Collectible>();
            if (col != null && !col.IsPicked)
            {
                col.Collect();
                count++;
            }
        }
        Debug.Log($"[CompanionManager] Collected {count} Ch4 collectibles (journals registered + hidden).");
    }

    /// <summary>
    /// Marks the SaveRoom_Ch4 chapter-transition cutscene as already played so
    /// re-entering the save room doesn't replay the "CHƯƠNG 4" banner. The
    /// checkpoint is NOT set here — it is set later to SaveRoom_Ch5's position.
    /// </summary>
    private void MarkSaveRoomCh4CutscenePlayed()
    {
        var saveRoomGO = GameObject.Find("=== WORLD ===/StoryZones/Ch4_Residential/SaveRoom_Ch4");
        if (saveRoomGO == null) return;
        var sr = saveRoomGO.GetComponent<SaveRoom>();
        if (sr == null) return;
        var field = typeof(SaveRoom).GetField("_cutscenePlayed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(sr, true);
        Debug.Log("[CompanionManager] Marked SaveRoom_Ch4 cutscene as played (no re-trigger on re-enter).");
    }

    /// <summary>
    /// Resolves the Chapter 5 SaveRoom's checkpoint position — the position
    /// the player should respawn at after dying during the Ch5 boss fight.
    /// Used by the skip flow so death respawns the player at the save zone
    /// (where they can heal + re-prepare) instead of at the boss-fight
    /// teleport spot.
    ///
    /// Falls back to skipTeleportPosition if SaveRoom_Ch5 is not found, so
    /// the checkpoint is always set to a sane value.
    /// </summary>
    private Vector3 ResolveChapter5SaveRoomCheckpoint()
    {
        var saveRoomGO = GameObject.Find("=== WORLD ===/StoryZones/Ch5_ApartmentBridge/SaveRoom_Ch5");
        if (saveRoomGO != null)
        {
            var sr = saveRoomGO.GetComponent<SaveRoom>();
            if (sr != null)
            {
                // SaveRoom.CheckpointPosition returns _checkpointPos, which is
                // set in Start() to respawnPoint.position (or transform.position
                // if respawnPoint is null). At runtime (after Start), this is
                // the correct respawn position.
                var cp = sr.CheckpointPosition;
                if (cp != Vector3.zero)
                {
                    Debug.Log($"[CompanionManager] Skip checkpoint set to SaveRoom_Ch5 at {cp}.");
                    return cp;
                }
            }
        }
        // Fallback: use the teleport position if SaveRoom_Ch5 isn't found.
        Debug.LogWarning("[CompanionManager] SaveRoom_Ch5 not found; falling back to skipTeleportPosition for checkpoint.");
        return skipTeleportPosition;
    }

    /// <summary>
    /// Teleports the player and companion to the given world position. The
    /// player is warped via a combination of PlayerStats.Respawn (heals + fires
    /// OnRespawn event), direct Rigidbody positioning, and Transform sync.
    /// The companion is warped via CompanionAI.TeleportNearPlayer.
    ///
    /// NOTE: Time.timeScale MUST be > 0 for the Rigidbody→Transform sync to
    /// happen on the next physics step. This method temporarily restores
    /// timeScale if it's 0 (e.g. right after a cutscene).
    /// </summary>
    private void TeleportPlayerAndCompanion(Vector3 position)
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            Debug.LogWarning("[CompanionManager] Player not found; cannot teleport.");
            return;
        }

        // Restore timeScale if it's 0 — the Rigidbody→Transform sync needs a
        // physics step, which only runs when timeScale > 0.
        if (Time.timeScale == 0f)
        {
            bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
            bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
            if (!pauseOpen && !gameOver) Time.timeScale = 1f;
        }

        // 1) Try PlayerStats.Respawn (fires OnRespawn event → PlayerMovement.TeleportPlayer).
        var ps = playerGO.GetComponentInParent<PlayerStats>();
        if (ps == null) ps = playerGO.GetComponentInChildren<PlayerStats>();
        if (ps != null)
        {
            ps.Respawn(position);
            Debug.Log($"[CompanionManager] Player teleported to {position} via PlayerStats.Respawn.");
        }

        // 2) Direct warp on the Rigidbody's GameObject. The Rigidbody is
        //    typically on the tagged "Player" child (not the root), so we set
        //    both rb.position and the transform of the rb's GameObject.
        var rb = playerGO.GetComponentInParent<Rigidbody>();
        if (rb == null) rb = playerGO.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            // Also set the transform of the GameObject that owns the Rigidbody
            // so it syncs immediately (without waiting for a physics step).
            rb.transform.position = position;
        }
        else
        {
            // No Rigidbody — just set the transform directly.
            playerGO.transform.position = position;
        }

        // 3) Sync transforms so the hierarchy reflects the new positions
        //    immediately (critical when timeScale was just restored from 0).
        Physics.SyncTransforms();

        Debug.Log($"[CompanionManager] Player direct-warp to {position} (rb+transform+sync).");

        // 4) Teleport the companion near the player.
        if (CompanionAI != null)
        {
            CompanionAI.TeleportNearPlayer(2.5f);
            Debug.Log("[CompanionManager] Companion teleported near player.");
        }
    }

    /// <summary>
    /// Plays the skip-path cutscene ("Tìm thấy kíp nổ") as a narrative bridge
    /// between Chapter 4 and Chapter 5. Creates a transient CutscenePlayer,
    /// plays it, and yields until it completes.
    /// </summary>
    private System.Collections.IEnumerator PlaySkipCutscene()
    {
        var go = new GameObject("CompanionSkipCutscene");
        // Parent under the manager so it doesn't get destroyed prematurely.
        go.transform.SetParent(transform, false);
        var cutscene = go.AddComponent<CutscenePlayer>();
        cutscene.title = skipCutsceneTitle;
        cutscene.body = skipCutsceneBody;
        cutscene.hold = skipCutsceneHold;
        cutscene.fadeIn = 0.6f;
        cutscene.fadeOut = 1.0f;

        // Ensure timeScale is 1 before the cutscene starts so CutscenePlayer
        // captures prevTimeScale=1 and restores it to 1 (not 0) when done.
        // The dialogue bubble / ResolveChoice path may have left it at 0.
        if (Time.timeScale == 0f)
        {
            bool pauseOpen = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
            bool gameOver = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
            if (!pauseOpen && !gameOver) Time.timeScale = 1f;
        }

        bool done = false;
        cutscene.Play(() => done = true);

        // Wait until the cutscene finishes (it pauses Time.timeScale internally).
        while (!done) yield return null;

        // Clean up the transient cutscene GameObject.
        Destroy(go);
    }
}
