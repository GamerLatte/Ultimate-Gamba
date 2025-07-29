public interface IGame {
    void Start(GameSession session);
    void HandleAction(GameSession session, string player, string action);
}
