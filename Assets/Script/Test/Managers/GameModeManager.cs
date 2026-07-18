using UnityEngine;

public enum GameMode
{
    Story,
    Endless
}

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance;

    [SerializeField] private GameMode currentMode = GameMode.Story;

    public static GameMode CurrentMode => Instance != null ? Instance.currentMode : GameMode.Story;

    private void Awake()
    {
        Instance = this;
    }

    public void SetMode(GameMode mode)
    {
        currentMode = mode;
    }
}
