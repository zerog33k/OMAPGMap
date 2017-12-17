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
using Newtonsoft.Json.Linq;
#if NETCOREAPP2_0
using OMAPGServiceData.Models;
#else
using OMAPGMap.Models;
#endif  

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
        public List<Raid> Raids = new List<Raid>();
        //pokemon, gyms, raids, trash
        public bool[] LayersEnabled = { true, false, true};
        public static int[] DefaultHidden = { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 132, 144, 145, 146, 150, 151, 161, 162, 163, 164, 165, 166, 167, 168, 172, 173, 174, 175, 182, 186, 192, 196, 197, 199, 208, 212, 230, 233, 236, 238, 239, 240, 243, 244, 245, 249, 250, 251, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386 };
        public static int[] DefaultTrash = { 1, 4, 7, 21, 23, 25, 27, 29, 30, 32, 33, 35, 37, 39, 41, 43, 46, 48, 50, 52, 54, 56, 58, 60, 63, 66, 69, 72, 74, 77, 79, 81, 84, 86, 88, 90, 92, 96, 98, 100, 102, 104, 109, 111, 116, 118, 120, 124, 129, 133, 138, 140, 147, 152, 155, 158, 170, 177, 183, 185, 187, 188, 190, 191, 194, 198, 200, 202, 203, 204, 206, 207, 209, 211, 215, 216, 218, 220, 223, 228, 231, 
            //gen 3 list
            253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378 };
        public static int NumberPokemon = 378;
        public bool NotifyEnabled { get; set; } = true;
        public bool Notify90Enabled { get; set; } = true;
        public bool Notify100Enabled { get; set; } = true;
        public int NotifyDistance { get; set; } = 3;
        public bool LegondaryRaids { get; set; } = true;
        public bool Level4Raids { get; set; } = true;
        public bool Level3Raids { get; set; } = true;
        public bool Level2Raids { get; set; } = true;
        public bool Level1Raids { get; set; } = true;

        public List<int> PokemonTrash = new List<int>(DefaultTrash);
        public List<int> PokemonHidden = new List<int>(DefaultHidden);
        public List<int> NotifyPokemon = new List<int>();

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
            rval = response.StatusCode != System.Net.HttpStatusCode.Unauthorized;
            return rval;
        }

        public async Task LoadData(int lastId)
        {
            if (LayersEnabled[0])
            {
                Console.WriteLine("loading Pokemon");
                await LoadPokemon(lastId);
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

        public async Task LoadPokemon(int lastId)
        {
            var authData = string.Format("{0}:{1}", Username, Password);
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));
            CleanUpExpired();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            try
            {
                var response = await client.GetAsync($"{pokemonURL}?last_id={lastId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var pokes = JsonConvert.DeserializeObject<List<Pokemon>>(content);
                    Pokemon.AddRange(pokes);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task LoadGyms()
        {
            try
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
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task LoadRaids()
        {
            var authData = string.Format("{0}:{1}", Username, Password);
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

            CleanUpExpiredRaids();
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
                var response = await client.GetAsync(raidsURL);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var raids = JsonConvert.DeserializeObject<List<Raid>>(content);
                    foreach (var r in raids)
                    {
                        if (!Raids.Exists(r2 => r2.id.Equals(r.id)))
                        {
                            Raids.Add(r);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
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
            var toRemove = Raids.RemoveAll(r => (r.TimeEnd < now) || (r.pokemon_id == 0 && r.TimeBattle < now));
        }

        public async Task UpdateDeviceInfo(string deviceId, double lat, double lon)
        {
            using (var client = new HttpClient())
            {
                var str = NotifyPokemon.ToDelimitedString(":");
                var jobj = JObject.FromObject(new
                {
                    DeviceId = deviceId,
                    Ostype = 1,
                    NotifyPokemonStr = str,
                    LocationLat = lat,
                    LocationLon = lon,
                    DistanceAlert = NotifyDistance,
                    NotifyEnabled = NotifyEnabled,
                    Notify90 = Notify90Enabled,
                    Notify100 = Notify100Enabled
                });
                var content = new StringContent(jobj.ToString(), Encoding.UTF8, "application/json");
                try
                {
                    var results = await client.PutAsync($"{baseURL}service/api/device", content);
                    if (results.IsSuccessStatusCode)
                    {
                        Console.WriteLine("updated device data!");
                    }
                    else
                    {
                        Console.WriteLine("updated device failed :/");
                    }
                } catch(Exception) 
                {
                    //swallow silently
                }
            }
        }
    }
}
