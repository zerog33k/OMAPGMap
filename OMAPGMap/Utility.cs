using System;
using System.Reflection;

namespace OMAPGMap
{
	public static class Utility
	{
		public static DateTime FromUnixTime(long unixTime)
		{
			return epoch.AddSeconds(unixTime);
		}

        public static long ToUnixTime(this DateTime date)
        {
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);

		public static double MilesToLatitudeDegrees(double miles)
		{
			double earthRadius = 3960.0; // in miles
			double radiansToDegrees = 180.0 / Math.PI;
			return (miles / earthRadius) * radiansToDegrees;
		}

		public static double MilesToLongitudeDegrees(double miles, double atLatitude)
		{
			double earthRadius = 3960.0; // in miles
			double degreesToRadians = Math.PI / 180.0;
			double radiansToDegrees = 180.0 / Math.PI;
			// derive the earth's radius at that point in latitude
			double radiusAtLatitude = earthRadius * Math.Cos(atLatitude * degreesToRadians);
			return (miles / radiusAtLatitude) * radiansToDegrees;
		}

		public static string TimeAgo(DateTime dt)
		{
            TimeSpan span = DateTime.UtcNow - dt;
			if (span.Days > 365)
			{
				int years = (span.Days / 365);
				if (span.Days % 365 != 0)
					years += 1;
				return String.Format("about {0} {1} ago",
				years, years == 1 ? "year" : "years");
			}
			if (span.Days > 30)
			{
				int months = (span.Days / 30);
				if (span.Days % 31 != 0)
					months += 1;
				return String.Format("about {0} {1} ago",
				months, months == 1 ? "month" : "months");
			}
			if (span.Days > 0)
				return String.Format("about {0} {1} ago",
				span.Days, span.Days == 1 ? "day" : "days");
			if (span.Hours > 0)
				return String.Format("about {0} {1} ago",
				span.Hours, span.Hours == 1 ? "hour" : "hours");
			if (span.Minutes > 0)
				return String.Format("about {0} {1} ago",
				span.Minutes, span.Minutes == 1 ? "minute" : "minutes");
			if (span.Seconds > 5)
				return String.Format("about {0} seconds ago", span.Seconds);
			if (span.Seconds <= 5)
				return "just now";
			return string.Empty;
		}
	}
}
