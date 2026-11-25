namespace CatAndMouse
{
    class Program
    {
        static void Main(string[] args)
        {
            Game.InputFile = "1.ChaseData.txt";
            Game.OutFile = "1.PursuitLog.txt";

            Game game = new Game(16);
            game.Run();
        }
    }
}
