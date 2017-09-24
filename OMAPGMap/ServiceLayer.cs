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

			var client = new HttpClient(new NSUrlSessionHandler());
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            var response = await client.GetAsync($"{pokemonURL}?last_id={lastId}");
			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
                var pokes = JsonConvert.DeserializeObject<List<Pokemon>>(content);
                CleanUpExpired();
                Pokemon.AddRange(pokes);
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
                    if(!Gyms.ContainsKey(g.id))
                    {
                        Gyms[g.id] = g;
                    } else //update the old one
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

			var client = new HttpClient(new NSUrlSessionHandler());
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            var response = await client.GetAsync(raidsURL);
			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
                var raids = JsonConvert.DeserializeObject<List<Raid>>(content);
                CleanUpExpiredRaids();

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
            var toRemove = Raids.Values.Where(r => r.TimeEnd < now);
            foreach(var r in toRemove)
            {
                Raids.Remove(r.id);
            }
		}
    }
}
