using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    public DialogueData dialogue;
    [SerializeField] private DialogueSystem sys;

    public string playerTag = "Player";
    public GameObject hintUI;
    public Transform dialogueAnchor;

    // ✅ 新增：直接持有 PanelDialogue 上的 Follower 引用
    [SerializeField] private DialogueWorldFollower follower;

    public bool enableDebugLogs = false;
    bool inRange;

    void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        if (sys == null) sys = FindFirstObjectByType<DialogueSystem>();
#else
        if (sys == null) sys = FindObjectOfType<DialogueSystem>();
#endif
        if (follower == null)
        {
#if UNITY_2023_1_OR_NEWER
            follower = FindFirstObjectByType<DialogueWorldFollower>();
#else
            follower = FindObjectOfType<DialogueWorldFollower>();
#endif
            if (enableDebugLogs) Debug.Log($"[NPC] Auto-bound Follower = {(follower?follower.name:"NULL")}", this);
        }
        if (hintUI) hintUI.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other){
        if (other.CompareTag(playerTag)){
            inRange = true;
            if (hintUI) hintUI.SetActive(true);
        }
    }
    void OnTriggerExit2D(Collider2D other){
        if (other.CompareTag(playerTag)){
            inRange = false;
            if (hintUI) hintUI.SetActive(false);
        }
    }

    void Update()
    {
        if (inRange && Input.GetKeyDown(KeyCode.E))
        {
            if (hintUI) hintUI.SetActive(false);
            if (sys != null && !sys.IsPlaying)
            {
                sys.StartDialogue(dialogue);

                // ✅ 直接设置目标（优先用你Inspector里拖进来的 follower）
                if (follower) follower.SetTarget(dialogueAnchor ? dialogueAnchor : transform);
                else if (enableDebugLogs) Debug.LogWarning("[NPC] Follower is NULL", this);
            }
        }
    }
}
