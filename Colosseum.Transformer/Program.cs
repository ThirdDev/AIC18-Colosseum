﻿using Colosseum.Experiment;
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
            var keys = new HashSet<string>();

            Console.WriteLine("Path?");
            var path = Console.ReadLine();

            var files = Directory.GetFiles(path, "*.json");

            Console.WriteLine();
            Console.WriteLine();

            foreach (var file in files)
            {
                Console.WriteLine($"Processing {Path.GetFileName(file)}...");

                var json = File.ReadAllText(file);
                var data = JsonConvert.DeserializeObject<List<TowerStateResult>>(json);

                var code = new StringBuilder();

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

                    var codeLine = $"{key}\n{string.Join(',', value.Item1)}\n{string.Join(',', value.Item2)}\n";

                    code.Append(codeLine);
                }


                var outputFile = Path.Combine(path, "datafiles", Path.GetFileNameWithoutExtension(file) + ".sgdf");

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

            for (var i = a1.Count - 1; i >= 0; i--)
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
