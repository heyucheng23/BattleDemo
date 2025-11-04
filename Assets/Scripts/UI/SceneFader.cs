using UnityEngine;

public class SceneFader : MonoBehaviour
{
    private void Start()
    {
        var fader = FindObjectOfType<ScreenFader>();
        if (fader != null)
        {
            // 从全黑淡入到透明
            fader.FadeIn(0.6f);
        }
    }
}
