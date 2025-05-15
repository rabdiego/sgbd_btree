using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BTreeIndex {
    /*
    O arquivo .txt possui um formato
    id;leaf;keys;parent;children;prev;next;refs
    Separados por ponto e vírgula, e valores multivariados
    são separados por vírgulas (no caso de keys, children e refs).
    A primeira linha do arquivo possui alguns metadados importantes,
    sendo esses o rootId e o nextId, sendo o id da raíz, e o próximo id
    possível, respectivamente. Para facilitar nossas vidas, decidimos 
    que o id de cada nó é exatamente igual à sua posição no arquivo.
    */
    public class IndexTree {
        private readonly int order;
        private readonly string filePath;
        private IndexNode nodeBuffer;

        private int pageSize;

        // Construtor, se passa a ordem e o caminho para o .txt
        public IndexTree(int order, string filePath) {
            this.order = order;
            this.pageSize = this.setPageSize(order);
            this.filePath = filePath;
            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0) {
                nodeBuffer = new IndexNode { id = 2, isLeaf = true };
                File.Create(filePath).Close();

                WriteFixedLine(0, "nextId:3;rootId:2");
                WriteFixedLine(1, "id;leaf;keys;parent;children;prev;next;refs");
                WriteFixedLine(2, nodeBuffer.Serialize());
            }
        }

        /*
        Calcula automaticamente o tamanho necessário de cada
        página (linha do .txt) a partir da ordem da árvore
        */
        public int setPageSize(int order) {
            return 37 + 12*order;
        }

        /*
        Método auxiliar para escrever uma linha no tamanho fixo
        setado pelo método anterior
        */
        public void WriteFixedLine(int line, string content) {
            Encoding encoding = Encoding.ASCII;
            byte[] bytes = encoding.GetBytes(content);

            if (bytes.Length > this.pageSize - 1)
                throw new InvalidOperationException($"Line too long for fixed size of {this.pageSize}");

            byte[] padded = new byte[this.pageSize];
            Array.Copy(bytes, padded, bytes.Length);

            for (int i = bytes.Length; i < this.pageSize - 1; i++)
                padded[i] = (byte)' ';
            padded[this.pageSize - 1] = (byte)'\n';

            using (FileStream fs = new FileStream(this.filePath, FileMode.Open, FileAccess.Write)) {
                fs.Seek(line * this.pageSize, SeekOrigin.Begin);
                fs.Write(padded, 0, this.pageSize);
            }
        }

        /*
        Método de busca por chave, se retorna uma
        lista com todas as referências (linhas do .csv) nela
        */
        public List<int> Search(int key) {
            LoadNode(GetRootId());
            List<int> references = new List<int>();
            bool gotToEnd = false;

            while (!nodeBuffer.isLeaf) {
                int i = 0;
                while (i < nodeBuffer.keys.Count && key > nodeBuffer.keys[i]) i++;
                LoadNode(nodeBuffer.children[i]);
            }

            while (!gotToEnd) {
                for (int i = 0; i < nodeBuffer.keys.Count; i++) {
                    if (nodeBuffer.keys[i] == key) {
                        references.Add(nodeBuffer.refs[i]);
                        Console.WriteLine(nodeBuffer.refs[i]);
                    } else if (nodeBuffer.keys[i] > key) {
                        gotToEnd = true;
                    }
                }

                LoadNode(nodeBuffer.next);
            }

            return references;
        }

        // Seta ou reseta o id da raíz
        public void WriteRootId(int rootId) {
            int nextId = GetNextId();
            WriteFixedLine(0, $"nextId:{nextId};rootId:{rootId}");
        }

        /*
        Método de alto nível para inserção.
        Utilizamos uma pilha para lembrar os ids que passamos
        durante a ida até a folha em questão.
        */
        public void Insert(int key, int refId) {
            Stack<int> path = new Stack<int>();
            LoadNode(GetRootId()); // raiz

            while (!nodeBuffer.isLeaf) {
                path.Push(nodeBuffer.id);
                int i = 0;
                while (i < nodeBuffer.keys.Count && key >= nodeBuffer.keys[i]) i++;
                LoadNode(nodeBuffer.children[i]);
            }

            InsertInLeaf(key, refId);

            if (nodeBuffer.keys.Count > order) {
                SplitLeaf(path);
            } else {
                WriteNode(nodeBuffer, true);
            }
        }

        /*
        Método para inserir um nó numa folha que possui espaço,
        reescrevendo-a no arquivo .txt
        */
        private void InsertInLeaf(int key, int refId) {
            int i = 0;
            while (i < nodeBuffer.keys.Count && key > nodeBuffer.keys[i]) i++;
            nodeBuffer.keys.Insert(i, key);
            nodeBuffer.refs.Insert(i, refId);
        }

        /*
        Método para splitar uma folha, fazendo com que até a metade dela
        permaneça na linha original, e a segunda metade vá para uma nova
        linha.
        */
        private void SplitLeaf(Stack<int> path) {
            int mid = (nodeBuffer.keys.Count + 1) / 2;

            List<int> newKeys = nodeBuffer.keys.GetRange(mid, nodeBuffer.keys.Count - mid);
            List<int> newRefs = nodeBuffer.refs.GetRange(mid, nodeBuffer.refs.Count - mid);

            nodeBuffer.keys.RemoveRange(mid, nodeBuffer.keys.Count - mid);
            nodeBuffer.refs.RemoveRange(mid, nodeBuffer.refs.Count - mid);

            int newLeafId = GetNextId();

            // Salva o leaf atual antes de carregar novo conteúdo
            int oldNext = nodeBuffer.next;
            int oldId = nodeBuffer.id;
            int oldParent = nodeBuffer.parent;

            WriteNode(nodeBuffer, true);

            // Cria novo nó folha no buffer
            nodeBuffer.id = newLeafId;
            nodeBuffer.isLeaf = true;
            nodeBuffer.keys = newKeys;
            nodeBuffer.refs = newRefs;
            nodeBuffer.prev = oldId;
            nodeBuffer.next = oldNext;
            nodeBuffer.parent = oldParent;
            nodeBuffer.children = new List<int>();

            WriteNode(nodeBuffer, false);

            // Atualiza ponteiro next do antigo leaf
            LoadNode(oldId);
            nodeBuffer.next = newLeafId;
            WriteNode(nodeBuffer, true);

            // Atualiza ponteiro prev do antigo next (se existir)
            if (oldNext != -1) {
                LoadNode(oldNext);
                nodeBuffer.prev = newLeafId;
                WriteNode(nodeBuffer, true);
            }

            // Carrega o novo leaf de volta para obter chave promovida
            LoadNode(newLeafId);
            int promotedKey = nodeBuffer.keys[0];

            if (path.Count == 0) {
                // Criar nova raiz
                int newRootId = GetNextId();
                int leftChildId = oldId;
                int rightChildId = newLeafId;

                // Prepara buffer como nova raiz
                nodeBuffer.id = newRootId;
                nodeBuffer.isLeaf = false;
                nodeBuffer.keys = new List<int> { promotedKey };
                nodeBuffer.children = new List<int> { leftChildId, rightChildId };
                nodeBuffer.parent = -1;
                nodeBuffer.refs = new List<int>();
                nodeBuffer.prev = -1;
                nodeBuffer.next = -1;

                WriteNode(nodeBuffer, false);

                // Atualiza pais dos filhos
                LoadNode(leftChildId);
                nodeBuffer.parent = newRootId;
                WriteNode(nodeBuffer, true);

                LoadNode(rightChildId);
                nodeBuffer.parent = newRootId;
                WriteNode(nodeBuffer, true);
                WriteRootId(newRootId);
            } else {
                int parentId = path.Pop();
                LoadNode(parentId);

                int insertIndex = 0;
                while (insertIndex < nodeBuffer.keys.Count && promotedKey > nodeBuffer.keys[insertIndex]) insertIndex++;

                nodeBuffer.keys.Insert(insertIndex, promotedKey);
                nodeBuffer.children.Insert(insertIndex + 1, newLeafId);

                WriteNode(nodeBuffer, true);

                if (nodeBuffer.keys.Count > order) {
                    SplitInternal(path);
                }
            }
        }

        /*
        Similar ao splitleaf, para nós internos, mas também trata
        quando precisamos fazer o split interno mais de uma vez.
        */
        private void SplitInternal(Stack<int> path) {
            int mid = nodeBuffer.keys.Count / 2;
            int promotedKey = nodeBuffer.keys[mid];

            List<int> rightKeys = nodeBuffer.keys.GetRange(mid + 1, nodeBuffer.keys.Count - (mid + 1));
            List<int> rightChildren = nodeBuffer.children.GetRange(mid + 1, nodeBuffer.children.Count - (mid + 1));

            int newNodeId = GetNextId();
            int leftNodeId = nodeBuffer.id;
            int oldParent = nodeBuffer.parent;

            nodeBuffer.keys.RemoveRange(mid, nodeBuffer.keys.Count - mid);
            nodeBuffer.children.RemoveRange(mid + 1, nodeBuffer.children.Count - (mid + 1));

            WriteNode(nodeBuffer, true); // salva nó esquerdo

            // monta nó direito no buffer
            nodeBuffer.id = newNodeId;
            nodeBuffer.isLeaf = false;
            nodeBuffer.keys = rightKeys;
            nodeBuffer.children = rightChildren;
            nodeBuffer.parent = oldParent;
            nodeBuffer.refs = new List<int>();
            nodeBuffer.prev = -1;
            nodeBuffer.next = -1;

            WriteNode(nodeBuffer, false); // salva nó direito

            // atualiza os pais dos filhos do novo nó
            foreach (int childId in rightChildren) {
                LoadNode(childId);
                nodeBuffer.parent = newNodeId;
                WriteNode(nodeBuffer, true);
            }

            if (path.Count == 0) {
                int newRootId = GetNextId();

                nodeBuffer.id = newRootId;
                nodeBuffer.isLeaf = false;
                nodeBuffer.keys = new List<int> { promotedKey };
                nodeBuffer.children = new List<int> { leftNodeId, newNodeId };
                nodeBuffer.parent = -1;
                nodeBuffer.refs = new List<int>();
                nodeBuffer.prev = -1;
                nodeBuffer.next = -1;

                WriteNode(nodeBuffer, false);

                LoadNode(leftNodeId);
                nodeBuffer.parent = newRootId;
                WriteNode(nodeBuffer, true);

                LoadNode(newNodeId);
                nodeBuffer.parent = newRootId;
                WriteNode(nodeBuffer, true);
                WriteRootId(newRootId);
            } else {
                int parentId = path.Pop();
                LoadNode(parentId);

                int insertIndex = 0;
                while (insertIndex < nodeBuffer.keys.Count && promotedKey > nodeBuffer.keys[insertIndex]) insertIndex++;

                nodeBuffer.keys.Insert(insertIndex, promotedKey);
                nodeBuffer.children.Insert(insertIndex + 1, newNodeId);

                WriteNode(nodeBuffer, true);

                if (nodeBuffer.keys.Count > order)
                    SplitInternal(path);
            }
        }

        // Método para atualizar o buffer com um novo nó
        private void LoadNode(int id) {
            using (FileStream fs = new FileStream(this.filePath, FileMode.Open, FileAccess.Read)) {
                fs.Seek(id * this.pageSize, SeekOrigin.Begin);
                byte[] buffer = new byte[this.pageSize];
                fs.Read(buffer, 0, this.pageSize);
                string line = Encoding.ASCII.GetString(buffer).Trim();
                nodeBuffer = IndexNode.Deserialize(line);
                nodeBuffer.id = id;
            }
        }

        /*
        Método para escrever um nó no arquivo.txt
        Possui um booleano dizendo se é para incluir um novo nó, ou
        apenas reescrever um. Isso é importante para atualizar o nextId
        */
        public void WriteNode(IndexNode node, bool rewrite) {
            int nextId = rewrite ? node.id : this.GetNextId();
            WriteFixedLine(nextId, node.Serialize());

            if (!rewrite) {
                int rootId = GetRootId();
                WriteFixedLine(0, $"nextId:{nextId + 1};rootId:{rootId}");
            }
        }

        /*
        Getters do nextId, rootId e height
        */
        public int GetNextId() {
            int nextId = 0;
            
            using (FileStream fs = new FileStream(this.filePath, FileMode.Open, FileAccess.Read)) {
                fs.Seek(0, SeekOrigin.Begin);

                byte[] buffer = new byte[this.pageSize];
                int bytesRead = fs.Read(buffer, 0, this.pageSize);

                if (bytesRead > 0) {
                    string line = Encoding.ASCII.GetString(buffer).Trim();
                    string idString = line.Split(';')[0].Split(':')[1];
                    int.TryParse(idString, out nextId);
                }
            }

            return nextId;
        }

        public int GetRootId() {
            int rootId = 0;
            
            using (FileStream fs = new FileStream(this.filePath, FileMode.Open, FileAccess.Read)) {
                fs.Seek(0, SeekOrigin.Begin);

                byte[] buffer = new byte[this.pageSize];
                int bytesRead = fs.Read(buffer, 0, this.pageSize);

                if (bytesRead > 0) {
                    string line = Encoding.ASCII.GetString(buffer).Trim();
                    string idString = line.Split(';')[1].Split(':')[1];
                    int.TryParse(idString, out rootId);
                }
            }

            return rootId;
        }

        public int GetHeight() {
            LoadNode(GetRootId());
            int height = 0;

            while (!nodeBuffer.isLeaf) {
                LoadNode(nodeBuffer.children[0]);
                height++;
            }

            return height;
        }
    }
}
