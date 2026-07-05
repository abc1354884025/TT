using System;
using UnityEngine;

    /// <summary>
    /// 网格单元格数据抽象基类。每种益智游戏继承此类定义自己的格子状态。
    /// </summary>
    [Serializable]
    public abstract class GridCellData : ICloneable
    {
        /// <summary>在网格中的位置</summary>
        public Vector2Int Position;

        /// <summary>是否为线索格（不可修改的预设格子）</summary>
        public bool IsLocked;

        /// <summary>重置为默认状态</summary>
        public abstract void ResetToDefault();

        /// <summary>深拷贝</summary>
        public abstract object Clone();
    }
