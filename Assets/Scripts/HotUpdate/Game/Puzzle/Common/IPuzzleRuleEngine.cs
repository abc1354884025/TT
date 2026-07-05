using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// 益智游戏规则引擎接口。每种游戏实现自己的规则验证逻辑。
    /// 泛型参数 TGrid 为具体游戏的棋盘类型。
    /// </summary>
    public interface IPuzzleRuleEngine
    {
        /// <summary>检查当前棋盘状态是否为有效解（全部规则满足）</summary>
        bool IsSolutionValid();

        /// <summary>检查某个操作是否合法（用于实时反馈）</summary>
        bool IsMoveValid(PuzzleMove move);

        /// <summary>返回违反规则的所有格子坐标（用于高亮错误）</summary>
        List<Vector2Int> GetViolations();
    }
