using System;
using System.Collections.Generic;
using System.Linq;

namespace BTreeIndex {
    public class IndexTree {
        /*
            Tem que refazer essa merda toda, pra ser só uma classe
            que manipula o arquivo txt
        */
        private readonly int order;
        private IndexNode root;

        public IndexTree(int order) {
            this.order = order;
            this.root = new IndexNode(true);
        }

        public void insert(int ano_colheita, int linha) {
            var newNode = this.internalInsert(this.root, new IndexRegistry(ano_colheita, linha));

            if (newNode != null) {
                var newRoot = new IndexNode(false);
                newRoot.keys.Add(newNode.Item1);
                newRoot.children.Add(this.root);
                newRoot.children.Add(newNode.Item2);
                this.root = newRoot;
            }
        }

        private Tuple<int, IndexNode> internalInsert(IndexNode node, IndexRegistry registry) {
            int i = node.keys.BinarySearch(registry.ano_colheita);
            if (i < 0) {
                i = ~i;
            }
            
            if (node.isLeaf) {
                if (i < node.keys.Count && node.keys[i] == registry.ano_colheita) {
                    node.registries[i].Add(registry);
                } else {
                    node.keys.Insert(i, registry.ano_colheita);
                    node.registries.Insert(i, new List<IndexRegistry> { registry });
                }

                if (node.keys.Count > this.order) {
                    return this.splitLeaf(node);
                }

                return null;
            }

            var result = this.internalInsert(node.children[i], registry);
            if (result != null) {
                int chosenKey = result.Item1;
                IndexNode chosenNode = result.Item2;

                int j = node.keys.BinarySearch(chosenKey);
                if (j < 0) {
                    j = ~j;
                }

                node.keys.Insert(j, chosenKey);
                node.children.Insert(j+1, chosenNode);

                if (node.keys.Count > order) {
                    return this.splitInternal(node);
                }
            }

            return null;
        }

        private Tuple<int, IndexNode> splitLeaf(IndexNode leaf) {
            int half = leaf.keys.Count / 2;
            var newLeaf = new IndexNode(true);

            newLeaf.keys.AddRange(leaf.keys.GetRange(half, leaf.keys.Count - half));
            newLeaf.registries.AddRange(leaf.registries.GetRange(half, leaf.registries.Count - half));

            leaf.keys.RemoveRange(half, leaf.keys.Count - half);
            leaf.registries.RemoveRange(half, leaf.registries.Count - half);

            newLeaf.next = leaf.next;
            leaf.next = newLeaf;

            return Tuple.Create(newLeaf.keys[0], newLeaf);
        }

        private Tuple<int, IndexNode> splitInternal(IndexNode node) {
            int half = node.keys.Count / 2;
            int chosenKey = node.keys[half];

            var newNode = new IndexNode(false);
            newNode.keys.AddRange(node.keys.GetRange(half + 1, node.keys.Count - half - 1));
            newNode.children.AddRange(node.children.GetRange(half + 1, node.children.Count - half - 1));

            node.keys.RemoveRange(half, node.keys.Count - half);
            node.children.RemoveRange(half + 1, node.children.Count - half - 1);

            return Tuple.Create(chosenKey, newNode);
        }

        public List<int> search(int ano_colheita) {
            var node = this.root;
            while (!node.isLeaf) {
                int i = node.keys.BinarySearch(ano_colheita);
                if (i < 0) {
                    i = ~i;
                }
                node = node.children[i];
            }

            int j = node.keys.BinarySearch(ano_colheita);
            if (j >= 0) {
                return node.registries[j].Select(r => r.linha).ToList();
            }
            return new List<int>();
        }

        public void printLeafs() {
            var node = this.root;
            while (!node.isLeaf) {
                node = node.children[0];
            }

            while (node != null) {
                Console.WriteLine("[ ");
                for (int i = 0; i < node.keys.Count; i++) {
                    Console.WriteLine($"{node.keys[i]} (linhas: {string.Join(",", node.registries[i].Select(r => r.linha))})");
                }
                Console.WriteLine("] -> ");
                node = node.next;
            }

            Console.WriteLine("null");
        }
    }
}