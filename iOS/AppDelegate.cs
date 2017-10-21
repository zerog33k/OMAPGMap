using Foundation;
using Security;
using UIKit;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;
using System.Linq;
using System.Collections.Generic;

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
            var gen3Added = NSUserDefaults.StandardUserDefaults.BoolForKey("gen3Added");
			if (trash != null)
			{
                var trashInt = trash.Select(l => int.Parse(l));
                ServiceLayer.SharedInstance.PokemonTrash = new List<int>(trashInt);
                if(!gen3Added)
                {
                    if (!ServiceLayer.SharedInstance.PokemonTrash.Contains(353))
                    {
                        ServiceLayer.SharedInstance.PokemonTrash.Add(353);
                    }
                    if (!ServiceLayer.SharedInstance.PokemonTrash.Contains(355))
                    {
                        ServiceLayer.SharedInstance.PokemonTrash.Add(355);
                    }
                    if (!ServiceLayer.SharedInstance.PokemonTrash.Contains(302))
                    {
                        ServiceLayer.SharedInstance.PokemonTrash.Add(302);
                    }
                    NSUserDefaults.StandardUserDefaults.SetBool(true, "gen3Added");
                    var trashStrings = ServiceLayer.SharedInstance.PokemonTrash.Select(t => t.ToString()).ToArray();
                    var tosave = NSArray.FromStrings(trashStrings);
                    NSUserDefaults.StandardUserDefaults.SetValueForKey(tosave, new NSString("trash"));
                } else 
                {
                    NSUserDefaults.StandardUserDefaults.SetBool(true, "gen3Added");
                }
			}

			MobileCenter.Start("10303f1b-f9aa-47dd-873d-495ba59a22d6", typeof(Analytics), typeof(Crashes));

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
            UIApplication.SharedApplication.OpenUrl(new NSUrl($"https://www.google.com/maps/dir/?api=1&query={lat.ToString("F5")},{lon.ToString("F5")},6z"));
        }
    }
}

