using System.Collections.Generic;

namespace BTreeIndex {
    public class IndexNode {
        public bool isLeaf { get; set; }
        public List<int> keys { get; set; } = new List<int>();
        public List<IndexNode> children { get; set; } = new List<IndexNode>();
        public List<List<IndexRegistry>> registries { get; set; } = new List<List<IndexRegistry>>();
        public IndexNode next { get; set; } = null;

        public IndexNode(bool isLeaf) {
            this.isLeaf = isLeaf;
        }
    }
}