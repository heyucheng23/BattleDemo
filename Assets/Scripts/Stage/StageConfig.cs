using UnityEngine;

[System.Serializable]
public class StageConfig {
    public int HP_boss;    // Boss 总血量
    public int ATK_boss;   // Boss 每回合伤害（先做固定值）
    public int T_max;      // 最大玩家回合数
    public int B;          // 预算
    public int HP0;        // 英雄基础 HP
    public int ATK0;       // 英雄基础 ATK
    public int DEF0;       // 英雄基础 DEF
}

public static class StageConfigLoader {
    // 从 Resources/Data/{name}.json 加载
    public static StageConfig Load(string name = "boss_stage") {
        TextAsset ta = Resources.Load<TextAsset>($"Data/{name}");
        if (ta == null) {
            Debug.LogError($"StageConfigLoader: missing Resources/Data/{name}.json");
            return null;
        }
        return JsonUtility.FromJson<StageConfig>(ta.text);
    }
}
