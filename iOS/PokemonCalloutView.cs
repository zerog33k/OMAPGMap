using Foundation;
using System;
using UIKit;

namespace OMAPGMap.iOS
{
    public partial class PokemonCalloutView : UIView
    {
        public PokemonCalloutView (IntPtr handle) : base (handle)
        {
        }
        public UILabel DetailsLabel { get { return detailsLabel; } }
        public UILabel Move1Label { get { return move1Label; } }
        public UILabel Move2Label { get { return move2Label; } }
        public UILabel DistanceLabel { get { return distanceLabel; } }
        public UIButton DirectionsButton { get { return directionsButton; } }
        public UIButton NotifyButotn { get { return notifyButton; } }
        public UIButton HideButton { get { return hideButton; } }
        public UIStackView Stack { get { return stackView; } }
        public UILabel IVLabl { get { return IVLabel; } }
    }
}