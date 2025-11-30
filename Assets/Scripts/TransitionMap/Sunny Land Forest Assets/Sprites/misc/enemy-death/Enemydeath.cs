using UnityEngine;

public class AutoDestroyAfterAnimation : MonoBehaviour
{
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (anim == null) return;

        var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.normalizedTime >= 1f && !anim.IsInTransition(0))
        {
            Destroy(gameObject);
        }
    }
}
