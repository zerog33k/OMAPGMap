﻿using System;
using CoreGraphics;
using CoreLocation;
using MapKit;
using OMAPGMap.Models;
using UIKit;

namespace OMAPGMap.iOS.Annotations
{
    public class GymAnnotationView : MKAnnotationView
    {
        private Gym _gym;

        public Gym Gym { get => _gym; set => _gym = value; }

        public MKMapView Map { get; set; }

        public GymAnnotationView(IMKAnnotation annotation, string reuseIdentifier) : base(annotation, reuseIdentifier)
        {
            _gym = annotation as Gym;
        }

        public override UIKit.UIView DetailCalloutAccessoryView
        {
            get
            {
				var stack = new UIStackView(new CGRect(0, 0, 200, 200));
				stack.Axis = UILayoutConstraintAxis.Vertical;
				stack.Spacing = 3.0f;
				var line1 = new UILabel();
				line1.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
				line1.Text = $"Last modified {Utility.TimeAgo(_gym.LastModifedDate)}";
				stack.AddArrangedSubview(line1);
				var line2 = new UILabel();
				line2.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
				line2.Text = $"Slots Available: {_gym.slots_available}";
				stack.AddArrangedSubview(line2);
				var line3 = new UILabel();
				line3.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
				line3.Text = $"Guarding Pokemon: {_gym.pokemon_name}({_gym.pokemon_id})";
				stack.AddArrangedSubview(line3);
				if (Map.UserLocation != null)
				{
					var dist = Map.UserLocation.Location.DistanceFrom(new CLLocation(_gym.lat, _gym.lon));
					var distMiles = dist * 0.00062137;
					var distLabel = new UILabel();
					distLabel.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
					distLabel.Text = $"{distMiles.ToString("F1")} miles away";
					stack.AddArrangedSubview(distLabel);
				}
                return base.DetailCalloutAccessoryView;
            }
            set
            {
                base.DetailCalloutAccessoryView = value;
            }
        }
    }
}
