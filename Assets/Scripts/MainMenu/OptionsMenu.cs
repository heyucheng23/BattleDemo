using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour {
    public Slider musicSlider; public Slider sfxSlider;
    const string KM="opt_music", KS="opt_sfx";

    void OnEnable(){
        float m=PlayerPrefs.GetFloat(KM,0.8f), s=PlayerPrefs.GetFloat(KS,0.8f);
        if(musicSlider) musicSlider.value=m; if(sfxSlider) sfxSlider.value=s;
    }
    public void OnMusicChanged(float v){ PlayerPrefs.SetFloat(KM,v); /* TODO: 应用到Mixer */ }
    public void OnSfxChanged(float v){ PlayerPrefs.SetFloat(KS,v);   /* TODO: 应用到Mixer */ }
}
