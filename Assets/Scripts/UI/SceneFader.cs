using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SceneFader : MonoBehaviour
{
    [Tooltip("淡入 / 淡出的时间（秒）")]
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private bool isFading = false;

    private void Awake()
    {
        // 自动获取同一个物体上的 CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        // 场景一开始先黑 → 再淡出
        canvasGroup.alpha = 1f;
        StartCoroutine(Fade(1f, 0f));
    }

    /// <summary>
    /// 供外部调用：淡出并切场景
    /// </summary>
    public void FadeToScene(string sceneName)
    {
        if (isFading) return;
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator Fade(float from, float to)
    {
        isFading = true;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(from, to, progress);
            yield return null;
        }

        canvasGroup.alpha = to;
        isFading = false;
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        // 先从当前透明 → 全黑
        yield return Fade(0f, 1f);

        // 加载新场景
        SceneManager.LoadScene(sceneName);
    }
}
