namespace BTreeIndex {
    public class IndexRegistry {
        public int ano_colheita { get; set; }  // Campo da tabela a ser indexada
        public int linha { get; set; } // Variável que aponta para aonde o registro está presente no arquivo CSV

        public IndexRegistry(int ano_colheita, int linha) {
            this.ano_colheita = ano_colheita;
            this.linha = linha;
        }
    }
}