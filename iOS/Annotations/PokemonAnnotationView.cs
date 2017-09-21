using System;
using CoreGraphics;
using Foundation;
using MapKit;
using ObjCRuntime;
using OMAPGMap.Models;
using UIKit;

namespace OMAPGMap.iOS.Annotations
{
    public class PokemonAnnotationView : MapCountdownAnnotationView
    {

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

    }
}
