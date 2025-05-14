using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BTreeIndex {
    public class IndexTree {
        private readonly int order;
        private readonly string filePath;
        private int rootId = 0;

        public IndexTree(int order, string filePath) {
            this.order = order;
            this.filePath = filePath;
            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0) {
                var root = new IndexNode { id = 0, isLeaf = true };
                File.WriteAllLines(filePath, new[] { root.Serialize() });
            }
        }

        public void Insert(int key, int lineRef) {
            var node = ReadNode(rootId);
            var splitResult = InternalInsert(node, key, lineRef);

            if (splitResult != null) {
                var newRoot = new IndexNode {
                    id = GetNextId(),
                    isLeaf = false,
                    keys = new List<int> { splitResult.Item1 },
                    children = new List<int> { rootId, splitResult.Item2 }
                };

                WriteNode(newRoot);
                rootId = newRoot.id;
            }
        }

        private Tuple<int, int> InternalInsert(IndexNode node, int key, int lineRef) {
            if (node.isLeaf) {
                int pos = node.keys.BinarySearch(key);
                if (pos < 0) pos = ~pos;

                node.keys.Insert(pos, key);
                node.refs.Insert(pos, new List<int> { lineRef });

                if (node.keys.Count > order) {
                    return SplitLeaf(node);
                } else {
                    WriteNode(node);
                    return null;
                }
            } else {
                int i = node.keys.BinarySearch(key);
                if (i < 0) i = ~i;
                int childId = node.children[i];

                var childNode = ReadNode(childId);
                var result = InternalInsert(childNode, key, lineRef);

                if (result != null) {
                    int middleKey = result.Item1;
                    int newChildId = result.Item2;

                    int insertPos = node.keys.BinarySearch(middleKey);
                    if (insertPos < 0) insertPos = ~insertPos;
                    node.keys.Insert(insertPos, middleKey);
                    node.children.Insert(insertPos + 1, newChildId);

                    if (node.keys.Count > order) {
                        return SplitInternal(node);
                    } else {
                        WriteNode(node);
                    }
                } else {
                    WriteNode(node);
                }

                return null;
            }
        }

        private Tuple<int, int> SplitLeaf(IndexNode leaf) {
            int newId = GetNextId();
            var newLeaf = new IndexNode {
                id = newId,
                isLeaf = true,
                parent = leaf.parent
            };

            int mid = leaf.keys.Count / 2;

            newLeaf.keys.AddRange(leaf.keys.Skip(mid));
            newLeaf.refs.AddRange(leaf.refs.Skip(mid));

            leaf.keys.RemoveRange(mid, leaf.keys.Count - mid);
            leaf.refs.RemoveRange(mid, leaf.refs.Count - mid);

            newLeaf.next = leaf.next;
            newLeaf.prev = leaf.id;
            leaf.next = newId;

            WriteNode(leaf);
            WriteNode(newLeaf);
            UpdateNeighborLinks(newLeaf);

            return Tuple.Create(newLeaf.keys[0], newId);
        }

        private Tuple<int, int> SplitInternal(IndexNode node) {
            int newId = GetNextId();
            int mid = node.keys.Count / 2;

            var newNode = new IndexNode {
                id = newId,
                isLeaf = false,
                parent = node.parent
            };

            newNode.keys.AddRange(node.keys.Skip(mid + 1));
            newNode.children.AddRange(node.children.Skip(mid + 1));

            int middleKey = node.keys[mid];

            node.keys.RemoveRange(mid, node.keys.Count - mid);
            node.children.RemoveRange(mid + 1, node.children.Count - (mid + 1));

            WriteNode(node);
            WriteNode(newNode);

            return Tuple.Create(middleKey, newId);
        }

        private void UpdateNeighborLinks(IndexNode newLeaf) {
            if (newLeaf.next != -1) {
                var nextNode = ReadNode(newLeaf.next);
                nextNode.prev = newLeaf.id;
                WriteNode(nextNode);
            }
        }

        private IndexNode ReadNode(int id) {
            var sr = new StreamReader(filePath);
            for (int i = 0; i <= id; i++) {
                var line = sr.ReadLine();
                if (i == id) {
                    sr.Dispose();
                    return IndexNode.Deserialize(line);
                }
            }
            sr.Dispose();
            throw new Exception($"Node {id} not found");
        }

        public void WriteNode(IndexNode node) {
            // Read the entire file and keep it in memory
            List<string> lines;
            using (var reader = new StreamReader(filePath)) {
                lines = reader.ReadToEnd().Split('\n').ToList();
            }

            // Modify or add the node representation (this depends on your logic)
            string nodeLine = node.Serialize();
            int index = lines.FindIndex(line => line.StartsWith(node.id.ToString())); // Find node by ID
            if (index >= 0) {
                // If node exists, update it
                lines[index] = nodeLine;
            } else {
                // If node doesn't exist, add it
                lines.Add(nodeLine);
            }

            // Write back to the file (overwriting the entire content)
            using (var writer = new StreamWriter(filePath, false)) {
                foreach (var line in lines) {
                    writer.WriteLine(line);
                }
            }
        }

        private int GetNextId() {
            return File.ReadAllLines(filePath).Length;
        }
    }
}
