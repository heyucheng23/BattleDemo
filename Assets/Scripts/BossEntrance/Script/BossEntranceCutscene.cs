using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;   // 如果你用 TextMeshPro

public class BossEntranceCutscene : MonoBehaviour
{
    [Header("角色与目标点")]
    public Transform hero;        // Player
    public Transform boss;        // Boss
    public Transform heroTarget;  // 英雄最终站位
    public Transform bossTarget;  // Boss 最终站位
    public float moveSpeed = 2f;

    [Header("对话 UI")]
    public GameObject dialoguePanel;   // 对话框整体
    public TextMeshProUGUI dialogueText;
    public string[] dialogueLines;     // 在 Inspector 里填几句对白
    public float afterDialogueDelay = 1f;
    public string bossBattleSceneName = "BossBattle";  // 之后要加载的战斗场景名

    private int currentLine = 0;
    private bool heroArrived = false;
    private bool bossArrived = false;
    private bool dialogueStarted = false;

    void Start()
    {
        // 关闭玩家和 Boss 的控制脚本
        var heroMove = hero.GetComponent<PlayerMovement>();
        if (heroMove != null) heroMove.enabled = false;

        var bossEnemy = boss.GetComponent<Enemy>();
        if (bossEnemy != null) bossEnemy.enabled = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    void Update()
    {
        // 1. 让英雄和 Boss 慢慢走到目标位置
        if (!heroArrived)
        {
            hero.position = Vector3.MoveTowards(
                hero.position,
                heroTarget.position,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(hero.position, heroTarget.position) < 0.05f)
                heroArrived = true;
        }

        if (!bossArrived)
        {
            boss.position = Vector3.MoveTowards(
                boss.position,
                bossTarget.position,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(boss.position, bossTarget.position) < 0.05f)
                bossArrived = true;
        }

        // 2. 两边都到位后，触发对话
        if (heroArrived && bossArrived && !dialogueStarted)
        {
            dialogueStarted = true;
            StartCoroutine(StartDialogue());
        }

        // 3. 对话中按下某个键切下一句
        if (dialogueStarted && dialoguePanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                ShowNextLine();
            }
        }
    }

    IEnumerator StartDialogue()
    {
        yield return new WaitForSeconds(0.5f);

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            currentLine = 0;
            if (dialogueLines.Length > 0)
                dialogueText.text = dialogueLines[0];
        }
    }

    void ShowNextLine()
    {
        currentLine++;
        if (currentLine < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLine];
        }
        else
        {
            // 对话结束
            StartCoroutine(EndCutsceneAndGoBattle());
        }
    }

    IEnumerator EndCutsceneAndGoBattle()
    {
        dialoguePanel.SetActive(false);
        yield return new WaitForSeconds(afterDialogueDelay);

        if (!string.IsNullOrEmpty(bossBattleSceneName))
        {
            SceneManager.LoadScene(bossBattleSceneName);
        }
    }
}
