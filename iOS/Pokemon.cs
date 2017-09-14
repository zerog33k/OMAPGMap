using System;
using CoreLocation;
using Foundation;
using MapKit;

namespace OMAPGMap.Models
{
    public partial class Pokemon : NSObject, IMKAnnotation
    {
        public Pokemon()
        {
        }

        public CLLocationCoordinate2D Coordinate => new CLLocationCoordinate2D(lat, lon);

        public string title { get 
            { return $"{name} ({gender}) - #{pokemon_id}"; } }
    }
}
