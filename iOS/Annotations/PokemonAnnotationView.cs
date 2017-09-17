using System;
using CoreGraphics;
using Foundation;
using MapKit;
using ObjCRuntime;
using OMAPGMap.Models;
using UIKit;

namespace OMAPGMap.iOS
{
    public class PokemonAnnotationView : MapCountdownAnnotationView
    {
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
        }

    }
}
