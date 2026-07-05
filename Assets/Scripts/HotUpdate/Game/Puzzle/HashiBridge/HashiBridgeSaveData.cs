using System;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>数桥存档数据</summary>
    [Serializable]
    public class HashiBridgeSaveData
    {
        public int Width;
        public int Height;
        public List<BridgeRecord> PlacedBridges = new List<BridgeRecord>();

        public void FromGrid(HashiBridgeGrid grid)
        {
            Width = grid.Grid.Width;
            Height = grid.Grid.Height;
            PlacedBridges.Clear();
            foreach (var kv in grid.Bridges)
            {
                PlacedBridges.Add(new BridgeRecord(kv.Key.Item1.x, kv.Key.Item1.y, kv.Key.Item2.x, kv.Key.Item2.y, kv.Value));
            }
        }

        public void ToGrid(HashiBridgeGrid grid)
        {
            foreach (var br in PlacedBridges)
                grid.ModifyBridge(
                    new Vector2Int(br.X1, br.Y1),
                    new Vector2Int(br.X2, br.Y2),
                    br.Count);
        }

        public string ToJson() => JsonUtility.ToJson(this);
        public static HashiBridgeSaveData FromJson(string json) => JsonUtility.FromJson<HashiBridgeSaveData>(json);
    }
