using System;
using UnityEngine;

    /// <summary>
    /// 关卡描述抽象基类。每个益智游戏的 LevelData 继承此类。
    /// </summary>
    [Serializable]
    public abstract class PuzzleLevelData
    {
        /// <summary>关卡唯一标识</summary>
        public string LevelId;

        /// <summary>关卡序号（从 0 开始）</summary>
        public int LevelIndex;

        /// <summary>网格宽度</summary>
        public int GridWidth;

        /// <summary>网格高度</summary>
        public int GridHeight;

        /// <summary>游戏类型</summary>
        public PuzzleType PuzzleType;

        /// <summary>难度（1-5 星）</summary>
        public int Difficulty = 1;

        /// <summary>关卡名称（显示用）</summary>
        public string DisplayName;

        /// <summary>解析原始字符串数据并返回关卡对象（由子类实现）</summary>
        public abstract void Parse(string rawData);
    }

    /// <summary>关卡列表项数据（用于关卡选择 UIList）</summary>
    public class LevelSelectItemData
    {
        public int LevelIndex;
        public string Label;
        public bool IsUnlocked;
        public bool IsCompleted;
        public float BestTime;     // -1 = 未完成
        public string LevelId;
    }
