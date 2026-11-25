namespace CatAndMouse;
internal static class TestRunner
{
    private const int TestCount = 3;

    public static bool RunAll()
    {
        string originalInput = Game.InputFile;
        string originalOutput = Game.OutFile;
        int passed = 0;

        try
        {
            for (int testNumber = 1; testNumber <= TestCount; testNumber++)
            {
                if (RunSingle(testNumber))
                {
                    passed++;
                }
            }
        }
        finally
        {
            Game.InputFile = originalInput;
            Game.OutFile = originalOutput;
        }

        Console.WriteLine();
        Console.WriteLine($"Итого: пройдено {passed} из {TestCount}.");
        return passed == TestCount;
    }

    private static bool RunSingle(int testNumber)
    {
        Game.InputFile = $"{testNumber}.ChaseData.txt";
        Game.OutFile = $"{testNumber}.Result.txt";
        string expectedFile = $"{testNumber}.PursuitLog.txt";

        Console.WriteLine($"===== Тест {testNumber}: {Game.InputFile} -> {Game.OutFile} =====");
        new Game().Run();

        string expected = Normalize(File.ReadAllText(expectedFile));
        string actual = Normalize(File.ReadAllText(Game.OutFile));

        if (string.Equals(expected, actual, StringComparison.Ordinal))
        {
            Console.WriteLine($"[PASS] Тест {testNumber}: вывод совпадает с {expectedFile}.");
            return true;
        }

        ReportFirstDifference(testNumber, expected, actual);
        return false;
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static void ReportFirstDifference(int testNumber, string expected, string actual)
    {
        string[] expectedLines = expected.Split('\n');
        string[] actualLines = actual.Split('\n');
        int maxLines = Math.Max(expectedLines.Length, actualLines.Length);

        for (int index = 0; index < maxLines; index++)
        {
            string expectedLine = index < expectedLines.Length ? expectedLines[index] : "<нет строки>";
            string actualLine = index < actualLines.Length ? actualLines[index] : "<нет строки>";

            if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                Console.WriteLine($"[FAIL] Тест {testNumber}: расхождение в строке {index + 1}");
                Console.WriteLine($"  ожидалось: \"{expectedLine}\"");
                Console.WriteLine($"  получено:  \"{actualLine}\"");
                return;
            }
        }
    }
}
