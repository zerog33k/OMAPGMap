using System;
using CoreLocation;
using Foundation;
using MapKit;

namespace OMAPGMap.Models
{
    public partial class Gym : NSObject, IMKAnnotation
    {
        public Gym()
        {
        }

        public CLLocationCoordinate2D Coordinate => new CLLocationCoordinate2D(lat, lon);

		[Export("title")]
        public string title { get => name;}
		
    }
}
