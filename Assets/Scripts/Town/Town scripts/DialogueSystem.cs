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

    [Header("Optional: Shop Panel (for AfterAction.OpenShopPanel)")]
    [Tooltip("如果某个 DialogueData 的 AfterAction=OpenShopPanel，就会在结束时把这个面板 SetActive(true)。")]
    public GameObject shopPanelToOpen;

    // Runtime state
    private DialogueData data;
    private int idx = 0;
    private bool typing = false;
    private Coroutine co;

    public bool IsPlaying => panel != null && panel.activeSelf;

    void Awake()
    {
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

    /// <summary>让对话框跟随一个世界坐标锚点（比如 NPC 头顶）</summary>
    public void SetFollowerTarget(Transform t)
    {
        if (!panel) return;

        var follower = panel.GetComponent<DialogueWorldFollower>();
        if (!follower)
            follower = panel.GetComponentInChildren<DialogueWorldFollower>(true);

        if (follower)
            follower.SetTarget(t);
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
                // 跳过打字机，直接显示整行
                if (co != null) StopCoroutine(co);

                if (txtLine && data != null && data.lines != null && idx < data.lines.Length)
                    txtLine.text = data.lines[idx].text;

                typing = false;
                if (txtHint) txtHint.text = hintWhenComplete;
            }
            else
            {
                // 下一句 / 结束
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

    /// <summary>关闭对话，并根据当前 DialogueData 的 AfterAction 执行后续动作。</summary>
    public void End()
    {
        if (panel) panel.SetActive(false);

        // 取消跟随
        DialogueWorldFollower follower = null;
        if (panel)
        {
            follower = panel.GetComponent<DialogueWorldFollower>();
            if (!follower) follower = panel.GetComponentInChildren<DialogueWorldFollower>(true);
        }
        if (follower) follower.SetTarget(null);

        // 先把当前 data 缓存下来，等会儿用它的 afterAction
        DialogueData.AfterAction action = DialogueData.AfterAction.None;
        string sceneName = null;

        if (data != null)
        {
            action = data.afterAction;
            sceneName = data.shopSceneName;
        }

        // 重置本地状态
        data = null;
        idx = 0;
        typing = false;
        co = null;

        // ========= 根据 AfterAction 执行后续 =========
        switch (action)
        {
            case DialogueData.AfterAction.None:
                // 什么都不做，只是关掉对话
                break;

            case DialogueData.AfterAction.OpenShopScene:
                if (!string.IsNullOrEmpty(sceneName))
                {
                    SceneManager.LoadScene(sceneName);
                }
                else
                {
                    Debug.LogWarning("[DialogueSystem] AfterAction.OpenShopScene set, but shopSceneName is empty.");
                }
                break;

            case DialogueData.AfterAction.OpenShopPanel:
                if (shopPanelToOpen != null)
                {
                    shopPanelToOpen.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("[DialogueSystem] AfterAction.OpenShopPanel set, but shopPanelToOpen is not assigned.", this);
                }
                break;
        }
    }
}
