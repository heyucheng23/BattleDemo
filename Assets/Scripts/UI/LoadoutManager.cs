using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadoutManager : MonoBehaviour
{
    // ---------------- Catalog ----------------
    [Header("Catalog (drag your SO assets here)")]
    public List<WeaponSO> weapons;          // 4
    public List<ArmorSO> armors;            // 3
    public AccessorySO strengthRing;        // ATK 附饰
    public AccessorySO defenseAmulet;       // DEF 附饰
    public ConsumableSO healthPotion;       // 药水
    public ConsumableSO damageBomb;         // 炸弹

    // ---------------- Stage ----------------
    [Header("Stage Config (auto from JSON)")]
    public StageConfig stage;               // 从 Resources/Data/boss_stage.json 读入

    // ---------------- UI ----------------
    [Header("UI")]
    public TMP_Text textBudget;
    public TMP_Dropdown dropdownWeapon;     // 可隐藏但仍需要绑定
    public TMP_Dropdown dropdownArmor;      // 可隐藏但仍需要绑定
    public Toggle toggleRing;
    public Toggle toggleAmulet;
    public TMP_InputField inputPotions;
    public TMP_InputField inputBombs;
    public Button btnSuggest;
    public Button btnStart;

    // 左侧角色信息（可选）
    [Header("Hero Info (optional)")]
    public TMP_Text heroNameText;
    public TMP_Text statHPText;
    public TMP_Text statATKText;
    public TMP_Text statDEFText;
    public Image heroPortrait;              // 可选
    public Sprite heroPlaceholder;          // 可选占位立绘

    // ---------------- Selections ----------------
    int weaponIndex = -1;   // -1 表示未选（Dropdown 的 0 = None，所以 index = dropdown.value - 1）
    int armorIndex  = -1;
    bool pickRing, pickAmulet;
    int potions, bombs;

    // ---------------- Unity ----------------
    void Awake()
    {
        // 读取关卡配置（JSON）
        stage = StageConfigLoader.Load();
    }

    void Start()
    {
        // --- Dropdown 选项（即使 UI 隐藏，仍用于内部选择）
        if (dropdownWeapon)
        {
            dropdownWeapon.ClearOptions();
            var wOpts = new List<string> { "(None)" };
            foreach (var w in weapons)
                wOpts.Add($"{w.displayName} (+{w.atkBonus} ATK) [{w.cost}]");
            dropdownWeapon.AddOptions(wOpts);
            dropdownWeapon.value = 0;
            dropdownWeapon.onValueChanged.AddListener(OnWeaponChanged);
        }

        if (dropdownArmor)
        {
            dropdownArmor.ClearOptions();
            var aOpts = new List<string> { "(None)" };
            foreach (var a in armors)
                aOpts.Add($"{a.displayName} (+{a.defBonus} DEF) [{a.cost}]");
            dropdownArmor.AddOptions(aOpts);
            dropdownArmor.value = 0;
            dropdownArmor.onValueChanged.AddListener(OnArmorChanged);
        }

        // --- Toggles / Inputs
        if (toggleRing)   toggleRing.onValueChanged.AddListener(v => { pickRing = v; RefreshUI(); });
        if (toggleAmulet) toggleAmulet.onValueChanged.AddListener(v => { pickAmulet = v; RefreshUI(); });

        if (inputPotions)
        {
            inputPotions.text = "0";
            inputPotions.onValueChanged.AddListener(_ =>
            {
                potions = ParseInt(inputPotions.text);
                RefreshUI();
            });
        }

        if (inputBombs)
        {
            inputBombs.text = "0";
            inputBombs.onValueChanged.AddListener(_ =>
            {
                bombs = ParseInt(inputBombs.text);
                RefreshUI();
            });
        }

        // --- Buttons
        if (btnSuggest) btnSuggest.onClick.AddListener(SuggestOptimal);
        if (btnStart)   btnStart.onClick.AddListener(ProceedToBattle); // 只要把新按钮拖到这里就能接管

        // --- 左侧信息初始化（可选）
        if (heroNameText) heroNameText.text = "Hero";
        if (heroPortrait && heroPlaceholder) heroPortrait.sprite = heroPlaceholder;

        RefreshUI();
    }

    // ---------------- Helpers ----------------
    int ParseInt(string s) => int.TryParse(s, out var v) ? Mathf.Max(0, v) : 0;

    public void OnWeaponChanged(int dropdownValue)
    {
        weaponIndex = dropdownValue - 1; // 0=none
        RefreshUI();
    }

    public void OnArmorChanged(int dropdownValue)
    {
        armorIndex = dropdownValue - 1; // 0=none
        RefreshUI();
    }

    int TotalCost()
    {
        int c = 0;
        if (weaponIndex >= 0) c += weapons[weaponIndex].cost;
        if (armorIndex  >= 0) c += armors[armorIndex].cost;
        if (pickRing   && strengthRing)  c += strengthRing.cost;
        if (pickAmulet && defenseAmulet) c += defenseAmulet.cost;
        c += potions * (healthPotion ? healthPotion.cost : 0);
        c += bombs   * (damageBomb   ? damageBomb.cost   : 0);
        return c;
    }

    int TotalATK()
    {
        int atk = stage != null ? stage.ATK0 : 0;
        if (weaponIndex >= 0) atk += weapons[weaponIndex].atkBonus;
        if (pickRing   && strengthRing)  atk += strengthRing.atkBonus;
        if (pickAmulet && defenseAmulet) atk += defenseAmulet.atkBonus;
        return atk;
    }

    int TotalDEF()
    {
        int df = stage != null ? stage.DEF0 : 0;
        if (armorIndex >= 0) df += armors[armorIndex].defBonus;
        if (pickRing   && strengthRing)  df += strengthRing.defBonus;
        if (pickAmulet && defenseAmulet) df += defenseAmulet.defBonus;
        return df;
    }

    bool WithinBudget() => TotalCost() <= (stage != null ? stage.B : int.MaxValue);

    void RefreshUI()
    {
        // 预算
        int cost = TotalCost();
        if (textBudget)
        {
            int B = stage != null ? stage.B : 0;
            textBudget.text = $"Budget: {cost} / {B}";
            textBudget.color = cost <= B ? Color.white : Color.red;
        }

        // 左侧信息（可选）
        if (statHPText && stage != null)  statHPText.text  = $"HP: {stage.HP0}";
        if (statATKText)                  statATKText.text = $"ATK: {TotalATK()}";
        if (statDEFText)                  statDEFText.text = $"DEF: {TotalDEF()}";

        // 开始按钮可用性
        if (btnStart) btnStart.interactable = WithinBudget();
    }

    // ---------------- Suggest Best ----------------
    void SuggestOptimal()
    {
        if (stage == null) { Debug.LogWarning("Stage not loaded."); return; }

        int bestCost = int.MaxValue;
        int bestW = -1, bestA = -1, bestP = 0, bestB = 0;
        bool bestRing = false, bestAmulet = false;

        var accSets = new List<(bool ring, bool amulet)>
        {
            (false,false),(true,false),(false,true),(true,true)
        };

        for (int w = -1; w < weapons.Count; w++)
        for (int a = -1; a < armors.Count;  a++)
        foreach (var acc in accSets)
        {
            int baseCost = 0;
            int atk = stage.ATK0;

            if (w >= 0) { baseCost += weapons[w].cost; atk += weapons[w].atkBonus; }
            if (a >= 0) { baseCost += armors[a].cost; }
            if (acc.ring   && strengthRing)  { baseCost += strengthRing.cost;  atk += strengthRing.atkBonus; }
            if (acc.amulet && defenseAmulet) { baseCost += defenseAmulet.cost; }

            if (baseCost > stage.B) continue;
            int left = stage.B - baseCost;

            int maxP = healthPotion ? left / healthPotion.cost : 0;
            for (int p = 0; p <= maxP; p++)
            {
                int left2 = left - p * (healthPotion ? healthPotion.cost : 0);
                int maxB  = damageBomb ? left2 / damageBomb.cost : 0;
                for (int b = 0; b <= maxB; b++)
                {
                    int totalDmg = stage.T_max * atk + b * (damageBomb ? damageBomb.damageAmount : 0);
                    int hpNet    = stage.HP0 + p * (healthPotion ? healthPotion.healAmount : 0)
                                   - stage.T_max * stage.ATK_boss;

                    if (totalDmg < stage.HP_boss || hpNet < 0) continue;

                    int spend = baseCost
                              + p * (healthPotion ? healthPotion.cost : 0)
                              + b * (damageBomb   ? damageBomb.cost   : 0);

                    if (spend < bestCost)
                    {
                        bestCost = spend; bestW = w; bestA = a; bestP = p; bestB = b;
                        bestRing = acc.ring; bestAmulet = acc.amulet;
                    }
                }
            }
        }

        if (bestCost == int.MaxValue)
        {
            Debug.LogWarning("No winning loadout within budget.");
            return;
        }

        // 回填到内部与 UI（利用原 Dropdown/Toggle/Input 触发逻辑）
        if (dropdownWeapon) dropdownWeapon.value = bestW + 1;
        if (dropdownArmor)  dropdownArmor.value  = bestA + 1;
        if (toggleRing)     toggleRing.isOn      = bestRing;
        if (toggleAmulet)   toggleAmulet.isOn    = bestAmulet;
        if (inputPotions)   inputPotions.text    = bestP.ToString();
        if (inputBombs)     inputBombs.text      = bestB.ToString();

        // 同步内部字段
        OnWeaponChanged(bestW + 1);
        OnArmorChanged(bestA + 1);
        potions = bestP; bombs = bestB;

        RefreshUI();
    }

    // ---------------- Proceed ----------------
    public void ProceedToBattle()
    {
        // 保存选择
        PlayerPrefs.SetInt("weaponIndex", weaponIndex);
        PlayerPrefs.SetInt("armorIndex",  armorIndex);
        PlayerPrefs.SetInt("pickRing",    pickRing ? 1 : 0);
        PlayerPrefs.SetInt("pickAmulet",  pickAmulet ? 1 : 0);
        PlayerPrefs.SetInt("potions",     potions);
        PlayerPrefs.SetInt("bombs",       bombs);

        // 衍生数据
        PlayerPrefs.SetInt("playerATK",      TotalATK());
        PlayerPrefs.SetInt("playerDEF",      TotalDEF()); // 当前未用于伤害，留作扩展
        PlayerPrefs.SetInt("totalCost",      TotalCost());
        PlayerPrefs.SetInt("healPerPotion",  healthPotion ? healthPotion.healAmount : 0);
        PlayerPrefs.SetInt("dmgPerBomb",     damageBomb   ? damageBomb.damageAmount : 0);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
    }
}
