using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// Central PlayFab integration: handles authentication (register / login / logout)
/// and cloud save/load of all player data (best score, best wave, achievements,
/// stats). Designed as a singleton that persists across scenes.
///
/// Data is stored using PlayFab UserData (key/value pairs). Each save uploads a
/// JSON blob per data category. On login, all data is pulled from the cloud and
/// merged into PlayerPrefs so existing systems (AchievementManager, GameOverManager,
/// MainMenuManager) keep working unchanged.
/// </summary>
public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance { get; private set; }

    [Header("PlayFab Settings")]
    [Tooltip("Your PlayFab Title ID from the PlayFab Game Manager (https://developer.playfab.com).")]
    public string titleId = "";

    [Header("Auto-save")]
    [Tooltip("If true, uploads player data to the cloud at regular intervals while logged in.")]
    public bool autoSaveEnabled = true;
    [Tooltip("Auto-save interval in seconds.")]
    public float autoSaveInterval = 60f;

    /// <summary>True if the player is currently logged in to PlayFab.</summary>
    public bool IsLoggedIn { get; private set; }

    /// <summary>The PlayFab EntityId / PlayFabId of the currently logged-in player.</summary>
    public string PlayFabId { get; private set; }

    /// <summary>The display name (username) of the currently logged-in player.</summary>
    public string Username { get; private set; }

    // ---- Events ----
    /// <summary>Fired on successful login or register. (username)</summary>
    public event Action<string> OnLoginSuccess;

    /// <summary>Fired on login/register failure. (errorMessage)</summary>
    public event Action<string> OnLoginError;

    /// <summary>Fired when cloud data has been downloaded and merged into PlayerPrefs. (success)</summary>
    public event Action<bool> OnCloudDataLoaded;

    /// <summary>Fired when cloud data has been uploaded. (success)</summary>
    public event Action<bool> OnCloudDataSaved;

    /// <summary>Fired when the player has logged out.</summary>
    public event Action OnLogout;

    // ---- Cloud data keys ----
    private const string KeyPlayerStats = "player_stats";
    private const string KeyAchievements = "achievements";

    private float _autoSaveTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!string.IsNullOrEmpty(titleId))
            PlayFabSettings.TitleId = titleId;
    }

    private void Start()
    {
        // If TitleId is set in PlayFabSettings via editor config, use that
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId) && !string.IsNullOrEmpty(titleId))
            PlayFabSettings.TitleId = titleId;
    }

    private void Update()
    {
        if (!autoSaveEnabled || !IsLoggedIn) return;
        _autoSaveTimer += Time.deltaTime;
        if (_autoSaveTimer >= autoSaveInterval)
        {
            _autoSaveTimer = 0f;
            SaveAllToCloud();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && IsLoggedIn)
            SaveAllToCloud();
    }

    private void OnApplicationQuit()
    {
        if (IsLoggedIn)
            SaveAllToCloud();
    }

    // =========================================================================
    //  Authentication
    // =========================================================================

    /// <summary>Register a new account with username + password.</summary>
    public void Register(string username, string password, Action<bool, string> callback = null)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            string msg = "Username and password cannot be empty.";
            OnLoginError?.Invoke(msg);
            callback?.Invoke(false, msg);
            return;
        }

        if (password.Length < 6)
        {
            string msg = "Password must be at least 6 characters.";
            OnLoginError?.Invoke(msg);
            callback?.Invoke(false, msg);
            return;
        }

        var request = new RegisterPlayFabUserRequest
        {
            Username = username,
            Password = password,
            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(request,
            result =>
            {
                Debug.Log($"[PlayFab] Registration successful: {result.Username}");
                PlayFabId = result.PlayFabId;
                Username = result.Username;
                IsLoggedIn = true;
                // Clear any local data left over from a previous account so the
                // new account starts at 0 (no inherited BestScore / achievements).
                ClearLocalPlayerData();
                OnLoginSuccess?.Invoke(Username);
                // Upload the (now clean) local data to the new account
                SaveAllToCloud();
                callback?.Invoke(true, null);
            },
            error =>
            {
                string msg = ParseError(error);
                Debug.LogWarning($"[PlayFab] Registration failed: {msg}");
                OnLoginError?.Invoke(msg);
                callback?.Invoke(false, msg);
            });
    }

    /// <summary>Login with username + password.</summary>
    public void Login(string username, string password, Action<bool, string> callback = null)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            string msg = "Username and password cannot be empty.";
            OnLoginError?.Invoke(msg);
            callback?.Invoke(false, msg);
            return;
        }

        var request = new LoginWithPlayFabRequest
        {
            Username = username,
            Password = password,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetUserData = true,
                UserDataKeys = new List<string> { KeyPlayerStats, KeyAchievements }
            }
        };

        PlayFabClientAPI.LoginWithPlayFab(request,
            result =>
            {
                Debug.Log($"[PlayFab] Login successful: {username}");
                PlayFabId = result.PlayFabId;
                Username = username;
                IsLoggedIn = true;

                // Clear any local data left over from a previous account so the
                // merge below starts from a clean slate (no stale BestScore /
                // achievements from another user).
                ClearLocalPlayerData();

                // Merge cloud data into PlayerPrefs
                MergeCloudDataToLocal(result.InfoResultPayload);

                OnLoginSuccess?.Invoke(Username);
                callback?.Invoke(true, null);
            },
            error =>
            {
                string msg = ParseError(error);
                Debug.LogWarning($"[PlayFab] Login failed: {msg}");
                OnLoginError?.Invoke(msg);
                callback?.Invoke(false, msg);
            });
    }

    /// <summary>Logout and clear local session.</summary>
    public void Logout()
    {
        if (!IsLoggedIn) return;
        SaveAllToCloud();
        PlayFabClientAPI.ForgetAllCredentials();
        IsLoggedIn = false;
        PlayFabId = null;
        Username = null;
        // Clear local player data so the next account starts fresh and does
        // not inherit the previous user's BestScore / BestWave / achievements.
        ClearLocalPlayerData();
        Debug.Log("[PlayFab] Logged out.");
        OnLogout?.Invoke();
    }

    // =========================================================================
    //  Cloud Save / Load
    // =========================================================================

    /// <summary>
    /// Clear all player-specific local data (PlayerPrefs + AchievementManager
    /// in-memory cache). Call this when switching accounts so the new account
    /// does not inherit the previous user's BestScore / BestWave / achievements.
    /// </summary>
    public void ClearLocalPlayerData()
    {
        PlayerPrefs.DeleteKey("BestScore");
        PlayerPrefs.DeleteKey("BestWave");

        var am = AchievementManager.Instance;
        if (am != null && am.achievements != null)
        {
            foreach (var ach in am.achievements)
            {
                if (ach == null) continue;
                PlayerPrefs.DeleteKey(ach.UnlockedKey);
                PlayerPrefs.DeleteKey(ach.ProgressKey);
            }
        }

        PlayerPrefs.Save();

        // Reload AchievementManager's in-memory cache so it reflects the
        // now-cleared PlayerPrefs (all achievements locked, progress at 0).
        if (am != null)
            am.ReloadState();

        Debug.Log("[PlayFab] Cleared local player data (PlayerPrefs + achievements).");
    }

    /// <summary>Upload all local player data to PlayFab cloud.</summary>
    public void SaveAllToCloud()
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("[PlayFab] Cannot save: not logged in.");
            return;
        }

        var data = new Dictionary<string, string>
        {
            [KeyPlayerStats] = BuildPlayerStatsJson(),
            [KeyAchievements] = BuildAchievementsJson()
        };

        var request = new UpdateUserDataRequest { Data = data };
        PlayFabClientAPI.UpdateUserData(request,
            result =>
            {
                Debug.Log("[PlayFab] Cloud save successful.");
                OnCloudDataSaved?.Invoke(true);
            },
            error =>
            {
                Debug.LogWarning($"[PlayFab] Cloud save failed: {ParseError(error)}");
                OnCloudDataSaved?.Invoke(false);
            });
    }

    /// <summary>Download player data from PlayFab cloud and merge into PlayerPrefs.</summary>
    public void LoadFromCloud(Action<bool> callback = null)
    {
        if (!IsLoggedIn)
        {
            callback?.Invoke(false);
            return;
        }

        var request = new GetUserDataRequest
        {
            Keys = new List<string> { KeyPlayerStats, KeyAchievements }
        };

        PlayFabClientAPI.GetUserData(request,
            result =>
            {
                MergeCloudDataToLocal(result.Data);
                OnCloudDataLoaded?.Invoke(true);
                callback?.Invoke(true);
            },
            error =>
            {
                Debug.LogWarning($"[PlayFab] Cloud load failed: {ParseError(error)}");
                OnCloudDataLoaded?.Invoke(false);
                callback?.Invoke(false);
            });
    }

    // =========================================================================
    //  Data Serialization
    // =========================================================================

    private string BuildPlayerStatsJson()
    {
        var data = new PlayerStatsCloudData
        {
            bestScore = PlayerPrefs.GetInt("BestScore", 0),
            bestWave = PlayerPrefs.GetInt("BestWave", 0),
            playTime = 0f,
            totalKills = 0,
            totalDeaths = 0
        };

        // Pull live stats from PlayerStatsTracker if available
        var tracker = PlayerStatsTracker.Instance;
        if (tracker != null)
        {
            data.playTime = tracker.GetPlayTime();
            data.totalKills = tracker.zombieKills;
            data.totalDeaths = 0; // deaths not tracked persistently
        }

        return JsonUtility.ToJson(data);
    }

    private string BuildAchievementsJson()
    {
        var data = new AchievementsCloudData { entries = new List<AchievementEntry>() };

        var am = AchievementManager.Instance;
        if (am != null && am.achievements != null)
        {
            foreach (var ach in am.achievements)
            {
                if (ach == null) continue;
                data.entries.Add(new AchievementEntry
                {
                    id = ach.id,
                    unlocked = PlayerPrefs.GetInt(ach.UnlockedKey, 0) == 1,
                    progress = PlayerPrefs.GetInt(ach.ProgressKey, 0)
                });
            }
        }

        return JsonUtility.ToJson(data);
    }

    private void MergeCloudDataToLocal(PlayFab.ClientModels.GetPlayerCombinedInfoResultPayload info)
    {
        if (info == null || info.UserData == null)
        {
            OnCloudDataLoaded?.Invoke(false);
            return;
        }

        var dict = new Dictionary<string, string>();
        foreach (var kvp in info.UserData)
            dict[kvp.Key] = kvp.Value.Value;
        MergeCloudDataToLocal(dict);
    }

    private void MergeCloudDataToLocal(Dictionary<string, UserDataRecord> data)
    {
        if (data == null)
        {
            OnCloudDataLoaded?.Invoke(false);
            return;
        }

        var dict = new Dictionary<string, string>();
        foreach (var kvp in data)
            dict[kvp.Key] = kvp.Value.Value;
        MergeCloudDataToLocal(dict);
    }

    private void MergeCloudDataToLocal(Dictionary<string, string> data)
    {
        if (data == null)
        {
            OnCloudDataLoaded?.Invoke(false);
            return;
        }

        // Merge player stats — always take the higher value (best score/wave)
        if (data.TryGetValue(KeyPlayerStats, out string statsJson))
        {
            var cloud = JsonUtility.FromJson<PlayerStatsCloudData>(statsJson);
            if (cloud != null)
            {
                int localBestScore = PlayerPrefs.GetInt("BestScore", 0);
                int localBestWave = PlayerPrefs.GetInt("BestWave", 0);
                if (cloud.bestScore > localBestScore)
                    PlayerPrefs.SetInt("BestScore", cloud.bestScore);
                if (cloud.bestWave > localBestWave)
                    PlayerPrefs.SetInt("BestWave", cloud.bestWave);
                PlayerPrefs.Save();
                Debug.Log($"[PlayFab] Merged player stats: BestScore={cloud.bestScore}, BestWave={cloud.bestWave}");
            }
        }

        // Merge achievements — take unlocked state and max progress
        if (data.TryGetValue(KeyAchievements, out string achJson))
        {
            var cloud = JsonUtility.FromJson<AchievementsCloudData>(achJson);
            if (cloud != null && cloud.entries != null)
            {
                foreach (var entry in cloud.entries)
                {
                    if (string.IsNullOrEmpty(entry.id)) continue;

                    // Find the matching AchievementData to get the PlayerPrefs keys
                    var am = AchievementManager.Instance;
                    if (am != null && am.achievements != null)
                    {
                        foreach (var ach in am.achievements)
                        {
                            if (ach != null && ach.id == entry.id)
                            {
                                if (entry.unlocked)
                                    PlayerPrefs.SetInt(ach.UnlockedKey, 1);
                                int localProgress = PlayerPrefs.GetInt(ach.ProgressKey, 0);
                                if (entry.progress > localProgress)
                                    PlayerPrefs.SetInt(ach.ProgressKey, entry.progress);
                                break;
                            }
                        }
                    }
                }
                PlayerPrefs.Save();
                Debug.Log($"[PlayFab] Merged {cloud.entries.Count} achievement entries from cloud.");
            }
        }

        // Reload AchievementManager's in-memory cache so it reflects the
        // freshly merged PlayerPrefs values (unlock state + progress).
        var amReload = AchievementManager.Instance;
        if (amReload != null)
            amReload.ReloadState();

        OnCloudDataLoaded?.Invoke(true);
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private string ParseError(PlayFabError error)
    {
        if (error == null) return "Unknown error";
        if (!string.IsNullOrEmpty(error.ErrorMessage)) return error.ErrorMessage;
        return error.Error.ToString();
    }

    // =========================================================================
    //  Data Structures
    // =========================================================================

    [Serializable]
    public class PlayerStatsCloudData
    {
        public int bestScore;
        public int bestWave;
        public float playTime;
        public int totalKills;
        public int totalDeaths;
    }

    [Serializable]
    public class AchievementsCloudData
    {
        public List<AchievementEntry> entries;
    }

    [Serializable]
    public class AchievementEntry
    {
        public string id;
        public bool unlocked;
        public int progress;
    }
}
