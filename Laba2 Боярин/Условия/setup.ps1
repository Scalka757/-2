$ErrorActionPreference = 'Stop'

$ProjectName = 'CatAndMouse'
$ProjectRoot = Join-Path (Get-Location) $ProjectName

New-Item -ItemType Directory -Force -Path $ProjectRoot | Out-Null
Set-Location $ProjectRoot

New-Item -ItemType Directory -Force -Path 'Models'  | Out-Null
New-Item -ItemType Directory -Force -Path 'Core'    | Out-Null
New-Item -ItemType Directory -Force -Path 'Testing' | Out-Null

# ---------------------------------------------------------------- CatAndMouse.csproj
Set-Content -Encoding UTF8 -Path 'CatAndMouse.csproj' -Value @'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>CatAndMouse</RootNamespace>
    <AssemblyName>CatAndMouse</AssemblyName>
  </PropertyGroup>

  <!-- Входные и эталонные файлы копируются рядом с exe, чтобы запуск из IDE тоже работал. -->
  <ItemGroup>
    <None Update="ChaseData.txt;*.ChaseData.txt;*.PursuitLog.txt">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
'@

# ---------------------------------------------------------------- Models/State.cs
Set-Content -Encoding UTF8 -Path 'Models/State.cs' -Value @'
namespace CatAndMouse;

/// <summary>Состояние игрока.</summary>
public enum State
{
    Winner,
    Looser,
    Playing,
    NotInGame
}
'@

# ---------------------------------------------------------------- Models/GameState.cs
Set-Content -Encoding UTF8 -Path 'Models/GameState.cs' -Value @'
namespace CatAndMouse;

/// <summary>Состояние игры: идёт (Start) или завершена (End).</summary>
public enum GameState
{
    Start,
    End
}
'@

# ---------------------------------------------------------------- Models/Player.cs
Set-Content -Encoding UTF8 -Path 'Models/Player.cs' -Value @'
namespace CatAndMouse;

/// <summary>
/// Игрок (кот или мышь) на кольцевом поле из клеток 1..N.
/// </summary>
public sealed class Player
{
    private readonly string _name;
    private int _position;
    private State _state;
    private long _distanceTraveled;

    public Player(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _name = name;
        _position = 0;
        _state = State.NotInGame;
        _distanceTraveled = 0;
    }

    public string Name => _name;

    public State State => _state;

    public long DistanceTraveled => _distanceTraveled;

    /// <summary>Позиция задана (игрок хотя бы раз выставлен на поле).</summary>
    public bool HasPosition => _state != State.NotInGame;

    public int Position => HasPosition
        ? _position
        : throw new InvalidOperationException($"Игрок '{_name}' ещё не в игре.");

    /// <summary>
    /// Первый вызов (игрок NotInGame) задаёт начальную позицию и не считается ходом.
    /// Последующие вызовы сдвигают игрока по кольцу на <paramref name="value"/> клеток.
    /// </summary>
    public void Move(int value, int fieldSize)
    {
        if (fieldSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldSize), fieldSize, "Размер поля должен быть положительным.");
        }

        if (_state == State.NotInGame)
        {
            PlaceAt(value, fieldSize);
            return;
        }

        if (_state != State.Playing)
        {
            throw new InvalidOperationException($"Игрок '{_name}' уже завершил игру ({_state}).");
        }

        _position = WrapPosition(_position, value, fieldSize);
        _distanceTraveled += Math.Abs((long)value);
    }

    public void Win() => _state = State.Winner;

    public void Lose() => _state = State.Looser;

    private void PlaceAt(int cell, int fieldSize)
    {
        if (cell < 1 || cell > fieldSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cell), cell, $"Начальная позиция игрока '{_name}' должна быть в диапазоне 1..{fieldSize}.");
        }

        _position = cell;
        _state = State.Playing;
    }

    /// <summary>
    /// Переводим позицию в 0-based, берём остаток по модулю N (с коррекцией отрицательного
    /// остатка, т.к. в C# % сохраняет знак делимого) и возвращаем обратно в 1-based.
    /// long исключает переполнение при больших значениях хода.
    /// </summary>
    private static int WrapPosition(int current, int steps, int fieldSize)
    {
        long zeroBased = ((long)current - 1 + steps) % fieldSize;
        if (zeroBased < 0)
        {
            zeroBased += fieldSize;
        }

        return (int)zeroBased + 1;
    }
}
'@

