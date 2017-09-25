﻿using System;
using CoreGraphics;
using CoreLocation;
using MapKit;
using OMAPGMap.Models;
using UIKit;

namespace OMAPGMap.iOS.Annotations
{
    public class RaidAnnotationView : MapCountdownAnnotationView
    {
        public MKMapView Map { get; set; }

        private Raid _raid;

        public RaidAnnotationView(IMKAnnotation annotate, string resueID) : base(annotate, resueID)
        {
            var raid = annotate as Raid;
            if(raid != null)
            {
                Raid = raid;
            }

        }

        public Raid Raid 
        {
			set
			{
                _raid = value;
                if (_raid != null)
				{
                    if(_raid.pokemon_id == 0)
                    {
                        img.Image = UIImage.FromBundle($"egg{_raid.level}");
                        CountdownDate = _raid.TimeBattle;
                    } else
                    {
                        img.Image = UIImage.FromBundle($"raid{_raid.pokemon_id}");
                        CountdownDate = _raid.TimeEnd;
                    }
				}
			}
        }

        public override void UpdateTime(DateTime now)
        {
            if (_raid.pokemon_id == 0 && CountdownDate != _raid.TimeBattle)
			{
				img.Image = UIImage.FromBundle($"egg{_raid.level}");
				CountdownDate = _raid.TimeBattle;
			}
            else if(_raid.pokemon_id != 0 && CountdownDate != _raid.TimeEnd )
			{
				img.Image = UIImage.FromBundle($"raid{_raid.pokemon_id}");
				CountdownDate = _raid.TimeEnd;
			}

            base.UpdateTime(now);
        }

        public override UIView DetailCalloutAccessoryView
        {
            get
            {
				var stack = new UIStackView(new CGRect(0, 0, 200, 200));
				stack.Axis = UILayoutConstraintAxis.Vertical;
				stack.Spacing = 3.0f;
				var line1 = new UILabel();
				line1.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
				line1.Text = $"CP: {_raid.cp}";
				stack.AddArrangedSubview(line1);
				var line2 = new UILabel();
				line2.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
				line2.Text = $"Move 1: {_raid.move_1}";
				stack.AddArrangedSubview(line2);
				var line3 = new UILabel();
				line3.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
				line3.Text = $"Move 2: {_raid.move_2}";
				stack.AddArrangedSubview(line3);
				var line4 = new UILabel();
				line4.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
				line4.Text = $"Gym Name: {_raid.name}";
				stack.AddArrangedSubview(line4);
				var line5 = new UILabel();
				line5.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
				line5.Text = $"Gym Control: {Enum.GetName(typeof(Team), _raid.team)}";
				stack.AddArrangedSubview(line5);
                if (Map.UserLocation != null)
				{
					var dist = Map.UserLocation.Location.DistanceFrom(new CLLocation(_raid.lat, _raid.lon));
					var distMiles = dist * 0.00062137;
					var distLabel = new UILabel();
					distLabel.Font = UIFont.SystemFontOfSize(13.0f, UIFontWeight.Light);
					distLabel.Text = $"{distMiles.ToString("F1")} miles away";
					stack.AddArrangedSubview(distLabel);
				}
                return stack;
            }
            set
            {
                base.DetailCalloutAccessoryView = value;
            }
        }
    }
}