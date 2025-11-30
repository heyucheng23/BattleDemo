using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("要跟随的目标（玩家）")]
    public Transform target;

    [Header("相机平滑时间")]
    public float smoothTime = 0.2f;

    [Header("相机相对玩家的偏移量")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        // 保持相机自己的 Z（一般是 -10）
        Vector3 targetPos = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        ) + new Vector3(offset.x, offset.y, 0f);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );
    }
}
