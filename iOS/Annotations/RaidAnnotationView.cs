using System;
using MapKit;
using OMAPGMap.Models;
using UIKit;

namespace OMAPGMap.iOS.Annotations
{
    public class RaidAnnotationView : MapCountdownAnnotationView
    {
        private Raid _raid;

        public RaidAnnotationView(IMKAnnotation annotate, string resueID) : base(annotate, resueID)
        {
        }

        public Raid Raid {
			set
			{
                _raid = value;
                if (_raid != null)
				{
                    if(_raid.pokemon_id == 0)
                    {
                        img.Image = UIImage.FromBundle($"egg{_raid.level.ToString("D3")}");
                        CountdownDate = _raid.TimeSpawn;
                    } else
                    {
                        
                    }
					
					
				}
			}
        }
    }
}
