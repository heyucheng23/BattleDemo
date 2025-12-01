using UnityEngine;

public class ExplosionAutoDestroy : MonoBehaviour
{
    public float life = 0.5f; // 爆炸动画长度

    void Start()
    {
        Destroy(gameObject, life);
    }
}
