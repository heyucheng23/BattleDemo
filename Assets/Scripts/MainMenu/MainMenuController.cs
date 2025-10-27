using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour {
    public GameObject menuRoot;      // MenuRoot
    public GameObject panelOptions;  // PanelOptions
    public ScreenFader fader;        // FadeOverlay 上的 ScreenFader
    public string gameSceneName="Loadout";
    public float fadeTime=0.35f;

    void Start(){
        if(panelOptions) panelOptions.SetActive(false);
        if(menuRoot) menuRoot.SetActive(true);
        if(fader) fader.FadeIn(fadeTime);
    }

    public void OnStartGame(){
        if(fader) fader.FadeOut(fadeTime, ()=>SceneManager.LoadSceneAsync(gameSceneName));
        else SceneManager.LoadScene(gameSceneName);
    }
    public void OnOpenOptions(){ if(menuRoot) menuRoot.SetActive(false); if(panelOptions) panelOptions.SetActive(true); }
    public void OnBack(){ if(panelOptions) panelOptions.SetActive(false); if(menuRoot) menuRoot.SetActive(true); }
    public void OnQuit(){
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
}
