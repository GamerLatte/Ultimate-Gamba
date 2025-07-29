public class GameManager {
    private Dictionary<string, GameSession> sessions = new();

    public void HandleCommand(string args) {
        var split = args.Split(' ');
        var action = split[0].ToLower();

        switch (action) {
            case "start":
                StartGame(split[1], split.Skip(2).ToArray());
                break;
            case "join":
                JoinGame(split[1], split[2]);
                break;
        }
    }

    private void StartGame(string gameType, string[] players) {
        IGame game = gameType switch {
            "roulette" => new RouletteGame(),
            "blackjack" => new BlackjackGame(),
            "holdem" => new TexasHoldEmGame(),
            _ => null
        };

        if (game == null) return;

        var session = new GameSession(game, players.ToList());
        sessions.Add(Guid.NewGuid().ToString(), session);
        game.Start(session);
    }

    private void JoinGame(string sessionId, string playerName) {
        if (sessions.TryGetValue(sessionId, out var session)) {
            session.AddPlayer(playerName);
        }
    }
}
