using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public Transform cam;              // 拖 Main Camera（没拖会自动找）
    [Range(0f, 1.5f)] public float mulX = 0.3f;  // X轴跟随比例：0=不动，1=完全跟随
    [Range(0f, 1.5f)] public float mulY = 0.0f;  // Y轴（横版可设0）
    public bool lockZToInitial = true;           // 保持原Z不变

    Vector3 lastCamPos;
    float initialZ;

    void Start()
    {
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (cam) lastCamPos = cam.position;
        initialZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (!cam) return;
        Vector3 delta = cam.position - lastCamPos;
        transform.position += new Vector3(delta.x * mulX, delta.y * mulY, 0f);
        if (lockZToInitial) transform.position = new Vector3(transform.position.x, transform.position.y, initialZ);
        lastCamPos = cam.position;
    }
}
