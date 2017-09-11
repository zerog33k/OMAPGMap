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
	[Register ("PokemonCalloutView")]
	partial class PokemonCalloutView
	{
		[Outlet]
		UIKit.UIButton directionsButton { get; set; }

		[Outlet]
		UIKit.UILabel distanceLabel { get; set; }

		[Outlet]
		UIKit.UIButton hideButton { get; set; }

		[Outlet]
		UIKit.UILabel IVLabel { get; set; }

		[Outlet]
		UIKit.UILabel move1Label { get; set; }

		[Outlet]
		UIKit.UILabel move2Label { get; set; }

		[Outlet]
		UIKit.UILabel nameLabel { get; set; }

		[Outlet]
		UIKit.UIButton notifyButton { get; set; }

		[Outlet]
		UIKit.UIStackView stackView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (directionsButton != null) {
				directionsButton.Dispose ();
				directionsButton = null;
			}

			if (distanceLabel != null) {
				distanceLabel.Dispose ();
				distanceLabel = null;
			}

			if (hideButton != null) {
				hideButton.Dispose ();
				hideButton = null;
			}

			if (move1Label != null) {
				move1Label.Dispose ();
				move1Label = null;
			}

			if (move2Label != null) {
				move2Label.Dispose ();
				move2Label = null;
			}

			if (nameLabel != null) {
				nameLabel.Dispose ();
				nameLabel = null;
			}

			if (notifyButton != null) {
				notifyButton.Dispose ();
				notifyButton = null;
			}

			if (stackView != null) {
				stackView.Dispose ();
				stackView = null;
			}

			if (IVLabel != null) {
				IVLabel.Dispose ();
				IVLabel = null;
			}
		}
	}
}
