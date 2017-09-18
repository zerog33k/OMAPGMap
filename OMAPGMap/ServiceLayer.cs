using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using KeyChain.Net;
using MoreLinq;
using Newtonsoft.Json;

using OMAPGMap.Models;

namespace OMAPGMap
{
    public class ServiceLayer
    {
		private static readonly Lazy<ServiceLayer> lazy = new Lazy<ServiceLayer>(() => new ServiceLayer());
        public static ServiceLayer SharedInstance { get { return lazy.Value; } }

        private static string baseURL = "https://www.omahapgmap.com";
        private static string pokemonURL = $"{baseURL}/data";
        private static string gymsURL = $"{baseURL}/gym_data";
        private static string raidsURL = $"{baseURL}/raids";

        private ServiceLayer()
        {
        }

        public ConcurrentDictionary<string, Pokemon> Pokemon = new ConcurrentDictionary<string, Pokemon>();
        public ConcurrentDictionary<string, Gym> Gyms = new ConcurrentDictionary<string, Gym>();
        public ConcurrentDictionary<string, Raid> Raids = new ConcurrentDictionary<string, Raid>();
                                        //pokemon, gyms, raids, trash
        public bool[] LayersEnabled = { true, true, false, false, };

        private int lastId = 0;

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public IKeyChainHelper KeyHelper { get; set; }

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
            if(LayersEnabled[0])
            {
                Console.WriteLine("loading Pokemon");
                await LoadPokemon();
            }
            if(LayersEnabled[1])
            {
                Console.WriteLine("loading Gyms");
                await LoadGyms();
            }
        }

        public async Task LoadPokemon()
        {
            var authData = string.Format("{0}:{1}", Username, Password);
			var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

			var client = new HttpClient(new NSUrlSessionHandler());
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            var response = await client.GetAsync($"{pokemonURL}?last_id={lastId}");
			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
                var pokes = JsonConvert.DeserializeObject<List<Pokemon>>(content);
                CleanUpExpired();
                foreach (var p in pokes)
                {
                    Pokemon.TryAdd(p.id, p);
                }
			}
			else
			{
                Pokemon.Clear();
			}
        }

		public async Task LoadGyms()
		{
			var authData = string.Format("{0}:{1}", Username, Password);
			var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

			var client = new HttpClient(new NSUrlSessionHandler());
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            var response = await client.GetAsync(gymsURL);
			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
                var gyms = JsonConvert.DeserializeObject<List<Gym>>(content);
                foreach (var g in gyms)
				{
                    Gym thisGym;
                    Gyms.TryGetValue(g.id, out thisGym);
                    if(thisGym == null)
                    {
                        Gyms.TryAdd(g.id, g);
                    } else //update the old one
                    {
                        g.CopyProperties(thisGym);
                    }
				}
			}
			else
			{
				Gyms.Clear();
			}
		}

		public async Task LoadRaids()
		{
			var authData = string.Format("{0}:{1}", Username, Password);
			var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

			var client = new HttpClient(new NSUrlSessionHandler());
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            var response = await client.GetAsync(raidsURL);
			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
                var raids = JsonConvert.DeserializeObject<List<Raid>>(content);
				CleanUpExpired();
                foreach (var r in raids)
				{
                    Raids.TryAdd(r.id, r);
				}
			}
			else
			{
				Raids.Clear();
			}
		}

        public IList<Pokemon> CleanUpExpired()
        {
            var rval = new List<Pokemon>();
            var now = DateTime.UtcNow;
			foreach (var p in Pokemon.Values) //remove all expired
			{
				if (p.ExpiresDate < now)
				{
					Pokemon p2;
					Pokemon.TryRemove(p.id, out p2);
                    rval.Add(p2);
				}
			}
            return rval;
        }
    }
}
