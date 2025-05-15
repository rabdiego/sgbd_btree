using System;
using System.Collections.Generic;
using System.Linq;

namespace BTreeIndex {
    public class IndexNode {
        public int id { get; set; }
        public bool isLeaf { get; set; }
        public List<int> keys { get; set; } = new List<int>();
        public int parent { get; set; } = -1;
        public List<int> children { get; set; } = new List<int>();
        public int prev { get; set; } = -1;
        public int next { get; set; } = -1;
        public List<int> refs { get; set; } = new List<int>();

        public string Serialize() {
            string keysStr = string.Join(",", keys);
            string childrenStr = children.Count > 0 ? string.Join(",", children) : "null";
            string refsStr = string.Join(",", refs);

            return $"{id};{(isLeaf ? 1 : 0)};{keysStr};{parent};{childrenStr};{(prev == -1 ? "null" : prev.ToString())};{(next == -1 ? "null" : next.ToString())};{refsStr}";
        }

        public static IndexNode Deserialize(string line) {
            var parts = line.Split(';');
            var node = new IndexNode {
                id = int.Parse(parts[0]),
                isLeaf = parts[1] == "1",
                keys = parts[2] == "" ? new List<int>() : parts[2].Split(',').Select(int.Parse).ToList(),
                parent = int.Parse(parts[3]),
                children = parts[4] == "null" ? new List<int>() : parts[4].Split(',').Select(int.Parse).ToList(),
                prev = parts[5] == "null" ? -1 : int.Parse(parts[5]),
                next = parts[6] == "null" ? -1 : int.Parse(parts[6]),
                refs = parts[7] == "" ? new List<int>() : parts[7].Split(',').Select(int.Parse).ToList()
            };
            return node;
        }
    }
}