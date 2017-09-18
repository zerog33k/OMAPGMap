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
		UIKit.UIVisualEffectView effectOverlay { get; set; }

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
		UIKit.UIButton signInButton { get; set; }

		[Outlet]
		UIKit.UITextField username { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (effectOverlay != null) {
				effectOverlay.Dispose ();
				effectOverlay = null;
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

			if (signInButton != null) {
				signInButton.Dispose ();
				signInButton = null;
			}

			if (username != null) {
				username.Dispose ();
				username = null;
			}

			if (layerSelectButton != null) {
				layerSelectButton.Dispose ();
				layerSelectButton = null;
			}
		}
	}
}
