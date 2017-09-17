using System;
using UIKit;
using CoreLocation;
using MapKit;
using Foundation;
using OMAPGMap.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ObjCRuntime;
using CoreGraphics;

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
			username.ShouldReturn += (textField) =>
			{
                password.BecomeFirstResponder();
				return true;
			};
            password.ShouldReturn += (textField) => 
            {
                SignInButton_TouchUpInside(password, null);
                return true;
            };
            if (ServiceLayer.SharedInstance.Username == "")
            {
				loader.Alpha = 0.0f;
				loadingLabel.Alpha = 0.0f;
				username.BecomeFirstResponder();
            }
            else
            {
                var loggedIn = false;
                try
                {
                    loggedIn = await ServiceLayer.SharedInstance.VerifyCredentials();
                }
                catch(Exception){} //swallow exception
                if (loggedIn)
                {
                    username.Alpha = 0.0f;
                    password.Alpha = 0.0f;
                    signInButton.Alpha = 0.0f;
                    await LoggedIn();
                }
                else
                {
					loader.Alpha = 0.0f;
					loadingLabel.Alpha = 0.0f;
					username.BecomeFirstResponder();
                }
            }
            signInButton.TouchUpInside += SignInButton_TouchUpInside;
        }

        private async Task LoggedIn()
        {
            await ServiceLayer.SharedInstance.LoadPokemon();
            loader.StopAnimating();
            map.Delegate = this;
            map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Values.Where(p => !p.trash).ToArray());
            UIView.Animate(0.3, () =>
            {
                overlayView.Alpha = 0.0f;
                effectOverlay.Effect = null;
            }, () =>
            {
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
            if (pokemon != null)
            {
                annotateView.Pokemon = pokemon;
                annotateView.Frame = new CGRect(0, 0, 40, 55);
                annotateView.UpdateTime(DateTime.Now);
                annotateView.Map = mapView;

                var view = Runtime.GetNSObject<PokemonCalloutView>(NSBundle.MainBundle.LoadNib("PokemonCalloutView", null, null).ValueAt(0));
                //view.Frame = new CGRect(0, 0, 220, 200);
                var gender = pokemon.gender == PokeGender.Male ? "Male" : "Female";
                if (mapView.UserLocation != null)
                {
                    var dist =  mapView.UserLocation.Location.DistanceFrom(new CLLocation(pokemon.lat, pokemon.lon));
                    var distMiles = dist * 0.00062137;
                    view.DistanceLabel.Text = $"{distMiles.ToString("F1")} miles away";
                }
                if (string.IsNullOrEmpty(pokemon.move1))
                {
                    view.Stack.RemoveArrangedSubview(view.Move1Label);
                    view.Stack.RemoveArrangedSubview(view.Move2Label);
                    view.Stack.RemoveArrangedSubview(view.IVLabl);
                    view.Move1Label.RemoveFromSuperview();
                    view.Move2Label.RemoveFromSuperview();
                    view.IVLabl.RemoveFromSuperview();
                }
                else
                {
                    view.Move1Label.Text = $"Move 1: {pokemon.move1} ({pokemon.damage1} dps)";
                    view.Move2Label.Text = $"Move 2: {pokemon.move1} ({pokemon.damage2} dps)";
                    view.IVLabl.Text = $"IV: {pokemon.atk}atk {pokemon.def}def {pokemon.sta}sta";
                    var iv = (pokemon.atk + pokemon.def + pokemon.sta) / 45.0f;
                }
                annotateView.DetailCalloutAccessoryView = view;
                annotateView.CanShowCallout = true;

                return annotateView;
            }
            else
            {
                return null;
            }
        }

        void HandleTimerCallback(object state)
        {
            var toRemove = ServiceLayer.SharedInstance.CleanUpExpired();
            InvokeOnMainThread(() =>
            {
                try
                {
                    foreach(var p in toRemove)
                    {
                        map.RemoveAnnotation(p);
                    }
                    Console.WriteLine($"Removed {toRemove.Count()} pokemon");
                    var annotations = map.GetAnnotations(map.VisibleMapRect);
                    var now = DateTime.UtcNow;
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
                var toAdd = ServiceLayer.SharedInstance.Pokemon.Values.Where(p => !p.trash).Except(onMap);
                Console.WriteLine($"Adding {toAdd.Count()} mons to the map");
                foreach (var p in toAdd)
                {
                    map.AddAnnotation(p);
                }
            });
        }

        async void SignInButton_TouchUpInside(object sender, EventArgs e)
        {
            var errorMessage = "";
            if(username.Text == "" || password.Text == "")
            {
                errorMessage = "Username or password cannot be empty";
            }
            ServiceLayer.SharedInstance.Username = username.Text;
            ServiceLayer.SharedInstance.Password = password.Text;
            try{
                var loggedIn = await ServiceLayer.SharedInstance.VerifyCredentials();
                if(!loggedIn)
                {
                    errorMessage = "Username or password are incorrect";
                }
            } catch(Exception e2)
            {
                errorMessage = "An error occured when attempting to log in";
            }
            if(errorMessage == "")
            {
                password.ResignFirstResponder();
                username.Alpha = 0.0f;
                password.Alpha = 0.0f;
                signInButton.Alpha = 0.0f;
                loader.Alpha = 1.0f;
                loader.StartAnimating();
                loadingLabel.Alpha = 1.0f;
                var helper = new KeychainHelper();
                try
                {
                    NSUserDefaults.StandardUserDefaults.SetString(username.Text, "user");
                    NSUserDefaults.StandardUserDefaults.SetString(password.Text, "pass");
                    //helper.SetValueForKey(password.Text, username.Text);
                    await LoggedIn();
                } catch(Exception a)
                {
                    errorMessage = "An error occured when attempting to log in";
					var alert = UIAlertController.Create("Error", errorMessage, UIAlertControllerStyle.Alert);
					alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
					PresentViewController(alert, true, null);
                }
            } else
            {
                var alert = UIAlertController.Create("Error", errorMessage, UIAlertControllerStyle.Alert);
                alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
                PresentViewController(alert, true, null);
            }
        }

        public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
        {
            if(segue.Identifier == "LayerPopover")
            {
                var popVC = segue.DestinationViewController as LayerSelectorViewController;
                popVC.ModalPresentationStyle = UIModalPresentationStyle.Popover;
            }
        }
    }
}
