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
using MoreLinq;

namespace OMAPGMap.iOS
{
    public partial class ViewController : UIViewController, IMKMapViewDelegate, IUITableViewDelegate, IUITableViewDataSource, IUIPopoverPresentationControllerDelegate
    {
        CLLocationManager locationManager;
        Timer secondTimer;
        Timer minuteTimer;
        string[] Layers = { "Pokemon", "Gyms", "Raids", "Trash" };
        UITableViewController layersTableVC = null;
        nint lastId = 0;

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
                catch(Exception e)
                {
                    Console.WriteLine(e);
                } //swallow exception
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
            lastId = NSUserDefaults.StandardUserDefaults.IntForKey("LastId");
            await ServiceLayer.SharedInstance.LoadData(lastId);
            loader.StopAnimating();
            map.Delegate = this;
            if (ServiceLayer.SharedInstance.LayersEnabled[3])
            {
                lastId = ServiceLayer.SharedInstance.Pokemon.MaxBy(p => p.idValue).idValue;
                var l = ServiceLayer.SharedInstance.Pokemon.MinBy(p => p.idValue).idValue;
                NSUserDefaults.StandardUserDefaults.SetInt(l, "LastId");
                map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.ToArray());
            } else{
                map.AddAnnotations(ServiceLayer.SharedInstance.Pokemon.Where(p => !ServiceLayer.SharedInstance.PokemonTrash.Contains(p.pokemon_id)).ToArray());
            }
            map.AddAnnotations(ServiceLayer.SharedInstance.Gyms.Values.ToArray());
            map.AddAnnotations(ServiceLayer.SharedInstance.Raids.ToArray());
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
                    lastId = ServiceLayer.SharedInstance.Pokemon.MaxBy(p => p.idValue).idValue;
                    var l = ServiceLayer.SharedInstance.Pokemon.MinBy(p => p.idValue).idValue;
                    NSUserDefaults.StandardUserDefaults.SetInt(l, "LastId");
                    var onMap = map.Annotations.OfType<Pokemon>();
                    var toAdd = ServiceLayer.SharedInstance.Pokemon.Where(p => !ServiceLayer.SharedInstance.PokemonTrash.Contains(p.pokemon_id)).Except(onMap);
                    Console.WriteLine($"Adding {toAdd.Count()} mons to the map");
                    map.AddAnnotations(toAdd.ToArray());
                }
                var gymsOnMap = map.Annotations.OfType<Gym>();
                if(gymsOnMap.Count() == 0 && ServiceLayer.SharedInstance.LayersEnabled[1])
                {
                    map.AddAnnotations(ServiceLayer.SharedInstance.Gyms.Values.ToArray());
                    Console.WriteLine($"Adding {ServiceLayer.SharedInstance.Gyms.Count()} gyms to the map");
                } else if(gymsOnMap.Count() > 0 && ServiceLayer.SharedInstance.LayersEnabled[1])
                {
                    var before = DateTime.UtcNow.AddMinutes(2.0);
                    var toUpdate = gymsOnMap.Where(g => g.LastModifedDate > before);
                    foreach(var g in toUpdate) // update those that are on the map
                    {
                        var ga = map.ViewForAnnotation(g) as GymAnnotationView;
                        ga.Gym = g;
                    }
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
                    var toAdd = ServiceLayer.SharedInstance.Raids.Except(raidsOnMap);
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

    }
}
