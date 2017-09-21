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

		/// <summary>
		/// Extension for 'Object' that copies the properties to a destination object.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="destination">The destination.</param>
		public static void CopyProperties(this object source, object destination)
		{
			// If any this null throw an exception
			if (source == null || destination == null)
				throw new Exception("Source or/and Destination Objects are null");
			// Getting the Types of the objects
			Type typeDest = destination.GetType();
			Type typeSrc = source.GetType();

			// Iterate the Properties of the source instance and  
			// populate them from their desination counterparts  
			PropertyInfo[] srcProps = typeSrc.GetProperties();
			foreach (PropertyInfo srcProp in srcProps)
			{
				if (!srcProp.CanRead)
				{
					continue;
				}
				PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name);
				if (targetProperty == null)
				{
					continue;
				}
				if (!targetProperty.CanWrite)
				{
					continue;
				}
				if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
				{
					continue;
				}
				if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
				{
					continue;
				}
				if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
				{
					continue;
				}
				// Passed all tests, lets set the value
				targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
			}
		}

		public static string TimeAgo(DateTime dt)
		{
			TimeSpan span = DateTime.Now - dt;
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