# ---------------------------------------------------------------- Core/PursuitLog.cs
Set-Content -Encoding UTF8 -Path 'Core/PursuitLog.cs' -Value @'
using System.Globalization;
using System.Text;

namespace CatAndMouse;

/// <summary>
/// Формирует текст отчёта PursuitLog строго в формате эталонных файлов.
/// Строки разделяются '\n' (как в эталонах).
/// </summary>
internal sealed class PursuitLog
{
    private const string Title = "Cat and Mouse";
    private const string TableHeader = "Cat Mouse  Distance";
    private const string Separator = "-------------------";
    private const string TraveledHeader = "Distance traveled:   Mouse    Cat";
    private const string EvadedMessage = "Mouse evaded Cat";
    private const string NotInGameMark = "??";

    private const int CatColumnWidth = 3;
    private const int MouseColumnWidth = 6;
    private const int DistanceColumnWidth = 10;
    private const int MouseTraveledWidth = 26;
    private const int CatTraveledWidth = 7;
    private const int CaughtCellWidth = 2;

    private readonly StringBuilder _buffer = new();

    public void WriteHeader()
    {
        AppendLine(Title);
        AppendLine(string.Empty);
        AppendLine(TableHeader);
        AppendLine(Separator);
    }

    /// <summary>Строка таблицы; расстояние выводится только если оба игрока в игре.</summary>
    public void WriteRow(int? catPosition, int? mousePosition, int? distance)
    {
        string row = string.Create(
            CultureInfo.InvariantCulture,
            $"{FormatCell(catPosition),CatColumnWidth}{FormatCell(mousePosition),MouseColumnWidth}");

        if (distance.HasValue)
        {
            row += string.Create(CultureInfo.InvariantCulture, $"{distance.Value,DistanceColumnWidth}");
        }

        AppendLine(row);
    }

    public void WriteFooter(long mouseTraveled, long catTraveled, int? caughtAtCell)
    {
        AppendLine(Separator);
        AppendLine(string.Empty);
        AppendLine(string.Empty);
        AppendLine(TraveledHeader);
        AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{mouseTraveled,MouseTraveledWidth}{catTraveled,CatTraveledWidth}"));
        AppendLine(string.Empty);
        AppendLine(caughtAtCell.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"Mouse caught at: {caughtAtCell.Value,CaughtCellWidth}")
            : EvadedMessage);
    }

    public override string ToString() => _buffer.ToString();

    private static string FormatCell(int? position) =>
        position.HasValue
            ? position.Value.ToString(CultureInfo.InvariantCulture)
            : NotInGameMark;

    private void AppendLine(string line) => _buffer.Append(line).Append('\n');
}
'@

# ---------------------------------------------------------------- Core/Game.cs
Set-Content -Encoding UTF8 -Path 'Core/Game.cs' -Value @'
using System.Globalization;
using System.Text;

namespace CatAndMouse;

/// <summary>
/// Игра "кошки-мышки": читает команды из InputFile, ведёт игроков и пишет отчёт в OutFile и на экран.
/// </summary>
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

        // Фиксируем пути на момент запуска: статические поля могут быть изменены извне.
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

    /// <summary>Расстояние между игроками (линейное, как в эталонных логах) или null, если кто-то не в игре.</summary>
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
            // Кот не сумел поймать мышь.
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
'@

# ---------------------------------------------------------------- Testing/TestRunner.cs
Set-Content -Encoding UTF8 -Path 'Testing/TestRunner.cs' -Value @'
namespace CatAndMouse;

