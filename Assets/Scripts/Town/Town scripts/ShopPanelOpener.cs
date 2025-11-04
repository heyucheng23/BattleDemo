using UnityEngine;

public class ShopPanelOpener : MonoBehaviour
{
    public static ShopPanelOpener Instance;
    public GameObject shopRoot;   // 拖到你的 Loadout 面板根节点（同场景）

    void Awake()
    {
        Instance = this;
        if (shopRoot) shopRoot.SetActive(false);
    }

    public static void TryOpen()
    {
        if (Instance != null && Instance.shopRoot != null)
        {
            Instance.shopRoot.SetActive(true);
        }
        else
        {
            Debug.LogWarning("ShopPanelOpener not configured or shopRoot missing.");
        }
    }

    public static void TryClose()
    {
        if (Instance != null && Instance.shopRoot != null)
        {
            Instance.shopRoot.SetActive(false);
        }
    }
}
