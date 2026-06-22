using UnityEngine;

[CreateAssetMenu(menuName = "Collectibles/Journal")]
public class JournalData : ScriptableObject
{
    public int id;

    public string title;

    [TextArea(10, 20)]
    public string content;

    public Sprite image;

    public AudioClip voiceLog;
}