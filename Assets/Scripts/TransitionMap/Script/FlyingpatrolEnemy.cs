using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingPatrolEnemy : MonoBehaviour
{
    [Header("巡逻点（在场景放两个空物体）")]
    public Transform pointA;
    public Transform pointB;

    [Header("移动参数")]
    public float moveSpeed = 2f;
    public float arriveThreshold = 0.1f;

    [Header("初始朝向设置")]
    [Tooltip("如果你的 sprite 在 scale.x = 正 时朝右，勾上。")]
    public bool spriteFacesRightWhenScalePositive = true;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform target;
    private bool facingRight;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // 关键：根据贴图实际朝向 + scale，自动判断 facingRight
        facingRight = (transform.localScale.x > 0f) == spriteFacesRightWhenScalePositive;

        target = pointB;
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;

        Vector2 pos = rb.position;
        Vector2 targetPos = target.position;

        // 距离目标很近就换方向
        if (Vector2.Distance(pos, targetPos) <= arriveThreshold)
        {
            target = (target == pointA) ? pointB : pointA;
            targetPos = target.position;
        }

        // 计算移动方向
        Vector2 dir = (targetPos - pos).normalized;

        // 飞行移动
        rb.linearVelocity = dir * moveSpeed;

        // 自动翻转
        if (dir.x > 0f && !facingRight)
            Flip();
        else if (dir.x < 0f && facingRight)
            Flip();
    }

    void Flip()
    {
        facingRight = !facingRight;

        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }
}
