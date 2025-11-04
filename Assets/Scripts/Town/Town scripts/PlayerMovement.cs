using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;          // 移动速度
    public float jumpForce = 7f;          // 跳跃力度

    [Header("地面检测")]
    public Transform groundCheck;         // 脚底检测点
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;         // 哪些层算地面

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private float moveInput;
    private bool isGrounded;
    private bool isFacingRight = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // --------------------------
        // 1️⃣ 输入检测
        // --------------------------
        moveInput = Input.GetAxisRaw("Horizontal");

        // 跳跃
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // --------------------------
        // 2️⃣ 翻转朝向
        // --------------------------
        if (moveInput > 0 && !isFacingRight)
            Flip();
        else if (moveInput < 0 && isFacingRight)
            Flip();

        // --------------------------
        // 3️⃣ 更新动画参数
        // --------------------------
        if (anim != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(moveInput));
            anim.SetBool("Grounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        // --------------------------
        // 4️⃣ 地面检测
        // --------------------------
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // --------------------------
        // 5️⃣ 移动
        // --------------------------
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    void OnDrawGizmosSelected()
    {
        // 可视化地面检测点
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
