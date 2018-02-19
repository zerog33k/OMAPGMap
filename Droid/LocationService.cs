
using System;

using Android.App;
using Android.Content;
using Android.OS;

namespace OMAPGMap.Droid
{
    [Service(Label = "LocationService")]
    [IntentFilter(new String[] { "net.zerogeek.LocationService" })]
    public class LocationService : Service
    {
        IBinder binder;

        public override StartCommandResult OnStartCommand(Android.Content.Intent intent, StartCommandFlags flags, int startId)
        {
            // start your service logic here

            // Return the correct StartCommandResult for the type of service you are building
            return StartCommandResult.NotSticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            binder = new LocationServiceBinder(this);
            return binder;
        }
    }

    public class LocationServiceBinder : Binder
    {
        readonly LocationService service;

        public LocationServiceBinder(LocationService service)
        {
            this.service = service;
        }

        public LocationService GetLocationService()
        {
            return service;
        }
    }
}
