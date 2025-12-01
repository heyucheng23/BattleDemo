using UnityEngine;

public class BombProjectile : MonoBehaviour
{
    [Header("飞行参数")]
    public float travelTime = 0.5f;   // 飞行总时间
    public float arcHeight  = 2f;     // 抛物线最高点高度

    [Header("效果")]
    public GameObject explosionPrefab;   // 爆炸特效（可选）

    // 由 BattleSystem 注入
    private Vector3 _startPos;
    private Vector3 _targetPos;
    private int _damage;
    private BattleSystemTurnBased _battle;
    private bool _started = false;
    private float _elapsed = 0f;

    /// <summary>
    /// 由 BattleSystem 在 Instantiate 之后立即调用
    /// </summary>
    public void Init(Vector3 start, Vector3 target, int damage, BattleSystemTurnBased battle)
    {
        _startPos = start;
        _targetPos = target;
        _damage = damage;
        _battle = battle;

        transform.position = _startPos;
        _elapsed = 0f;
        _started = true;
    }

    void Update()
    {
        if (!_started) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, travelTime));

        // 平面线性插值
        Vector3 pos = Vector3.Lerp(_startPos, _targetPos, t);

        // 抛物线高度：一个简单的 4h * t(1-t) 曲线（0 和 1 为 0，中间最高）
        float h = 4f * arcHeight * t * (1f - t);
        pos.y += h;

        transform.position = pos;

        if (t >= 1f)
        {
            Explode();
        }
    }

    void Explode()
    {
        // 1) 特效
        if (explosionPrefab)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 2) 通知战斗系统：炸弹命中，去结算伤害 + 更新 UI
        if (_battle != null)
        {
            _battle.OnBombProjectileLanded(_damage);
        }

        // 3) 自己销毁
        Destroy(gameObject);
    }
}
