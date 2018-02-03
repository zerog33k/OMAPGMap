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
using System.Reactive.Linq;
using Akavache;
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
        private static string altURL = "http://107.189.42.114:7500";
        private string dataURL = baseURL;

        public UserSettings Settings { get; set; }

        private string pokemonURL => $"{dataURL}/data";
        private string gymsURL => $"{dataURL}/gym_data";
        private string raidsURL => $"{dataURL}/raids";

        private ServiceLayer()
        {
            BlobCache.ApplicationName = "OMAPGMap.zerogeek.net";
        }

        public List<Pokemon> Pokemon = new List<Pokemon>();
        public Dictionary<string, Gym> Gyms = new Dictionary<string, Gym>();
        public List<Raid> Raids = new List<Raid>();
        //pokemon, gyms, raids, trash

        public static int NumberPokemon = 378;

        private Coordinates _userLocation;
        public Coordinates UserLocation
        {
            set
            {
                _userLocation = value;
                var dist = Coordinates.DistanceBetween(value, BlairLocation);
                double distMiles = MetersToMiles(dist);
                if (distMiles < 5)
                {
                    dataURL = altURL;
                    Console.WriteLine($"loading data from {pokemonURL}");
                    _isAltLocation = true;
                }
            }
        }

        public static double MetersToMiles(double dist)
        {
            return dist * 0.0000062137;
        }

        public static Coordinates BlairLocation = new Coordinates(41.543834, -96.137934);
        private bool _isAltLocation = false;

        private string AuthHeader
        {
            get
            {
                var authData = string.Format("{0}:{1}", Settings.Username, Settings.Password);
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));
            }
        }

        public async Task InitalizeSettings()
        {
            Settings = await BlobCache.UserAccount.GetObject<UserSettings>("settings").Catch(Observable.Return(new UserSettings(true)));
            Settings.IgnorePokemon = Settings.IgnorePokemon.Distinct().ToList();
            Settings.NotifyPokemon = Settings.NotifyPokemon.Distinct().ToList();
            Settings.PokemonTrash = Settings.PokemonTrash.Distinct().ToList();
            Settings.LastUpdateTimestamp = Utility.ToUnixTime(DateTime.UtcNow.AddHours(-1.0));
        }

        public async Task SaveSettings()
        {
            await BlobCache.UserAccount.InsertObject("settings", Settings);
        }

        public async Task<bool> VerifyCredentials()
        {
            var rval = false;


            //var handler = new NSUrlSessionHandler();

            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", AuthHeader);
            var response = await client.GetAsync(baseURL);
            rval = response.StatusCode != System.Net.HttpStatusCode.Unauthorized;
            Settings.LoggedIn = rval;
            return rval;
        }


        public async Task LoadData()
        {
            if (Settings.PokemonEnabled || Settings.NinetyOnlyEnabled)
            {
                Console.WriteLine("loading Pokemon");
                await LoadPokemon();
            }
            if (Settings.GymsEnabled)
            {
                Console.WriteLine("loading Gyms");
                await LoadGyms();
            }
            if (Settings.RaidsEnabled)
            {
                Console.WriteLine("loading Raids");
                await LoadRaids();
            }

        }

        public async Task LoadPokemon()
        {
            CleanUpExpired();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", AuthHeader);
            try
            {
                var lastUpdate = Settings.LastUpdateTimestamp;
                var minTime = Utility.ToUnixTime(DateTime.UtcNow.AddHours(-1.0));

                var lid = lastUpdate > minTime ? lastUpdate : minTime;
                var response = await client.GetAsync($"{pokemonURL}?timestamp={lid}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var pokes = JsonConvert.DeserializeObject<List<Pokemon>>(content);
                    Pokemon.AddRange(pokes);
                    Settings.LastUpdateTimestamp = Pokemon.MaxBy(p => p?.timestamp)?.timestamp ?? lastUpdate;
                    await SaveSettings();
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
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", AuthHeader);
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
            CleanUpExpiredRaids();
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", AuthHeader);
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
                var str = Settings.NotifyPokemon.ToDelimitedString(":");
                var ignoreStr = Settings.IgnorePokemon.ToDelimitedString(":");
                var jobj = JObject.FromObject(new
                {
                    DeviceId = deviceId,
                    Ostype = 1,
                    NotifyPokemonStr = str,
                    LocationLat = lat,
                    LocationLon = lon,
                    DistanceAlert = Settings.NotifyDistance,
                    NotifyEnabled = Settings.NotifyEnabled,
                    Notify90 = Settings.Notify90Enabled,
                    Notify100 = Settings.Notify100Enabled,
                    MaxDistance = Settings.NotifyMaxDistance,
                    MinLevelAlert = Settings.NotifyLevel,
                    ignorePokemonStr = ignoreStr
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
