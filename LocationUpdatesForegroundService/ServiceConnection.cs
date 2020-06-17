using System;
using Android.Content;
using Android.OS;
using Object = Java.Lang.Object;

namespace LocationUpdatesForegroundService
{
    /// <summary>
    ///     A service connection.
    /// </summary>
    public class ServiceConnection : Object, IServiceConnection
    {
        private readonly Action<ComponentName, IBinder> _onServiceConnectedAction;
        private readonly Action<ComponentName> _onServiceDisconnectedAction;

        /// <param name="onServiceConnectedAction">The action to execute when a connection to the service has been established.</param>
        /// <param name="onServiceDisconnectedAction">The action to execute when a connection to the service has been lost.</param>
        public ServiceConnection(Action<ComponentName, IBinder> onServiceConnectedAction
            , Action<ComponentName> onServiceDisconnectedAction)
        {
            _onServiceConnectedAction = onServiceConnectedAction;
            _onServiceDisconnectedAction = onServiceDisconnectedAction;
        }

        /// <inheritdoc />
        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            _onServiceConnectedAction.Invoke(name, service);
        }

        /// <inheritdoc />
        public void OnServiceDisconnected(ComponentName name)
        {
            _onServiceDisconnectedAction.Invoke(name);
        }
    }
}