using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour
{
    [Header("生命值")]
    public int maxHealth = 5;
    public float invincibleTime = 1.5f;   // 无敌时间
    public float knockbackForce = 10f;    // 被打时击退力度
    public float hurtControlLock = 0.2f;  // 被打时暂时不能操作的时间

    private int currentHealth;
    private bool isInvincible = false;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PlayerMovement move;          // 你的移动脚本名如果不同就改成对应的

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        move = GetComponent<PlayerMovement>();
    }

    // 敌人调用的函数
    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isInvincible) return; // 无敌中不再受伤

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // 先清空当前速度，防止和击退混在一起
        rb.linearVelocity = Vector2.zero;

        // 击退（方向是“敌人→玩家”的方向）
        Vector2 force = hitDirection.normalized * knockbackForce;
        rb.AddForce(force, ForceMode2D.Impulse);

        // 短暂锁定玩家移动，避免你按键把击退抵消掉
        if (move != null) StartCoroutine(LockControl());

        // 开启无敌 + 闪烁
        StartCoroutine(InvincibleCoroutine());
    }

    void Die()
    {
        // TODO: 死亡动画 / 回到关卡等等
        Debug.Log("Player Dead");
    }

    IEnumerator LockControl()
    {
        if (move != null)
        {
            move.enabled = false;
            yield return new WaitForSeconds(hurtControlLock);
            move.enabled = true;
        }
    }

    IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;

        float timer = 0f;
        float flashInterval = 0.1f;
        bool visible = true;

        while (timer < invincibleTime)
        {
            visible = !visible;
            if (sr != null) sr.enabled = visible;

            timer += flashInterval;
            yield return new WaitForSeconds(flashInterval);
        }

        if (sr != null) sr.enabled = true;
        isInvincible = false;
    }
}
