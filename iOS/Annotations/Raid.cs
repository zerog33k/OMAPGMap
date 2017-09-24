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
		public string title { get => name; }
    }
}
