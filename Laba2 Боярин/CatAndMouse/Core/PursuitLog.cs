using System.Globalization;
using System.Text;

namespace CatAndMouse;
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
