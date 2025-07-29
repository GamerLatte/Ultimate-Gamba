public class GameSession {
    public IGame Game { get; }
    public List<string> Players { get; }

    public GameSession(IGame game, List<string> players) {
        Game = game;
        Players = players;
    }

    public void AddPlayer(string player) => Players.Add(player);
}
