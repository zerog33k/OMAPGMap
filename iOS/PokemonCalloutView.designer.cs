// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace OMAPGMap.iOS
{
    [Register ("PokemonCalloutView")]
    partial class PokemonCalloutView
    {
        [Outlet]
        UIKit.UILabel detailsLabel { get; set; }


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
        UIKit.UIButton notifyButton { get; set; }


        [Outlet]
        UIKit.UIStackView stackView { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (detailsLabel != null) {
                detailsLabel.Dispose ();
                detailsLabel = null;
            }

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

            if (IVLabel != null) {
                IVLabel.Dispose ();
                IVLabel = null;
            }

            if (move1Label != null) {
                move1Label.Dispose ();
                move1Label = null;
            }

            if (move2Label != null) {
                move2Label.Dispose ();
                move2Label = null;
            }

            if (notifyButton != null) {
                notifyButton.Dispose ();
                notifyButton = null;
            }

            if (stackView != null) {
                stackView.Dispose ();
                stackView = null;
            }
        }
    }
}