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
using System.Collections.Generic;
using Microsoft.AppCenter.Push;
using MoreLinq;

namespace OMAPGMap.iOS
{
    public partial class ViewController : UIViewController, IMKMapViewDelegate, IUITableViewDelegate, IUITableViewDataSource, IUIPopoverPresentationControllerDelegate
    {
        CLLocationManager locationManager;
        Timer secondTimer;
        Timer minuteTimer;
        string[] Layers = { "Pokemon", "Gyms", "Raids"};
        UITableViewController layersTableVC = null;
        int lastId = 0;
        bool mapLoaded = false;
        string notifyID = "";

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();
            loader.StartAnimating();
            var app = UIApplication.SharedApplication.Delegate as AppDelegate;
            app.MonitorBackgroundLocation();
            locationManager = new CLLocationManager();
            locationManager.RequestAlwaysAuthorization();
            locationManager.RequestWhenInUseAuthorization();
            locationManager.LocationsUpdated += LocationManager_LocationsUpdated;
            locationManager.StartUpdatingLocation();
            map.ShowsUserLocation = true;
            CLLocationCoordinate2D coords = new CLLocationCoordinate2D(41.2524, -95.9980);
            MKCoordinateSpan span = new MKCoordinateSpan(Utility.MilesToLatitudeDegrees(2), Utility.MilesToLongitudeDegrees(2, coords.Latitude));
            map.Region = new MKCoordinateRegion(coords, span);
            var credentialsVerified = ServiceLayer.SharedInstance.Username != "";
            username.Alpha = credentialsVerified ? 0.0f : 1.0f;
            password.Alpha = credentialsVerified ? 0.0f : 1.0f;
            signInButton.Alpha = credentialsVerified ? 0.0f : 1.0f;
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
            if (!credentialsVerified)
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
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    errorLabel.Hidden = false;
                    tryAgainButton.Hidden = false;
                } //swallow exception
                if (loggedIn)
                {
                    signInButton.Alpha = 0.0f;
                    await LoggedIn();
                }
                else
                {
                    if(credentialsVerified)
                    {
                        loadingLabel.Alpha = 0.0f;
                        errorLabel.Hidden = false;
                        tryAgainButton.Hidden = false;
                    } else {
                        username.BecomeFirstResponder();
                    }
					loader.Alpha = 0.0f;
                }
            }

            signInButton.TouchUpInside += SignInButton_TouchUpInside;
            layerSelectButton.TouchUpInside += LayerSelectButton_TouchUpInside;
            tryAgainButton.TouchUpInside += async (sender, e) => 
            {
                var loggedIn = false;
                try
                {
                    loggedIn = await ServiceLayer.SharedInstance.VerifyCredentials();
                }
                catch (Exception e2)
                {
                    Console.WriteLine(e2);
                    errorLabel.Hidden = false;
                    tryAgainButton.Hidden = false;
                } //swallow exception
                if (loggedIn)
                {
                    signInButton.Alpha = 0.0f;
                    await LoggedIn();
                }
            };
            currentLocationButton.TouchUpInside += (sender, e) => 
            {
                if (map.UserLocation != null)
                {
                    map.SetCenterCoordinate(map.UserLocation.Coordinate, true);
                }
            };
        }   

        private async Task LoggedIn()
        {
            lastId = (int) NSUserDefaults.StandardUserDefaults.IntForKey("LastId");
            await ServiceLayer.SharedInstance.LoadData(lastId);
            loader.StopAnimating();
            map.Delegate = this;
            map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Where(p => !ServiceLayer.SharedInstance.PokemonTrash.Contains(p.pokemon_id)).ToArray());
            map.AddAnnotations(ServiceLayer.SharedInstance.Gyms.Values.ToArray());
            var toAdd = new List<Raid>(ServiceLayer.SharedInstance.Raids);
            if (!ServiceLayer.SharedInstance.LegondaryRaids)
            {
                toAdd.RemoveAll(r => r.level == 5);
            }
            if (!ServiceLayer.SharedInstance.Level4Raids)
            {
                toAdd.RemoveAll(r => r.level == 4);
            }
            if (!ServiceLayer.SharedInstance.Level3Raids)
            {
                toAdd.RemoveAll(r => r.level == 3);
            }
            if (!ServiceLayer.SharedInstance.Level2Raids)
            {
                toAdd.RemoveAll(r => r.level == 2);
            }
            if (!ServiceLayer.SharedInstance.Level1Raids)
            {
                toAdd.RemoveAll(r => r.level == 1);
            }
            map.AddAnnotations(toAdd.ToArray());
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
            mapLoaded = true;
            Push.PushNotificationReceived += async (sender, e) => {

                // Add the notification message and title to the message
                var summary = $"Push notification received:" +
                                    $"\n\tNotification title: {e.Title}" +
                                    $"\n\tMessage: {e.Message}";
                // If there is custom data associated with the notification,
                // print the entries
                if (e.CustomData != null)
                {
                    var pokeID = e.CustomData["pokemon_id"];
                    var expires = long.Parse(e.CustomData["expires"]);
                    var lat = float.Parse(e.CustomData["lat"]);
                    var lon = float.Parse(e.CustomData["lon"]);
                    var expiresDate = Utility.FromUnixTime(expires);
                    Console.WriteLine($"opened with ID of {pokeID}");
                    if (mapLoaded)
                    {
                        await NotificationLaunched(pokeID, expiresDate, lat, lon);
                    }
                }

                // Send the notification summary to debug output
                System.Diagnostics.Debug.WriteLine(summary);
            };
            var app = UIApplication.SharedApplication.Delegate as AppDelegate;
            if(app.LaunchedNotification)
            {
                await NotificationLaunched(app.LaunchPokemon, app.LaunchExpires, app.LaunchLat, app.LaunchLon);
            }
        }

        void LocationManager_LocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            locationManager.StopUpdatingLocation();
            if(!map.UserLocationVisible)
            {
                CLLocationCoordinate2D coords = e.Locations[0].Coordinate;
				MKCoordinateSpan span = new MKCoordinateSpan(Utility.MilesToLatitudeDegrees(2), Utility.MilesToLongitudeDegrees(2, coords.Latitude));
                map.SetRegion(new MKCoordinateRegion(coords, span), true);
                ServiceLayer.SharedInstance.UserLocation = new Coordinates(coords.Latitude, coords.Longitude);
            }
            var app = UIApplication.SharedApplication.Delegate as AppDelegate;
            app.CurrentLocation = e.Locations.First();
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
                pokeAV.Map = mapView;
                pokeAV.ParentVC = this;
                annotateView.CanShowCallout = true;
            }
            var gym = annotation as Gym;
            if(gym != null)
            {
				annotateView = mapView.DequeueReusableAnnotation("Gym") ?? new GymAnnotationView(gym, "Gym");
				annotateView.Image = UIImage.FromBundle($"gym{(int)gym.team}");
				annotateView.Frame = new CGRect(0, 0, 40, 40);
                var gymAV = annotateView as GymAnnotationView;
                gymAV.Map = mapView;

				annotateView.CanShowCallout = true;
            }

            var raid = annotation as Raid;
            if(raid != null)
            {
                annotateView = mapView.DequeueReusableAnnotation("Raid") ?? new RaidAnnotationView(annotation, "Raid");
                var raidAV = annotateView as RaidAnnotationView;
                raidAV.Raid = raid;
				raidAV.Frame = new CGRect(0, 0, 40, 55);
                raidAV.Map = mapView;
				raidAV.UpdateTime(DateTime.Now);
                annotateView.CanShowCallout = true;
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
                    var pokes = ServiceLayer.SharedInstance.Pokemon.Where(p => p.ExpiresDate < now);
                    map.RemoveAnnotations(pokes.ToArray());
                    var raids = ServiceLayer.SharedInstance.Raids.Where(p => p.TimeEnd < now || (p.TimeBattle < now && p.pokemon_id == 0));
					map.RemoveAnnotations(raids.ToArray());
                    Console.WriteLine($"Removed {pokes.Count()} pokemon and {raids.Count()} raids");
                    var annotations = map.GetAnnotations(map.VisibleMapRect);
                    foreach (var a in annotations)
                    {
                        var a2 = a as IMKAnnotation;
                        if (a is Pokemon || a is Raid)
                        {
                            var annotateView = map.ViewForAnnotation(a2) as MapCountdownAnnotationView;
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
                activity.StartAnimating();
				try
				{
                    await ServiceLayer.SharedInstance.LoadData(lastId);
				}
				catch (Exception)
				{
					//swallow exception because it tastes good
				}
                if (ServiceLayer.SharedInstance.LayersEnabled[0])
                {
                    if (ServiceLayer.SharedInstance.Pokemon.Count() > 0)
                    {
                        lastId = ServiceLayer.SharedInstance.Pokemon.MaxBy(p => p?.idValue)?.idValue ?? lastId;
                        var l = ServiceLayer.SharedInstance.Pokemon.MinBy(p => p?.idValue)?.idValue ?? 0;
                        NSUserDefaults.StandardUserDefaults.SetInt(l, "LastId");
                        NSUserDefaults.StandardUserDefaults.Synchronize();
                    }
                    var onMap = map.Annotations.OfType<Pokemon>();
                    var toRemove = onMap.Where(p => p.ExpiresDate < DateTime.UtcNow);
                    map.RemoveAnnotations(toRemove.ToArray());
                    var onMapRaids = map.Annotations.OfType<Raid>();
                    var toRemoveRaids = onMapRaids.Where(r => r.TimeEnd < DateTime.UtcNow);
                    map.RemoveAnnotations(toRemoveRaids.ToArray());
                    var toAdd = ServiceLayer.SharedInstance.Pokemon.Where(p => !ServiceLayer.SharedInstance.PokemonTrash.Contains(p.pokemon_id)).Except(onMap);
                    Console.WriteLine($"Adding {toAdd.Count()} mons to the map");
                    map.AddAnnotations(toAdd.ToArray());
                }

                if(ServiceLayer.SharedInstance.LayersEnabled[1])
                {
                    var gymsOnMap = map.Annotations.OfType<Gym>();
                    if (gymsOnMap.Count() > 0)
                    {
                        map.RemoveAnnotations(gymsOnMap.ToArray());
                    }
                    map.AddAnnotations(ServiceLayer.SharedInstance.Gyms.Values.ToArray());
                }
				if (ServiceLayer.SharedInstance.LayersEnabled[2])
				{
                    var raidsOnMap = map.Annotations.OfType<Raid>().ToList();
                    var unhatched = raidsOnMap.Where(r => r.pokemon_id == 0);
                    var haveHatched = unhatched.Where((Raid r) =>
                    {
                        return ServiceLayer.SharedInstance.Raids.Any(r2 => r2.id.Equals(r.id)) && r.pokemon_id != 0;
                    });
                    map.RemoveAnnotations(haveHatched.ToArray());
                    raidsOnMap.RemoveAll(r => haveHatched.Contains(r));
                    var toAdd = ServiceLayer.SharedInstance.Raids.Except(raidsOnMap).ToList();
                    if(!ServiceLayer.SharedInstance.LegondaryRaids)
                    {
                        toAdd.RemoveAll(r => r.level == 5);
                    }
                    if (!ServiceLayer.SharedInstance.Level4Raids)
                    {
                        toAdd.RemoveAll(r => r.level == 4);
                    }
                    if (!ServiceLayer.SharedInstance.Level3Raids)
                    {
                        toAdd.RemoveAll(r => r.level == 3);
                    }
                    if (!ServiceLayer.SharedInstance.Level2Raids)
                    {
                        toAdd.RemoveAll(r => r.level == 2);
                    }
                    if (!ServiceLayer.SharedInstance.Level1Raids)
                    {
                        toAdd.RemoveAll(r => r.level == 1);
                    }
                    map.AddAnnotations(toAdd.ToArray());
					Console.WriteLine($"Adding {toAdd.Count()} raids to the map");
				}
                activity.StopAnimating();
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
                errorMessage = "An error occured when attempting to log in - server may be down.";
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
						map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Where(p => !ServiceLayer.SharedInstance.PokemonTrash.Contains(p.pokemon_id)).ToArray());
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
						map.AddAnnotations(ServiceLayer.SharedInstance.Raids.ToArray());
					}
                    break;
                case 3:
                    if (!ServiceLayer.SharedInstance.LayersEnabled[indexPath.Row])
                    {
                        var trashOnMap = map.Annotations.OfType<Pokemon>().Where(p => !ServiceLayer.SharedInstance.PokemonTrash.Contains(p.pokemon_id));
                        map.RemoveAnnotations(trashOnMap.ToArray());
                    } else 
                    {
                        map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Where(p => !ServiceLayer.SharedInstance.PokemonTrash.Contains(p.pokemon_id)).ToArray());
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

        public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
        {
            base.PrepareForSegue(segue, sender);
            var nav = segue.DestinationViewController as UINavigationController;
            if(UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
            {
                nav.ModalPresentationStyle = UIModalPresentationStyle.FormSheet;
            }
            var settings = nav.TopViewController as SettingsViewController;
            if(settings != null)
            {
                settings.ParentVC = this;
            }
        }

        public void TrashAdded(List<int> trash)
        {
            var toRemove = map.Annotations.OfType<Pokemon>().Where(p => trash.Contains(p.pokemon_id)).ToArray();
            map.RemoveAnnotations(toRemove);
            SaveTrashSettings();
        }

        public void TrashRemoved(List<int> notTrash)
        {
            var onMap = map.Annotations.OfType<Pokemon>().Where(p => notTrash.Contains(p.pokemon_id));
            var toAdd = ServiceLayer.SharedInstance.Pokemon.Where(p => notTrash.Contains(p.pokemon_id)).ToList();
            var add = toAdd.Except(onMap).ToArray();
            map.AddAnnotations(add);
            SaveTrashSettings();
        }

        public void SaveTrashSettings()
        {
			var trashStrings = ServiceLayer.SharedInstance.PokemonTrash.Select(t => t.ToString()).ToArray();
			var tosave = NSArray.FromStrings(trashStrings);
			NSUserDefaults.StandardUserDefaults.SetValueForKey(tosave, new NSString("trash"));
        }

        public async Task NotificationLaunched(string pokemonID, DateTime expires, float lat, float lon)
        {
            if (expires < DateTime.UtcNow)
            {
                var alert = UIAlertController.Create("Pokemon expired", "Looks like that Pokemon has despawned.", UIAlertControllerStyle.Alert);
                alert.AddAction(UIAlertAction.Create("Ok", UIAlertActionStyle.Default, null));
                PresentViewController(alert, true, null);
            }
            else
            {
                ServiceLayer.SharedInstance.LayersEnabled[0] = true;
                MKCoordinateSpan span = new MKCoordinateSpan(Utility.MilesToLatitudeDegrees(0.7), Utility.MilesToLongitudeDegrees(0.7, lat));
                var coords = new CLLocationCoordinate2D(lat, lon);
                var reg = new MKCoordinateRegion(coords, span);
                map.SetRegion(reg, true);
                await ServiceLayer.SharedInstance.LoadData(lastId);
                var poke = map.Annotations.OfType<Pokemon>().Where(p => p.id == pokemonID).FirstOrDefault();
                if (poke != null)
                {
                    map.SelectAnnotation(poke, true);
                }

            }
        }

        public void ApplySettings()
        {
            if(ServiceLayer.SharedInstance.LayersEnabled[2])
            {
                var l5 = map.Annotations.OfType<Raid>().Where(r => r.level == 5);
                var l4 = map.Annotations.OfType<Raid>().Where(r => r.level == 4);
                var l3 = map.Annotations.OfType<Raid>().Where(r => r.level == 3);
                var l2 = map.Annotations.OfType<Raid>().Where(r => r.level == 2);
                var l1 = map.Annotations.OfType<Raid>().Where(r => r.level == 1);
                if(ServiceLayer.SharedInstance.LegondaryRaids && l5.Count() == 0)
                {
                    var removeRaids = ServiceLayer.SharedInstance.Raids.Where(r => r.level == 5);
                    map.AddAnnotations(removeRaids.ToArray());
                } else if(!ServiceLayer.SharedInstance.LegondaryRaids && l5.Count() != 0)
                {
                    map.RemoveAnnotations(l5.ToArray());
                }

                if (ServiceLayer.SharedInstance.Level4Raids && l4.Count() == 0)
                {
                    var removeRaids = ServiceLayer.SharedInstance.Raids.Where(r => r.level == 4);
                    map.AddAnnotations(removeRaids.ToArray());
                }
                else if (!ServiceLayer.SharedInstance.Level4Raids && l4.Count() != 0)
                {
                    map.RemoveAnnotations(l4.ToArray());
                }

                if (ServiceLayer.SharedInstance.Level3Raids && l3.Count() == 0)
                {
                    var removeRaids = ServiceLayer.SharedInstance.Raids.Where(r => r.level == 3);
                    map.AddAnnotations(removeRaids.ToArray());
                }
                else if (!ServiceLayer.SharedInstance.Level3Raids && l3.Count() != 0)
                {
                    map.RemoveAnnotations(l3.ToArray());
                }

                if (ServiceLayer.SharedInstance.Level2Raids && l2.Count() == 0)
                {
                    var removeRaids = ServiceLayer.SharedInstance.Raids.Where(r => r.level == 2);
                    map.AddAnnotations(removeRaids.ToArray());
                }
                else if (!ServiceLayer.SharedInstance.Level2Raids && l2.Count() != 0)
                {
                    map.RemoveAnnotations(l2.ToArray());
                }

                if (ServiceLayer.SharedInstance.Level1Raids && l1.Count() == 0)
                {
                    var removeRaids = ServiceLayer.SharedInstance.Raids.Where(r => r.level == 1);
                    map.AddAnnotations(removeRaids.ToArray());
                }
                else if (!ServiceLayer.SharedInstance.Level1Raids && l1.Count() != 0)
                {
                    map.RemoveAnnotations(l1.ToArray());
                }
            }
        }
    }
}
