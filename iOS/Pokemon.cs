﻿using System;
using CoreLocation;
using Foundation;
using MapKit;

namespace OMAPGMap.Models
{
    public partial class Pokemon : NSObject, IMKAnnotation
    {
        public Pokemon() : base()
        {
            
        }

        public Pokemon(IntPtr ptr) : base(ptr)
        {
        }

        public CLLocationCoordinate2D Coordinate => new CLLocationCoordinate2D(lat, lon);

        [Export("title")]
        public string title 
        { get 
            {
                var t = $"{name} ({gender}) - #{pokemon_id}";
                if(!string.IsNullOrEmpty(move1))
                {
					t = $"{t} - {(iv * 100).ToString("F1")}%";
                }
                return t;
            } 
        }
    }
}
