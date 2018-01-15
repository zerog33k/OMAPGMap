using System;
using CoreGraphics;
using MapKit;
using UIKit;

namespace OMAPGMap.iOS.Annotations
{
    public abstract class MapCountdownAnnotationView : MKAnnotationView
    {
		protected UIImageView img = new UIImageView(new CGRect(0, 0, 40, 40));
		protected UILabel label = new UILabel(new CGRect(0, 40, 40, 15));
        protected DateTime CountdownDate;

        public UIColor LabelColor { set
            {
                label.Layer.BackgroundColor = value.CGColor;
            }
        }

        public MapCountdownAnnotationView(IMKAnnotation annotate, string resueID) : base(annotate, resueID)
        {
			AddSubview(img);
			AddSubview(label);
			label.Layer.CornerRadius = 3.0f;
            label.Layer.BackgroundColor = UIColor.LightGray.CGColor;
			label.TextAlignment = UITextAlignment.Center;
			label.Font = UIFont.SystemFontOfSize(12.0f, UIFontWeight.Light);
            img.ContentMode = UIViewContentMode.ScaleAspectFit;
        }

		public virtual void UpdateTime(DateTime now)
		{
            if (CountdownDate != null && !label.Hidden)
			{
				var diff = CountdownDate - now;
				label.Text = $"{diff.Minutes}:{diff.Seconds.ToString("D2")}";
                if (diff.Minutes < 1 && this is PokemonAnnotationView)
				{
					img.Alpha = diff.Seconds / 60.0f;
				}
			}
		}

        public bool TimerVisible {
            set {
                label.Hidden = !value;
            }
        }
    }
}
