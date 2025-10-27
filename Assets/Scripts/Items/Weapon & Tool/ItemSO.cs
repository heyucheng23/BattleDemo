using UnityEngine;

public class ItemSO : ScriptableObject
{
    public string displayName;
    public int cost;

    [Header("Visual")]
    public Sprite icon;   
}
