using UnityEngine;

public class StageConfigDebug : MonoBehaviour {
    void Start() {
        var s = StageConfigLoader.Load(); // 默认加载 boss_stage.json
        if (s != null) {
            Debug.Log($"✅ JSON Loaded → BossHP={s.HP_boss}, BossATK={s.ATK_boss}, T={s.T_max}, Budget={s.B}, HeroHP0={s.HP0}");
        } else {
            Debug.LogError("❌ JSON load failed!");
        }
    }
}
