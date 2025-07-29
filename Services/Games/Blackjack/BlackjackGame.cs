// BlackjackGame.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using Dalamud.Plugin;
using Newtonsoft.Json;

public class BlackjackGame : IGame {
    private Dictionary<string, List<string>> hands = new();
    private Dictionary<string, int> chips = new();
    private Dictionary<string, int> gil = new(); // Simulated Gil balances
    private Dictionary<string, int> bets = new();
    private Dictionary<string, string> betInputs = new();
    private Dictionary<string, string> buyInputs = new(); // NEW: input for chip purchases
    private List<string> deck;
    private GameSession currentSession;
    private string currentPlayer = string.Empty;
    private int currentPlayerIndex = 0;
    private bool showGui = false;
    private bool awaitingBets = true;
    private DalamudPluginInterface pluginInterface;
    private const string SaveFileName = "BlackjackChips.json";

    public BlackjackGame(DalamudPluginInterface pi) {
        pluginInterface = pi;
        LoadPersistentData();
    }

    public void Start(GameSession session) {
        currentSession = session;
        bets.Clear();
        betInputs.Clear();
        buyInputs.Clear();
        awaitingBets = true;

        foreach (var player in session.Players) {
            if (!chips.ContainsKey(player)) chips[player] = 1000;
            if (!gil.ContainsKey(player)) gil[player] = 10000;
            bets[player] = 0;
            betInputs[player] = "50";
            buyInputs[player] = "5"; // NEW: default buy input
        }

        showGui = true;
    }

    public void BeginRound() {
        deck = GenerateShuffledDeck();
        hands.Clear();
        foreach (var player in currentSession.Players) {
            hands[player] = new List<string> { DrawCard(), DrawCard() };
            SendMessage(player, $"Your hand: {string.Join(", ", hands[player])}");
        }
        currentPlayerIndex = 0;
        currentPlayer = currentSession.Players[currentPlayerIndex];
        awaitingBets = false;
    }

    public void HandleAction(GameSession session, string player, string action) {
        if (!hands.ContainsKey(player)) return;

        switch (action.ToLower()) {
            case "hit":
                var card = DrawCard();
                hands[player].Add(card);
                var total = CalculateHandValue(hands[player]);
                SendMessage(player, $"You drew: {card}. Hand: {string.Join(", ", hands[player])} (Total: {total})");

                if (total > 21) {
                    SendMessage(player, "Bust! You're out.");
                    AdvanceTurn();
                }
                break;

            case "stand":
                SendMessage(player, "You chose to stand.");
                AdvanceTurn();
                break;
        }
    }

    public void DrawGui() {
        if (!showGui || currentSession == null) return;

        ImGui.Begin("Blackjack Game Session");

        if (awaitingBets) {
            ImGui.Text("Place your bets!");
            foreach (var player in currentSession.Players) {
                var chipCount = chips.ContainsKey(player) ? chips[player] : 0;
                var gilCount = gil.ContainsKey(player) ? gil[player] : 0;
                ImGui.Separator();
                ImGui.Text($"Player: {player}");
                ImGui.Text($"Chips: {chipCount}");
                ImGui.Text($"Gil: {gilCount}");

                ImGui.PushID(player);
                var input = betInputs[player];
                var buf = new byte[32];
                System.Text.Encoding.UTF8.GetBytes(input, 0, input.Length, buf, 0);
                if (ImGui.InputText("Bet Amount", buf, (uint)buf.Length, ImGuiInputTextFlags.CharsDecimal)) {
                    betInputs[player] = System.Text.Encoding.UTF8.GetString(buf).TrimEnd('\0');
                }

                if (ImGui.Button("Place Bet")) {
                    if (int.TryParse(betInputs[player], out int amt)) {
                        PlaceBet(player, amt);
                    } else {
                        SendMessage(player, "Invalid bet amount.");
                    }
                }

                var buyBuf = new byte[32];
                var buyInput = buyInputs[player];
                System.Text.Encoding.UTF8.GetBytes(buyInput, 0, buyInput.Length, buyBuf, 0);
                if (ImGui.InputText("Buy Chips", buyBuf, (uint)buyBuf.Length, ImGuiInputTextFlags.CharsDecimal)) {
                    buyInputs[player] = System.Text.Encoding.UTF8.GetString(buyBuf).TrimEnd('\0');
                }

                if (ImGui.Button("Buy Chips##Buy")) {
                    if (int.TryParse(buyInputs[player], out int buyAmt)) {
                        BuyChips(player, buyAmt);
                    } else {
                        SendMessage(player, "Invalid chip purchase amount.");
                    }
                }
                ImGui.PopID();
            }

            if (AllBetsPlaced()) {
                if (ImGui.Button("Start Round")) {
                    BeginRound();
                }
            }
        } else {
            ImGui.Text($"Current Player: {currentPlayer}");
            foreach (var kvp in hands) {
                var player = kvp.Key;
                var hand = kvp.Value;
                var total = CalculateHandValue(hand);
                var chipCount = chips.ContainsKey(player) ? chips[player] : 0;
                var gilCount = gil.ContainsKey(player) ? gil[player] : 0;

                ImGui.Separator();
                ImGui.Text($"Player: {player}");
                ImGui.Text($"Hand: {string.Join(", ", hand)}");
                ImGui.Text($"Total: {total}");
                ImGui.Text($"Chips: {chipCount}");
                ImGui.Text($"Gil: {gilCount}");

                if (player == currentPlayer) {
                    if (ImGui.Button("Hit##" + player)) {
                        HandleAction(currentSession, player, "hit");
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Stand##" + player)) {
                        HandleAction(currentSession, player, "stand");
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Buy 5 Chips##" + player)) {
                        BuyChips(player, 5);
                    }
                }
            }
        }

        if (ImGui.Button("Close Game")) {
            showGui = false;
            currentSession = null;
            SavePersistentData();
        }

        ImGui.End();
    }

