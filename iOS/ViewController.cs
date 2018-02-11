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
        string[] Layers = { "Enabled Pokémon", "Gyms", "Raids", "All 90+ IV Pokémon" };//, "All Perfect Pokémon"};
        UITableViewController layersTableVC = null;
        bool mapLoaded = false;
        bool timersVisible = true;
        bool timersPreviouslyVisible = true;
        string notifyID = "";
        UserSettings settings => ServiceLayer.SharedInstance.Settings;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();
            loader.StartAnimating();
            var app = UIApplication.SharedApplication.Delegate as AppDelegate;
            await ServiceLayer.SharedInstance.InitalizeSettings();
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
            app.MigrageUserSettings();

            var credentialsVerified = settings.Username != "";
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
            await ServiceLayer.SharedInstance.SaveSettings();
            await ServiceLayer.SharedInstance.LoadData();
            loader.StopAnimating();
            map.Delegate = this;
            if (settings.PokemonEnabled)
            {
                map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Values.Where(p => !settings.PokemonTrash.Contains(p.pokemon_id)).ToArray());
            }
            if(settings.NinetyOnlyEnabled)
            {
                map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Values.Where(p => p.iv > 0.9f).ToArray());
            }
            map.AddAnnotations(ServiceLayer.SharedInstance.Gyms.Values.ToArray());
            var toAdd = new List<Raid>(ServiceLayer.SharedInstance.Raids);
            if (!settings.LegondaryRaids)
            {
                toAdd.RemoveAll(r => r.level == 5);
            }
            if (!settings.Level4Raids)
            {
                toAdd.RemoveAll(r => r.level == 4);
            }
            if (!settings.Level3Raids)
            {
                toAdd.RemoveAll(r => r.level == 3);
            }
            if (!settings.Level2Raids)
            {
                toAdd.RemoveAll(r => r.level == 2);
            }
            if (!settings.Level1Raids)
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
                if(pokemon.iv > 0.99f)
                {
                    pokeAV.LabelColor = UIColor.FromRGB(175, 116, 232);
                }
                else if (pokemon.iv > 0.9f)
                {
                    pokeAV.LabelColor = UIColor.FromRGB(106, 175, 106);
                }
                else
                {
                    pokeAV.LabelColor = UIColor.LightGray;
                }
                annotateView.CanShowCallout = true;
            }
            var gym = annotation as Gym;
            if(gym != null)
            {
				annotateView = mapView.DequeueReusableAnnotation("Gym") ?? new GymAnnotationView(gym, "Gym");
                var gymAV = annotateView as GymAnnotationView;
                gymAV.Image = UIImage.FromBundle($"gym{(int)gym.team}");
                gymAV.Frame = new CGRect(0, 0, 40, 40);
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
            var av = annotateView as MapCountdownAnnotationView;
            if(av != null)
            {
                av.TimerVisible = timersVisible;
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
                    var pokes = ServiceLayer.SharedInstance.Pokemon.Values.Where(p => p.ExpiresDate < now);
                    map.RemoveAnnotations(pokes.ToArray());
                    var raids = ServiceLayer.SharedInstance.Raids.Where(p => p.TimeEnd < now || (p.TimeBattle < now && p.pokemon_id == 0));
					map.RemoveAnnotations(raids.ToArray());
                    Console.WriteLine($"Removed {pokes.Count()} pokemon and {raids.Count()} raids");
                    var annotations = map.GetAnnotations(map.VisibleMapRect);
                    timersVisible = annotations.Count() < 100;
                    if(!timersVisible && timersPreviouslyVisible)
                    {
                        timersPreviouslyVisible = false;
                        foreach (var a in map.Annotations)
                        {
                            var a2 = a as IMKAnnotation;
                            if (a is Pokemon || a is Raid)
                            {
                                var annotateView = map.ViewForAnnotation(a2) as MapCountdownAnnotationView;
                                if (annotateView != null)
                                {
                                    annotateView.TimerVisible = timersVisible;
                                }
                            }
                        }
                    } else if(timersVisible)
                    {
                        timersPreviouslyVisible = true;
                        foreach (var a in annotations)
                        {
                            var a2 = a as IMKAnnotation;
                            if (a is Pokemon || a is Raid)
                            {
                                var annotateView = map.ViewForAnnotation(a2) as MapCountdownAnnotationView;
                                if (annotateView != null)
                                {
                                    annotateView.UpdateTime(now);
                                    annotateView.TimerVisible = timersVisible;
                                }
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
                    await ServiceLayer.SharedInstance.LoadData();
				}
				catch (Exception)
				{
					//swallow exception because it tastes good
				}
                if (settings.PokemonEnabled || settings.NotifyEnabled)
                {
                    var onMap = map.Annotations.OfType<Pokemon>();
                    var toRemove = onMap.Where(p => p.ExpiresDate < DateTime.UtcNow);
                    map.RemoveAnnotations(toRemove.ToArray());
                    var onMapRaids = map.Annotations.OfType<Raid>();
                    var toRemoveRaids = onMapRaids.Where(r => r.TimeEnd < DateTime.UtcNow);
                    map.RemoveAnnotations(toRemoveRaids.ToArray());
                    List<Pokemon> toAdd = new List<Pokemon>();
                    if(settings.NinetyOnlyEnabled)
                    {
                        toAdd.AddRange(ServiceLayer.SharedInstance.Pokemon.Values.Where(p => p.iv > 0.9).Except(onMap));
                    }
                    if(settings.PokemonEnabled)
                    {
                        toAdd.AddRange(ServiceLayer.SharedInstance.Pokemon.Values.Where(p => !settings.PokemonTrash.Contains(p.pokemon_id)).Except(onMap));
                    }
                    Console.WriteLine($"Adding {toAdd.Count()} mons to the map");
                    map.AddAnnotations(toAdd.ToArray());
                }

                if(settings.GymsEnabled)
                {
                    var gymsOnMap = map.Annotations.OfType<Gym>();
                    if (gymsOnMap.Count() > 0)
                    {
                        map.RemoveAnnotations(gymsOnMap.ToArray());
                    }
                    map.AddAnnotations(ServiceLayer.SharedInstance.Gyms.Values.ToArray());
                }
                if (settings.RaidsEnabled)
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
                    if(!settings.LegondaryRaids)
                    {
                        toAdd.RemoveAll(r => r.level == 5);
                    }
                    if (!settings.Level4Raids)
                    {
                        toAdd.RemoveAll(r => r.level == 4);
                    }
                    if (!settings.Level3Raids)
                    {
                        toAdd.RemoveAll(r => r.level == 3);
                    }
                    if (!settings.Level2Raids)
                    {
                        toAdd.RemoveAll(r => r.level == 2);
                    }
                    if (!settings.Level1Raids)
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
            settings.Username = username.Text;
            settings.Password = password.Text;
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
                layersTableVC.PreferredContentSize = new CGSize(230, 200);
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
            bool enabled = false;
            enabled = settings.PokemonEnabled && indexPath.Row == 0;
            enabled = settings.GymsEnabled && indexPath.Row == 1 || enabled;
            enabled = settings.RaidsEnabled && indexPath.Row == 2 || enabled;
            enabled = settings.NinetyOnlyEnabled && indexPath.Row == 3 || enabled;
            cell.Accessory = enabled ? UITableViewCellAccessory.Checkmark : UITableViewCellAccessory.None;
			return cell;
        }

        [Export("tableView:didSelectRowAtIndexPath:")]
        public async void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            switch(indexPath.Row)
            {
                case 0:
                    settings.PokemonEnabled = !settings.PokemonEnabled;
                    if(settings.PokemonEnabled)
                    {
                        var pokesOnMap = map.Annotations.OfType<Pokemon>();
                        var toAdd = ServiceLayer.SharedInstance.Pokemon.Values.Where(p => !settings.PokemonTrash.Contains(p.pokemon_id)).Except(pokesOnMap);
                        map.AddAnnotations(toAdd.ToArray());
                    } else
                    {
                        var pokesOnMap = map.Annotations.OfType<Pokemon>();
                        if(settings.NinetyOnlyEnabled)
                        {
                            pokesOnMap = pokesOnMap.Except(pokesOnMap.Where(p => p.iv > 0.9f));
                        }
                        map.RemoveAnnotations(pokesOnMap.ToArray());
                    }
                    break;
                case 1:
                    settings.GymsEnabled = !settings.GymsEnabled;
                    if(!settings.GymsEnabled)
                    {
						var gymsOnMap = map.Annotations.OfType<Gym>();
						map.RemoveAnnotations(gymsOnMap.ToArray());
                    } else
                    {
                        map.AddAnnotations(ServiceLayer.SharedInstance.Gyms.Values.ToArray());
                    }
                    break;
                case 2:
                    settings.RaidsEnabled = !settings.RaidsEnabled;
                    if (!settings.RaidsEnabled)
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
                    settings.NinetyOnlyEnabled = !settings.NinetyOnlyEnabled;
                    if(settings.NinetyOnlyEnabled)
                    {
                        var pokesOnMap = map.Annotations.OfType<Pokemon>();
                        var toAdd = ServiceLayer.SharedInstance.Pokemon.Values.Where(p => p.iv > 0.9f).Except(pokesOnMap);
                        map.AddAnnotations(toAdd.ToArray());
                    } else 
                    {
                        var toRemove = map.Annotations.OfType<Pokemon>().Where(p => p.iv > 0.9f);
                        if(settings.PokemonEnabled)
                        {
                            toRemove = toRemove.Except(toRemove.Where(p => !settings.PokemonTrash.Contains(p.pokemon_id)));
                        }
                        map.RemoveAnnotations(toRemove.ToArray());
                    }
                    break;
            }
            tableView.DeselectRow(indexPath, true);
            tableView.ReloadData();
            await ServiceLayer.SharedInstance.SaveSettings();
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
            var settingsVC = nav.TopViewController as SettingsViewController;
            if(settingsVC != null)
            {
                settingsVC.ParentVC = this;
            }
        }

        public void TrashAdded(List<int> trash)
        {
            var toRemove = map.Annotations.OfType<Pokemon>().Where(p => trash.Contains(p.pokemon_id)).ToArray();
            map.RemoveAnnotations(toRemove);
        }

        public void TrashRemoved(List<int> notTrash)
        {
            var onMap = map.Annotations.OfType<Pokemon>().Where(p => notTrash.Contains(p.pokemon_id));
            var toAdd = ServiceLayer.SharedInstance.Pokemon.Values.Where(p => notTrash.Contains(p.pokemon_id)).ToList();
            var add = toAdd.Except(onMap).ToArray();
            map.AddAnnotations(add);
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
                settings.PokemonEnabled = true;
                MKCoordinateSpan span = new MKCoordinateSpan(Utility.MilesToLatitudeDegrees(0.7), Utility.MilesToLongitudeDegrees(0.7, lat));
                var coords = new CLLocationCoordinate2D(lat, lon);
                var reg = new MKCoordinateRegion(coords, span);
                map.SetRegion(reg, true);
                await ServiceLayer.SharedInstance.LoadData();
                var poke = map.Annotations.OfType<Pokemon>().Where(p => p.id == pokemonID).FirstOrDefault();
                if (poke != null)
                {
                    map.SelectAnnotation(poke, true);
                }

            }
        }

        public async Task ApplySettings()
        {
            await ServiceLayer.SharedInstance.SaveSettings();
            if(settings.RaidsEnabled)
            {
                var l5 = map.Annotations.OfType<Raid>().Where(r => r.level == 5);
                var l4 = map.Annotations.OfType<Raid>().Where(r => r.level == 4);
                var l3 = map.Annotations.OfType<Raid>().Where(r => r.level == 3);
                var l2 = map.Annotations.OfType<Raid>().Where(r => r.level == 2);
                var l1 = map.Annotations.OfType<Raid>().Where(r => r.level == 1);
                if(settings.LegondaryRaids && l5.Count() == 0)
                {
                    var removeRaids = ServiceLayer.SharedInstance.Raids.Where(r => r.level == 5);
                    map.AddAnnotations(removeRaids.ToArray());
                } else if(!settings.LegondaryRaids && l5.Count() != 0)
                {
                    map.RemoveAnnotations(l5.ToArray());
                }

                if (settings.Level4Raids && l4.Count() == 0)
                {
                    var removeRaids = ServiceLayer.SharedInstance.Raids.Where(r => r.level == 4);
                    map.AddAnnotations(removeRaids.ToArray());
                }
                else if (!settings.Level4Raids && l4.Count() != 0)
                {
                    map.RemoveAnnotations(l4.ToArray());
                }

                if (settings.Level3Raids && l3.Count() == 0)
                {
                    var removeRaids = ServiceLayer.SharedInstance.Raids.Where(r => r.level == 3);
                    map.AddAnnotations(removeRaids.ToArray());
                }
                else if (!settings.Level3Raids && l3.Count() != 0)
                {
                    map.RemoveAnnotations(l3.ToArray());
                }

                if (settings.Level2Raids && l2.Count() == 0)
                {
                    var removeRaids = ServiceLayer.SharedInstance.Raids.Where(r => r.level == 2);
                    map.AddAnnotations(removeRaids.ToArray());
                }
                else if (!settings.Level2Raids && l2.Count() != 0)
                {
                    map.RemoveAnnotations(l2.ToArray());
                }

                if (settings.Level1Raids && l1.Count() == 0)
                {
                    var removeRaids = ServiceLayer.SharedInstance.Raids.Where(r => r.level == 1);
                    map.AddAnnotations(removeRaids.ToArray());
                }
                else if (!settings.Level1Raids && l1.Count() != 0)
                {
                    map.RemoveAnnotations(l1.ToArray());
                }
            }
        }

    }
}
