using System;
using CoreLocation;
using Foundation;
using MapKit;

namespace OMAPGMap.Models
{
    public partial class Pokemon : NSObject, IMKAnnotation
    {
        public Pokemon() : base()
        {
            
        }

        public Pokemon(IntPtr ptr) : base(ptr)
        {
        }

        public CLLocationCoordinate2D Coordinate => new CLLocationCoordinate2D(lat, lon);

        [Export("title")]
        public string title
        {
            get
            {
                return title;
            }
        }
    }
}
