using System;

/// <summary>物品 JSON 反序列化辅助——与 ItemData 同结构，用于 JsonUtility</summary>
[Serializable]
public class ItemConfig
{
    public ItemData[] items;
}
