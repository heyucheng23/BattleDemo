using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class AudioEventPlayer : MonoBehaviour
{
    [System.Serializable]
    public class EventClips
    {
        public string name = "footstep";         // 事件名：如 "footstep", "sword_swing", "death"
        public AudioClip[] clips;                 // 可放多个，随机播放
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f); // 音调随机范围
        [Tooltip("同一事件的最短间隔（秒），防止连发")] public float minInterval = 0.05f;

        // 内部用
        [HideInInspector] public float lastTimePlayed = -999f;
    }

    [Header("Audio")]
    public AudioSource audioSrc;                 // 可留空，Awake 会自动加
    [Tooltip("所有可触发的音效事件列表")]
    public List<EventClips> events = new List<EventClips>();

    // 可选：给“脚步”预留的快捷事件名（左右脚 Animation Event 可直接调用）
    [Header("Shortcut Names (可选)")]
    public string footstepEventName = "footstep";

    // 快速查表
    Dictionary<string, int> nameToIndex;

    void Awake()
    {
        if (!audioSrc)
        {
            audioSrc = GetComponent<AudioSource>();
            if (!audioSrc)
            {
                audioSrc = gameObject.AddComponent<AudioSource>();
                audioSrc.playOnAwake = false;
                audioSrc.loop = false;
                audioSrc.spatialBlend = 0f; // 2D；如果3D项目可设0.2~0.5
                audioSrc.volume = 1f;
            }
        }

        // 建名字→索引的字典（忽略大小写与空格）
        nameToIndex = new Dictionary<string, int>();
        for (int i = 0; i < events.Count; i++)
        {
            var key = Normalize(events[i].name);
            if (!string.IsNullOrEmpty(key) && !nameToIndex.ContainsKey(key))
                nameToIndex.Add(key, i);
        }
    }

    string Normalize(string s) => string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant();

    // ========== 动画事件（字符串参数）==========
    // 在 Animation Event 的 Function 里选这个方法，并在参数里填：death / sword_swing / footstep ...
    public void PlayEventByName(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;

        if (!nameToIndex.TryGetValue(Normalize(eventName), out int idx))
        {
            Debug.LogWarning($"[AudioEventPlayer] No event named '{eventName}'.");
            return;
        }
        PlayByIndex(idx);
    }

    // ========== 动画事件（整数参数）==========
    // 在 Animation Event 的 Function 里选这个方法，并把 Int 参数设成事件索引（从 0 开始）
    public void PlayEventById(int index)
    {
        PlayByIndex(index);
    }

    // ========== 左右脚快捷（不带参数）==========
    // Run 动画里放两帧事件，直接调用这两个就行（内部会触发 footstepEventName）
    public void OnAnim_Foot_L() => PlayEventByName(footstepEventName);
    public void OnAnim_Foot_R() => PlayEventByName(footstepEventName);

    // ========== 核心播放 ==========
    void PlayByIndex(int index)
    {
        if (index < 0 || index >= events.Count) return;

        var ev = events[index];
        if (ev.clips == null || ev.clips.Length == 0) return;

        // 最短间隔（同一事件防抖）
        float t = Time.time;
        if (t - ev.lastTimePlayed < ev.minInterval) return;
        ev.lastTimePlayed = t;

        var clip = ev.clips[Random.Range(0, ev.clips.Length)];
        float pitch = Random.Range(ev.pitchRange.x, ev.pitchRange.y);
        audioSrc.pitch = Mathf.Clamp(pitch, 0.5f, 2f);

        audioSrc.PlayOneShot(clip, ev.volume);
    }
}
