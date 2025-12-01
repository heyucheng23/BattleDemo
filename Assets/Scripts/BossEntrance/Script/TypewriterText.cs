using System.Collections;
using UnityEngine;
using TMPro;

public class TypewriterText : MonoBehaviour
{
    [Header("基本设置")]
    public TextMeshProUGUI text;          // 对话的 TMP 文本
    public float charsPerSecond = 30f;    // 每秒打几个字（20~40 比较舒服）

    [Header("音效（可选）")]
    public AudioSource audioSource;       // 播 blip 的 AudioSource
    public AudioClip blipClip;            // blip 音效

    public bool IsTyping { get; private set; }

    private string fullText;
    private Coroutine typingRoutine;

    void Awake()
    {
        if (text == null)
            text = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// 开始打字显示一整句
    /// </summary>
    public void StartTyping(string line)
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        fullText = line;
        typingRoutine = StartCoroutine(TypeCoroutine());
    }

    /// <summary>
    /// 跳过打字，直接显示整句
    /// </summary>
    public void Skip()
    {
        if (!IsTyping) return;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        text.text = fullText;
        IsTyping = false;
        typingRoutine = null;
    }

    private IEnumerator TypeCoroutine()
    {
        IsTyping = true;
        text.text = "";

        if (string.IsNullOrEmpty(fullText))
        {
            IsTyping = false;
            yield break;
        }

        float tPerChar = 1f / Mathf.Max(charsPerSecond, 1f);

        for (int i = 0; i < fullText.Length; i++)
        {
            char c = fullText[i];
            text.text += c;

            // 播放 blip 音效（只对字母/数字，不对空格标点）
            if (audioSource != null && blipClip != null)
            {
                if (char.IsLetterOrDigit(c))
                {
                    audioSource.PlayOneShot(blipClip);
                }
            }

            yield return new WaitForSeconds(tPerChar);
        }

        IsTyping = false;
        typingRoutine = null;
    }
}
