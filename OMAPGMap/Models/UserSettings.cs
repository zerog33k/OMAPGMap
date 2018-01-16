using System;
using System.Collections.Generic;

namespace OMAPGMap.Models
{
    public class UserSettings
    {
        public bool NotifyEnabled { get; set; } = true;
        public bool Notify90Enabled { get; set; } = true;
        public bool Notify100Enabled { get; set; } = true;
        public int NotifyDistance { get; set; } = 3;
        public bool LegondaryRaids { get; set; } = true;
        public bool Level4Raids { get; set; } = true;
        public bool Level3Raids { get; set; } = true;
        public bool Level2Raids { get; set; } = true;
        public bool Level1Raids { get; set; } = true;

        public int LastId { get; set; } = 0;
        public int MinId { get; set; } = 0;

        private static int[] DefaultHidden = { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 132, 144, 145, 146, 150, 151, 161, 162, 163, 164, 165, 166, 167, 168, 172, 173, 174, 175, 182, 186, 192, 196, 197, 199, 208, 212, 230, 233, 236, 238, 239, 240, 243, 244, 245, 249, 250, 251, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386 };
        private static int[] DefaultTrash = { 1, 4, 7, 21, 23, 25, 27, 29, 30, 32, 33, 35, 37, 39, 41, 43, 46, 48, 50, 52, 54, 56, 58, 60, 63, 66, 69, 72, 74, 77, 79, 81, 84, 86, 88, 90, 92, 96, 98, 100, 102, 104, 109, 111, 116, 118, 120, 124, 129, 133, 138, 140, 147, 152, 155, 158, 170, 177, 183, 185, 187, 188, 190, 191, 194, 198, 200, 202, 203, 204, 206, 207, 209, 211, 215, 216, 218, 220, 223, 228, 231, 
            //gen 3 list
            252, 255, 258, 261, 263, 265, 273, 280, 296, 300, 307, 309, 315, 316, 325, 361, 363 };

        public List<int> PokemonTrash = new List<int>(DefaultTrash);
        public List<int> PokemonHidden = new List<int>(DefaultHidden);
        public List<int> NotifyPokemon = new List<int>();

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool LoggedIn { get; set; } = false;

        public bool PokemonEnabled { get; set; } = true;
        public bool RaidsEnabled { get; set; } = false;
        public bool GymsEnabled { get; set; } = false;
        public bool NinetyOnlyEnabled { get; set; } = false;

        public void ResetTrash()
        {
            PokemonTrash.Clear();
            PokemonTrash.AddRange(DefaultTrash);
        }

    }
}
