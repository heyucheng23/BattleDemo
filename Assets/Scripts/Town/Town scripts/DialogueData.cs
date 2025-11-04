using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [System.Serializable] public struct Line { public string speaker; [TextArea(2,5)] public string text; }
    public Line[] lines;

    public enum AfterAction { None, OpenShopScene, OpenShopPanel }
    public AfterAction afterAction = AfterAction.None;
    public string shopSceneName = "Shop";
}
