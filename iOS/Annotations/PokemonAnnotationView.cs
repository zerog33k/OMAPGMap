using System;
using System.Collections.Generic;
using CoreGraphics;
using CoreLocation;
using Foundation;
using MapKit;
using ObjCRuntime;
using OMAPGMap.Models;
using UIKit;

namespace OMAPGMap.iOS.Annotations
{
    public class PokemonAnnotationView : MapCountdownAnnotationView
    {
        public MKMapView Map { get; set; }
        public ViewController ParentVC { get; set; }

        private Pokemon _pokemon;
        public Pokemon Pokemon
        {
            set{
                _pokemon = value;
                if (_pokemon != null)
                {
                    img.Image = UIImage.FromBundle(_pokemon.pokemon_id.ToString("D3"));
                    CountdownDate = _pokemon.ExpiresDate;
                }
            }
        }

        public PokemonAnnotationView(IMKAnnotation annotate, string resueID) : base(annotate, resueID)
        {
            Pokemon = annotate as Pokemon;
        }

        public override UIView DetailCalloutAccessoryView
        {
            get
            {
				var view = Runtime.GetNSObject<PokemonCalloutView>(NSBundle.MainBundle.LoadNib("PokemonCalloutView", null, null).ValueAt(0));
				//view.Frame = new CGRect(0, 0, 220, 200);
				var gender = _pokemon.gender == PokeGender.Male ? "Male" : "Female";
                if (Map.UserLocation != null)
				{
					var dist = Map.UserLocation.Location.DistanceFrom(new CLLocation(_pokemon.lat, _pokemon.lon));
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
                    view.DetailsLabel.RemoveFromSuperview();
					view.IVLabl.RemoveFromSuperview();
				}
				else
				{
					view.Move1Label.Text = $"Move 1: {_pokemon.move1} ({_pokemon.damage1} dps)";
					view.Move2Label.Text = $"Move 2: {_pokemon.move1} ({_pokemon.damage2} dps)";
					view.IVLabl.Text = $"IV: {_pokemon.atk}atk {_pokemon.def}def {_pokemon.sta}sta";
                    view.DetailsLabel.Text = $"CP: {_pokemon.cp} Level: {_pokemon.level}";
					var iv = (_pokemon.atk + _pokemon.def + _pokemon.sta) / 45.0f;
				}
                view.HideButton.TouchUpInside += HideButton_TouchUpInside;
                view.HideButton.SetTitle("Hide", UIControlState.Normal);
                view.DirectionsButton.TouchUpInside += DirectionsButton_TouchUpInside;
                view.NotifyButotn.TouchUpInside += NotifyButotn_TouchUpInside;
                var notifyLabel = ServiceLayer.SharedInstance.Settings.NotifyPokemon.Contains(_pokemon.pokemon_id) ? "Remove Notify" : "Notify";
                view.NotifyButotn.SetTitle(notifyLabel, UIControlState.Normal);
                return view;
            }
            set
            {
                base.DetailCalloutAccessoryView = value;
            }
        }

        async void HideButton_TouchUpInside(object sender, EventArgs e)
        {
            ServiceLayer.SharedInstance.Settings.PokemonTrash.Add(_pokemon.pokemon_id);
            var toRemove = new List<int>();
            toRemove.Add(_pokemon.pokemon_id);
            ParentVC.TrashAdded(toRemove);
            await ServiceLayer.SharedInstance.SaveSettings();
        }

        void DirectionsButton_TouchUpInside(object sender, EventArgs e)
        {
            var app = UIApplication.SharedApplication.Delegate as AppDelegate;
            app.OpenMapAppAtLocation(_pokemon.lat, _pokemon.lon);
        }

        void NotifyButotn_TouchUpInside(object sender, EventArgs e)
        {
            var button = sender as UIButton;
            if(ServiceLayer.SharedInstance.Settings.NotifyPokemon.Contains(_pokemon.pokemon_id))
            {
                ServiceLayer.SharedInstance.Settings.NotifyPokemon.Remove(_pokemon.pokemon_id);
                button.SetTitle("Notify", UIControlState.Normal);
            } else 
            {
                ServiceLayer.SharedInstance.Settings.NotifyPokemon.Add(_pokemon.pokemon_id);
                button.SetTitle("Remove Notify", UIControlState.Normal);
            }
            var app = UIApplication.SharedApplication.Delegate as AppDelegate;
            app.UpdateDeviceData();
        }
    }
}
