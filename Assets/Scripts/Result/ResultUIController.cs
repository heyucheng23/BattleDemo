using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultUIController : MonoBehaviour
{
    [Header("Result UI Refs")]
    public TMP_Text txtTitle;             // Victory / Defeat
    public TMP_Text txtTurns;             // 回合数
    public TMP_Text txtDuration;          // 战斗时长
    public TMP_Text txtHP;                // 我方HP起止
    public TMP_Text txtBossHP;            // Boss HP起止
    public TMP_Text txtDealtTaken;        // 伤害统计（造成/承受）
    public TMP_Text txtItems;             // 炸弹/药水统计
    public TMP_Text txtParams;            // 攻防/上限/Boss伤害等
    public TMP_Text txtMargins;           // hpMargin / dmgMargin

    [Header("可选：返回场景名")]
    public string mainMenuScene = "MainMenu";

    void Start()
    {
        // 读取
        bool  win          = PlayerPrefs.GetInt("result_win", 0) == 1;
        int   turns        = PlayerPrefs.GetInt("result_turns", 0);
        float dur          = PlayerPrefs.GetFloat("result_duration_sec", 0f);

        int pStart = PlayerPrefs.GetInt("result_player_hp_start", 0);
        int pEnd   = PlayerPrefs.GetInt("result_player_hp_end",   0);
        int bStart = PlayerPrefs.GetInt("result_boss_hp_start",   0);
        int bEnd   = PlayerPrefs.GetInt("result_boss_hp_end",     0);

        int dealt = PlayerPrefs.GetInt("result_player_damage_dealt", 0);
        int taken = PlayerPrefs.GetInt("result_boss_damage_dealt",   0);

        int bombsUsed   = PlayerPrefs.GetInt("result_bombs_used",      0);
        int potionsUsed = PlayerPrefs.GetInt("result_potions_used",    0);
        int healEach    = PlayerPrefs.GetInt("result_heal_per_potion", 0);
        int bombDmg     = PlayerPrefs.GetInt("result_bomb_damage",     0);

        int atk     = PlayerPrefs.GetInt("result_player_atk",        0);
        int def     = PlayerPrefs.GetInt("result_player_def",        0);
        int bossHit = PlayerPrefs.GetInt("result_boss_hit_per_turn", 0);
        int tmax    = PlayerPrefs.GetInt("result_tmax",              0);

        int hpMargin  = PlayerPrefs.GetInt("hpMargin",  0);
        int dmgMargin = PlayerPrefs.GetInt("dmgMargin", 0);

        // 显示
        if (txtTitle)    txtTitle.text    = win ? "Victory!" : "Defeat...";
        if (txtTurns)    txtTurns.text    = $"Turns Used: {turns} / {tmax}";
        if (txtDuration) txtDuration.text = $"Duration: {dur:0.00}s";

        if (txtHP)      txtHP.text      = $"Player HP: {pStart} → {pEnd}";
        if (txtBossHP)  txtBossHP.text  = $"Boss HP:   {bStart} → {bEnd}";
        if (txtDealtTaken)
            txtDealtTaken.text = $"Damage Dealt: {dealt}   |   Damage Taken: {taken}";

        if (txtItems)
        {
            string bombInfo   = bombsUsed > 0 ? $"{bombsUsed} × {bombDmg}" : "0";
            string potionInfo = potionsUsed > 0 ? $"{potionsUsed} × +{healEach}" : "0";
            txtItems.text = $"Bombs Used: {bombInfo}   |   Potions Used: {potionInfo}";
        }

        if (txtParams)
            txtParams.text = $"ATK: {atk}   DEF: {def}   Boss/Turn: {bossHit}";

        if (txtMargins)
            txtMargins.text = $"HP Margin: {hpMargin}   |   DMG Margin (ATK×Tmax − BossHP): {dmgMargin}";
    }

    // UI Button 事件（可选）
    public void OnRestart()
    {
        // 假设战斗场景名为 "Battle"（按你的项目改）
        SceneManager.LoadScene("Battle");
    }

    public void OnBackToMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuScene))
            SceneManager.LoadScene(mainMenuScene);
    }
}
