using System;
using OMAPGMap;

namespace OMAPGMap.Models
{
    public partial class Pokemon : IComparable
    {
        public string name { get; set; }
        public string id { get; set; }
        private int _id = -1;
        public int idValue 
        { 
            get
            {
                if(_id == -1)
                {
                    _id = int.Parse(id.Split('-')[1]);
                }
                return _id;
            } 
        }
        public double lat { get; set; }
        public double lon { get; set; }
        public int pokemon_id { get; set; }
        public bool trash { get; set; }
        public long expires_at {
            set => _expires = Utility.FromUnixTime(value);
        }
        private DateTime _expires;
        public DateTime ExpiresDate {get { return _expires; } }
        public PokeGender gender { get; set; }
        public int atk { get; set; }
        public string damage1 { get; set; }
        public string damage2 { get; set; }
        public int def { get; set; }
        public string move1 { get; set; }
        public string move2 { get; set; }
        public int sta { get; set; }
        public int level { get; set; }
        public int cp { get; set; }

        public int CompareTo(object obj)
        {
            return idValue.CompareTo(obj);
        }
    }

    public enum PokeGender { Male = 1, Female = 2}

}
