using UnityEngine;
using UnityEngine.UI;

public class UIFrameAnimationSafe : MonoBehaviour
{
    public Image targetImage;          // 拖HeroPortrait上的Image（可不填）
    public Sprite defaultSprite;       // 默认静态图（强烈建议填）
    public Sprite[] frames;            // 在Inspector里把切出来的帧全拖进来
    public float frameRate = 0.1f;     // 0.1 ≈ 10FPS

    int index = 0;
    float timer = 0f;

    void Awake()
    {
        if (!targetImage) targetImage = GetComponent<Image>();
    }

    void Start()
    {
        if (!targetImage) { enabled = false; return; }

        if (frames != null && frames.Length > 0)
        {
            targetImage.sprite = frames[0];   // 先给首帧，避免白板
        }
        else if (defaultSprite)
        {
            targetImage.sprite = defaultSprite; // 没有frames就用默认图
            enabled = false;                    // 不播放动画
        }
        else
        {
            Debug.LogWarning("[UIFrameAnimationSafe] No frames/defaultSprite set.");
            enabled = false; // 防止空值导致白板
        }
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || !targetImage) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            index = (index + 1) % frames.Length;
            targetImage.sprite = frames[index];
        }
    }
}
