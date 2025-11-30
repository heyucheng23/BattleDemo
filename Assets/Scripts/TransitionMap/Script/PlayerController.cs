using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;

    [Header("跳跃参数")]
    public float jumpForce = 12f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("攻击参数")]
    public KeyCode attackKey = KeyCode.J;
    public float attackCooldown = 0.4f;
    public Transform attackPoint;          
    public float attackRange = 0.5f;       
    public int attackDamage = 1;            // 改：层不再需要

    [Header("攻击音效（可留空）")]
    public AudioSource audioSource;
    public AudioClip attackSfx;

    private Rigidbody2D rb;
    private Animator anim;
    private bool isFacingRight = true;
    private float moveInput;
    private bool isGrounded = false;
    private float lastAttackTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // 1. 水平输入
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2. 跳跃
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 3. 攻击
        if (Input.GetKeyDown(attackKey) && Time.time >= lastAttackTime + attackCooldown)
        {
            DoAttack();
        }

        // 4. 翻转朝向
        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();

        // 5. 动画参数
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
        anim.SetBool("Grounded", isGrounded);
    }

    void FixedUpdate()
    {
        // 6. 地面检测
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // 7. 移动
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    // ======== 执行攻击（Tag 版本） ========
    void DoAttack()
    {
        lastAttackTime = Time.time;

        anim.SetTrigger("Attack");

        if (audioSource != null && attackSfx != null)
            audioSource.PlayOneShot(attackSfx);

        if (attackPoint == null) return;

        // 不再使用 LayerMask，直接搜所有碰撞体
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange
        );

        foreach (var col in hits)
        {
            // 用 Tag 识别敌人（最关键的修改）
            if (!col.CompareTag("Enemy"))
                continue;

            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                Vector2 dir = (col.transform.position - transform.position).normalized;
                enemy.TakeDamage(attackDamage, dir);
            }
        }
    }

    // ======== 翻转人物 ========
    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // ======== Gizmos 可视化 ========
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
