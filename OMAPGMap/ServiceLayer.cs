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

        private static string baseURL = "https://www.omahapgmap.com";
        private static string serviceURL = $"{baseURL}/data";

        private ServiceLayer()
        {
        }

        public ConcurrentDictionary<string, Pokemon> Pokemon = new ConcurrentDictionary<string, Pokemon>();
        private int lastId = 0;

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public async Task<bool> VerifyCredentials()
        {
            var rval = false;
            var authData = string.Format("{0}:{1}", Username, Password);
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

            var client = new HttpClient(new NSUrlSessionHandler());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            var response = await client.GetAsync(baseURL);
            rval = response.IsSuccessStatusCode;
            return rval;
        }

        public async Task LoadPokemon()
        {
            var authData = string.Format("{0}:{1}", Username, Password);
			var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

			var client = new HttpClient(new NSUrlSessionHandler());
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            var response = await client.GetAsync($"{serviceURL}?last_id={lastId}");
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

        public IList<Pokemon> CleanUpExpired()
        {
            var rval = new List<Pokemon>();
			var now = DateTime.Now;
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
