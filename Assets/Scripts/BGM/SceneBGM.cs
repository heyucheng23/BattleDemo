using UnityEngine;

public class SceneBGM : MonoBehaviour
{
    public AudioClip bgm;
    public bool keepLastMusic = false;
    [Range(0f, 1f)]
    public float volume = 1f;

    void Start()
    {
        if (!keepLastMusic && bgm != null)
        {
            BGMManager.Instance.Play(bgm, volume);
        }
    }
}
