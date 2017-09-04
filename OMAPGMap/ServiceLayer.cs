using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OMAPGMap.Models;

namespace OMAPGMap
{
    public class ServiceLayer
    {
		private static readonly Lazy<ServiceLayer> lazy = new Lazy<ServiceLayer>(() => new ServiceLayer());
        public static ServiceLayer SharedInstance { get { return lazy.Value; } }

        private string serviceURL = "https://www.omahapgmap.com/data";

        private ServiceLayer()
        {
        }

        public List<Pokemon> Pokemon = new List<Pokemon>();

        public async Task LoadPokemon()
        {
			var authData = string.Format("{0}:{1}", "zerogeek", "myster!UM");
			var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

			var client = new HttpClient(new NSUrlSessionHandler());
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
			var response = await client.GetAsync(serviceURL);
			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
                Pokemon = JsonConvert.DeserializeObject<List<Pokemon>>(content);
			}
			else
			{
                Pokemon.Clear();
			}
        }
    }
}
