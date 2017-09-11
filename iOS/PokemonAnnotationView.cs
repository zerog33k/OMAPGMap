using System;
using CoreGraphics;
using Foundation;
using MapKit;
using ObjCRuntime;
using OMAPGMap.Models;
using UIKit;

namespace OMAPGMap.iOS
{
    public class PokemonAnnotationView : MKAnnotationView
    {

        UIImageView img = new UIImageView(new CGRect(0, 0, 40, 40));
        UILabel label = new UILabel(new CGRect(0, 40, 40, 15));

        public MKMapView Map { get; set; }

        private Pokemon _pokemon;
        public Pokemon Pokemon
        {
            set{
                _pokemon = value;
                if (_pokemon != null)
                {
                    img.Image = UIImage.FromBundle(_pokemon.pokemon_id.ToString("D3"));
                }
            }
        }

        public PokemonAnnotationView(IMKAnnotation annotate, string resueID) : base(annotate, resueID)
        {
            _pokemon = annotate as Pokemon;
            AddSubview(img);
            AddSubview(label);
            label.Layer.CornerRadius = 3.0f;
            label.Layer.BackgroundColor = UIColor.LightGray.CGColor;
            label.TextAlignment = UITextAlignment.Center;
            label.Font = UIFont.SystemFontOfSize(12.0f, UIFontWeight.Light);

			//annotation detail callout
			var view = Runtime.GetNSObject<PokemonCalloutView>(NSBundle.MainBundle.LoadNib("PokemonCalloutView", null, null).ValueAt(0));
			view.Frame = new CGRect(0, 0, 220, 200);
            var gender = _pokemon.gender == PokeGender.Male ? "Male" : "Female";
			view.NameLabel.Text = $"{_pokemon.name} ({gender}) - #{_pokemon.pokemon_id}";
			if (Map.UserLocation != null)
			{
				var userPoint = MKMapPoint.FromCoordinate(Map.UserLocation.Location.Coordinate);
				var pokePoint = new MKMapPoint(_pokemon.lat, _pokemon.lon);
				var dist = MKGeometry.MetersBetweenMapPoints(userPoint, pokePoint);
				var distMiles = dist * 0.00062137;
				view.DistanceLabel.Text = $"{distMiles.ToString("F1")} miles away";
			}
			if (string.IsNullOrEmpty(_pokemon.move1))
			{
				view.Stack.RemoveArrangedSubview(view.Move1Label);
				view.Stack.RemoveArrangedSubview(view.Move2Label);
				view.Stack.RemoveArrangedSubview(view.IVLabl);
				view.Move1Label.RemoveFromSuperview();
				view.Move2Label.RemoveFromSuperview();
				view.IVLabl.RemoveFromSuperview();
			}
			else
			{
				view.Move1Label.Text = $"Move 1: {_pokemon.move1} ({_pokemon.damage1} dps)";
				view.Move2Label.Text = $"Move 2: {_pokemon.move1} ({_pokemon.damage2} dps)";
				view.IVLabl.Text = $"IV: {_pokemon.atk}atk {_pokemon.def}def {_pokemon.sta}sta";
				var iv = (_pokemon.atk + _pokemon.def + _pokemon.sta) / 45.0f;
				view.NameLabel.Text = $"{view.NameLabel.Text} - {iv.ToString("F1")}%";
			}
            DetailCalloutAccessoryView = view;
        }

        public void UpdateTime(DateTime now)
        {
            if (_pokemon != null)
            {
                var diff = _pokemon.ExpiresDate - now;
                label.Text = $"{diff.Minutes}:{diff.Seconds.ToString("D2")}";
                if (diff.Minutes < 1)
                {
                    img.Alpha = diff.Seconds / 60.0f;
                }
            }
        }
    }
}
