using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StationaryPlantEnemy : MonoBehaviour
{
    [Header("攻击参数")]
    public int contactDamage = 1;
    public float knockbackForce = 4f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;

    [Header("检测玩家的原点（可为空，就在自己中心）")]
    public Transform attackOrigin;

    [Header("动画参数名")]
    public string idleBoolName = "Idle";
    public string attackTriggerName = "Attack";

    [Header("初始朝向设置")]
    public bool spriteFacesRightWhenScalePositive = true;  
    // 你的原图如果在 scale.x = 正 时是朝右 → 保持 true
    // 如果原图默认是朝左 → 设成 false

    private Animator anim;
    private float lastAttackTime = -999f;
    private Transform player;
    private bool facingRight = true;

    void Awake()
    {
        anim = GetComponent<Animator>();

        if (attackOrigin == null)
            attackOrigin = transform;

        // 找 Player
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;

        // 根据当前 scale + 用户设置来确定初始朝向
        facingRight = (transform.localScale.x > 0) == spriteFacesRightWhenScalePositive;
    }

    void Update()
    {
        if (player == null) return;

        UpdateFacing(); // ← 自动转向玩家

        // Idle 常亮
        if (anim != null && !string.IsNullOrEmpty(idleBoolName))
            anim.SetBool(idleBoolName, true);

        // 攻击冷却
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        // 玩家是否在攻击范围内？
        float dist = Vector2.Distance(attackOrigin.position, player.position);

        if (dist <= attackRange)
        {
            DoAttack();
        }
    }

    // ===================== 自动转向逻辑 =====================
    void UpdateFacing()
    {
        float dir = player.position.x - transform.position.x;

        if (dir > 0 && !facingRight)
        {
            facingRight = true;
            ApplyFacing();
        }
        else if (dir < 0 && facingRight)
        {
            facingRight = false;
            ApplyFacing();
        }
    }

    void ApplyFacing()
    {
        Vector3 s = transform.localScale;

        // 根据图像默认方向翻转
        float flip = spriteFacesRightWhenScalePositive ? 1f : -1f;

        s.x = Mathf.Abs(s.x) * (facingRight ? flip : -flip);
        transform.localScale = s;
    }

    // ===================== 攻击行为 =====================
    void DoAttack()
    {
        lastAttackTime = Time.time;

        // 播放攻击动画
        if (anim != null && !string.IsNullOrEmpty(attackTriggerName))
            anim.SetTrigger(attackTriggerName);

        // 伤害玩家
        Collider2D hit = Physics2D.OverlapCircle(
            attackOrigin.position,
            attackRange,
            LayerMask.GetMask("Player") // 也可改用 tag 查找
        );

        if (hit != null)
        {
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                ph.TakeDamage(contactDamage, dir);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) attackOrigin = transform;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin.position, attackRange);
    }
}
