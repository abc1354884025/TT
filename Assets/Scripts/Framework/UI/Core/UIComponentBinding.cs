using System;
using UnityEngine;

/// <summary>
/// Inspector 自动绑定结构体。在 UIPanel Inspector 中将组件拖入即可。
/// </summary>
[Serializable]
public class UIComponentBinding
{
    public string FieldName;
    public Component BindTarget;
}
