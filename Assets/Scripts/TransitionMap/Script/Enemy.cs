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
    public float knockbackDistance = 0.15f;

    [Header("受击闪烁")]
    public float flashDuration = 0.25f;
    public float flashInterval = 0.06f;

    [Header("Hit Stun")]
    public float hitStunDuration = 0.05f;

    [Header("死亡特效")]
    public GameObject deathVfxPrefab;
    public Vector2 deathVfxOffset = new Vector2(0f, 0.4f);
    public float deathVfxLifeTime = 1f;

    [Header("死亡音效")]                   // ⭐ 新增
    public AudioClip deathSfx;             // ⭐ 新增
    public float deathSfxVolume = 1f;      // ⭐ 新增
    private AudioSource audioSrc;          // ⭐ 新增

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

        // ⭐ 新增：如果没有 AudioSource 自动添加
        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null)
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 0;   // 2D 音效（横版一般用 2D）
        }
    }

    // =============================
    // 被玩家攻击
    // =============================
    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (anim != null)
            anim.SetTrigger("Hurt");

        Vector2 knockOffset = hitDirection.normalized * knockbackDistance;
        rb.MovePosition(rb.position + knockOffset);

        StartCoroutine(HitStunRoutine());

        if (currentHealth <= 0)
            Die();
    }

    IEnumerator HitStunRoutine()
    {
        Vector2 originalVel = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;

        StartCoroutine(FlashRoutine());
        yield return new WaitForSeconds(hitStunDuration);

        rb.linearVelocity = originalVel;
    }

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

        sr.enabled = true;
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

        // ⭐ 死亡音效
        if (deathSfx != null && audioSrc != null)
        {
            audioSrc.PlayOneShot(deathSfx, deathSfxVolume);
        }

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
    // 碰撞造成伤害
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
