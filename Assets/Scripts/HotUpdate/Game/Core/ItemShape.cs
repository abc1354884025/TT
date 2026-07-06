using System;
using UnityEngine;

/// <summary>
/// 物品形状定义。用 bool[,] 存储，支持 90° 旋转。
/// JSON 格式：[[1,1],[1,0]] 表示 2x2 的 L 形。
/// </summary>
[Serializable]
public class ItemShape
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool[,] Cells { get; private set; }

    public ItemShape(int width, int height)
    {
        Width = width;
        Height = height;
        Cells = new bool[width, height];
    }

    public ItemShape(bool[,] cells)
    {
        Width = cells.GetLength(0);
        Height = cells.GetLength(1);
        Cells = (bool[,])cells.Clone();
    }

    /// <summary>从 JSON int[][] 创建形状</summary>
    public static ItemShape FromJson(int[][] matrix)
    {
        if (matrix == null || matrix.Length == 0)
            return new ItemShape(1, 1) { Cells = { [0, 0] = true } };

        int w = 0;
        foreach (var row in matrix)
            if (row.Length > w) w = row.Length;
        int h = matrix.Length;

        var shape = new ItemShape(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < matrix[y].Length; x++)
                shape.Cells[x, y] = matrix[y][x] != 0;
        return shape;
    }

    /// <summary>顺时针旋转 90°（返回新实例，原形状不变）</summary>
    public ItemShape Rotate(int times = 1)
    {
        times = ((times % 4) + 4) % 4;
        if (times == 0) return new ItemShape(Cells);

        bool[,] current = Cells;
        int w = Width, h = Height;

        for (int r = 0; r < times; r++)
        {
            bool[,] rotated = new bool[h, w]; // 旋转后宽高互换
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    rotated[h - 1 - y, x] = current[x, y];
            current = rotated;
            int tmp = w; w = h; h = tmp;
        }

        return new ItemShape(current);
    }

    /// <summary>获取所有被占用的格子偏移列表</summary>
    public Vector2Int[] GetOccupiedCells()
    {
        var list = new System.Collections.Generic.List<Vector2Int>();
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (Cells[x, y])
                    list.Add(new Vector2Int(x, y));
        return list.ToArray();
    }

    /// <summary>形状的格子数量</summary>
    public int CellCount
    {
        get
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (Cells[x, y]) count++;
            return count;
        }
    }

    /// <summary>调试输出</summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{Width}x{Height}:");
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
                sb.Append(Cells[x, y] ? "■" : "□");
            if (y < Height - 1) sb.AppendLine();
        }
        return sb.ToString();
    }
}
