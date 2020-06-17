using Android.OS;

namespace LocationUpdatesForegroundService
{
    /// <summary>
    ///     Class used for the client Binder.
    /// </summary>
    /// <remarks>Since this service runs in the same process as its clients, we don't need to deal with IPC.</remarks>
    public class LocationUpdatesServiceBinder : Binder
    {
        public LocationUpdatesServiceBinder(LocationUpdatesService service)
        {
            Service = service;
        }

        /// <summary>
        ///     The bound service.
        /// </summary>
        public LocationUpdatesService Service { get; }
    }
}