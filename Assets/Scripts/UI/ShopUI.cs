using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 右侧商品列表生成器（商店外观层）
/// - 读取 LoadoutManager 的物品目录
/// - 生成 ShopItem 行（武器/护甲/饰品：Buy按钮；消耗品：数量+/-）
/// - 通过调用 LoadoutManager 现有 UI 控件（下拉/切换/输入框）来复用预算与校验逻辑
/// 需要的 ShopItem 结构：
/// ShopItem
///   ├─ Icon      (Image)
///   ├─ Name      (TMP_Text)
///   ├─ Desc      (TMP_Text)
///   ├─ Price     (TMP_Text)
///   ├─ BuyBtn    (Button)
///   └─ QtyGroup  (Empty + Horizontal Layout Group)  // 里含：BtnMinus(Button)、QtyText(TMP_Text)、BtnPlus(Button)
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform contentRoot;     // RightPanel/ScrollView/Viewport/Content
    [SerializeField] private GameObject shopItemPrefab; // Assets/Prefabs/UI/ShopItem.prefab
    [SerializeField] private LoadoutManager loadout;    // 场景里的 LoadoutRoot（挂有 LoadoutManager）

    void Start()
    {
        if (!contentRoot || !shopItemPrefab || !loadout)
        {
            Debug.LogError("[ShopUI] Missing references. Please bind contentRoot / shopItemPrefab / loadout.");
            return;
        }

        // --- 武器 ---
        if (loadout.weapons != null)
        {
            foreach (var w in loadout.weapons)
            {
                if (w == null) continue;
                AddEquipRow(
                    icon: w.icon,
                    name: w.displayName,
                    desc: $"+{w.atkBonus} ATK",
                    price: w.cost,
                    onClick: () => SelectWeapon(w)
                );
            }
        }

        // --- 护甲 ---
        if (loadout.armors != null)
        {
            foreach (var a in loadout.armors)
            {
                if (a == null) continue;
                AddEquipRow(
                    icon: a.icon,
                    name: a.displayName,
                    desc: $"+{a.defBonus} DEF",
                    price: a.cost,
                    onClick: () => SelectArmor(a)
                );
            }
        }

        // --- 饰品（用 Buy 按钮切换 on/off） ---
        if (loadout.strengthRing)
        {
            AddEquipRow(
                icon: loadout.strengthRing.icon,
                name: loadout.strengthRing.displayName,
                desc: $"+{loadout.strengthRing.atkBonus} ATK",
                price: loadout.strengthRing.cost,
                onClick: () => { if (loadout.toggleRing) loadout.toggleRing.isOn = !loadout.toggleRing.isOn; }
            );
        }
        if (loadout.defenseAmulet)
        {
            AddEquipRow(
                icon: loadout.defenseAmulet.icon,
                name: loadout.defenseAmulet.displayName,
                desc: $"+{loadout.defenseAmulet.defBonus} DEF",
                price: loadout.defenseAmulet.cost,
                onClick: () => { if (loadout.toggleAmulet) loadout.toggleAmulet.isOn = !loadout.toggleAmulet.isOn; }
            );
        }

        // --- 消耗品（数量 +/-） ---
        if (loadout.healthPotion)
        {
            AddConsumableRow(
                icon: loadout.healthPotion.icon,
                name: loadout.healthPotion.displayName,
                desc: $"Heal {loadout.healthPotion.healAmount}",
                price: loadout.healthPotion.cost,
                onMinus: () => AddPotion(-1),
                onPlus:  () => AddPotion(+1)
            );
        }
        if (loadout.damageBomb)
        {
            AddConsumableRow(
                icon: loadout.damageBomb.icon,
                name: loadout.damageBomb.displayName,
                desc: $"DMG {loadout.damageBomb.damageAmount}",
                price: loadout.damageBomb.cost,
                onMinus: () => AddBomb(-1),
                onPlus:  () => AddBomb(+1)
            );
        }
    }

    // ===================== 行生成：装备/饰品（按钮选择） =====================
    void AddEquipRow(Sprite icon, string name, string desc, int price, System.Action onClick)
    {
        var go = Instantiate(shopItemPrefab, contentRoot);
        var tr = go.transform;

        Req(tr, "BuyBtn").gameObject.SetActive(true);
        var qtyGroup = Req(tr, "QtyGroup"); if (qtyGroup) qtyGroup.gameObject.SetActive(false);

        // Icon
        var iconImg = Req(tr, "Icon").GetComponent<Image>();
        if (iconImg)
        {
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.enabled = icon != null; // 没有图标时可以隐藏
        }

        // 文本
        ReqText(tr, "Name").text  = name;
        ReqText(tr, "Desc").text  = desc;
        ReqText(tr, "Price").text = price.ToString();

        // 按钮
        ReqButton(tr, "BuyBtn").onClick.AddListener(() =>
        {
            onClick?.Invoke();
            loadout.RefreshSend(); // 触发一次刷新（见下方扩展方法）
        });
    }

    // ===================== 行生成：消耗品（数量 +/-） =====================
    void AddConsumableRow(Sprite icon, string name, string desc, int price,
                          System.Action onMinus, System.Action onPlus)
    {
        var go = Instantiate(shopItemPrefab, contentRoot);
        var tr = go.transform;

        Req(tr, "BuyBtn").gameObject.SetActive(false);
        var qtyGroup = Req(tr, "QtyGroup"); qtyGroup.gameObject.SetActive(true);

        var iconImg = Req(tr, "Icon").GetComponent<Image>();
        if (iconImg)
        {
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.enabled = icon != null;
        }

        ReqText(tr, "Name").text  = name;
        ReqText(tr, "Desc").text  = desc;
        ReqText(tr, "Price").text = price.ToString();

        var qtyText = ReqText(tr, "QtyGroup/QtyText");
        // 初始值从 Loadout 的输入框读取（若为空则给 0）
        qtyText.text = GuessQtyFor(name).ToString();

        ReqButton(tr, "QtyGroup/BtnMinus").onClick.AddListener(() =>
        {
            onMinus?.Invoke();
            qtyText.text = GuessQtyFor(name).ToString();
            loadout.RefreshSend();
        });
        ReqButton(tr, "QtyGroup/BtnPlus").onClick.AddListener(() =>
        {
            onPlus?.Invoke();
            qtyText.text = GuessQtyFor(name).ToString();
            loadout.RefreshSend();
        });
    }

    // ===================== 与 LoadoutManager 的桥接 =====================
    void SelectWeapon(WeaponSO w)
    {
        if (!loadout || !loadout.dropdownWeapon) return;
        int idx = loadout.weapons.IndexOf(w);
        loadout.dropdownWeapon.value = idx + 1;  // 0 = None
        loadout.OnWeaponChanged(idx + 1);
    }

    void SelectArmor(ArmorSO a)
    {
        if (!loadout || !loadout.dropdownArmor) return;
        int idx = loadout.armors.IndexOf(a);
        loadout.dropdownArmor.value = idx + 1;   // 0 = None
        loadout.OnArmorChanged(idx + 1);
    }

    void AddPotion(int delta)
    {
        if (!loadout || !loadout.inputPotions) return;
        int v = ParseInt(loadout.inputPotions.text);
        v = Mathf.Max(0, v + delta);
        loadout.inputPotions.text = v.ToString();
        loadout.inputPotions.onValueChanged.Invoke(loadout.inputPotions.text);
    }

    void AddBomb(int delta)
    {
        if (!loadout || !loadout.inputBombs) return;
        int v = ParseInt(loadout.inputBombs.text);
        v = Mathf.Max(0, v + delta);
        loadout.inputBombs.text = v.ToString();
        loadout.inputBombs.onValueChanged.Invoke(loadout.inputBombs.text);
    }

    int GuessQtyFor(string displayName)
    {
        if (!loadout) return 0;
        if (loadout.healthPotion && displayName == loadout.healthPotion.displayName)
            return ParseInt(loadout.inputPotions ? loadout.inputPotions.text : "0");
        if (loadout.damageBomb && displayName == loadout.damageBomb.displayName)
            return ParseInt(loadout.inputBombs ? loadout.inputBombs.text : "0");
        return 0;
    }

    int ParseInt(string s) => int.TryParse(s, out var v) ? Mathf.Max(0, v) : 0;

    // ===================== 小工具（缺失提示更友好） =====================
    Transform Req(Transform root, string path)
    {
        var t = root.Find(path);
        if (!t) Debug.LogError($"[ShopItem] Missing child: {path}");
        return t;
    }
    TMP_Text ReqText(Transform root, string path)
    {
        var t = Req(root, path);
        var x = t ? t.GetComponent<TMP_Text>() : null;
        if (!x) Debug.LogError($"[ShopItem] '{path}' needs TMP_Text component");
        return x;
    }
    Button ReqButton(Transform root, string path)
    {
        var t = Req(root, path);
        var x = t ? t.GetComponent<Button>() : null;
        if (!x) Debug.LogError($"[ShopItem] '{path}' needs Button component");
        return x;
    }
}

// ------- 给 LoadoutManager 的一个很小的“刷新钩子”扩展 -------
// 这样 ShopUI 在点击后可以请求刷新预算/左侧数值；
// 如果你的 LoadoutManager 没有 public RefreshUI()，可以用 SendMessage 的方式。
public static class LoadoutManagerExt
{
    public static void RefreshSend(this LoadoutManager lm)
    {
        if (!lm) return;
        // 如果你在 LoadoutManager 里把 RefreshUI() 设成了 private，可用 SendMessage 调用
        lm.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);
    }
}
