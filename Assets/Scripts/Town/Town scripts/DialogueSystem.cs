using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DialogueSystem : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject panel;      // PanelDialogue (root)
    public TMP_Text txtSpeaker;   // Speaker label
    public TMP_Text txtLine;      // Dialogue content
    public TMP_Text txtHint;      // Hint text

    [Header("Typing")]
    [Tooltip("Seconds between characters when typing.")]
    public float charInterval = 0.02f;

    [Header("Input")]
    public KeyCode advanceKey = KeyCode.E;   // Only E to advance/skip

    [Header("Hint Text (English)")]
    [TextArea] public string hintWhileTyping  = "Press E to skip";
    [TextArea] public string hintWhenComplete = "Press E to continue";

    [Header("Next Scene")]
    [Tooltip("When the last line is confirmed with E, load this scene.")]
    public bool loadNextSceneOnEnd = true;
    public string nextSceneName = "Loadout"; // <-- change to your target scene name

    // Runtime state
    private DialogueData data;
    private int idx = 0;
    private bool typing = false;
    private Coroutine co;

    public bool IsPlaying => panel != null && panel.activeSelf;

    void Awake()
    {
        // Always start hidden even if panel is active in editor
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>Begin a dialogue with given data.</summary>
    public void StartDialogue(DialogueData d)
    {
        if (d == null) return;

        data = d;
        idx = 0;
        typing = false;

        if (panel != null) panel.SetActive(true);
        Show();
    }

    /// <summary>
    /// NPC passes the world anchor (e.g., DialogueAnchor at NPC head) to the follower.
    /// Works whether DialogueWorldFollower is on the panel itself or a child.
    /// </summary>
    public void SetFollowerTarget(Transform t)
    {
        if (!panel) return;

        // Prefer on panel, otherwise search children
        var follower = panel.GetComponent<DialogueWorldFollower>();
        if (!follower)
            follower = panel.GetComponentInChildren<DialogueWorldFollower>(true);

        if (follower)
            follower.SetTarget(t);
#if UNITY_EDITOR
        else
            Debug.LogWarning("[DialogueSystem] No DialogueWorldFollower found on panel or its children.", this);
#endif
    }

    void Show()
    {
        if (data == null || data.lines == null || idx >= data.lines.Length)
        {
            End();
            return;
        }

        var L = data.lines[idx];

        if (txtSpeaker) txtSpeaker.text = L.speaker;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(TypeLine(L.text));
    }

    IEnumerator TypeLine(string s)
    {
        typing = true;

        if (txtLine) txtLine.text = string.Empty;
        if (txtHint) txtHint.text = hintWhileTyping;

        foreach (char c in s)
        {
            if (txtLine) txtLine.text += c;
            yield return new WaitForSeconds(charInterval);
        }

        typing = false;
        if (txtHint) txtHint.text = hintWhenComplete;
    }

    void Update()
    {
        if (!IsPlaying) return;

        if (Input.GetKeyDown(advanceKey))
        {
            if (typing)
            {
                // Skip typing → show full current line immediately
                if (co != null) StopCoroutine(co);

                if (txtLine && data != null && data.lines != null && idx < data.lines.Length)
                    txtLine.text = data.lines[idx].text;

                typing = false;
                if (txtHint) txtHint.text = hintWhenComplete;
            }
            else
            {
                // Go next, or finish
                idx++;
                if (data == null || data.lines == null || idx >= data.lines.Length)
                {
                    End();
                }
                else
                {
                    Show();
                }
            }
        }
    }

    /// <summary>Close dialogue and optionally load the next scene.</summary>
    public void End()
    {
        if (panel) panel.SetActive(false);

        // Clear follower target so panel won't keep following anything after close
        DialogueWorldFollower follower = null;
        if (panel)
        {
            follower = panel.GetComponent<DialogueWorldFollower>();
            if (!follower) follower = panel.GetComponentInChildren<DialogueWorldFollower>(true);
        }
        if (follower) follower.SetTarget(null);

        // Cache transition flag before reset
        bool goNext = loadNextSceneOnEnd && !string.IsNullOrEmpty(nextSceneName);

        // Reset local state
        data = null;
        idx = 0;
        typing = false;
        co = null;

        // Scene transition
        if (goNext)
            SceneManager.LoadScene(nextSceneName);
    }
}
