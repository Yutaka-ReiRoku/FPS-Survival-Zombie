using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    public static JournalUI Instance;

    public GameObject panel;

    public TMP_Text title;

    public TMP_Text content;

    public Image image;

    public AudioSource audioSource;

    void Awake()
    {
        Instance = this;

        panel.SetActive(false);
    }

    public void Show(JournalData journal)
    {
        panel.SetActive(true);

        title.text = journal.title;
        content.text = journal.content;

        image.sprite = journal.image;

        if (journal.voiceLog != null)
        {
            audioSource.Stop();
            audioSource.clip = journal.voiceLog;
            audioSource.Play();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0;
    }

    public void Close()
    {
        panel.SetActive(false);

        audioSource.Stop();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1;
    }
}