/// <summary>
/// Прогоняет N.ChaseData.txt и посимвольно сравнивает результат с N.PursuitLog.txt.
/// Перед сравнением в эталоне CRLF приводится к LF (эталон мог быть сохранён в Windows).
/// </summary>
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
'@

# ---------------------------------------------------------------- Program.cs
Set-Content -Encoding UTF8 -Path 'Program.cs' -Value @'
namespace CatAndMouse;

/// <summary>
/// Клиентский класс.
///   CatAndMouse                      -> ChaseData.txt -> PursuitLog.txt
///   CatAndMouse <input> [<output>]   -> указанные файлы
///   CatAndMouse --test               -> прогон тестов 1..3 со сверкой с эталонами
/// </summary>
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
'@

# ---------------------------------------------------------------- Тестовые данные
Set-Content -Encoding UTF8 -Path '1.ChaseData.txt' -Value @'
   16
M         7
M        -5
P
C         6
M        -7
P
C         6
P
M         4
M         6
C         0
P
M         0
M         6
P
C        -1
P
C         1
C         4
P
M        -4
P
'@

Set-Content -Encoding UTF8 -Path '1.PursuitLog.txt' -Value @'
Cat and Mouse

Cat Mouse  Distance
-------------------
 ??     2
  6    11         5
 12    11         1
 12     5         7
 12    11         1
-------------------


Distance traveled:   Mouse    Cat
                        28      7

Mouse caught at: 11
'@

Set-Content -Encoding UTF8 -Path '2.ChaseData.txt' -Value @'
   20
M        19
M        -4
P
M        -1
P
M         3
P
M         0
P
C         3
M        -6
P
M         2
P
C         1
P
C         6
M         0
P
M         9
P
M        -1
P
M        -3
P
'@

Set-Content -Encoding UTF8 -Path '2.PursuitLog.txt' -Value @'
Cat and Mouse

Cat Mouse  Distance
-------------------
 ??    15
 ??    14
 ??    17
 ??    17
  3    11         8
  3    13        10
  4    13         9
 10    13         3
 10     2         8
 10     1         9
 10    18         8
-------------------


Distance traveled:   Mouse    Cat
                        29      7

Mouse evaded Cat
'@

Set-Content -Encoding UTF8 -Path '3.ChaseData.txt' -Value @'
   27
M         6
M         8
P
M         1
C        22
C         7
P
C       -10
M         1
P
C        11
P
C         5
P
C        -4
P
M         3
P
C        -8
P
M       -11
P
C       -15
P
'@

Set-Content -Encoding UTF8 -Path '3.PursuitLog.txt' -Value @'
Cat and Mouse

Cat Mouse  Distance
-------------------
 ??    14
  2    15        13
 19    16         3
  3    16        13
  8    16         8
  4    16        12
  4    19        15
 23    19         4
 23     8        15
-------------------


Distance traveled:   Mouse    Cat
                        24     60

Mouse caught at:  8
'@

# Входной файл по умолчанию (имя из условия задачи) — копия первого теста.
Copy-Item -Force -Path '1.ChaseData.txt' -Destination 'ChaseData.txt'

Write-Host ''
Write-Host "Проект '$ProjectName' успешно создан в: $ProjectRoot" -ForegroundColor Green
Write-Host ''
Write-Host 'Запуск всех 3 тестов со сверкой с эталонами:' -ForegroundColor Cyan
Write-Host "    cd `"$ProjectRoot`"; dotnet run -- --test"
Write-Host ''
Write-Host 'Обычный запуск (ChaseData.txt -> PursuitLog.txt):' -ForegroundColor Cyan
Write-Host "    cd `"$ProjectRoot`"; dotnet run"
Write-Host ''
Write-Host 'Запуск на своих файлах:' -ForegroundColor Cyan
Write-Host "    dotnet run -- 2.ChaseData.txt 2.MyLog.txt"
