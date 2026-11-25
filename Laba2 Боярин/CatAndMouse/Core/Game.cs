using System.Globalization;
using System.Text;

namespace CatAndMouse;
public sealed class Game
{
    public const int MaxFieldSize = 10_000;

    public static string InputFile = "ChaseData.txt";
    public static string OutFile = "PursuitLog.txt";

    private const string MouseCommand = "M";
    private const string CatCommand = "C";
    private const string PrintCommand = "P";

    private static readonly Encoding OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly Player _cat;
    private readonly Player _mouse;
    private readonly PursuitLog _log;
    private GameState _gameState;
    private int _fieldSize;
    private int? _caughtAtCell;
    private bool _hasRun;

    public Game()
    {
        _cat = new Player("Cat");
        _mouse = new Player("Mouse");
        _log = new PursuitLog();
        _gameState = GameState.Start;
        _fieldSize = 0;
        _caughtAtCell = null;
        _hasRun = false;
    }

    public GameState State => _gameState;

    public void Run()
    {
        if (_hasRun)
        {
            throw new InvalidOperationException("Экземпляр Game можно запустить только один раз.");
        }

        _hasRun = true;
        string inputFile = InputFile;
        string outFile = OutFile;

        string[] lines = File.ReadAllLines(inputFile);
        int firstCommandIndex = ReadFieldSize(lines);

        _log.WriteHeader();

        for (int index = firstCommandIndex; index < lines.Length && _gameState == GameState.Start; index++)
        {
            ExecuteCommand(lines[index], lineNumber: index + 1);
        }

        Finish(outFile);
    }

    private int ReadFieldSize(string[] lines)
    {
        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int size)
                || size < 1
                || size > MaxFieldSize)
            {
                throw new InvalidDataException(
                    $"Строка {index + 1}: размер поля должен быть целым числом от 1 до {MaxFieldSize}, получено '{trimmed}'.");
            }

            _fieldSize = size;
            return index + 1;
        }

        throw new InvalidDataException("Входной файл пуст: не указан размер поля.");
    }

    private void ExecuteCommand(string line, int lineNumber)
    {
        string[] tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return;
        }

        string command = tokens[0].ToUpperInvariant();
        try
        {
            switch (command)
            {
                case MouseCommand:
                    DoMoveCommand(_mouse, ParseMoveValue(tokens, lineNumber));
                    break;
                case CatCommand:
                    DoMoveCommand(_cat, ParseMoveValue(tokens, lineNumber));
                    break;
                case PrintCommand:
                    EnsureTokenCount(tokens, expected: 1, lineNumber);
                    DoPrintCommand();
                    break;
                default:
                    throw new InvalidDataException($"Строка {lineNumber}: неизвестная команда '{tokens[0]}'.");
            }
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidDataException($"Строка {lineNumber}: {ex.Message}", ex);
        }
    }

    private void DoMoveCommand(Player player, int value)
    {
        player.Move(value, _fieldSize);

        if (IsMouseCaught())
        {
            _caughtAtCell = _mouse.Position;
            _cat.Win();
            _mouse.Lose();
            _gameState = GameState.End;
        }
    }

    private void DoPrintCommand()
    {
        _log.WriteRow(GetPositionOrNull(_cat), GetPositionOrNull(_mouse), GetDistance());
    }

    private bool IsMouseCaught() =>
        _cat.State == State.Playing
        && _mouse.State == State.Playing
        && _cat.Position == _mouse.Position;
    private int? GetDistance() =>
        _cat.HasPosition && _mouse.HasPosition
            ? Math.Abs(_cat.Position - _mouse.Position)
            : null;

    private static int? GetPositionOrNull(Player player) =>
        player.HasPosition ? player.Position : null;

    private void Finish(string outFile)
    {
        if (_gameState == GameState.Start)
        {
            _mouse.Win();
            _cat.Lose();
            _gameState = GameState.End;
        }

        _log.WriteFooter(_mouse.DistanceTraveled, _cat.DistanceTraveled, _caughtAtCell);

        string report = _log.ToString();
        File.WriteAllText(outFile, report, OutputEncoding);
        Console.Write(report);
    }

    private static int ParseMoveValue(string[] tokens, int lineNumber)
    {
        EnsureTokenCount(tokens, expected: 2, lineNumber);

        if (!int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidDataException($"Строка {lineNumber}: '{tokens[1]}' не является целым числом.");
        }

        return value;
    }

    private static void EnsureTokenCount(string[] tokens, int expected, int lineNumber)
    {
        if (tokens.Length != expected)
        {
            throw new InvalidDataException(
                $"Строка {lineNumber}: команда '{tokens[0]}' ожидает {expected - 1} аргумент(ов), получено {tokens.Length - 1}.");
        }
    }
}
