using System;
using System.Globalization;
using Android.Content;
using Android.Locations;
using Android.Preferences;

namespace LocationUpdatesForegroundService
{
    /// <summary>
    ///     The location update service utilities.
    /// </summary>
    public static class Utils
    {
        public const string KeyRequestingLocationUpdates = "requesting_locaction_updates";

        /// <summary>
        ///     Returns <c>true</c> if requesting location updates, otherwise returns <c>false</c>.
        /// </summary>
        /// <param name="context">The <see cref="Context" />.</param>
        /// <returns><c>true</c> if requesting location updates, otherwise returns <c>false</c>.</returns>
        public static bool RequestingLocationUpdates(Context context)
        {
            return PreferenceManager.GetDefaultSharedPreferences(context)
                .GetBoolean(KeyRequestingLocationUpdates, false);
        }

        /// <summary>
        ///     Stores the location updates state in SharedPreferences.
        /// </summary>
        /// <param name="context">The <see cref="Context" />.</param>
        /// <param name="requestingLocationUpdates">The location updates state.</param>
        public static void SetRequestingLocationUpdates(Context context, bool requestingLocationUpdates)
        {
            PreferenceManager.GetDefaultSharedPreferences(context)
                .Edit()
                .PutBoolean(KeyRequestingLocationUpdates, requestingLocationUpdates)
                .Apply();
        }

        /// <summary>
        ///     Returns the <see cref="Location" /> object as a human readable string.
        /// </summary>
        /// <param name="location">The <see cref="Location" />.</param>
        /// <returns>The <see cref="Location" /> object as a human readable string.</returns>
        public static string GetLocationText(Location location)
        {
            return location == null ? "Unknown location" : "(" + location.Latitude + ", " + location.Longitude + ")";
        }

        /// <summary>
        ///     Returns the location title.
        /// </summary>
        /// <param name="context">The <see cref="Context" />.</param>
        /// <returns>The location title.</returns>
        public static string GetLocationTitle(Context context)
        {
            return context.GetString(Resource.String.location_updated,
                DateTime.Now.ToString("g", CultureInfo.InvariantCulture));
        }
    }
}