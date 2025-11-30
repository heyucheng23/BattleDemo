using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GroundPatrolEnemy : MonoBehaviour
{
    [Header("巡逻点（在场景里放两个空物体，拖进来）")]
    public Transform pointA;
    public Transform pointB;

    [Header("移动参数")]
    public float moveSpeed = 2f;          // 速度
    public float arriveThreshold = 0.05f; // 认为到达目标的误差

    [Header("初始朝向设置")]
    [Tooltip("如果本来 scale.x > 0 时朝右，就勾上；如果素材默认朝左，就不要勾。")]
    public bool spriteFacesRightWhenScalePositive = true;

    [Header("动画设置（可选）")]
    public string idleStateName = "Idle"; // Animator 里的 Idle 状态名

    private Rigidbody2D rb;
    private Animator anim;
    private bool movingToB = true;   // 当前是否在往 B 走
    private bool facingRight;        // 当前是否朝右

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // 根据当前缩放和你填的选项，推断一开始的 facingRight
        facingRight = (transform.localScale.x > 0f) == spriteFacesRightWhenScalePositive;

        // 一直播放 Idle（只要 Animator 里有这个状态并设成 Loop，就会循环）
        if (anim != null && !string.IsNullOrEmpty(idleStateName))
        {
            anim.Play(idleStateName);
        }
    }

    void FixedUpdate()
    {
        if (pointA == null || pointB == null) return;

        // 目标点：在 A、B 之间切换
        Transform target = movingToB ? pointB : pointA;

        // 沿 X 轴向目标移动（用 MoveTowards 防止冲过头）
        float currentX = rb.position.x;
        float targetX = target.position.x;

        float newX = Mathf.MoveTowards(
            currentX,
            targetX,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(new Vector2(newX, rb.position.y));

        // 计算移动方向
        float dir = targetX - currentX;

        // 根据移动方向翻转朝向
        if (dir > 0.01f && !facingRight)
        {
            Flip();    // 往右走但是没朝右 → 翻一下
        }
        else if (dir < -0.01f && facingRight)
        {
            Flip();    // 往左走但是还朝右 → 翻一下
        }

        // 到达目标点（或非常接近）后，切换目标点
        if (Mathf.Abs(newX - targetX) <= arriveThreshold)
        {
            movingToB = !movingToB;
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }
}
