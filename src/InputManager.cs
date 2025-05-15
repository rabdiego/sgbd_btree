using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BTreeIndex {
    public class InputManager {
        private string inputPath { get; set; }
        private string outPath { get; set; }
        private string indexPath { get; set; }
        private string csvPath { get; set; }
        private IndexTree index { get; set; }

        public InputManager(string inputPath, string outPath, string indexPath, string csvPath) {
            this.inputPath = inputPath;
            this.outPath = outPath;
            this.indexPath = indexPath;
            this.csvPath = csvPath;

            if (!File.Exists(outPath)) {
                File.Create(outPath).Close();
            }
        }

        public void ProccessInput() {
            bool firstLine = true;

            using (StreamWriter writer = new StreamWriter(this.outPath)) {
            using (StreamReader reader = new StreamReader(this.inputPath)) {
                string line;
                while (((line = reader.ReadLine()) != null)) {
                    if (firstLine) {
                        writer.WriteLine(line);
                        int nChildren = int.Parse(line.Split('/')[1]);
                        this.index = new IndexTree(nChildren-1, this.indexPath);
                        firstLine = false;
                    } else {
                        var operation = line.Split(':');
                        int value = int.Parse(operation[1]);

                        if (operation[0].Equals("INC")) {
                            int totalTuples = 0;
                            using (StreamReader csvReader = new StreamReader(this.csvPath)) {
                                string csvLine = csvReader.ReadLine();
                                while ((csvLine = csvReader.ReadLine()) != null) {
                                    var csvValues = csvLine.Split(',');
                                    int ano_colheita = int.Parse(csvValues[2]);
                                    if (ano_colheita == value) {
                                        int vinhoId = int.Parse(csvValues[0]) + 1;
                                        this.index.Insert(ano_colheita, vinhoId);
                                        totalTuples++;
                                    }
                                }
                            }
                            writer.WriteLine($"INC:{value}/{totalTuples}");
                        } else if (operation[0].Equals("BUS=")) {
                            List<int> searchResults = this.index.Search(value);
                            writer.WriteLine($"BUS=:{value}/{searchResults.Count}");
                        }
                    }
                }
                
                writer.WriteLine($"H/{this.index.GetHeight()}");
            }
            }
        }
    }
}