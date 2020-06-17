using System;
using Android.Gms.Tasks;
using Object = Java.Lang.Object;

namespace LocationUpdatesForegroundService
{
    /// <summary>
    ///     An on complete listener.
    /// </summary>
    public class OnCompleteListener : Object, IOnCompleteListener
    {
        private readonly Action<Task> _onCompleteAction;

        /// <param name="onCompleteAction">The action to execute on complete.</param>
        public OnCompleteListener(Action<Task> onCompleteAction)
        {
            _onCompleteAction = onCompleteAction;
        }

        /// <inheritdoc />
        public void OnComplete(Task task)
        {
            _onCompleteAction.Invoke(task);
        }
    }
}