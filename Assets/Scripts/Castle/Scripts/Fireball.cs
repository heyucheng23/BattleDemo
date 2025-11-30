using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("移动")]
    public float speed = 6f;          // 子弹速度
    public bool shootLeft = true;     // true = 向左发射，false = 向右

    [Header("伤害")]
    public int damage = 1;            // 伤害值
    public float knockbackForce = 5f; // 击退力度

    [Header("其他")]
    public float lifeTime = 3f;       // 最长存活时间（秒）

    private Vector2 dir;

    void Start()
    {
        // 只水平移动
        dir = shootLeft ? Vector2.left : Vector2.right;

        // 一段时间后自动销毁，防止占内存
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(dir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 撞到玩家
        if (collision.CompareTag("Player"))
        {
            PlayerHealth player = collision.GetComponent<PlayerHealth>();
            if (player != null)
            {
                // 方向：火球 -> 玩家 的方向，用来算击退
                Vector2 hitDir = (collision.transform.position - transform.position).normalized;
                player.TakeDamage(damage, hitDir * knockbackForce);
            }

            Destroy(gameObject);
            return;
        }

        // 撞到地面 / 墙（Tilemap 那一层的物体，请设置 Tag = Ground）
        if (collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
