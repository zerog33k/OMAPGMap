using System;
using CoreGraphics;
using MapKit;
using OMAPGMap.Models;
using UIKit;

namespace OMAPGMap.iOS
{
    public class PokemonAnnotationView : MKAnnotationView
    {

        UIImageView img = new UIImageView(new CGRect(0, 0, 40, 40));
        UILabel label = new UILabel(new CGRect(0, 40, 40, 15));

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
