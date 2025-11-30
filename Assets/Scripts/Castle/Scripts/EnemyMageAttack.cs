using UnityEngine;
using System.Collections;

public class EnemyMageAttack : MonoBehaviour
{
    [Header("Fireball Settings")]
    public GameObject fireballPrefab;
    public Transform firePoint;
    public float attackInterval = 2f;
    public float castDelay = 0.4f;
    public float fireballSpeed = 6f;

    [Header("Direction (✔ = Left, ✘ = Right)")]
    public bool shootLeft = true;

    private float timer = 0f;
    private Animator anim;
    private Enemy enemy;
    private bool isAttacking = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        enemy = GetComponent<Enemy>();

        ApplyFacing();
    }

    void Update()
    {
        if (enemy == null) return;
        if (isAttacking) return;

        timer += Time.deltaTime;

        if (timer >= attackInterval)
        {
            StartCoroutine(CastAndShootRoutine());
            timer = 0f;
        }
    }

    IEnumerator CastAndShootRoutine()
    {
        isAttacking = true;

        // 保证朝向正确
        ApplyFacing();

        if (anim != null)
            anim.SetTrigger("Cast");

        yield return new WaitForSeconds(castDelay);

        Shoot();

        isAttacking = false;
    }

    void Shoot()
    {
        if (fireballPrefab == null || firePoint == null) return;

        GameObject fb = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);

        Fireball fireball = fb.GetComponent<Fireball>();
        fireball.speed = fireballSpeed;
        fireball.shootLeft = shootLeft;
    }

    // 🎯 这个函数自动翻转角色
    void ApplyFacing()
    {
        Vector3 scale = transform.localScale;

        if (shootLeft)
        {
            // ✔ 勾选 = 朝左
            scale.x = Mathf.Abs(scale.x);      // 如果你的 sprite 正方向是左
        }
        else
        {
            // ✘ 不勾 = 朝右
            scale.x = -Mathf.Abs(scale.x);
        }

        transform.localScale = scale;
    }
}
