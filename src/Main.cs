using System;
using System.Collections.Generic;
using BTreeIndex;

namespace Program {
    class Index {
        public static void Main() {
            var inputManager = new InputManager("in.txt", "out.txt", "index.txt", "vinhos.csv");
            inputManager.ProccessInput();
        }
    }
}