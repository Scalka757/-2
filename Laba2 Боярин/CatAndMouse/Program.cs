namespace CatAndMouse;
internal static class Program
{
    private const string TestFlag = "--test";

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1 && string.Equals(args[0], TestFlag, StringComparison.OrdinalIgnoreCase))
            {
                return TestRunner.RunAll() ? 0 : 1;
            }

            if (args.Length > 2)
            {
                Console.Error.WriteLine("Использование: CatAndMouse [<входной файл> [<выходной файл>]] | --test");
                return 2;
            }

            if (args.Length >= 1)
            {
                Game.InputFile = args[0];
            }

            if (args.Length == 2)
            {
                Game.OutFile = args[1];
            }

            var game = new Game();
            game.Run();
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Console.Error.WriteLine($"Ошибка: {ex.Message}");
            return 1;
        }
    }
}