    private void PlaceBet(string player, int amount) {
        if (chips[player] >= amount) {
            chips[player] -= amount;
            bets[player] += amount;
            SendMessage(player, $"You bet {amount} chips.");
        } else {
            SendMessage(player, "Not enough chips to place bet.");
        }
    }

    private bool AllBetsPlaced() {
        return currentSession.Players.All(p => bets.ContainsKey(p) && bets[p] > 0);
    }

    private void BuyChips(string player, int amount) {
        int cost = amount;
        if (gil[player] >= cost) {
            gil[player] -= cost;
            chips[player] += amount;
            SendMessage(player, $"Purchased {amount} chips for {cost} gil.");
        } else {
            SendMessage(player, "Not enough gil to purchase chips.");
        }
    }

    private void AdvanceTurn() {
        currentPlayerIndex++;
        if (currentPlayerIndex >= currentSession.Players.Count) {
            showGui = false;
            EvaluateWinners();
            currentSession = null;
            SavePersistentData();
        } else {
            currentPlayer = currentSession.Players[currentPlayerIndex];
        }
    }

    private void EvaluateWinners() {
        var validPlayers = hands
            .Where(kvp => CalculateHandValue(kvp.Value) <= 21)
            .OrderByDescending(kvp => CalculateHandValue(kvp.Value))
            .ToList();

        if (!validPlayers.Any()) {
            foreach (var player in hands.Keys) {
                SendMessage(player, "No winners this round.");
            }
            return;
        }

        var bestValue = CalculateHandValue(validPlayers.First().Value);
        var winners = validPlayers.Where(kvp => CalculateHandValue(kvp.Value) == bestValue).ToList();

        foreach (var kvp in hands) {
            var isWinner = winners.Any(w => w.Key == kvp.Key);
            var bet = bets.ContainsKey(kvp.Key) ? bets[kvp.Key] : 0;
            if (isWinner) {
                chips[kvp.Key] += bet * 2;
                SendMessage(kvp.Key, $"You win {bet * 2} chips!");
            } else {
                SendMessage(kvp.Key, "You lost your bet.");
            }
        }
    }

    private List<string> GenerateShuffledDeck() {
        var suits = new[] { "H", "D", "C", "S" };
        var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        var deck = suits.SelectMany(suit => ranks.Select(rank => rank + suit)).ToList();
        return deck.OrderBy(_ => Guid.NewGuid()).ToList();
    }

    private string DrawCard() {
        var card = deck[0];
        deck.RemoveAt(0);
        return card;
    }

    private int CalculateHandValue(List<string> hand) {
        int value = 0;
        int aceCount = 0;
        foreach (var card in hand) {
            var rank = card[..^1];
            if (int.TryParse(rank, out var num))
                value += num;
            else if (rank is "J" or "Q" or "K")
                value += 10;
            else if (rank == "A") {
                value += 11;
                aceCount++;
            }
        }

        while (value > 21 && aceCount > 0) {
            value -= 10;
            aceCount--;
        }

        return value;
    }

    private void SavePersistentData() {
        var data = new PersistentChipsData {
            Chips = chips,
            Gil = gil
        };
        var path = Path.Combine(pluginInterface.ConfigDirectory.FullName, SaveFileName);
        File.WriteAllText(path, JsonConvert.SerializeObject(data));
    }

    private void LoadPersistentData() {
        var path = Path.Combine(pluginInterface.ConfigDirectory.FullName, SaveFileName);
        if (!File.Exists(path)) return;

        var data = JsonConvert.DeserializeObject<PersistentChipsData>(File.ReadAllText(path));
        if (data != null) {
            chips = data.Chips ?? new();
            gil = data.Gil ?? new();
        }
    }

    private void SendMessage(string player, string message) {
        ChatHelper.SendMessage(player, message);
    }

    private class PersistentChipsData {
        public Dictionary<string, int> Chips { get; set; }
        public Dictionary<string, int> Gil { get; set; }
    }
}
