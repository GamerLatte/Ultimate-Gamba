// PlayerData.cs
using System;

namespace GambaPlugin
{
    public class PlayerData
    {
        public string PlayerName { get; set; }
        public int Chips { get; set; }

        public PlayerData(string name, int chips = 0)
        {
            PlayerName = name;
            Chips = chips;
        }

        public void AddChips(int amount)
        {
            Chips += amount;
        }

        public bool RemoveChips(int amount)
        {
            if (amount > Chips) return false;
            Chips -= amount;
            return true;
        }
    }
}
