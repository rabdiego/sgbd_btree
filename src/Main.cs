using System;
using System.Collections.Generic;
using BTreeIndex;

namespace Program {
    class Index {
        public static void Main() {
            var index = new IndexTree(3, "index.txt");
            index.Insert(1990, 200);
            index.Insert(1988, 100);
            index.Insert(1989, 300);
            index.Insert(2000, 400);
        }
    }
}