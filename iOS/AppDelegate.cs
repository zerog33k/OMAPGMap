using Foundation;
using Security;
using UIKit;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Push;
using System.Linq;
using System.Collections.Generic;
using CoreLocation;
using Microsoft.AppCenter.iOS.Bindings;
using System;
using Akavache;
using System.Threading.Tasks;

namespace OMAPGMap.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        // class-level declarations

        public override UIWindow Window
        {
            get;
            set;
        }

        CLLocationManager locManager;
        private CLLocation currentLocation;
        public CLLocation CurrentLocation 
        { 
            set
            {
                currentLocation = value;
                UpdateDeviceData();
            }
        }

        public bool LaunchedNotification { get; set; } = false;
        public string LaunchPokemon { get; set; }
        public float LaunchLat { get; set; }
        public float LaunchLon { get; set; }
        public DateTime LaunchExpires { get; set; }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            AppCenter.Start("10303f1b-f9aa-47dd-873d-495ba59a22d6", typeof(Analytics), typeof(Crashes), typeof(Push));
            //var helper = new KeychainHelper();


            if (launchOptions != null)
            {
                var loc = launchOptions[UIApplication.LaunchOptionsLocationKey] as NSNumber;
                if (loc != null && loc.BoolValue)
                {
                    MonitorBackgroundLocation();
                }
                var notifyDict = launchOptions[UIApplication.LaunchOptionsRemoteNotificationKey] as NSDictionary;
                if(notifyDict != null && notifyDict.ContainsKey(new NSString("mobile_center")))
                {
                    var mcDict = notifyDict[new NSString("mobile_center")] as NSDictionary;
                    if(mcDict != null)
                    {
                        var pokeID = mcDict.ObjectForKey(new NSString("pokemon_id")) as NSString;
                        var lat = mcDict.ObjectForKey(new NSString("lat")) as NSString;
                        var lon = mcDict.ObjectForKey(new NSString("lon")) as NSString;
                        var expires = mcDict.ObjectForKey(new NSString("expires")) as NSString;
                        LaunchedNotification = true;
                        LaunchPokemon = pokeID.ToString();
                        LaunchLat = float.Parse(lat.ToString());
                        LaunchLon = float.Parse(lon.ToString());
                        LaunchExpires = Utility.FromUnixTime(long.Parse(expires.ToString()));
                    }
                }
            }
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;


            return true;
        }

        public override void OnResignActivation(UIApplication application)
        {
            // Invoked when the application is about to move from active to inactive state.
            // This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
            // or when the user quits the application and it begins the transition to the background state.
            // Games should use this method to pause the game.
        }

        public override void DidEnterBackground(UIApplication application)
        {
            // Use this method to release shared resources, save user data, invalidate timers and store the application state.
            // If your application supports background exection this method is called instead of WillTerminate when the user quits.
        }

        public override void WillEnterForeground(UIApplication application)
        {
            // Called as part of the transiton from background to active state.
            // Here you can undo many of the changes made on entering the background.
        }

        public override void OnActivated(UIApplication application)
        {
            // Restart any tasks that were paused (or not yet started) while the application was inactive. 
            // If the application was previously in the background, optionally refresh the user interface.
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
        }

        public override void WillTerminate(UIApplication application)
        {
            BlobCache.Shutdown().Wait();
        }

        public void OpenMapAppAtLocation(double lat, double lon)
        {
            var url = $"https://maps.google.com/maps?q={lat.ToString("F6")},{lon.ToString("F6")}";
            UIApplication.SharedApplication.OpenUrl(new NSUrl(url));
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            Push.RegisteredForRemoteNotifications(deviceToken);
            UpdateDeviceData();
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            Push.FailedToRegisterForRemoteNotifications(error);
        }

        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, System.Action<UIBackgroundFetchResult> completionHandler)
        {
            var result = Push.DidReceiveRemoteNotification(userInfo);
            if (result)
            {
                completionHandler?.Invoke(UIBackgroundFetchResult.NewData);
            }
            else
            {
                completionHandler?.Invoke(UIBackgroundFetchResult.NoData);
            }
        }

        public void MonitorBackgroundLocation()
        {
            locManager = new CLLocationManager();
            locManager.RequestAlwaysAuthorization();
            locManager.StartMonitoringSignificantLocationChanges();
            locManager.LocationsUpdated += LocationsUpdated;
        }

        async void LocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            await ServiceLayer.SharedInstance.InitalizeSettings();
            CurrentLocation = e.Locations.First();
            UpdateDeviceData();
        }

        public async void UpdateDeviceData()
        {
            if(currentLocation != null)
            {
                var deviceID = MSAppCenter.InstallId().ToString();
                await ServiceLayer.SharedInstance.UpdateDeviceInfo(deviceID, currentLocation.Coordinate.Latitude, currentLocation.Coordinate.Longitude);
            }
        }

        public void MigrageUserSettings()
        {
            if(!NSUserDefaults.StandardUserDefaults.BoolForKey("akavache"))
            {
                NSUserDefaults.StandardUserDefaults.SetBool(true, new NSString("akavache"));
                var user = NSUserDefaults.StandardUserDefaults.StringForKey("user");
                var pass = NSUserDefaults.StandardUserDefaults.StringForKey("pass");
                var settings = ServiceLayer.SharedInstance.Settings;
                if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                {
                    settings.Username = user;
                    settings.Password = pass;
                }
                var layers = NSUserDefaults.StandardUserDefaults.StringArrayForKey("layers");
                if (layers != null)
                {
                    var layersBool = layers.Select(l => bool.Parse(l)).ToArray();
                    settings.PokemonEnabled = layersBool[0];
                    settings.RaidsEnabled = layersBool[1];
                    settings.GymsEnabled = layersBool[2];
                }

                var trash = NSUserDefaults.StandardUserDefaults.StringArrayForKey("trash");
                if (trash != null)
                {
                    var trashInt = trash.Select(l => int.Parse(l));
                    settings.PokemonTrash = new List<int>(trashInt);
                }
                var notify = NSUserDefaults.StandardUserDefaults.StringArrayForKey("notify");
                if (notify != null)
                {
                    var notifyInt = notify.Select(l => int.Parse(l));
                    settings.NotifyPokemon = new List<int>(notifyInt);
                    settings.NotifyEnabled = NSUserDefaults.StandardUserDefaults.BoolForKey("notifyEnabled");
                    settings.Notify90Enabled = NSUserDefaults.StandardUserDefaults.BoolForKey("notify90");
                    settings.Notify100Enabled = NSUserDefaults.StandardUserDefaults.BoolForKey("notify100");
                    settings.NotifyDistance = (int)NSUserDefaults.StandardUserDefaults.IntForKey("notifyDistance");
                }
                if (NSUserDefaults.StandardUserDefaults.ValueForKey(new NSString("raid5")) != null)
                {
                    settings.LegondaryRaids = NSUserDefaults.StandardUserDefaults.BoolForKey("raid5");
                    settings.Level4Raids = NSUserDefaults.StandardUserDefaults.BoolForKey("raid4");
                    settings.Level3Raids = NSUserDefaults.StandardUserDefaults.BoolForKey("raid3");
                    settings.Level2Raids = NSUserDefaults.StandardUserDefaults.BoolForKey("raid2");
                    settings.Level1Raids = NSUserDefaults.StandardUserDefaults.BoolForKey("raid1");
                }
                BlobCache.UserAccount.InsertObject("settings", settings);
            }

        }
    }
}

