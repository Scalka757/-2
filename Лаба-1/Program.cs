using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GeneticSearch
{
    struct GeneticData
    {
        public string protein;
        public string organism;
        public string amino_acids;
    }

    internal class Program
    {
        private const string StudentName = "Ramanchuk Viachaslau";
        private const string SequencesFile = "sequences.txt";
        private const string CommandsFile = "commands.txt";
        private const string OutputFile = "genedata.txt";
        private static readonly string Separator = new string('-', 74);

        private static string DecodeRle(string sequence)
        {
            StringBuilder result = new StringBuilder();
            int i = 0;

            while (i < sequence.Length)
            {
                if (char.IsDigit(sequence[i]))
                {
                    int count = sequence[i] - '0';

                    if (i + 1 >= sequence.Length)
                    {
                        result.Append(sequence[i]);
                        i++;
                        continue;
                    }

                    result.Append(sequence[i + 1], count);
                    i += 2;
                }
                else
                {
                    result.Append(sequence[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        private static string EncodeRle(string sequence)
        {
            if (string.IsNullOrEmpty(sequence))
                return "";

            StringBuilder result = new StringBuilder();
            int i = 0;

            while (i < sequence.Length)
            {
                int j = i + 1;
                while (j < sequence.Length && sequence[j] == sequence[i])
                    j++;

                int count = j - i;
                if (count > 2)
                {
                    result.Append(count);
                    result.Append(sequence[i]);
                }
                else
                {
                    result.Append(sequence[i], count);
                }

                i = j;
            }

            return result.ToString();
        }

        private static List<GeneticData> ReadProteins(string filename)
        {
            List<GeneticData> proteins = new List<GeneticData>();
            int lineNumber = 0;

            foreach (string originalLine in File.ReadLines(filename, Encoding.UTF8))
            {
                lineNumber++;
                string line = originalLine.TrimEnd('\r', '\n');

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split('\t');
                if (parts.Length < 3)
                {
                    throw new FormatException(
                        $"Строка {lineNumber} в {filename}: ожидались 3 поля, разделённые табуляцией.");
                }

                GeneticData data;
                data.protein = parts[0].Trim();
                data.organism = parts[1].Trim();
                data.amino_acids = DecodeRle(string.Concat(parts.Skip(2)).Trim());

                proteins.Add(data);
            }

            return proteins;
        }

        private static List<GeneticData> FindProteins(List<GeneticData> proteins, string name)
        {
            return proteins.Where(p => p.protein == name).ToList();
        }

        private static List<string> CommandSearch(List<GeneticData> proteins, string query)
        {
            query = DecodeRle(query.Trim());
            List<GeneticData> matches = proteins
                .Where(p => p.amino_acids.Contains(query))
                .ToList();

            List<string> lines = new List<string>
            {
                $"search   {query}",
                "organism\t\t\tprotein"
            };

            if (matches.Count == 0)
            {
                lines.Add("NOT FOUND");
                return lines;
            }

            foreach (GeneticData data in matches)
                lines.Add($"{data.organism}\t\t{data.protein}");

            return lines;
        }

        private static List<string> CommandDiff(List<GeneticData> proteins, string name1, string name2)
        {
            List<GeneticData> first = FindProteins(proteins, name1);
            List<GeneticData> second = FindProteins(proteins, name2);

            List<string> lines = new List<string>
            {
                $"diff   {name1}   {name2}",
                "amino-acids difference:"
            };

            List<string> missing = new List<string>();
            if (first.Count == 0) missing.Add(name1);
            if (second.Count == 0) missing.Add(name2);

            if (missing.Count > 0)
            {
                lines.Add("MISSING: " + string.Join(", ", missing));
                return lines;
            }

            string seq1 = first[0].amino_acids;
            string seq2 = second[0].amino_acids;

            int differences = 0;
            int commonLength = Math.Min(seq1.Length, seq2.Length);

            for (int i = 0; i < commonLength; i++)
            {
                if (seq1[i] != seq2[i])
                    differences++;
            }

            differences += Math.Abs(seq1.Length - seq2.Length);
            lines.Add(differences.ToString());
            return lines;
        }

        private static List<string> CommandMode(List<GeneticData> proteins, string name)
        {
            List<GeneticData> matches = FindProteins(proteins, name);
            List<string> lines = new List<string>
            {
                $"mode   {name}",
                "amino-acid occurs:"
            };

            if (matches.Count == 0 || string.IsNullOrEmpty(matches[0].amino_acids))
            {
                lines.Add("MISSING: " + name);
                return lines;
            }

            Dictionary<char, int> counts = new Dictionary<char, int>();
            foreach (char aminoAcid in matches[0].amino_acids)
            {
                if (counts.ContainsKey(aminoAcid))
                    counts[aminoAcid]++;
                else
                    counts[aminoAcid] = 1;
            }

            int highestCount = counts.Values.Max();
            char mostFrequent = counts
                .Where(pair => pair.Value == highestCount)
                .Select(pair => pair.Key)
                .Min();

            lines.Add($"{mostFrequent}          {highestCount}");
            return lines;
        }

        private static void ProcessCommands(List<GeneticData> proteins, string commandsFilename, string outputFilename)
        {
            using (StreamReader commands = new StreamReader(commandsFilename, Encoding.UTF8))
            using (StreamWriter output = new StreamWriter(outputFilename, false, new UTF8Encoding(false)))
            {
                output.WriteLine(StudentName);
                output.WriteLine("Genetic Searching");

                int commandNumber = 0;
                int lineNumber = 0;
                string line;

                while ((line = commands.ReadLine()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split('\t');
                    string operation = parts[0].Trim().ToLowerInvariant();
                    string[] parameters = parts.Skip(1).ToArray();

                    commandNumber++;
                    List<string> result;

                    if (operation == "search" && parameters.Length >= 1)
                    {
                        result = CommandSearch(proteins, parameters[0]);
                    }
                    else if (operation == "diff" && parameters.Length >= 2)
                    {
                        result = CommandDiff(proteins, parameters[0].Trim(), parameters[1].Trim());
                    }
                    else if (operation == "mode" && parameters.Length >= 1)
                    {
                        result = CommandMode(proteins, parameters[0].Trim());
                    }
                    else
                    {
                        result = new List<string>
                        {
                            line,
                            $"ERROR: invalid command or parameters on line {lineNumber}"
                        };
                    }

                    output.WriteLine(Separator);
                    output.WriteLine($"{commandNumber:000}   {result[0]}");

                    for (int i = 1; i < result.Count; i++)
                        output.WriteLine(result[i]);
                }

                output.WriteLine(Separator);
            }
        }

        private static void Main(string[] args)
        {
            string baseDirectory = AppContext.BaseDirectory;
            string sequencesPath = Path.Combine(baseDirectory, SequencesFile);
            string commandsPath = Path.Combine(baseDirectory, CommandsFile);
            string outputPath = Path.Combine(baseDirectory, OutputFile);

            if (!File.Exists(sequencesPath))
                throw new FileNotFoundException("Не найден файл: " + sequencesPath);
            if (!File.Exists(commandsPath))
                throw new FileNotFoundException("Не найден файл: " + commandsPath);

            List<GeneticData> proteins = ReadProteins(sequencesPath);
            ProcessCommands(proteins, commandsPath, outputPath);

            Console.WriteLine($"Готово. Результат записан в {OutputFile}");
        }
    }
}
