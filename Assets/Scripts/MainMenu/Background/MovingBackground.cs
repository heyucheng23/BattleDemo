using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UVScroller : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.02f, 0f); // x>0 向右，y>0 向上
    RawImage ri;

    void Awake() { ri = GetComponent<RawImage>(); }

    void Update()
    {
        if (!ri || ri.texture == null) return;
        var r = ri.uvRect;
        r.x += speed.x * Time.unscaledDeltaTime;
        r.y += speed.y * Time.unscaledDeltaTime;
        ri.uvRect = r; // 自动循环
    }
}
