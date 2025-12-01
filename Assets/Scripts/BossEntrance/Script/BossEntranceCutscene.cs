using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class BossEntranceCutscene : MonoBehaviour
{
    [Header("角色与目标点")]
    public Transform hero;        // 主角
    public Transform heroTarget;  // 主角要走到的目标点
    public Transform boss;        // Boss
    public Transform bossTarget;  // Boss 要走到的目标点
    public float moveSpeed = 2f;  // 走路速度

    [Header("动画（只负责走路/站立）")]
    public Animator heroAnimator;
    public Animator bossAnimator;
    public string moveSpeedParam = "MoveSpeed";  // Animator 里的 float 参数名

    [Header("对话 UI")]
    public GameObject dialoguePanel;        // 对话框整体 Panel
    public TextMeshProUGUI dialogueText;    // 显示对白的 TMP 文本
    [TextArea(2, 5)]
    public string[] dialogueLines;          // 在 Inspector 里填入多句对白

    [Header("打字机效果")]
    public TypewriterText typewriter;       // 挂在 DialogueText 上的 TypewriterText 脚本

    [Header("结束设置")]
    public float afterDialogueDelay = 1f;   // 对话结束后等待时间
    public string nextSceneName = "Battle"; // 要切换到的战斗场景名

    private bool heroArrived = false;
    private bool bossArrived = false;
    private bool dialogueStarted = false;
    private bool dialogueFinished = false;
    private int currentLineIndex = 0;

    void Start()
    {
        // 自动补 Animator（如果没在 Inspector 里手动拖）
        if (heroAnimator == null && hero != null)
            heroAnimator = hero.GetComponent<Animator>();
        if (bossAnimator == null && boss != null)
            bossAnimator = boss.GetComponent<Animator>();

        // 禁用玩家控制脚本，防止 Cutscene 中乱动
        if (hero != null)
        {
            var move = hero.GetComponent<PlayerMovement>();
            if (move != null) move.enabled = false;
        }

        // 一开始隐藏对话框
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    void Update()
    {
        // 1. 英雄 / Boss 往目标点移动，并切换走路/站立动画
        MoveCharacter(hero, heroTarget, heroAnimator, ref heroArrived);
        MoveCharacter(boss, bossTarget, bossAnimator, ref bossArrived);

        // 2. 双方都到达后，开启对话
        if (heroArrived && bossArrived && !dialogueStarted)
        {
            dialogueStarted = true;
            StartCoroutine(StartDialogueRoutine());
        }

        // 3. 对话中按键逻辑：正在打字 → 跳满；打完 → 下一句
        if (dialogueStarted && !dialogueFinished && dialoguePanel != null && dialoguePanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                // 如果还在打字，先跳到整句
                if (typewriter != null && typewriter.IsTyping)
                {
                    typewriter.Skip();
                }
                else
                {
                    // 已经打完，进入下一句或结束
                    ShowNextLine();
                }
            }
        }
    }

    /// <summary>
    /// 移动角色，并根据是否在移动设置 MoveSpeed 参数
    /// </summary>
    void MoveCharacter(Transform character, Transform target, Animator anim, ref bool arrivedFlag)
    {
        if (character == null || target == null || arrivedFlag) return;

        float dist = Vector3.Distance(character.position, target.position);

        if (dist > 0.05f)
        {
            // 没到：继续走
            character.position = Vector3.MoveTowards(
                character.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

            if (anim != null && !string.IsNullOrEmpty(moveSpeedParam))
                anim.SetFloat(moveSpeedParam, moveSpeed);  // >0 代表走路
        }
        else
        {
            // 到了：停下并切到 Idle
            arrivedFlag = true;

            if (anim != null && !string.IsNullOrEmpty(moveSpeedParam))
                anim.SetFloat(moveSpeedParam, 0f);          // 0 代表站立
        }
    }

    IEnumerator StartDialogueRoutine()
    {
        // 给一点停顿，看起来更自然
        yield return new WaitForSeconds(0.5f);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        currentLineIndex = 0;

        if (dialogueLines != null && dialogueLines.Length > 0)
        {
            string line = dialogueLines[currentLineIndex];

            if (typewriter != null)
                typewriter.StartTyping(line);
            else if (dialogueText != null)
                dialogueText.text = line;
        }
    }

    void ShowNextLine()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
            return;

        currentLineIndex++;

        if (currentLineIndex < dialogueLines.Length)
        {
            string line = dialogueLines[currentLineIndex];

            if (typewriter != null)
                typewriter.StartTyping(line);
            else if (dialogueText != null)
                dialogueText.text = line;
        }
        else
        {
            // 没有更多台词了
            dialogueFinished = true;
            StartCoroutine(EndDialogueAndGoNextScene());
        }
    }

    IEnumerator EndDialogueAndGoNextScene()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        yield return new WaitForSeconds(afterDialogueDelay);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
