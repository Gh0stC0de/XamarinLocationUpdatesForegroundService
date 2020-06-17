using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Preferences;
using Android.Provider;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.LocalBroadcastManager.Content;
using Google.Android.Material.Snackbar;

namespace LocationUpdatesForegroundService
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        /// <summary>
        ///     Used in checking for runtime permissions.
        /// </summary>
        private const int RequestPermissionsRequestCode = 34;

        private static readonly string Tag = typeof(MainActivity).FullName;

        /// <summary>
        ///     Tracks the bound state of the service.
        /// </summary>
        private bool _isBound;

        /// <summary>
        ///     The BroadcastReceiver used to listen from broadcasts from the service.
        /// </summary>
        private LocationBroadcastReceiver _locationBroadcastReceiver;

        // UI elements.
        private Button _requestLocationUpdatesButton;
        private Button _removeLocationUpdatesButton;

        /// <summary>
        ///     A reference to the service used to get location updates.
        /// </summary>
        private LocationUpdatesService _service;

        /// <summary>
        ///     Monitors the state of the connection to the service.
        /// </summary>
        private ServiceConnection _serviceConnection;

        /// <inheritdoc />
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _locationBroadcastReceiver = new LocationBroadcastReceiver(this);
            SetContentView(Resource.Layout.activity_main);

            if (Utils.RequestingLocationUpdates(this))
                if (!CheckPermissions())
                    RequestPermissions();
        }

        /// <inheritdoc />
        protected override void OnStart()
        {
            base.OnStart();
            PreferenceManager.GetDefaultSharedPreferences(this)
                .RegisterOnSharedPreferenceChangeListener(this);

            _requestLocationUpdatesButton = (Button) FindViewById(Resource.Id.request_location_updates_button);
            _removeLocationUpdatesButton = (Button) FindViewById(Resource.Id.remove_location_updates_button);

            _requestLocationUpdatesButton.SetOnClickListener(new OnClickListener(view =>
            {
                if (!CheckPermissions())
                    RequestPermissions();
                else
                    _service.RequestLocationUpdates();
            }));

            _removeLocationUpdatesButton.SetOnClickListener(new OnClickListener(view =>
            {
                _service.RemoveLocationUpdates();
            }));

            // Restore the state of the buttons when the activity (re)launches.
            SetButtonsState(Utils.RequestingLocationUpdates(this));

            if (_serviceConnection == null)
                _serviceConnection = new ServiceConnection((name, binder) =>
                    {
                        var serviceBinder = (LocationUpdatesServiceBinder) binder;
                        _service = serviceBinder.Service;
                        _isBound = true;
                    },
                    name =>
                    {
                        _service = null;
                        _isBound = false;
                    });

            // Bind to the service. If the service is in foreground mode, this signals to the service
            // that since this activity is in the foreground, the service can exit foreground mode.
            BindService(new Intent(this, typeof(LocationUpdatesService)), _serviceConnection, Bind.AutoCreate);
        }

        /// <inheritdoc />
        protected override void OnResume()
        {
            base.OnResume();
            LocalBroadcastManager.GetInstance(this).RegisterReceiver(_locationBroadcastReceiver,
                new IntentFilter(LocationUpdatesService.ActionBroadcast));
        }

        /// <inheritdoc />
        protected override void OnPause()
        {
            LocalBroadcastManager.GetInstance(this).UnregisterReceiver(_locationBroadcastReceiver);
            base.OnPause();
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            if (_isBound)
            {
                // Unbind from the service. This signals to the service that this activity is no longer
                // in the foreground, and the service can respond by promoting itself to a foreground
                // service.
                UnbindService(_serviceConnection);
                _isBound = false;
            }

            PreferenceManager.GetDefaultSharedPreferences(this)
                .UnregisterOnSharedPreferenceChangeListener(this);
            base.OnStop();
        }

        /// <summary>
        ///     Returns the current state of the permissions needed.
        /// </summary>
        /// <returns>The current state of the permissions needed.</returns>
        private bool CheckPermissions()
        {
            return Permission.Granted == ContextCompat.CheckSelfPermission(this,
                Manifest.Permission.AccessFineLocation);
        }

        /// <summary>
        ///     Request the required permissions.
        /// </summary>
        private void RequestPermissions()
        {
            var shouldProvideRationale =
                ActivityCompat.ShouldShowRequestPermissionRationale(this,
                    Manifest.Permission.AccessFineLocation);
            // Provide an additional rationale to the user. This would happen if the user denied the
            // request previously, but didn't check the "Don't ask again" checkbox.
            if (shouldProvideRationale)
            {
                Log.Info(Tag, "Displaying permission rationale to provide additional context.");
                Snackbar.Make(
                        FindViewById(Resource.Id.activity_main),
                        Resource.String.permission_rationale,
                        Snackbar.LengthIndefinite)
                    .SetAction(Resource.String.ok, view =>
                    {
                        // Request permission
                        ActivityCompat.RequestPermissions(this,
                            new[] {Manifest.Permission.AccessFineLocation},
                            RequestPermissionsRequestCode);
                    })
                    .Show();
            }
            else
            {
                Log.Info(Tag, "Requesting permission");
                // Request permission. It's possible this can be auto answered if device policy
                // sets the permission in a given state or the user denied the permission
                // previously and checked "Never ask again".
                ActivityCompat.RequestPermissions(this,
                    new[] {Manifest.Permission.AccessFineLocation},
                    RequestPermissionsRequestCode);
            }
        }

        /// <inheritdoc />
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        {
            Log.Info(Tag, "onRequestPermissionResult");
            if (requestCode == RequestPermissionsRequestCode)
            {
                if (grantResults.Length <= 0)
                {
                    // If user interaction was interrupted, the permission request is cancelled and you
                    // receive empty arrays.
                    Log.Info(Tag, "User interaction was cancelled.");
                }
                else if (grantResults[0] == Permission.Granted)
                {
                    // Permission was granted.
                    _service.RequestLocationUpdates();
                }
                else
                {
                    // Permission denied.
                    SetButtonsState(false);
                    Snackbar.Make(
                            FindViewById(Resource.Id.activity_main),
                            Resource.String.permission_denied_explanation,
                            Snackbar.LengthIndefinite)
                        .SetAction(Resource.String.settings, view =>
                        {
                            // Build intent that displays the App settings screen.
                            var intent = new Intent();
                            intent.SetAction(
                                Settings.ActionApplicationDetailsSettings);
                            var uri = Uri.Parse("package" + ApplicationContext.PackageName);
                            intent.SetData(uri);
                            intent.SetFlags(ActivityFlags.NewTask);
                            StartActivity(intent);
                        })
                        .Show();
                }
            }
        }

        /// <inheritdoc />
        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            if (key.Equals(Utils.KeyRequestingLocationUpdates))
                SetButtonsState(sharedPreferences.GetBoolean(Utils.KeyRequestingLocationUpdates,
                    false));
        }

        /// <summary>
        ///     Sets the buttons state.
        /// </summary>
        /// <param name="isRequestingLocationUpdates">The location updates requesting state.</param>
        private void SetButtonsState(bool isRequestingLocationUpdates)
        {
            if (isRequestingLocationUpdates)
            {
                _requestLocationUpdatesButton.Enabled = false;
                _removeLocationUpdatesButton.Enabled = true;
            }
            else
            {
                _requestLocationUpdatesButton.Enabled = true;
                _removeLocationUpdatesButton.Enabled = false;
            }
        }
    }
}