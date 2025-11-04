using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class DialogueWorldFollower : MonoBehaviour
{
    [Header("Target")]
    public Transform worldTarget;                 // NPC头顶的 DialogueAnchor
    public Vector2 screenOffset = new Vector2(0f, 80f);

    [Header("Clamp")]
    public bool clampToCanvas = true;
    public Vector2 clampPadding = new Vector2(20f, 20f);

    [Header("Debug")]
    public bool logDebug = false;

    Camera cam;
    Canvas canvas;
    RectTransform rect;
    RectTransform canvasRect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            if (logDebug) Debug.LogWarning("[Follower] No Canvas found in parents.", this);
        }
        else
        {
            canvasRect = canvas.transform as RectTransform;
            if (logDebug) Debug.Log($"[Follower] Canvas mode={canvas.renderMode}", this);
        }

        // 推荐：底边对齐锚点
        if (rect) rect.pivot = new Vector2(0.5f, 0f);

        cam = Camera.main;
    }

    public void SetTarget(Transform t)
    {
        worldTarget = t;
        if (logDebug) Debug.Log($"[Follower] SetTarget = {(t ? t.name : "NULL")}", this);
    }

    void LateUpdate()
    {
        if (!isActiveAndEnabled || rect == null || canvasRect == null) return;

        if (worldTarget == null)
        {
            // 没目标 → 不动
            return;
        }

        if (cam == null) cam = Camera.main;

        // 1) 世界 → 屏幕
        Vector3 screenPos = cam.WorldToScreenPoint(worldTarget.position);

        // 2) 屏幕 → Canvas局部
        Vector2 localPoint;
        Camera canvasCam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvasCam, out localPoint);

        // 3) 偏移
        localPoint += screenOffset;

        // 4) 约束
        if (clampToCanvas)
        {
            Vector2 half = canvasRect.rect.size * 0.5f;
            localPoint.x = Mathf.Clamp(localPoint.x, -half.x + clampPadding.x, half.x - clampPadding.x);
            localPoint.y = Mathf.Clamp(localPoint.y, -half.y + clampPadding.y, half.y - clampPadding.y);
        }

        rect.anchoredPosition = localPoint;

        if (logDebug)
            Debug.Log($"[Follower] anchored={rect.anchoredPosition}, target={worldTarget.position}", this);
    }
}
