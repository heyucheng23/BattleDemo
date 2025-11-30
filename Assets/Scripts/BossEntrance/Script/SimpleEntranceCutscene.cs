using UnityEngine;

public class SimpleEntranceCutscene : MonoBehaviour
{
    [Header("角色与目标点")]
    public Transform hero;
    public Transform heroTarget;
    public Transform boss;
    public Transform bossTarget;
    public float moveSpeed = 2f;

    [Header("对话框（只控制显示/隐藏）")]
    public GameObject dialoguePanel;

    private bool dialogueShown = false;

    void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    void Update()
    {
        // 1. 英雄移动
        if (hero != null && heroTarget != null)
        {
            hero.position = Vector3.MoveTowards(
                hero.position,
                heroTarget.position,
                moveSpeed * Time.deltaTime
            );
        }

        // 2. Boss 移动
        if (boss != null && bossTarget != null)
        {
            boss.position = Vector3.MoveTowards(
                boss.position,
                bossTarget.position,
                moveSpeed * Time.deltaTime
            );
        }

        // 3. 两个都到达后，显示对话框
        if (!dialogueShown &&
            hero != null && heroTarget != null &&
            boss != null && bossTarget != null &&
            Vector3.Distance(hero.position, heroTarget.position) < 0.1f &&
            Vector3.Distance(boss.position, bossTarget.position) < 0.1f)
        {
            dialogueShown = true;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            Debug.Log("Cutscene: both arrived, show dialogue!");
        }
    }
}
