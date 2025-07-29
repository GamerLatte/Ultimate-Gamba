// PluginMain.cs
using Dalamud.Plugin;

public class PluginMain : IDalamudPlugin {
    public string Name => "Gambler";
    private GameManager gameManager;

    public void Initialize(DalamudPluginInterface pluginInterface) {
        gameManager = new GameManager();
        pluginInterface.CommandManager.AddHandler("/gamble", new Dalamud.Game.Command.CommandInfo(OnCommand) {
            HelpMessage = "Run gambling games like /gamble start blackjack"
        });
    }

    private void OnCommand(string command, string args) {
        gameManager.HandleCommand(args);
    }

    public void Dispose() {
        // Clean up
    }
}
