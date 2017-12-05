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

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {

            //var helper = new KeychainHelper();
            var user = NSUserDefaults.StandardUserDefaults.StringForKey("user");
            var pass = NSUserDefaults.StandardUserDefaults.StringForKey("pass");
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                ServiceLayer.SharedInstance.Username = user;
                ServiceLayer.SharedInstance.Password = pass;
            }
            var layers = NSUserDefaults.StandardUserDefaults.StringArrayForKey("layers");
            if(layers != null)
            {
                var layersBool = layers.Select(l => bool.Parse(l));
                ServiceLayer.SharedInstance.LayersEnabled = layersBool.ToArray();
            }

            var trash = NSUserDefaults.StandardUserDefaults.StringArrayForKey("trash");
            var gen3Added = NSUserDefaults.StandardUserDefaults.BoolForKey("gen3Added2");
			if (trash != null)
			{
                var trashInt = trash.Select(l => int.Parse(l));
                ServiceLayer.SharedInstance.PokemonTrash = new List<int>(trashInt);
                if(!gen3Added)
                {
                    for (int i = 253; i < 379; i++)
                    {
                        if(!ServiceLayer.SharedInstance.PokemonTrash.Contains(i))
                        {
                            ServiceLayer.SharedInstance.PokemonTrash.Add(i);
                        }
                    }
                    NSUserDefaults.StandardUserDefaults.SetBool(true, "gen3Added2");
                    var trashStrings = ServiceLayer.SharedInstance.PokemonTrash.Select(t => t.ToString()).ToArray();
                    var tosave = NSArray.FromStrings(trashStrings);
                    NSUserDefaults.StandardUserDefaults.SetValueForKey(tosave, new NSString("trash"));
                } else 
                {
                    NSUserDefaults.StandardUserDefaults.SetBool(true, "gen3Added2");
                }
			}
            var notify = NSUserDefaults.StandardUserDefaults.StringArrayForKey("notify");
            if (notify != null)
            {
                var notifyInt = notify.Select(l => int.Parse(l));
                ServiceLayer.SharedInstance.NotifyPokemon = new List<int>(notifyInt);
                ServiceLayer.SharedInstance.NotifyEnabled = NSUserDefaults.StandardUserDefaults.BoolForKey("notifyEnabled");
                ServiceLayer.SharedInstance.Notify90Enabled = NSUserDefaults.StandardUserDefaults.BoolForKey("notify90");
                ServiceLayer.SharedInstance.Notify100Enabled = NSUserDefaults.StandardUserDefaults.BoolForKey("notify100");
            }
            if (launchOptions != null)
            {
                var loc = launchOptions[UIApplication.LaunchOptionsLocationKey] as NSNumber;
                if (loc != null && loc.BoolValue)
                {
                    MonitorBackgroundLocation();
                }
            }

            AppCenter.Start("10303f1b-f9aa-47dd-873d-495ba59a22d6", typeof(Analytics), typeof(Crashes), typeof(Push));

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
        }

        public override void WillTerminate(UIApplication application)
        {
            // Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.
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

        //public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, System.Action<UIBackgroundFetchResult> completionHandler)
        //{
        //    var result = Push.DidReceiveRemoteNotification(userInfo);
        //    if (result)
        //    {
        //        completionHandler?.Invoke(UIBackgroundFetchResult.NewData);
        //    }
        //    else
        //    {
        //        completionHandler?.Invoke(UIBackgroundFetchResult.NoData);
        //    }
        //}

        public void MonitorBackgroundLocation()
        {
            locManager = new CLLocationManager();
            locManager.RequestAlwaysAuthorization();
            locManager.StartMonitoringSignificantLocationChanges();
            locManager.LocationsUpdated += LocationsUpdated;
        }

        void LocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            CurrentLocation = e.Locations.First();
            UpdateDeviceData();
        }

        public async void UpdateDeviceData()
        {
            if(currentLocation != null)
            {
                var notifyStrings = ServiceLayer.SharedInstance.NotifyPokemon.Select(t => t.ToString()).ToArray();
                var tosave = NSArray.FromStrings(notifyStrings);
                NSUserDefaults.StandardUserDefaults.SetValueForKey(tosave, new NSString("notify"));
                NSUserDefaults.StandardUserDefaults.SetBool(ServiceLayer.SharedInstance.NotifyEnabled, new NSString("notifyEnabled"));
                NSUserDefaults.StandardUserDefaults.SetBool(ServiceLayer.SharedInstance.Notify90Enabled, new NSString("notify100"));
                NSUserDefaults.StandardUserDefaults.SetBool(ServiceLayer.SharedInstance.Notify100Enabled, new NSString("notify90"));
                NSUserDefaults.StandardUserDefaults.SetInt(ServiceLayer.SharedInstance.NotifyDistance, new NSString("notifyDistance"));
                var installId = MSAppCenter.InstallId();
                await ServiceLayer.SharedInstance.UpdateDeviceInfo(installId.ToString(), currentLocation.Coordinate.Latitude, currentLocation.Coordinate.Longitude);
            }
        }
    }
}

