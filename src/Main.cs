using System;
using BTreeIndex;

namespace Program {
    class Index {
        public static void Main() {
            var btree = new IndexTree(3);
            btree.insert(2018, 0);
            btree.insert(2005, 1);
            btree.insert(2022, 2);
            btree.insert(1932, 3);
            btree.insert(2005, 4);
            btree.printLeafs();

            var lines = btree.search(2005);
            Console.WriteLine($"Linhas para o ano 2005: {string.Join(", ", lines)}");
        }
    }
}