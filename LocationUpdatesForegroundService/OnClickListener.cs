using System;
using Android.Views;
using Object = Java.Lang.Object;

namespace LocationUpdatesForegroundService
{
    /// <summary>
    ///     An on click listener.
    /// </summary>
    public class OnClickListener : Object, View.IOnClickListener
    {
        private readonly Action<View> _onClickAction;

        /// <param name="onClickAction">The action to execute on click.</param>
        public OnClickListener(Action<View> onClickAction)
        {
            _onClickAction = onClickAction;
        }

        /// <inheritdoc />
        public void OnClick(View v)
        {
            _onClickAction.Invoke(v);
        }
    }
}