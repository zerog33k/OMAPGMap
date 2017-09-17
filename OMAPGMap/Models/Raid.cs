using System;
namespace OMAPGMap.Models
{
    public class Raid
    {
        public string id { get; set; }
        public int level { get; set; }
        public int pokemon_id { get; set; }
        public string pokemon_name { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
        public Team team { get; set; }
        public string name { get; set; }
        public int cp { get; set; }
        public string move_1 { get; set; }
        public string move_2 { get; set; }
        private DateTime _time_spawn;
        public long time_spawn { set => _time_spawn = Utility.FromUnixTime(value); }
        public DateTime TimeSpawn { get => _time_spawn; }
        private DateTime _time_battle;
        public long time_battle { set => _time_battle = Utility.FromUnixTime(value); }
        public DateTime TimeBattle { get => _time_battle; }
        private DateTime _time_end;
        public long time_end { set => _time_end = Utility.FromUnixTime(value); }
        public DateTime TimeEnd { get => _time_end; }

    }
}
