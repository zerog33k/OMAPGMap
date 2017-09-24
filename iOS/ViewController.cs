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
using OMAPGMap.iOS.Annotations;
using OMAPGMap;

namespace OMAPGMap.iOS
{
    public partial class ViewController : UIViewController, IMKMapViewDelegate, IUITableViewDelegate, IUITableViewDataSource, IUIPopoverPresentationControllerDelegate
    {
        CLLocationManager locationManager;
        Timer secondTimer;
        Timer minuteTimer;
        string[] Layers = { "Pokemon", "Gyms", "Raids", "Trash" };
        UITableViewController layersTableVC = null;

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
            layerSelectButton.TouchUpInside += LayerSelectButton_TouchUpInside;
        }   

        private async Task LoggedIn()
        {
            await ServiceLayer.SharedInstance.LoadData();
            loader.StopAnimating();
            map.Delegate = this;
            if (ServiceLayer.SharedInstance.LayersEnabled[3])
            {
                map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.ToArray());
            } else{
                map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Where(p => !p.trash).ToArray());
            }
            map.AddAnnotations(ServiceLayer.SharedInstance.Gyms.Values.ToArray());
            map.AddAnnotations(ServiceLayer.SharedInstance.Raids.Values.ToArray());
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
        protected internal MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            MKAnnotationView annotateView = null;

            var pokemon = annotation as Pokemon;
            if (pokemon != null)
            {
                annotateView = mapView.DequeueReusableAnnotation("Pokemkon") ?? new PokemonAnnotationView(annotation, "Pokemon");
                var pokeAV = annotateView as PokemonAnnotationView;
                pokeAV.Pokemon = pokemon;
                pokeAV.Frame = new CGRect(0, 0, 40, 55);
                pokeAV.UpdateTime(DateTime.Now);

                //callout stuff
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
            }
            var gym = annotation as Gym;
            if(gym != null)
            {
				annotateView = mapView.DequeueReusableAnnotation("Gym") ?? new MKAnnotationView(gym, "Gym");
				annotateView.Image = UIImage.FromBundle($"gym{(int)gym.team}");
				annotateView.Frame = new CGRect(0, 0, 40, 40);

                var stack = new UIStackView(new CGRect(0, 0, 200, 200));
                stack.Axis = UILayoutConstraintAxis.Vertical;
                stack.Spacing = 3.0f;
                var line1 = new UILabel();
                line1.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
                line1.Text = $"Last modified {Utility.TimeAgo(gym.LastModifedDate)}";
                stack.AddArrangedSubview(line1);
				var line2 = new UILabel();
				line2.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
                line2.Text = $"Slots Available: {gym.slots_available}";
                stack.AddArrangedSubview(line2);
				var line3 = new UILabel();
				line3.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
                line3.Text = $"Guarding Pokemon: {gym.pokemon_name}({gym.pokemon_id})";
				stack.AddArrangedSubview(line3);
				if (mapView.UserLocation != null)
				{
					var dist = mapView.UserLocation.Location.DistanceFrom(new CLLocation(gym.lat, gym.lon));
					var distMiles = dist * 0.00062137;
					var distLabel = new UILabel();
					distLabel.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
					distLabel.Text = $"{distMiles.ToString("F1")} miles away";
                    stack.AddArrangedSubview(distLabel);
				}
                annotateView.DetailCalloutAccessoryView = stack;
				annotateView.CanShowCallout = true;
            }

            var raid = annotation as Raid;
            if(raid != null)
            {
                annotateView = mapView.DequeueReusableAnnotation("Raid") ?? new RaidAnnotationView(annotation, "Raid");
                var raidAV = annotateView as RaidAnnotationView;
                raidAV.Raid = raid;
				raidAV.Frame = new CGRect(0, 0, 40, 55);
				raidAV.UpdateTime(DateTime.Now);
            }
            return annotateView;
        }

        void HandleTimerCallback(object state)
        {
            InvokeOnMainThread(() =>
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var pokes = map.Annotations.OfType<Pokemon>().Where(p => p.ExpiresDate < now);
                    map.RemoveAnnotations(pokes.ToArray());
                    Console.WriteLine($"Removed {pokes.Count()} pokemon");
                    var annotations = map.GetAnnotations(map.VisibleMapRect);
                    foreach (var a in annotations)
                    {
                        var a2 = a as IMKAnnotation;
                        if (a is Pokemon || a is Raid)
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
            
            InvokeOnMainThread(async () => 
            {
				try
				{
					await ServiceLayer.SharedInstance.LoadData();
				}
				catch (Exception)
				{
					//swallow exception because it tastes good
				}
                if (ServiceLayer.SharedInstance.LayersEnabled[0])
                {
                    var onMap = map.Annotations.OfType<Pokemon>();
                    var toAdd = ServiceLayer.SharedInstance.Pokemon.Where(p => !p.trash).Except(onMap);
                    Console.WriteLine($"Adding {toAdd.Count()} mons to the map");
                    map.AddAnnotations(toAdd.ToArray());
                }
                var gymsOnMap = map.Annotations.OfType<Gym>();
                if(gymsOnMap.Count() == 0 && ServiceLayer.SharedInstance.LayersEnabled[1])
                {
                    map.AddAnnotations(ServiceLayer.SharedInstance.Gyms.Values.ToArray());
                    Console.WriteLine($"Adding {ServiceLayer.SharedInstance.Gyms.Count()} gyms to the map");
                }

				if (ServiceLayer.SharedInstance.LayersEnabled[2])
				{
                    var raidsOnMap = map.Annotations.OfType<Raid>();
                    var toAdd = ServiceLayer.SharedInstance.Raids.Values.Except(raidsOnMap);
                    foreach (var r in toAdd)
                    {
                        Raid thisRaid;
                        if(ServiceLayer.SharedInstance.Raids.TryGetValue(r.id, out thisRaid))
                        {
                            map.AddAnnotation(thisRaid);
                        }
                        //map.AddAnnotations(toAdd.ToArray());
                    }
					Console.WriteLine($"Adding {toAdd.Count()} raids to the map");
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
            } catch(Exception)
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
                } catch(Exception)
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

        void LayerSelectButton_TouchUpInside(object sender, EventArgs e)
        {
            if (layersTableVC == null)
            {
                layersTableVC = new UITableViewController();
                layersTableVC.ModalPresentationStyle = UIModalPresentationStyle.Popover;
                layersTableVC.PreferredContentSize = new CGSize(200, 200);
                layersTableVC.TableView.DataSource = this;
                layersTableVC.TableView.Delegate = this;
            }

            layersTableVC.PopoverPresentationController.Delegate = this;
            layersTableVC.PopoverPresentationController.SourceView = layerSelectButton;
            layersTableVC.PopoverPresentationController.PermittedArrowDirections = UIPopoverArrowDirection.Any;
            layersTableVC.PopoverPresentationController.SourceRect = new CGRect(0, 0, 30.0f, 30.0f);
            PresentViewController(layersTableVC, true, null);

        }

        public nint RowsInSection(UITableView tableView, nint section)
        {
            return Layers.Length;
        }

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = new UITableViewCell(UITableViewCellStyle.Default, "cell");
			cell.TextLabel.Text = Layers[indexPath.Row];
			cell.Accessory = ServiceLayer.SharedInstance.LayersEnabled[indexPath.Row] ? UITableViewCellAccessory.Checkmark : UITableViewCellAccessory.None;
			return cell;
        }

        [Export("tableView:didSelectRowAtIndexPath:")]
        public void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
			ServiceLayer.SharedInstance.LayersEnabled[indexPath.Row] = !ServiceLayer.SharedInstance.LayersEnabled[indexPath.Row];
            var layers = ServiceLayer.SharedInstance.LayersEnabled.Select(l => l.ToString()).ToArray();
            NSUserDefaults.StandardUserDefaults.SetValueForKey(NSArray.FromStrings(layers), new NSString("layers"));
            NSUserDefaults.StandardUserDefaults.Synchronize();
			tableView.DeselectRow(indexPath, true);
            tableView.ReloadData();
            switch(indexPath.Row)
            {
                case 0:
                    if (!ServiceLayer.SharedInstance.LayersEnabled[indexPath.Row])
                    {
                        var pokesOnMap = map.Annotations.OfType<Pokemon>();
                        map.RemoveAnnotations(pokesOnMap.ToArray());
                    } else
                    {
						map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Where(p => !p.trash).ToArray());
                    }
                    break;
                case 1:
                    if(!ServiceLayer.SharedInstance.LayersEnabled[indexPath.Row])
                    {
						var gymsOnMap = map.Annotations.OfType<Gym>();
						map.RemoveAnnotations(gymsOnMap.ToArray());
                    } else
                    {
                        map.AddAnnotations(ServiceLayer.SharedInstance.Gyms.Values.ToArray());
                    }
                    break;
                case 2:
					if (!ServiceLayer.SharedInstance.LayersEnabled[indexPath.Row])
					{
						var raidsOnMap = map.Annotations.OfType<Raid>();
                        map.RemoveAnnotations(raidsOnMap.ToArray());
					}
					else
					{
						map.AddAnnotations(ServiceLayer.SharedInstance.Raids.Values.ToArray());
					}
                    break;
                case 3:
                    if (!ServiceLayer.SharedInstance.LayersEnabled[indexPath.Row])
                    {
                        var trashOnMap = map.Annotations.OfType<Pokemon>().Where(p => p.trash);
                        map.RemoveAnnotations(trashOnMap.ToArray());
                    } else 
                    {
                        map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Where(p => p.trash).ToArray());
                    }
                    break;
            }
            layersTableVC.DismissViewController(true, () => { refreshMap(null); });
        }

        [Export("adaptivePresentationStyleForPresentationController:traitCollection:")]
        public UIModalPresentationStyle GetAdaptivePresentationStyle(UIPresentationController controller, UITraitCollection traitCollection)
        {
            return UIModalPresentationStyle.None;
        }
    }
}
