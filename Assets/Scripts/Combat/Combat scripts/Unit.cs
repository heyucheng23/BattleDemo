using UnityEngine;

public class Unit : MonoBehaviour {
    public string unitName = "Hero";
    public int maxHP = 100;
    public int currentHP;
    public int battleATK;   // 含装备后的ATK
    public int battleDEF;   // 目前未必用到，留存

    void Awake() {
        currentHP = maxHP;
    }

    public bool TakeDamage(int dmg) {
        currentHP = Mathf.Max(0, currentHP - Mathf.Max(0, dmg));
        return currentHP == 0;
    }

    public void Heal(int amount) {
        currentHP = Mathf.Min(maxHP, currentHP + Mathf.Max(0, amount));
    }
}
