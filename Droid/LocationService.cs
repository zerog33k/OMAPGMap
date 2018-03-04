
using System;

using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Util;
using Microsoft.AppCenter;

namespace OMAPGMap.Droid
{
    [Service(Label = "LocationService")]
    [IntentFilter(new String[] { "net.zerogeek.LocationService" })]
    public class LocationService : Service, ILocationListener
    {
        IBinder binder;

        public event EventHandler<LocationChangedEventArgs> LocationChanged = delegate { };
        public event EventHandler<ProviderDisabledEventArgs> ProviderDisabled = delegate { };
        public event EventHandler<ProviderEnabledEventArgs> ProviderEnabled = delegate { };
        public event EventHandler<StatusChangedEventArgs> StatusChanged = delegate { };

        public LocationService()
        {
        }

        // Set our location manager as the system location service
        protected LocationManager LocMgr = Android.App.Application.Context.GetSystemService("location") as LocationManager;

        readonly string logTag = "LocationService";

        public override void OnCreate()
        {
            base.OnCreate();
            Log.Debug(logTag, "OnCreate called in the Location Service");
        }

        // This gets called when StartService is called in our App class
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Log.Debug(logTag, "LocationService started");

            return StartCommandResult.Sticky;
        }

        // This gets called once, the first time any client bind to the Service
        // and returns an instance of the LocationServiceBinder. All future clients will
        // reuse the same instance of the binder
        public override IBinder OnBind(Intent intent)
        {
            Log.Debug(logTag, "Client now bound to service");

            binder = new LocationServiceBinder(this);
            return binder;
        }

        // Handle location updates from the location manager
        public void StartLocationUpdates()
        {
            //we can set different location criteria based on requirements for our app -
            //for example, we might want to preserve power, or get extreme accuracy
            var locationCriteria = new Criteria();

            locationCriteria.Accuracy = Accuracy.Medium;
            locationCriteria.PowerRequirement = Power.Low;

            // get provider: GPS, Network, etc.
            var locationProvider = LocMgr.GetBestProvider(locationCriteria, true);
            Log.Debug(logTag, string.Format("You are about to get location updates via {0}", locationProvider));

            // Get an initial fix on location
            LocMgr.RequestLocationUpdates(locationProvider, 60000, 0, this);

            Log.Debug(logTag, "Now sending location updates");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Log.Debug(logTag, "Service has been terminated");

            // Stop getting updates from the location manager:
            LocMgr.RemoveUpdates(this);
        }

        #region ILocationListener implementation
        // ILocationListener is a way for the Service to subscribe for updates
        // from the System location Service

        public async void OnLocationChanged(Android.Locations.Location location)
        {
            this.LocationChanged(this, new LocationChangedEventArgs(location));

            var installID = await AppCenter.GetInstallIdAsync();
            var install = installID.ToString();

            await ServiceLayer.SharedInstance.UpdateDeviceInfo(install, location.Latitude, location.Longitude);

            // This should be updating every time we request new location updates
            // both when the app is in the background, and in the foreground
            Log.Debug(logTag, String.Format("Latitude is {0}", location.Latitude));
            Log.Debug(logTag, String.Format("Longitude is {0}", location.Longitude));
            Log.Debug(logTag, String.Format("Altitude is {0}", location.Altitude));
            Log.Debug(logTag, String.Format("Speed is {0}", location.Speed));
            Log.Debug(logTag, String.Format("Accuracy is {0}", location.Accuracy));
            Log.Debug(logTag, String.Format("Bearing is {0}", location.Bearing));
        }

        public void OnProviderDisabled(string provider)
        {
            this.ProviderDisabled(this, new ProviderDisabledEventArgs(provider));
        }

        public void OnProviderEnabled(string provider)
        {
            this.ProviderEnabled(this, new ProviderEnabledEventArgs(provider));
        }

        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
            this.StatusChanged(this, new StatusChangedEventArgs(provider, status, extras));
        }

        #endregion
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

    public class ServiceConnectedEventArgs : EventArgs
    {
        public IBinder Binder { get; set; }
    }

    public class LocationServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public event EventHandler<ServiceConnectedEventArgs> ServiceConnected = delegate { };

        public LocationServiceBinder Binder
        {
            get { return this.binder; }
            set { this.binder = value; }
        }
        protected LocationServiceBinder binder;

        public LocationServiceConnection(LocationServiceBinder binder)
        {
            if (binder != null)
            {
                this.binder = binder;
            }
        }

        // This gets called when a client tries to bind to the Service with an Intent and an 
        // instance of the ServiceConnection. The system will locate a binder associated with the 
        // running Service 
        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            // cast the binder located by the OS as our local binder subclass
            LocationServiceBinder serviceBinder = service as LocationServiceBinder;
            if (serviceBinder != null)
            {
                this.binder = serviceBinder;
                Log.Debug("ServiceConnection", "OnServiceConnected Called");
                // raise the service connected event
                this.ServiceConnected(this, new ServiceConnectedEventArgs() { Binder = service });

                // now that the Service is bound, we can start gathering some location data
                serviceBinder.GetLocationService().StartLocationUpdates();
            }
        }

        // This will be called when the Service unbinds, or when the app crashes
        public void OnServiceDisconnected(ComponentName name)
        {
            Log.Debug("ServiceConnection", "Service unbound");
        }
    }
}
