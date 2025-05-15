using System;
using System.Collections.Generic;
using BTreeIndex;

namespace Program {
    class Index {
        public static void Main() {
            var index = new IndexTree(10, "index.txt");
            index.Insert(1960, 200);
            index.Insert(1980, 100);
            index.Insert(1940, 300);
            index.Insert(2010, 200);
            index.Insert(1820, 100);
            index.Insert(1990, 300);
            index.Insert(2025, 300);
            index.Insert(2025, 400);
            index.Insert(1750, 300);
            index.Insert(2003, 300);
            index.Insert(2004, 300);
            index.Insert(2005, 300);
            index.Insert(2006, 300);
            index.Insert(2007, 300);
            Console.WriteLine(string.Join(",", index.Search(2025)));
        }
    }
}