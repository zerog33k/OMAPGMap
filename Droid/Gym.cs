using Android.Gms.Maps.Model;

namespace OMAPGMap.Models
{
    public partial class Gym
    {
        private LatLng _latLng = null;
        public LatLng Location
        {
            get
            {
                if (_latLng == null)
                {
                    _latLng = new LatLng(lat, lon);
                }
                return _latLng;
            }
        }

        public Marker GymMarker { get; set; }
    }
}
