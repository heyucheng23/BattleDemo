using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent(typeof(Image))]
public class ScreenFaderV2 : MonoBehaviour
{
    public bool blockInputDuringFade = true;
    public bool disableAfterFadeIn = false;

    Image img;
    CanvasGroup cg;

    void Awake()
    {
        img = GetComponent<Image>();
        if (!TryGetComponent(out cg)) cg = gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        img.raycastTarget = false;
    }

    public void FadeIn(float t = 0.35f, Action onDone = null)
    { StopAllCoroutines(); gameObject.SetActive(true);
      StartCoroutine(CoFade(1,0,t,()=>{ if(disableAfterFadeIn) gameObject.SetActive(false); onDone?.Invoke(); })); }

    public void FadeOut(float t = 0.35f, Action onDone = null)
    { StopAllCoroutines(); gameObject.SetActive(true); StartCoroutine(CoFade(0,1,t,onDone)); }

    IEnumerator CoFade(float a, float b, float t, Action done)
    {
        if (blockInputDuringFade) cg.blocksRaycasts = true;
        var c = img.color; float e=0;
        while (e < t){ e += Time.unscaledDeltaTime;
            img.color = new Color(c.r,c.g,c.b, Mathf.Lerp(a,b,e/Mathf.Max(0.001f,t)));
            yield return null; }
        img.color = new Color(c.r,c.g,c.b,b);
        cg.blocksRaycasts = false;
        done?.Invoke();
    }
}
