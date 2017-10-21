using System;
using OMAPGMap;

namespace OMAPGMap.Models
{
    public partial class Gym 
    {
        public string id { get; set; }
        public int sigting_id { get; set; }
        public int pokemon_id { get; set; }
        public string pokemon_name { get; set; }
        public Team team { get; set;  }
        public double lat { get; set; }
        public double lon { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string description { get; set; }
        public int? slots_available { get; set; }
        public bool is_in_battle { get; set; }
        private DateTime _last_modifed;
        private long _modified;
        public long last_modified
        {
            set
            {
                _last_modifed = Utility.FromUnixTime(value);
                _modified = value;
            }
            get => _modified;
        }

        public DateTime LastModifedDate { get => _last_modifed; }

		public void update(Gym gym)
		{
            pokemon_id = gym.pokemon_id;
            pokemon_name = gym.pokemon_name;
            team = gym.team;
            slots_available = gym.slots_available;
            is_in_battle = gym.is_in_battle;
            last_modified = gym.last_modified;
		}

    }



    public enum Team { Mystic = 1, Instinct = 2, Valor = 3, None = 0 }


}
