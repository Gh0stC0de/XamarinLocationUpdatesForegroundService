using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.LocalBroadcastManager.Content;
using Java.Lang;

namespace LocationUpdatesForegroundService
{
    /// <summary>
    ///     The location updates service.
    /// </summary>
    [Service(Name = "com.companyname.locationupdatesforegroundservice.LocationUpdatesService",
        Enabled = true, Exported = true)]
    public class LocationUpdatesService : Service
    {
        private const string Package_Name = "com.companyname.locationupdatesforegroundservice"; //TODO is a bit unsexy

        /// <summary>
        ///     The name of the channel for notifications.
        /// </summary>
        private const string ChannelId = "channel_01";

        public const string ActionBroadcast = Package_Name + ".broadcast";
        public const string ExtraLocation = Package_Name + ".location";
        private const string ExtraStartedFromNotification = Package_Name + ".started_from_notification";

        /// <summary>
        ///     The desired interval for location updates. Inexact. Updates may be more or less frequent.
        /// </summary>
        private const long UpdateIntervalInMilliseconds = 10000;

        /// <summary>
        ///     The fastest rate for active location updates. Updates will never be more frequent than this value.
        /// </summary>
        private const long FastestUpdateIntervalInMilliseconds = UpdateIntervalInMilliseconds / 2;

        /// <summary>
        ///     The identifier for the notification displayed for the foreground service.
        /// </summary>
        private const int NotificationId = 12345678;

        private static readonly string Tag = typeof(LocationUpdatesService).FullName;

        /// <summary>
        ///     The binder.
        /// </summary>
        private IBinder _binder;

        /// <summary>
        ///     Used to check whether the bound activity has really gone away and not unbound as part of an
        ///     orientation change. We create a foreground service notification only if the former takes place.
        /// </summary>
        private bool _changingConfiguration;

        /// <summary>
        ///     Provides access to the <see cref="FusedLocationProviderApi" />.
        /// </summary>
        private FusedLocationProviderClient _fusedLocationClient;

        /// <summary>
        ///     The current location.
        /// </summary>
        private Location _location;

        /// <summary>
        ///     Callback for changes in location.
        /// </summary>
        private LocationCallback _locationCallback;

        /// <summary>
        ///     Contains parameters used by <see cref="FusedLocationProviderApi" />.
        /// </summary>
        private LocationRequest _locationRequest;

        /// <summary>
        ///     The notification manager.
        /// </summary>
        private NotificationManager _notificationManager;

        /// <summary>
        ///     The service handler.
        /// </summary>
        private Handler _serviceHandler;

        /// <inheritdoc />
        public override void OnCreate()
        {
            _fusedLocationClient = LocationServices.GetFusedLocationProviderClient(this);

            _locationCallback = new LocationCallback();
            _locationCallback.LocationResult += (sender, args) => OnNewLocation(args.Result.LastLocation);

            CreateLocationRequest();
            GetLastLocation();

            var handlerThread = new HandlerThread(Tag);
            handlerThread.Start();
            _serviceHandler = new Handler(handlerThread.Looper);
            _notificationManager = (NotificationManager) GetSystemService(NotificationService);

            // Android O requires a Notification Channel.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var name = GetString(Resource.String.app_name);
                // Create the channel for the notification
                var mChannel = new NotificationChannel(ChannelId, name, NotificationImportance.Low);

                // Set the Notification Channel for the Notification Manager.
                _notificationManager.CreateNotificationChannel(mChannel);
            }
        }

        /// <inheritdoc />
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Log.Info(Tag, "Service started");
            var startedFromNotification = intent.GetBooleanExtra(ExtraStartedFromNotification,
                false);

            // We got here because the user decided to remove location updates from the notification.
            if (startedFromNotification)
            {
                RemoveLocationUpdates();
                StopSelf();
            }

