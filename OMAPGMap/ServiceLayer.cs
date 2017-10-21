using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using Newtonsoft.Json;
using OMAPGMap.Models;

namespace OMAPGMap
{
    public class ServiceLayer
    {
        private static readonly Lazy<ServiceLayer> lazy = new Lazy<ServiceLayer>(() => new ServiceLayer());
        public static ServiceLayer SharedInstance { get { return lazy.Value; } }

        private static string baseURL = "http://zerogeek.net/map";
        private static string pokemonURL = $"{baseURL}/data";
        private static string gymsURL = $"{baseURL}/gym_data";
        private static string raidsURL = $"{baseURL}/raids";

        private ServiceLayer()
        {
        }

        public List<Pokemon> Pokemon = new List<Pokemon>();
        public Dictionary<string, Gym> Gyms = new Dictionary<string, Gym>();
        public Dictionary<string, Raid> Raids = new Dictionary<string, Raid>();
        //pokemon, gyms, raids, trash
        public bool[] LayersEnabled = { true, false, true, false, };
        public static int[] DefaultHidden = { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 132, 144, 145, 146, 150, 151, 161, 162, 163, 164, 165, 166, 167, 168, 172, 173, 174, 175, 182, 186, 192, 196, 197, 199, 208, 212, 230, 233, 236, 238, 239, 240, 243, 244, 245, 249, 250, 251 };
        public static int[] DefaultTrash = { 1, 4, 7, 21, 23, 25, 27, 29, 30, 32, 33, 35, 37, 39, 41, 43, 46, 48, 50, 52, 54, 56, 58, 60, 63, 66, 69, 72, 74, 77, 79, 81, 84, 86, 88, 90, 92, 96, 98, 100, 102, 104, 109, 111, 116, 118, 120, 124, 129, 133, 138, 140, 147, 152, 155, 158, 170, 177, 183, 185, 187, 188, 190, 191, 194, 198, 200, 202, 203, 204, 206, 207, 209, 211, 215, 216, 218, 220, 223, 228, 231, 234, 246, 302, 355, 353 };
        public static int[] Gen3 = { 302, 353, 354, 355, 356 };
        public static int NumberPokemon = 251;
        public static int HighestPokemonId = 356;

        public List<int> PokemonTrash = new List<int>(DefaultTrash);
        public List<int> PokemonHidden = new List<int>(DefaultHidden);

        public int LastId = 0;

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public async Task<bool> VerifyCredentials()
        {
            var rval = false;
            var authData = string.Format("{0}:{1}", Username, Password);
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

            //var handler = new NSUrlSessionHandler();
            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            var response = await client.GetAsync(baseURL);
            rval = response.IsSuccessStatusCode;
            return rval;
        }

        public async Task LoadData()
        {
            if (LayersEnabled[0])
            {
                Console.WriteLine("loading Pokemon");
                await LoadPokemon();
            }
            if (LayersEnabled[1])
            {
                Console.WriteLine("loading Gyms");
                await LoadGyms();
            }
            if (LayersEnabled[2])
            {
                Console.WriteLine("loading Raids");
                await LoadRaids();
            }

        }

        public async Task LoadPokemon()
        {
            var authData = string.Format("{0}:{1}", Username, Password);
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));
            CleanUpExpired();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            try
            {
                var response = await client.GetAsync($"{pokemonURL}?last_id={LastId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var pokes = JsonConvert.DeserializeObject<List<Pokemon>>(content);
                    Pokemon.AddRange(pokes);
                    LastId = Pokemon.MaxBy(p => p.idValue).idValue;
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task LoadGyms()
        {
            var authData = string.Format("{0}:{1}", Username, Password);
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            var response = await client.GetAsync(gymsURL);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var gyms = JsonConvert.DeserializeObject<List<Gym>>(content);
                foreach (var g in gyms)
                {
                    if (!Gyms.ContainsKey(g.id))
                    {
                        Gyms[g.id] = g;
                    }
                    else //update the old one
                    {
                        Gyms[g.id].update(g);
                    }
                }
            }
        }

        public async Task LoadRaids()
        {
            var authData = string.Format("{0}:{1}", Username, Password);
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

            CleanUpExpiredRaids();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            var response = await client.GetAsync(raidsURL);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var raids = JsonConvert.DeserializeObject<List<Raid>>(content);


                foreach (var r in raids)
                {
                    if (!Raids.ContainsKey(r.id))
                    {
                        Raids.Add(r.id, r);
                    }
                    else //update the old one
                    {
                        Raids[r.id].Update(r);
                    }
                }
            }
        }

        public void CleanUpExpired()
        {
            var now = DateTime.UtcNow;
            Pokemon.RemoveAll(p => p.ExpiresDate < now);
        }

        public void CleanUpExpiredRaids()
        {
            var now = DateTime.UtcNow;
            var toRemove = Raids.Values.Where(r => (r.TimeEnd < now) || (r.pokemon_id == 0 && r.TimeBattle < now));
            foreach (var r in toRemove)
            {
                Raids.Remove(r.id);
            }
        }
    }
}
