// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace OMAPGMap.iOS
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		UIKit.UIActivityIndicatorView activity { get; set; }

		[Outlet]
		UIKit.UIButton currentLocationButton { get; set; }

		[Outlet]
		UIKit.UIVisualEffectView effectOverlay { get; set; }

		[Outlet]
		UIKit.UILabel errorLabel { get; set; }

		[Outlet]
		UIKit.UIButton layerSelectButton { get; set; }

		[Outlet]
		UIKit.UIActivityIndicatorView loader { get; set; }

		[Outlet]
		UIKit.UILabel loadingLabel { get; set; }

		[Outlet]
		MapKit.MKMapView map { get; set; }

		[Outlet]
		UIKit.UIView overlayView { get; set; }

		[Outlet]
		UIKit.UITextField password { get; set; }

		[Outlet]
		UIKit.UIButton settingsSelectButton { get; set; }

		[Outlet]
		UIKit.UIButton signInButton { get; set; }

		[Outlet]
		UIKit.UIButton tryAgainButton { get; set; }

		[Outlet]
		UIKit.UITextField username { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (activity != null) {
				activity.Dispose ();
				activity = null;
			}

			if (effectOverlay != null) {
				effectOverlay.Dispose ();
				effectOverlay = null;
			}

			if (errorLabel != null) {
				errorLabel.Dispose ();
				errorLabel = null;
			}

			if (layerSelectButton != null) {
				layerSelectButton.Dispose ();
				layerSelectButton = null;
			}

			if (loader != null) {
				loader.Dispose ();
				loader = null;
			}

			if (loadingLabel != null) {
				loadingLabel.Dispose ();
				loadingLabel = null;
			}

			if (map != null) {
				map.Dispose ();
				map = null;
			}

			if (overlayView != null) {
				overlayView.Dispose ();
				overlayView = null;
			}

			if (password != null) {
				password.Dispose ();
				password = null;
			}

			if (settingsSelectButton != null) {
				settingsSelectButton.Dispose ();
				settingsSelectButton = null;
			}

			if (signInButton != null) {
				signInButton.Dispose ();
				signInButton = null;
			}

			if (username != null) {
				username.Dispose ();
				username = null;
			}

			if (currentLocationButton != null) {
				currentLocationButton.Dispose ();
				currentLocationButton = null;
			}

			if (tryAgainButton != null) {
				tryAgainButton.Dispose ();
				tryAgainButton = null;
			}
		}
	}
}
