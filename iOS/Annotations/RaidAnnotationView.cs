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
                        img.Image = UIImage.FromBundle($"raid{_raid.pokemon_id.ToString("D3")}");
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
				img.Image = UIImage.FromBundle($"raid{_raid.pokemon_id.ToString("D3")}");
				CountdownDate = _raid.TimeEnd;
			}

            base.UpdateTime(now);
        }

    }
}
