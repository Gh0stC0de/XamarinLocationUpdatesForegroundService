using Android.Content;
using Android.Locations;
using Android.Widget;

namespace LocationUpdatesForegroundService
{
    /// <summary>
    ///     Receiver for broadcasts sent by <see cref="LocationUpdatesService" />.
    /// </summary>
    public class LocationBroadcastReceiver : BroadcastReceiver
    {
        private readonly Context _context;

        public LocationBroadcastReceiver(Context context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public override void OnReceive(Context context, Intent intent)
        {
            var location = (Location) intent.GetParcelableExtra(LocationUpdatesService.ExtraLocation);
            if (location != null)
                Toast.MakeText(_context, Utils.GetLocationText(location), ToastLength.Short)
                    .Show();
        }
    }
}