            // Tells the system to not try to recreate the service after it has been killed.
            return StartCommandResult.NotSticky;
        }

        /// <inheritdoc />
        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            _changingConfiguration = true;
        }

        /// <inheritdoc />
        public override IBinder OnBind(Intent intent)
        {
            // Called when a client (MainActivity in case of this sample) comes to the foreground
            // and binds with this service. The service should cease to be a foreground service
            // when that happens.
            Log.Info(Tag, "in onBind()");
            StopForeground(true);
            _changingConfiguration = false;

            _binder = new LocationUpdatesServiceBinder(this);
            return _binder;
        }

        /// <inheritdoc />
        public override void OnRebind(Intent intent)
        {
            // Called when a client (MainActivity in case of this sample) returns to the foreground
            // and binds once again with this service. The service should cease to be a foreground
            // service when that happens.
            Log.Info(Tag, "in onRebind()");
            StopForeground(true);
            _changingConfiguration = false;
            base.OnRebind(intent);
        }

        /// <inheritdoc />
        public override bool OnUnbind(Intent intent)
        {
            Log.Info(Tag, "Last client unbound from service");

            // Called when the last client (MainActivity in case of this sample) unbinds from this
            // service. If this method is called due to a configuration change in MainActivity, we
            // do nothing. Otherwise, we make this service a foreground service.
            if (!_changingConfiguration && Utils.RequestingLocationUpdates(this))
            {
                Log.Info(Tag, "Starting foreground service");

                StartForeground(NotificationId, GetNotification());
            }

            return true; // Ensures onRebind() is called when a client re-binds.
        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            _serviceHandler.RemoveCallbacksAndMessages(null);
        }

        /// <summary>
        ///     Makes a request for location updates.
        /// </summary>
        public void RequestLocationUpdates()
        {
            Log.Info(Tag, "Requesting location updates");
            Utils.SetRequestingLocationUpdates(this, true);
            StartService(new Intent(ApplicationContext, typeof(LocationUpdatesService)));
            try
            {
                _fusedLocationClient.RequestLocationUpdates(_locationRequest,
                    _locationCallback, Looper.MyLooper());
            }
            catch (SecurityException exception)
            {
                Utils.SetRequestingLocationUpdates(this, false);
                Log.Error(Tag, "Lost location permission. Could not request updates. " + exception);
            }
        }

        /// <summary>
        ///     Removes location updates.
        /// </summary>
        public void RemoveLocationUpdates()
        {
            Log.Info(Tag, "Removing location updates");
            try
            {
                _fusedLocationClient.RemoveLocationUpdates(_locationCallback);
                Utils.SetRequestingLocationUpdates(this, false);
                StopSelf();
            }
            catch (SecurityException unlikely)
            {
                Utils.SetRequestingLocationUpdates(this, true);
                Log.Error(Tag, "Lost location permission. Could not remove updates. " + unlikely);
            }
        }

        /// <summary>
        ///     Returns the <see cref="NotificationCompat" /> used as part of the foreground service.
        /// </summary>
        /// <returns>The <see cref="NotificationCompat" /> used as part of the foreground service.</returns>
        private Notification GetNotification()
        {
            var intent = new Intent(this, typeof(LocationUpdatesService));

            var text = Utils.GetLocationText(_location);

            // Extra to help us figure out if we arrived in onStartCommand via the notification or not.
            intent.PutExtra(ExtraStartedFromNotification, true);

            // The PendingIntent that leads to a call to onStartCommand() in this service.
            var servicePendingIntent = PendingIntent.GetService(this, 0, intent,
                PendingIntentFlags.UpdateCurrent);

            // The PendingIntent to launch activity.
            var activityPendingIntent = PendingIntent.GetActivity(this, 0,
                new Intent(this, typeof(MainActivity)), 0);

            var builder = new NotificationCompat.Builder(this, ChannelId)
                .AddAction(Resource.Drawable.ic_launch, GetString(Resource.String.launch_activity),
                    activityPendingIntent)
                .AddAction(Resource.Drawable.ic_cancel, GetString(Resource.String.remove_location_updates),
                    servicePendingIntent)
                .SetContentText(text)
                .SetContentTitle(Utils.GetLocationTitle(this))
                .SetOngoing(true)
                .SetPriority((int) NotificationPriority.Low)
                .SetSmallIcon(Resource.Mipmap.ic_launcher)
                .SetTicker(text)
                .SetWhen(JavaSystem.CurrentTimeMillis());

            // Set the Channel ID for Android O.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) builder.SetChannelId(ChannelId);

            return builder.Build();
        }

        /// <summary>
        ///     Gets the last location.
        /// </summary>
        private void GetLastLocation()
        {
            try
            {
                _fusedLocationClient.GetLastLocation()
                    .AddOnCompleteListener(new OnCompleteListener(task =>
                    {
                        if (task.IsSuccessful && task.Result != null)
                            _location = (Location) task.Result;
                        else
                            Log.Warn(Tag, "Failed to get location.");
                    }));
            }
            catch (SecurityException exception)
            {
                Log.Error(Tag, "Lost location permission." + exception);
            }
        }

        /// <summary>
        ///     Gets called when there is a new location update.
        /// </summary>
        /// <param name="location">The location.</param>
        private void OnNewLocation(Location location)
        {
            Log.Info(Tag, "New location: " + location);

            _location = location;

            // Notify anyone listening for broadcasts about the new location.
            var intent = new Intent(ActionBroadcast);
            intent.PutExtra(ExtraLocation, location);
            LocalBroadcastManager.GetInstance(ApplicationContext).SendBroadcast(intent);

            // Update notification content if running as a foreground service.
            if (ServiceIsRunningInForeground(this)) _notificationManager.Notify(NotificationId, GetNotification());
        }

        /// <summary>
        ///     Sets the location request parameters.
        /// </summary>
        private void CreateLocationRequest()
        {
            _locationRequest = new LocationRequest();
            _locationRequest.SetInterval(UpdateIntervalInMilliseconds);
            _locationRequest.SetFastestInterval(FastestUpdateIntervalInMilliseconds);
            _locationRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
        }

        /// <summary>
        ///     Returns <c>true</c> if this is a foreground service.
        /// </summary>
        /// <param name="context">The <see cref="Context" />.</param>
        /// <returns><c>true</c> if this is a foreground service.</returns>
        public bool ServiceIsRunningInForeground(Context context)
        {
            var manager = (ActivityManager) context.GetSystemService(ActivityService);
            return manager.GetRunningServices(Integer.MaxValue)
                .Where(service => Class.Name.Equals(service.Service.ClassName))
                .Any(service => service.Foreground);
        }
    }
}