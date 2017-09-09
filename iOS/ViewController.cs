using System;
using System.Text;
using UIKit;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;
using CoreLocation;
using MapKit;
using Foundation;
using OMAPGMap.Models;
using System.Linq;
using System.Threading;

namespace OMAPGMap.iOS
{
    public partial class ViewController : UIViewController, IMKMapViewDelegate
    {
        CLLocationManager locationManager;
        Timer secondTimer;
        Timer minuteTimer;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();
            loader.StartAnimating();
			locationManager = new CLLocationManager();
			locationManager.RequestWhenInUseAuthorization();
            locationManager.LocationsUpdated += LocationManager_LocationsUpdated;
            locationManager.StartUpdatingLocation();
            map.ShowsUserLocation = true;
            CLLocationCoordinate2D coords = new CLLocationCoordinate2D(41.2524, -95.9980);
			MKCoordinateSpan span = new MKCoordinateSpan(Utility.MilesToLatitudeDegrees(2), Utility.MilesToLongitudeDegrees(2, coords.Latitude));
			map.Region = new MKCoordinateRegion(coords, span);
            await ServiceLayer.SharedInstance.LoadPokemon();
            loader.StopAnimating();
            map.Delegate = this;
            map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Values.Where(p => !p.trash).ToArray());
            UIView.Animate(0.3, () => 
            {
                overlayView.Alpha = 0.0f;
                effectOverlay.Effect = null;
            }, () => {
                effectOverlay.Hidden = true;
            });

            secondTimer = new Timer(HandleTimerCallback, null, 1000, 1000);
            minuteTimer = new Timer(refreshMap, null, 60000, 60000);
        }

        void LocationManager_LocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            locationManager.StopUpdatingLocation();
            if(!map.UserLocationVisible)
            {
                CLLocationCoordinate2D coords = e.Locations[0].Coordinate;
				MKCoordinateSpan span = new MKCoordinateSpan(Utility.MilesToLatitudeDegrees(2), Utility.MilesToLongitudeDegrees(2, coords.Latitude));
                map.SetRegion(new MKCoordinateRegion(coords, span), true);
            }
        }

        [Export("mapView:viewForAnnotation:")]
        public MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            var annotateView = mapView.DequeueReusableAnnotation("Pokemkon") as PokemonAnnotationView;
            if(annotateView == null)
            {
                annotateView = new PokemonAnnotationView(annotation, "Pokemon");
            }
            var pokemon = annotation as Pokemon;
            annotateView.Pokemon = pokemon;
            annotateView.Frame = new CoreGraphics.CGRect(0, 0, 40, 55);
            annotateView.UpdateTime(DateTime.Now);
            return annotateView;
        }


        void HandleTimerCallback(object state)
        {

            InvokeOnMainThread(() =>
            {
                try
                {
                    var annotations = map.GetAnnotations(map.VisibleMapRect);
                    var now = DateTime.Now;
                    foreach (var a in annotations)
                    {
                        var a2 = a as Pokemon;
                        if (a2 != null)
                        {
                            var annotateView = map.ViewForAnnotation(a2) as PokemonAnnotationView;
                            if (annotateView != null)
                            {
                                annotateView.UpdateTime(now);
                            }
                            if (a2.ExpiresDate < now)
                            {
                                map.RemoveAnnotation(a2);
                                Pokemon p;
                                ServiceLayer.SharedInstance.Pokemon.TryRemove(a2.id, out p);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            });
            
        }

        async void refreshMap(object state)
        {
            await ServiceLayer.SharedInstance.LoadPokemon();
			InvokeOnMainThread(() =>
            {
                var onMap = map.Annotations;

            }
        }
    }
}
