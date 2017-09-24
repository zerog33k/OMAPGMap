using System;
using CoreLocation;
using Foundation;
using MapKit;

namespace OMAPGMap.Models
{
    public partial class Raid : NSObject, IMKAnnotation
    {
		public Raid(IntPtr ptr) : base(ptr)
        {
            
        }

        public Raid() : base()
        {
            
        }

		public CLLocationCoordinate2D Coordinate => new CLLocationCoordinate2D(lat, lon);

		[Export("title")]
		public string title 
        { 
            get
            {
                if(pokemon_id == 0)
                {
                    return $"Upcoming raid level {level}";
                } else
                {
                    return $"{pokemon_name} (#${pokemon_id}) Raid - Level {level}";
                }
            }
        }
    }
}
