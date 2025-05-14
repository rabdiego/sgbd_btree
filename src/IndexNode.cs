using System.Collections.Generic;

namespace BTreeIndex {
    public enum NodeType {
        Leaf,
        Internal
    }

    public class IndexNode {
        public bool isLeaf { get; set; }
        public List<int> keys { get; set; }  // ano_colheita
        public int parent { get; set; }
        public List<int> children { get; set; }
        public int prev { get; set; }
        public int next { get; set; }
        public List<List<int>> refs { get; set; }  // linhas

        public IndexNode(bool isLeaf, List<int> keys, int parent, List<int> children, int prev, int next, List<List<int>> refs) {
            this.isLeaf = isLeaf;
            this.keys = keys;
            this.parent = parent;
            this.children = children;
            this.prev = prev;
            this.next = next;
            this.refs = refs;
        }
    }
}