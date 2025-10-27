using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent(typeof(Image))]
public class ScreenFader : MonoBehaviour {
    Image img;
    void Awake(){ img = GetComponent<Image>(); var c=img.color; c.a=Mathf.Clamp01(c.a); img.color=c; }
    public void FadeIn(float t=0.35f, Action cb=null){ StartCoroutine(F(1,0,t,cb)); }
    public void FadeOut(float t=0.35f, Action cb=null){ StartCoroutine(F(0,1,t,cb)); }
    IEnumerator F(float a,float b,float t,Action cb){
        float e=0; var c=img.color;
        while(e<t){ e+=Time.unscaledDeltaTime; float k=Mathf.Clamp01(e/t);
            img.color=new Color(c.r,c.g,c.b,Mathf.Lerp(a,b,k)); yield return null; }
        img.color=new Color(c.r,c.g,c.b,b); cb?.Invoke();
    }
}
