using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    [Header("生命值")]
    public int maxHealth = 3;
    public float deathDelay = 0.2f;

    [Header("接触伤害")]
    public int contactDamage = 1;
    public float knockbackForceToPlayer = 5f;

    [Header("击退")]
    public float knockbackDistance = 0.15f;   // 被击退的距离（你可以改成 0.1~0.3）

    [Header("受击闪烁设置")]
    public float flashDuration = 0.25f;
    public float flashInterval = 0.06f;

    [Header("Hit Stun")]
    public float hitStunDuration = 0.05f; // 敌人被攻击时停顿的时间

    [Header("死亡特效")]
    public GameObject deathVfxPrefab;
    public Vector2 deathVfxOffset = new Vector2(0f, 0.4f);
    public float deathVfxLifeTime = 1f;

    private int currentHealth;
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private SpriteRenderer sr;
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    // =============================
    // 被玩家攻击
    // =============================
    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isDead) return;

        currentHealth -= damage;

        // 播放受击动画
        if (anim != null)
            anim.SetTrigger("Hurt");

        // 轻微击退
        Vector2 knockOffset = hitDirection.normalized * knockbackDistance;
        rb.MovePosition(rb.position + knockOffset);

        // HitStun + 闪烁
        StartCoroutine(HitStunRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // =============================
    // Hit Stun + 闪烁协程
    // =============================
    IEnumerator HitStunRoutine()
    {
        // 暂停移动（保存速度）
        Vector2 originalVel = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;

        // 闪烁效果
        StartCoroutine(FlashRoutine());

        // 停顿 hitstunDuration 秒
        yield return new WaitForSeconds(hitStunDuration);

        // 恢复移动
        rb.linearVelocity = originalVel;
    }

    // =============================
    // 受击闪烁
    // =============================
    IEnumerator FlashRoutine()
    {
        float timer = 0f;
        bool visible = true;

        while (timer < flashDuration)
        {
            visible = !visible;
            sr.enabled = visible;

            timer += flashInterval;
            yield return new WaitForSeconds(flashInterval);
        }

        sr.enabled = true; // 最后确保恢复可见
    }

    // =============================
    // 死亡
    // =============================
    void Die()
    {
        if (isDead) return;
        isDead = true;

        col.enabled = false;
        rb.linearVelocity = Vector2.zero;

        if (anim != null)
            anim.SetTrigger("Die");

        // 死亡特效
        if (deathVfxPrefab != null)
        {
            var fx = Instantiate(
                deathVfxPrefab,
                transform.position + (Vector3)deathVfxOffset,
                Quaternion.identity
            );

            Destroy(fx, deathVfxLifeTime);
        }

        Destroy(gameObject, deathDelay);
    }

    // =============================
    // 接触伤害（撞到玩家）
    // =============================
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.collider.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector2 dir = (collision.transform.position - transform.position).normalized;
                playerHealth.TakeDamage(contactDamage, dir * knockbackForceToPlayer);
            }
        }
    }
}
