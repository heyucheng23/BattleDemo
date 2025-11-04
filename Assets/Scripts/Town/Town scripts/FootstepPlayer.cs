using UnityEngine;

public class FootstepPlayer : MonoBehaviour {
    public AudioSource audioSrc;
    public AudioClip[] clips;  // 多个脚步声，避免单调

    // 在 Animation Event 中调用
    public void PlayFootstep() {
        if (clips.Length == 0) return;
        audioSrc.pitch = Random.Range(0.9f, 1.1f);
        audioSrc.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}
