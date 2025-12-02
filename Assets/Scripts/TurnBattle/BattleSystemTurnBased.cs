using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public enum BattleState
{
    Start,
    PlayerTurn,
    EnemyTurn,
    Busy,
    End
}

public class BattleSystemTurnBased : MonoBehaviour
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
    [Tooltip("可选：UI 根物体上的 CanvasGroup")]
    public CanvasGroup uiCanvasGroup;

    [Header("Animator（可选）")]
    public Animator playerAnim; // 需要有 Trigger: Attack, Throw, Heal, Skill, Hurt, Death, Win
    public Animator bossAnim;   // 需要有 Trigger: Attack, Hurt, Death

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
    [Range(0f, 2f)] public float turnGap            = 0.10f; // 每一轮（玩家+Boss）结束后的间隙

    [Header("（可选）全局减速，运行时自动恢复")]
    [Range(0.2f, 1.2f)]
    public float globalTimeScale = 1.0f;
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

    [Header("指令按钮（回合制用）")]
    public Button btnAttack;
    public Button btnBomb;
    public Button btnHeal;
    public Button btnSkill;

    [Header("Bomb 视觉（扔一颗炸弹 + 抛物线 + 爆炸）")]
    public GameObject bombPrefab;          // 一个带 Sprite 的炸弹预制体（只负责飞的）
    public GameObject bombExplosionPrefab; // 爆炸特效预制体（在 Boss 位置生成）
    public Transform  bombSpawnPoint;      // 炸弹起点（一般是玩家手边，一个空物体）
    public Transform  bombTargetPoint;     // 炸弹目标点（一般是 Boss 身上/脚下的空物体）
    public float      bombFlightDuration = 0.5f; // 炸弹飞行时间
    public float      bombArcHeight      = 2f;   // 抛物线高度

    [Header("Skill Extra Delay (技能动画后额外等待时间)")]
    [Range(0f, 3f)]
    public float skillExtraDelay = 0.8f;   // 你可以调大让 Boss 后手更慢

    [Header("Skill 使用次数")]
    public int maxSkillUses = 2;           // ⭐ Skill 总共可用次数
    int skillUsesLeft;                     // ⭐ 当前剩余次数

    // ---- 关卡/战斗静态数据 ----
    StageConfig S;
    int bossHitPerTurn;
    int playerATK, playerDEF;
    int potions, healPerPotion;
    int bombsLeft, bombDmg;
    int potionsLeft;

    // ---- 战斗过程累计 ----
    int turnsUsed;
    int totalPlayerDamageDealt;
    int totalBossDamageDealt;
    int bombsUsed;
    int potionsUsed;
    int startPlayerHP, startBossHP;
    float battleStartTime, battleEndTime;

    // 状态机
    BattleState state = BattleState.Start;
    bool battleEnded = false;

    // 滑条插值协程
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
            HP0 = 250,
            HP_boss = 905,
            ATK0 = 40,
            DEF0 = 0,
            ATK_boss = 32,
            T_max = 15
        };

        // 读取 Loadout 购买结果
        playerATK      = PlayerPrefs.GetInt("playerATK", S.ATK0);
        playerDEF      = PlayerPrefs.GetInt("playerDEF", S.DEF0);
        potions        = PlayerPrefs.GetInt("potions", 0);
        bombsLeft      = PlayerPrefs.GetInt("bombs", 0);
        healPerPotion  = PlayerPrefs.GetInt("healPerPotion", 0);
        bombDmg        = PlayerPrefs.GetInt("dmgPerBomb", 0);

        potionsLeft = potions;
        bombsUsed   = 0;
        potionsUsed = 0;

        // 初始化单位
        if (boss)
        {
            boss.maxHP     = Mathf.Max(1, S.HP_boss);
            boss.currentHP = boss.maxHP;
        }
        if (player)
        {
            player.maxHP     = Mathf.Max(1, S.HP0);
            player.currentHP = Mathf.Clamp(S.HP0, 1, 999999);
            player.battleATK = Mathf.Max(0, playerATK);
            player.battleDEF = Mathf.Max(0, playerDEF);
        }

        bossHitPerTurn = Mathf.Max(0, S.ATK_boss);

        if (sliderPlayerHP) sliderPlayerHP.wholeNumbers = useWholeNumberSlider;
        if (sliderBossHP)   sliderBossHP.wholeNumbers   = useWholeNumberSlider;

        // 记录
        turnsUsed = 0;
        totalPlayerDamageDealt = 0;
        totalBossDamageDealt   = 0;
        startPlayerHP = player ? player.currentHP : 0;
        startBossHP   = boss ? boss.currentHP   : 0;
        battleStartTime = Time.unscaledTime;

        // 动画速度
        if (playerAnim) playerAnim.speed = animSpeed;
        if (bossAnim)   bossAnim.speed   = animSpeed;

        // Skill 次数
        skillUsesLeft = Mathf.Max(0, maxSkillUses);

        // 按钮回调
        if (btnAttack) btnAttack.onClick.AddListener(OnClick_Attack);
        if (btnBomb)   btnBomb.onClick.AddListener(OnClick_Bomb);
        if (btnHeal)   btnHeal.onClick.AddListener(OnClick_Heal);
        if (btnSkill)  btnSkill.onClick.AddListener(OnClick_Skill);

        RefreshHUD(true);
        if (txtInfo) txtInfo.text = "Battle start!";

        state = BattleState.Start;
        BeginPlayerTurn();
    }

    // ============================
    //           状态切换
    // ============================

    void BeginPlayerTurn()
    {
        if (battleEnded) return;
        if (IsDead(player) || IsDead(boss))
        {
            EndBattle();
            return;
        }

        state = BattleState.PlayerTurn;
        if (txtInfo) txtInfo.text = "Your turn! Choose an action.";

        SetCommandButtonsInteractable(true);
    }

    void BeginEnemyTurn()
    {
        if (battleEnded) return;
        if (IsDead(player) || IsDead(boss))
        {
            EndBattle();
            return;
        }

        state = BattleState.EnemyTurn;
        SetCommandButtonsInteractable(false);
        StartCoroutine(EnemyTurnRoutine());
    }

    void SetCommandButtonsInteractable(bool interactable)
    {
        if (btnAttack) btnAttack.interactable = interactable;

        if (btnBomb)
            btnBomb.interactable = interactable && (bombsLeft > 0 && bombDmg > 0);

        if (btnHeal)
            btnHeal.interactable = interactable && (potionsLeft > 0 && healPerPotion > 0);

        if (btnSkill)
            btnSkill.interactable = interactable && (skillUsesLeft > 0);  // ⭐ Skill 没次数就灰掉
    }

    // ============================
    //         按钮事件
    // ============================

    public void OnClick_Attack()
    {
        if (state != BattleState.PlayerTurn || battleEnded) return;
        StartCoroutine(PlayerAttackRoutine());
    }

    public void OnClick_Bomb()
    {
        if (state != BattleState.PlayerTurn || battleEnded) return;
        if (bombsLeft <= 0 || bombDmg <= 0) return;
        StartCoroutine(PlayerBombRoutine());
    }

    public void OnClick_Heal()
    {
        if (state != BattleState.PlayerTurn || battleEnded) return;
        if (potionsLeft <= 0 || healPerPotion <= 0) return;
        StartCoroutine(PlayerHealRoutine());
    }

    public void OnClick_Skill()
    {
        if (state != BattleState.PlayerTurn || battleEnded) return;
        if (skillUsesLeft <= 0) return;           // ⭐ 没次数直接不响应
        StartCoroutine(PlayerSkillRoutine());
    }

    // ============================
    //         玩家行动
    // ============================

    IEnumerator PlayerAttackRoutine()
    {
        if (!player || !boss) yield break;
        state = BattleState.Busy;
        SetCommandButtonsInteractable(false);

        if (playerAnim) playerAnim.SetTrigger("Attack");
        yield return Delay(preHitDelayPlayer);

        int before = boss.currentHP;
        int rawDmg   = player.battleATK;
        int finalDmg = Mathf.Max(0, rawDmg);
        boss.TakeDamage(finalDmg);

        int dealt = Mathf.Clamp(before - Mathf.Max(0, boss.currentHP), 0, finalDmg);
        totalPlayerDamageDealt += dealt;

        if (txtInfo) txtInfo.text = $"You hit the boss for {dealt} damage.";
        if (bossAnim) bossAnim.SetTrigger("Hurt");
        RefreshHUD();

        yield return Delay(postHitDelayPlayer);

        if (IsDead(boss))
        {
            EndBattle();
            yield break;
        }

        yield return Delay(turnGap);
        BeginEnemyTurn();
    }

    IEnumerator PlayerBombRoutine()
    {
        if (!player || !boss) yield break;
        state = BattleState.Busy;
        SetCommandButtonsInteractable(false);

        if (playerAnim) playerAnim.SetTrigger("Throw");
        yield return Delay(bombPreDelay);

        Vector3 startPos = bombSpawnPoint ? bombSpawnPoint.position : player.transform.position;
        Vector3 endPos   = bombTargetPoint ? bombTargetPoint.position : boss.transform.position;

        GameObject bombObj = null;
        if (bombPrefab)
            bombObj = Instantiate(bombPrefab, startPos, Quaternion.identity);

        if (bombObj)
            yield return StartCoroutine(AnimateBombProjectile(bombObj.transform, startPos, endPos, bombFlightDuration, bombArcHeight));
        else
            yield return Delay(bombFlightDuration);

        int beforeHP = boss.currentHP;
        int rawDmg   = Mathf.Max(0, bombDmg);
        boss.TakeDamage(rawDmg);
        int dealt = Mathf.Clamp(beforeHP - Mathf.Max(0, boss.currentHP), 0, rawDmg);
        totalPlayerDamageDealt += dealt;

        bombsLeft -= 1;
        bombsUsed += 1;
        if (bombsLeft < 0) bombsLeft = 0;

        if (txtInfo) txtInfo.text = $"You throw a bomb and deal {dealt} damage! Bombs left: {bombsLeft}.";
        if (bossAnim) bossAnim.SetTrigger("Hurt");
        RefreshHUD();

        if (bombExplosionPrefab)
        {
            GameObject boom = Instantiate(bombExplosionPrefab, endPos, Quaternion.identity);
            Destroy(boom, 1.0f);
        }

        if (bombObj) Destroy(bombObj);

        yield return Delay(bombPostDelay);

        if (IsDead(boss))
        {
            EndBattle();
            yield break;
        }

        yield return Delay(turnGap);
        BeginEnemyTurn();
    }

    IEnumerator PlayerHealRoutine()
    {
        if (!player) yield break;
        state = BattleState.Busy;
        SetCommandButtonsInteractable(false);

        if (playerAnim) playerAnim.SetTrigger("Heal");
        yield return Delay(preHitDelayPlayer);

        int beforeHP = player.currentHP;
        player.Heal(healPerPotion);
        int healed = Mathf.Clamp(player.currentHP - beforeHP, 0, healPerPotion);

        if (healed > 0)
        {
            potionsLeft -= 1;
            potionsUsed += 1;
            if (potionsLeft < 0) potionsLeft = 0;
        }

        if (txtInfo)
        {
            if (healed > 0)
                txtInfo.text = $"You use a potion and recover {healed} HP. Potions left: {potionsLeft}.";
            else
                txtInfo.text = "Your HP is already full.";
        }

        RefreshHUD();
        yield return Delay(postHitDelayPlayer);

        yield return Delay(turnGap);
        BeginEnemyTurn();
    }

    IEnumerator PlayerSkillRoutine()
    {
        if (!player || !boss) yield break;
        state = BattleState.Busy;
        SetCommandButtonsInteractable(false);

        if (playerAnim) playerAnim.SetTrigger("Skill");

        yield return Delay(preHitDelayPlayer);

        int rawDmg   = Mathf.RoundToInt(player.battleATK * 1.5f);
        int beforeHP = boss.currentHP;

        boss.TakeDamage(rawDmg);
        int dealt = Mathf.Clamp(beforeHP - Mathf.Max(0, boss.currentHP), 0, rawDmg);

        // ⭐ 扣除次数
        skillUsesLeft = Mathf.Max(0, skillUsesLeft - 1);

        if (txtInfo)
            txtInfo.text = $"You cast a skill and deal {dealt} damage! (Skill left: {skillUsesLeft}/{maxSkillUses})";

        if (bossAnim) bossAnim.SetTrigger("Hurt");
        RefreshHUD();

        yield return Delay(postHitDelayPlayer);
        yield return Delay(skillExtraDelay);

        if (IsDead(boss))
        {
            EndBattle();
            yield break;
        }

        yield return Delay(turnGap);
        BeginEnemyTurn();
    }

    // 抛物线动画
    IEnumerator AnimateBombProjectile(Transform bomb, Vector3 start, Vector3 end, float duration, float arcHeight)
    {
        if (!bomb) yield break;

        duration = Mathf.Max(0.01f, duration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);

            Vector3 pos = Vector3.Lerp(start, end, normalized);
            float arc = 4f * normalized * (1f - normalized);
            pos.y += arc * arcHeight;

            bomb.position = pos;

            Vector3 dir = (end - start).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bomb.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            yield return null;
        }

        bomb.position = end;
    }

    // ============================
    //         Boss 行动
    // ============================

    IEnumerator EnemyTurnRoutine()
    {
        if (!player || !boss) yield break;

        if (txtInfo) txtInfo.text = "Boss turn!";
        if (bossAnim) bossAnim.SetTrigger("Attack");
        yield return Delay(preHitDelayBoss);

        int before = player.currentHP;
        int rawDmg   = bossHitPerTurn;
        int finalDmg = Mathf.Max(0, rawDmg - player.battleDEF);
        player.TakeDamage(finalDmg);

        int taken = Mathf.Clamp(before - Mathf.Max(0, player.currentHP), 0, finalDmg);
        totalBossDamageDealt += taken;

        if (txtInfo) txtInfo.text = $"Boss hits you for {taken} damage.";
        if (playerAnim) playerAnim.SetTrigger("Hurt");
        RefreshHUD();

        yield return Delay(postHitDelayBoss);

        if (IsDead(player))
        {
            EndBattle();
            yield break;
        }

        turnsUsed++;

        if (turnsUsed >= S.T_max)
        {
            EndBattle();
            yield break;
        }

        yield return Delay(turnGap);
        BeginPlayerTurn();
    }

    // ============================
    //          战斗结束
    // ============================

    void EndBattle()
    {
        if (battleEnded) return;
        battleEnded = true;
        state = BattleState.End;

        bool playerDead = (player == null || player.currentHP <= 0);
        bool bossDead   = (boss   == null || boss.currentHP   <= 0);

        bool win      = !playerDead && bossDead;
        bool lose     =  playerDead && !bossDead;
        bool bothDead =  playerDead && bossDead;

        if (txtInfo)
        {
            if      (win)      txtInfo.text = "Victory!";
            else if (lose)     txtInfo.text = "Defeat...";
            else if (bothDead) txtInfo.text = "Both sides fell...";
            else               txtInfo.text = "Battle ended.";
        }

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

        ResetAllTriggers();

        if (win)
        {
            if (playerAnim) playerAnim.SetTrigger("Win");
            if (bossAnim)   bossAnim.SetTrigger("Death");
        }
        else if (lose)
        {
            if (playerAnim) playerAnim.SetTrigger("Death");
        }
        else if (bothDead)
        {
            if (playerAnim) playerAnim.SetTrigger("Death");
            if (bossAnim)   bossAnim.SetTrigger("Death");
        }

        if (endPanel)
        {
            if (txtEndTitle)
            {
                if      (win)      txtEndTitle.text = "Victory!";
                else if (lose)     txtEndTitle.text = "Defeat...";
                else if (bothDead) txtEndTitle.text = "Both sides fell...";
                else               txtEndTitle.text = "Battle ended.";
            }
            endPanel.SetActive(true);
        }
        else
        {
            StartCoroutine(GoToResultAfterDelay(0.6f));
        }
    }

    IEnumerator GoToResultAfterDelay(float t)
    {
        yield return Delay(t);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Result");
    }

    // ============================
    //          通用工具
    // ============================

    IEnumerator Delay(float t)
    {
        if (t <= 0f)
        {
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

        // 按钮状态也随时刷新一下
        SetCommandButtonsInteractable(state == BattleState.PlayerTurn && !battleEnded);
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
            playerAnim.ResetTrigger("Throw");
            playerAnim.ResetTrigger("Heal");
            playerAnim.ResetTrigger("Skill");
            playerAnim.ResetTrigger("Hurt");
            playerAnim.ResetTrigger("Death");
            playerAnim.ResetTrigger("Win");
        }
    }

    public void OnClick_GoToResult()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Result");
    }

    // =========================================
    // 由炸弹抛物体调用：落地后造成伤害 + 播放爆炸动画
    // =========================================
    public void OnBombProjectileLanded(int dmg)
    {
        if (boss == null || boss.currentHP <= 0 || battleEnded)
            return;

        int beforeHP = boss.currentHP;
        boss.TakeDamage(dmg);

        int dealt = Mathf.Clamp(beforeHP - boss.currentHP, 0, dmg);
        totalPlayerDamageDealt += dealt;

        if (txtInfo)
            txtInfo.text = $"Bomb hits boss for {dealt}!";

        if (bossAnim) bossAnim.SetTrigger("Hurt");
        RefreshHUD();

        if (IsDead(boss))
        {
            EndBattle();
        }
    }

    // ============================
    //  鼠标悬停提示（在 EventTrigger 里调用）
    // ============================

    public void OnHoverSkill()
    {
        if (!txtInfo) return;
        txtInfo.text = skillUsesLeft > 0
            ? $"Skill uses left: {skillUsesLeft}/{maxSkillUses}"
            : "No Skill uses left.";
    }

    public void OnHoverHeal()
    {
        if (!txtInfo) return;
        txtInfo.text = potionsLeft > 0
            ? $"Potions left: {potionsLeft}"
            : "No potions left.";
    }

    public void OnHoverBomb()
    {
        if (!txtInfo) return;
        txtInfo.text = bombsLeft > 0
            ? $"Bombs left: {bombsLeft}"
            : "No bombs left.";
    }

    public void OnHoverExit()
    {
        if (txtInfo && state == BattleState.PlayerTurn && !battleEnded)
            txtInfo.text = "Your turn! Choose an action.";
    }
}
