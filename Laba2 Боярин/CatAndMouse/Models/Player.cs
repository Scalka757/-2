namespace CatAndMouse;
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
    public bool HasPosition => _state != State.NotInGame;

    public int Position => HasPosition
        ? _position
        : throw new InvalidOperationException($"Игрок '{_name}' ещё не в игре.");
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
