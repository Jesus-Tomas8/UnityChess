using UnityEngine;
using UnityEngine.UI;

public class EndgameBanner : MonoBehaviour
{
    [ContextMenu("TEST - Show")]
    void TestShowCtx() { Show("Test banner"); }

    [ContextMenu("TEST - Hide")]
    void TestHideCtx() { Hide(); }


    public CanvasGroup group;
    public Text label;  // or TMP_Text if you use TextMeshPro

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(string message)
    {
        if (label) label.text = message;
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        Time.timeScale = 0f; // pause game; remove if you prefer
    }

    public void Hide()
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        Time.timeScale = 1f;
    }
}
