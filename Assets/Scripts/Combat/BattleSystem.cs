using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BattleSystem : MonoBehaviour
{
    [Header("Unit Refs (场景里的对象)")]
    public Unit player;
    public Unit boss;

    [Header("UI Refs")]
    public TMP_Text txtInfo;
    public TMP_Text txtPlayerHP;
    public TMP_Text txtBossHP;
    public Slider   sliderPlayerHP;
    public Slider   sliderBossHP;
    [Tooltip("可选：UI 根物体上的 CanvasGroup，用于防止alpha=0导致看不见")]
    public CanvasGroup uiCanvasGroup;

    [Header("Animator（可选）")]
    public Animator playerAnim;
    public Animator bossAnim;

    [Header("动画速度（仅影响动画，不改节奏）")]
    [Range(0.5f, 1.5f)]
    public float animSpeed = 0.85f;

    [Header("战斗节奏（影响回合推进 & 伤害结算命中点）")]
    public bool  useRealtimeDelays = false;      // 使用 WaitForSecondsRealtime（不受 timeScale 影响）
    [Range(0f, 2f)] public float preHitDelayPlayer  = 0.20f;
    [Range(0f, 2f)] public float postHitDelayPlayer = 0.35f;
    [Range(0f, 2f)] public float preHitDelayBoss    = 0.20f;
    [Range(0f, 2f)] public float postHitDelayBoss   = 0.35f;
    [Range(0f, 2f)] public float bombPreDelay       = 0.25f;
    [Range(0f, 2f)] public float bombPostDelay      = 0.45f;
    [Range(0f, 2f)] public float turnGap            = 0.10f; // 每个小回合结束后的间隙

    [Header("（可选）全局减速，运行时自动恢复")]
    [Range(0.2f, 1.2f)]
    public float globalTimeScale = 1.0f; // ≠1 时启用时设置，禁用/退出时恢复
    float _timeScaleBackup = 1f;

    [Header("可选：血条填充Image（做渐变/换色用）")]
    public Image playerFillImage;
    public Image bossFillImage;
    public bool  enableHpColorLerp = true;

    [Header("血条/数值参数")]
    [Range(0.01f, 0.5f)] public float hpLerpTime = 0.15f;
    public bool useWholeNumberSlider = true;
    public bool invertBossSlider = true;

    [Header("End Panel（战斗结束弹层）")]
    public GameObject endPanel;
    public TMP_Text   txtEndTitle;

    // ---- 关卡/战斗静态数据 ----
    StageConfig S;
    int bossHitPerTurn;
    int playerATK, playerDEF;
    int potions, healPerPotion;
    int bombsLeft, bombDmg;

    // ---- 战斗过程累计 ----
    int turnsUsed;
    int totalPlayerDamageDealt;
    int totalBossDamageDealt;
    int bombsUsed;
    int potionsUsed;
    int startPlayerHP, startBossHP;
    float battleStartTime, battleEndTime;

    // ---- 防止重复协程 ----
    readonly Dictionary<Slider, Coroutine> _sliderLerpRoutines = new();

    void OnEnable()
    {
        _timeScaleBackup = Time.timeScale;
        if (Mathf.Abs(globalTimeScale - 1f) > 0.001f)
            Time.timeScale = Mathf.Clamp(globalTimeScale, 0.01f, 10f);
    }

    void OnDisable()
    {
        Time.timeScale = _timeScaleBackup;
    }

    void Awake()
    {
        if (uiCanvasGroup)
        {
            uiCanvasGroup.alpha = 1f;
            uiCanvasGroup.interactable = true;
            uiCanvasGroup.blocksRaycasts = true;
        }
        if (sliderBossHP && invertBossSlider)
            sliderBossHP.direction = Slider.Direction.RightToLeft;
        if (endPanel) endPanel.SetActive(false);
    }

    void Start()
    {
        // 配置
        S = StageConfigLoader.Load();
        if (S == null) S = new StageConfig
        {
            HP0 = 100,
            HP_boss = 300,
            ATK0 = 20,
            DEF0 = 0,
            ATK_boss = 25,
            T_max = 10
        };

        // 读取选择
        playerATK      = PlayerPrefs.GetInt("playerATK", S.ATK0);
        playerDEF      = PlayerPrefs.GetInt("playerDEF", S.DEF0);
        potions        = PlayerPrefs.GetInt("potions", 0);
        bombsLeft      = PlayerPrefs.GetInt("bombs", 0);
        healPerPotion  = PlayerPrefs.GetInt("healPerPotion", 0);
        bombDmg        = PlayerPrefs.GetInt("dmgPerBomb", 0);

        // 初始化单位
        if (boss)
        {
            boss.maxHP     = Mathf.Max(1, S.HP_boss);
            boss.currentHP = boss.maxHP;
        }
        if (player)
        {
            player.maxHP     = Mathf.Max(1, S.HP0);
            player.currentHP = Mathf.Clamp(S.HP0 + potions * healPerPotion, 1, 999999);
            player.battleATK = Mathf.Max(0, playerATK);
            player.battleDEF = Mathf.Max(0, playerDEF);
        }

        bossHitPerTurn = Mathf.Max(0, S.ATK_boss);

        if (sliderPlayerHP) sliderPlayerHP.wholeNumbers = useWholeNumberSlider;
        if (sliderBossHP)   sliderBossHP.wholeNumbers   = useWholeNumberSlider;

        // 累计
        turnsUsed = 0;
        totalPlayerDamageDealt = 0;
        totalBossDamageDealt   = 0;
        bombsUsed   = 0;
        potionsUsed = potions; // 开场并入HP
        startPlayerHP = player ? player.currentHP : 0;
        startBossHP   = boss ? boss.currentHP : 0;
        battleStartTime = Time.unscaledTime;

        // 动画速度
        if (playerAnim) playerAnim.speed = animSpeed;
        if (bossAnim)   bossAnim.speed   = animSpeed;

        RefreshHUD(true);
        if (txtInfo) txtInfo.text = "Battle start!";
        StartCoroutine(BattleLoop());
    }

    IEnumerator BattleLoop()
    {
        while (true)
        {
            // Player 回合
            yield return PlayerTurn();
            if (IsDead(boss)) break;
            yield return Delay(turnGap);

            // Boss 回合
            yield return EnemyTurn();
            if (IsDead(player)) break;
            yield return Delay(turnGap);

            if (++turnsUsed >= S.T_max) break;
        }

        // —— 终局判定 —— 
        bool playerDead = (player == null || player.currentHP <= 0);
        bool bossDead   = (boss   == null || boss.currentHP   <= 0);

        bool win      = !playerDead && bossDead;
        bool lose     =  playerDead && !bossDead;
        bool bothDead =  playerDead && bossDead;

        if (txtInfo)
        {
            if      (win)      txtInfo.text = "Victory!";
            else if (lose)     txtInfo.text = "Defeat...";
            else if (bothDead) txtInfo.text = "Both fell...";
            else               txtInfo.text = "Battle End.";
        }

        // 记录
        battleEndTime = Time.unscaledTime;
        int endPlayerHP = player ? Mathf.Max(0, player.currentHP) : 0;
        int endBossHP   = boss   ? Mathf.Max(0, boss.currentHP)   : 0;

        PlayerPrefs.SetInt("hpMargin",  endPlayerHP);
        PlayerPrefs.SetInt("dmgMargin", playerATK * S.T_max - S.HP_boss);

        PlayerPrefs.SetInt("result_win",                 win ? 1 : 0);
        PlayerPrefs.SetInt("result_turns",               turnsUsed);
        PlayerPrefs.SetFloat("result_duration_sec",      Mathf.Max(0f, battleEndTime - battleStartTime));
        PlayerPrefs.SetInt("result_player_hp_start",     startPlayerHP);
        PlayerPrefs.SetInt("result_player_hp_end",       endPlayerHP);
        PlayerPrefs.SetInt("result_boss_hp_start",       startBossHP);
        PlayerPrefs.SetInt("result_boss_hp_end",         endBossHP);
        PlayerPrefs.SetInt("result_player_damage_dealt", totalPlayerDamageDealt);
        PlayerPrefs.SetInt("result_boss_damage_dealt",   totalBossDamageDealt);
        PlayerPrefs.SetInt("result_bombs_used",          bombsUsed);
        PlayerPrefs.SetInt("result_potions_used",        potionsUsed);
        PlayerPrefs.SetInt("result_heal_per_potion",     healPerPotion);
        PlayerPrefs.SetInt("result_bomb_damage",         bombDmg);
        PlayerPrefs.SetInt("result_player_atk",          playerATK);
        PlayerPrefs.SetInt("result_player_def",          playerDEF);
        PlayerPrefs.SetInt("result_boss_hit_per_turn",   bossHitPerTurn);
        PlayerPrefs.SetInt("result_tmax",                S.T_max);
        PlayerPrefs.Save();

        // 保险：清触发器
        ResetAllTriggers();

        // 正确触发动画
        if (win)
        {
            if (playerAnim) playerAnim.SetTrigger("Win");
            if (bossAnim)   bossAnim.SetTrigger("Death");
        }
        else if (lose)
        {
            if (playerAnim) playerAnim.SetTrigger("Death");
            // Boss 无 Win 动画则保持 Idle
        }
        else if (bothDead)
        {
            if (playerAnim) playerAnim.SetTrigger("Death");
            if (bossAnim)   bossAnim.SetTrigger("Death");
        }

        // 弹面板或后备跳场景
        if (endPanel)
        {
            if (txtEndTitle)
            {
                if      (win)      txtEndTitle.text = "Victory!";
                else if (lose)     txtEndTitle.text = "Defeat...";
                else if (bothDead) txtEndTitle.text = "Both fell...";
                else               txtEndTitle.text = "Battle End.";
            }
            endPanel.SetActive(true);
            yield break;
        }
        else
        {
            yield return Delay(0.6f);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Result");
        }
    }

    IEnumerator PlayerTurn()
    {
        if (!player || !boss) yield break;

        // 首回合扔炸弹
        if (turnsUsed == 0 && bombsLeft > 0 && bombDmg > 0)
        {
            if (playerAnim) playerAnim.SetTrigger("Attack"); // 可选：抬手扔的感觉
            yield return Delay(bombPreDelay);

            int extraTotal = bombsLeft * bombDmg;
            int hpBefore   = boss.currentHP;

            boss.TakeDamage(extraTotal);
            int realDealt  = Mathf.Clamp(hpBefore - Mathf.Max(0, boss.currentHP), 0, extraTotal);

            totalPlayerDamageDealt += realDealt;
            bombsUsed += bombsLeft;
            bombsLeft = 0;

            if (txtInfo) txtInfo.text = $"You throw bombs for {realDealt}!";
            if (bossAnim) bossAnim.SetTrigger("Hurt");
            RefreshHUD();

            yield return Delay(bombPostDelay);
            if (IsDead(boss)) yield break;
        }

        // 普通攻击：命中点前等待 -> 结算伤害 -> 命中后停顿
        if (playerAnim) playerAnim.SetTrigger("Attack");
        yield return Delay(preHitDelayPlayer);

        int before = boss.currentHP;
        boss.TakeDamage(player.battleATK);
        int dealt = Mathf.Clamp(before - Mathf.Max(0, boss.currentHP), 0, player.battleATK);
        totalPlayerDamageDealt += dealt;

        if (txtInfo) txtInfo.text = $"You hit for {dealt}.";
        if (bossAnim) bossAnim.SetTrigger("Hurt");
        RefreshHUD();

        yield return Delay(postHitDelayPlayer);
    }

    IEnumerator EnemyTurn()
    {
        if (!player || !boss) yield break;

        if (bossAnim) bossAnim.SetTrigger("Attack");
        yield return Delay(preHitDelayBoss);

        int before = player.currentHP;
        player.TakeDamage(bossHitPerTurn);
        int taken = Mathf.Clamp(before - Mathf.Max(0, player.currentHP), 0, bossHitPerTurn);
        totalBossDamageDealt += taken;

        if (txtInfo) txtInfo.text = $"Boss hits for {taken}.";
        if (playerAnim) playerAnim.SetTrigger("Hurt");
        RefreshHUD();

        yield return Delay(postHitDelayBoss);
    }

    // 统一延迟：返回 IEnumerator 以兼容 Realtime / 普通等待
    IEnumerator Delay(float t)
    {
        if (t <= 0f)
        {
            // 0s：让出一帧，视觉更顺（若想立刻继续可改成 yield break）
            yield return null;
            yield break;
        }

        if (useRealtimeDelays)
            yield return new WaitForSecondsRealtime(t);
        else
            yield return new WaitForSeconds(t);
    }

    void RefreshHUD(bool isInstant = false)
    {
        if (player && txtPlayerHP)
            txtPlayerHP.text = $"HP: {Mathf.Max(0, player.currentHP)}/{player.maxHP}";
        if (boss && txtBossHP)
            txtBossHP.text   = $"Boss HP: {Mathf.Max(0, boss.currentHP)}/{boss.maxHP}";

        if (player && sliderPlayerHP)
        {
            sliderPlayerHP.maxValue = player.maxHP;
            if (isInstant) sliderPlayerHP.value = Mathf.Clamp(player.currentHP, 0, player.maxHP);
            else SmoothSet(sliderPlayerHP, Mathf.Clamp(player.currentHP, 0, player.maxHP), hpLerpTime);
        }
        if (boss && sliderBossHP)
        {
            sliderBossHP.maxValue = boss.maxHP;
            if (isInstant) sliderBossHP.value = Mathf.Clamp(boss.currentHP, 0, boss.maxHP);
            else SmoothSet(sliderBossHP, Mathf.Clamp(boss.currentHP, 0, boss.maxHP), hpLerpTime);
        }

        if (enableHpColorLerp)
        {
            if (player && playerFillImage)
                playerFillImage.color = HpColor((float)player.currentHP / Mathf.Max(1, player.maxHP));
            if (boss && bossFillImage)
                bossFillImage.color   = HpColor((float)boss.currentHP   / Mathf.Max(1, boss.maxHP));
        }
    }

    Color HpColor(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        if (ratio >= 0.5f)
        {
            float t = Mathf.InverseLerp(0.5f, 1f, ratio);
            return Color.Lerp(new Color(1f, 0.83f, 0.22f), new Color(0.21f, 0.88f, 0.42f), t);
        }
        else
        {
            float t = Mathf.InverseLerp(0f, 0.5f, ratio);
            return Color.Lerp(new Color(1f, 0.83f, 0.22f), new Color(0.89f, 0.23f, 0.23f), 1f - t);
        }
    }

    void SmoothSet(Slider s, float to, float t)
    {
        if (!s) return;
        if (_sliderLerpRoutines.TryGetValue(s, out var running) && running != null)
            StopCoroutine(running);
        _sliderLerpRoutines[s] = StartCoroutine(LerpSlider(s, to, t));
    }

    IEnumerator LerpSlider(Slider s, float to, float t = 0.15f)
    {
        if (!s) yield break;
        float from = s.value;
        float e = 0f;
        while (e < t)
        {
            e += Time.deltaTime;
            s.value = Mathf.Lerp(from, to, e / t);
            yield return null;
        }
        s.value = to;
    }

    bool IsDead(Unit u) => !u || u.currentHP <= 0;

    void ResetAllTriggers()
    {
        if (bossAnim)
        {
            bossAnim.ResetTrigger("Attack");
            bossAnim.ResetTrigger("Hurt");
            bossAnim.ResetTrigger("Death");
            bossAnim.ResetTrigger("Win");
        }
        if (playerAnim)
        {
            playerAnim.ResetTrigger("Attack");
            playerAnim.ResetTrigger("Hurt");
            playerAnim.ResetTrigger("Death");
            playerAnim.ResetTrigger("Win");
        }
    }

    // EndPanel 按钮：跳转 Result
    public void OnClick_GoToResult()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Result");
    }
}
