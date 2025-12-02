using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneStartFader : MonoBehaviour
{
    [Header("淡入参数")]
    [Range(0.1f, 3f)]
    public float fadeDuration = 0.8f;       // 淡入时间
    public Color fadeColor = Color.black;   // 一开始的遮罩颜色（一般黑色）

    private CanvasGroup canvasGroup;
    private GameObject canvasRoot;          // 记录我们创建的 Canvas，方便最后销毁

    void Start()
    {
        SetupCanvas();
        StartCoroutine(FadeIn());
    }

    void SetupCanvas()
    {
        // 1. 创建一个全屏 Canvas（只负责显示，不接收点击）
        canvasRoot = new GameObject("SceneStartFaderCanvas");
        canvasRoot.transform.SetParent(transform);
        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;   // 在最上层

        // ❌ 不要加 GraphicRaycaster，这样整个 Canvas 不会拦截 UI 事件
        // canvasRoot.AddComponent<GraphicRaycaster>();

        // 2. 创建一个全屏 Image 作为遮罩
        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasRoot.transform);
        Image img = imageGO.AddComponent<Image>();
        img.color = fadeColor;

        // ⭐ 关键：让这块遮罩不拦截点击
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 3. 加 CanvasGroup 控制透明度
        canvasGroup = imageGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f; // 一开始全黑
    }

    IEnumerator FadeIn()
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeDuration); // 1 → 0
            canvasGroup.alpha = a;
            yield return null;
        }

        canvasGroup.alpha = 0f;

        // ⭐ 关键：淡入完成后删除整个 Canvas，彻底不留遮挡物
        if (canvasRoot != null)
        {
            Destroy(canvasRoot);
        }
    }
}
