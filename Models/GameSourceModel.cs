namespace ReenLauncher.Models
{
    class GameSourceModel
    {
        public Game game { get; set; }
        public Jre jre { get; set; }
        public Mod[] mods { get; set; }
    }

    class Game
    {
        public string url { get; set; }
    }

    class Jre
    {
        public string url { get; set; }
    }

    class Mod
    {
        public string name { get; set; }
        public string sha { get; set; }
        public string url { get; set; }
    }
}
