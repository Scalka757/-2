namespace CatAndMouse
{
    enum GameState
    {
        Start,
        End
    }

    class Game
    {
        public static string InputFile = "ChaseData.txt";
        public static string OutFile = "PursuitLog.txt";

        public int size;
        public Player cat;
        public Player mouse;
        public GameState state;

        private StreamWriter writer;
        private int caughtLocation = -1;

        public Game(int size)
        {
            this.size = size;
            cat = new Player("Cat", size);
            mouse = new Player("Mouse", size);
            state = GameState.Start;
        }

        public void Run()
        {
            string[] lines = File.ReadAllLines(InputFile);

            if (lines.Length > 0 && int.TryParse(lines[0].Trim(), out int fileSize))
            {
                this.size = fileSize;
                cat = new Player("Cat", size);
                mouse = new Player("Mouse", size);
            }

            using (writer = new StreamWriter(OutFile))
            {
                WriteHeader();

                int lineIndex = 1;
                while (state != GameState.End)
                {
                    if (lineIndex >= lines.Length)
                    {
                        state = GameState.End;
                        break;
                    }

                    string line = lines[lineIndex].Trim();
                    lineIndex++;

                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    char command = parts[0][0];

                    if (command == 'P')
                    {
                        DoPrintCommand();
                    }
                    else if (command == 'M' || command == 'C')
                    {
                        int steps = int.Parse(parts[1]);
                        DoMoveCommand(command, steps);

                        if (cat.state == State.Playing && mouse.state == State.Playing && cat.location == mouse.location)
                        {
                            caughtLocation = mouse.location;
                            cat.state = State.Winner;
                            mouse.state = State.Looser;
                            state = GameState.End;
                        }
                    }
                }

                if (caughtLocation == -1)
                {
                    mouse.state = State.Winner;
                    cat.state = State.Looser;
                }

                WriteFooter();
            }
        }

        private void DoMoveCommand(char command, int steps)
        {
            switch (command)
            {
                case 'M':
                    mouse.Move(steps);
                    break;
                case 'C':
                    cat.Move(steps);
                    break;
            }
        }

        private void DoPrintCommand()
        {
            string catPos = cat.state == State.NotInGame ? "??" : cat.location.ToString();
            string mousePos = mouse.state == State.NotInGame ? "??" : mouse.location.ToString();

            if (cat.state != State.NotInGame && mouse.state != State.NotInGame)
            {
                int distance = GetDistance();
                string row = $"{catPos,3}{mousePos,6}{distance,10}";
                writer.WriteLine(row);
                Console.WriteLine(row);
            }
            else
            {
                string row = $"{catPos,3}{mousePos,6}";
                writer.WriteLine(row);
                Console.WriteLine(row);
            }
        }

        private int GetDistance()
        {
            return Math.Abs(cat.location - mouse.location);
        }

        private void WriteHeader()
        {
            PrintLine("Cat and Mouse");
            PrintLine("");
            PrintLine("Cat Mouse  Distance");
            PrintLine("-------------------");
        }

        private void WriteFooter()
        {
            PrintLine("-------------------");
            PrintLine("");
            PrintLine("");
            PrintLine("Distance traveled:   Mouse    Cat");
            PrintLine($"{mouse.distanceTraveled,26}{cat.distanceTraveled,7}");
            PrintLine("");

            if (caughtLocation != -1)
            {
                PrintLine($"Mouse caught at: {caughtLocation,2}");
            }
            else
            {
                PrintLine("Mouse evaded Cat");
            }
        }

        private void PrintLine(string text)
        {
            writer.WriteLine(text);
            Console.WriteLine(text);
        }
    }
}
