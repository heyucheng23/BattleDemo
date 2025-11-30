using UnityEngine;

public class CameraFollowWithBounds : MonoBehaviour
{
    public Transform target;    // 玩家
    public float smoothTime = 0.2f;

    [Header("相机可移动的边界（以世界坐标设置）")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // 相机想要去的位置（不包含 Z）
        float targetX = Mathf.Clamp(target.position.x, minX, maxX);
        float targetY = Mathf.Clamp(target.position.y, minY, maxY);

        Vector3 targetPos = new Vector3(targetX, targetY, transform.position.z);

        // 平滑移动相机
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );
    }
}
