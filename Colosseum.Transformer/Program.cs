using Colosseum.Experiment;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Colosseum.Transformer
{
    class Program
    {
        static void Main(string[] args)
        {
            HashSet<String> keys = new HashSet<string>();

            Console.WriteLine("Path?");
            string path = Console.ReadLine();

            var files = Directory.GetFiles(path, "*.json");

            Console.WriteLine();
            Console.WriteLine();

            foreach (var file in files)
            {
                Console.WriteLine($"Processing {Path.GetFileName(file)}...");

                string json = File.ReadAllText(file);
                var data = JsonConvert.DeserializeObject<List<TowerStateResult>>(json);

                StringBuilder code = new StringBuilder();

                foreach (var item in data)
                {
                    var key = item.TowerState.ToString();

                    //TODO: Do something for when more than one gene?
                    if (item.Genes[0].NormalizedGene == null)
                    {
                        Console.WriteLine($"Invalid item {item.Genes[0].Gene.Id}.");
                        continue;
                    }
                    var value = ReallyNormalizeGene(item.Genes[0].NormalizedGene);

                    if (keys.Contains(key))
                    {
                        Console.WriteLine("Holy shit! Duplicated key!!!");
                        return;
                    }

                    var codeLine = $"data.put(\"{key}\", new byte[][] {{ new byte[] {{{string.Join(',', value.Item1)}}}, new byte[] {{{string.Join(',', value.Item2)}}} }});\r\n";

                    code.Append(codeLine);
                }


                var outputFile = Path.Combine(path, "java", Path.GetFileNameWithoutExtension(file) + ".java");

                var outputDirectory = Path.GetDirectoryName(outputFile);
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                File.WriteAllText(outputFile, code.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("Finished.");
            Console.ReadKey();
        }

        private static (int[], int[]) ReallyNormalizeGene(int[] normalizedGene)
        {
            var a1 = normalizedGene.Take(normalizedGene.Length / 2).ToList();
            var a2 = normalizedGene.Skip(normalizedGene.Length / 2).ToList();

            while (a1.Count > 0 && a1[0] == 0 && a2[0] == 0)
            {
                a1.RemoveAt(0);
                a2.RemoveAt(0);
            }

            for (int i = a1.Count - 1; i >= 0; i--)
            {
                if (a1[i] == 0 && a2[i] == 0)
                {
                    a1.RemoveAt(i);
                    a2.RemoveAt(i);
                }
            }

            return (a1.ToArray(), a2.ToArray());
        }
    }
}
