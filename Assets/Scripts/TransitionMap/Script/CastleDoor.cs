using UnityEngine;
using UnityEngine.SceneManagement;

public class CastleDoor : MonoBehaviour
{
    [Header("要切换到的场景名")]
    public string nextSceneName = "Castle";

    [Header("需要玩家停留多少秒才切换（可选）")]
    public float delayBeforeLoad = 0f;

    private bool isLoading = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading) return;

        if (other.CompareTag("Player"))
        {
            isLoading = true;

            if (delayBeforeLoad > 0)
                Invoke(nameof(LoadScene), delayBeforeLoad);
            else
                LoadScene();
        }
    }

    void LoadScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
