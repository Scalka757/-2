namespace CatAndMouse
{
    enum State
    {
        Winner,
        Looser,
        Playing,
        NotInGame
    }

    class Player
    {
        public string name;
        public int location;
        public State state = State.NotInGame;
        public int distanceTraveled = 0;
        private int fieldSize;

        public Player(string name, int fieldSize)
        {
            this.name = name;
            this.location = -1;
            this.fieldSize = fieldSize;
        }

        public void Move(int steps)
        {
            if (state == State.NotInGame)
            {
                location = steps;
                state = State.Playing;
                return;
            }

            int newLocation = (location - 1 + steps) % fieldSize;
            if (newLocation < 0)
            {
                newLocation += fieldSize;
            }
            location = newLocation + 1;

            distanceTraveled += Math.Abs(steps);
        }
    }
}
