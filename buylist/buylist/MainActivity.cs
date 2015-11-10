using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Preferences;

namespace buylist
{
    [Activity(Label = "Shop off", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            bool isfirstime = prefs.GetBoolean("launched_once", false);

            if ( !isfirstime )
            {
                ISharedPreferencesEditor editor = prefs.Edit();
                editor.PutBoolean("launched_once", true);
                // editor.Commit();    // applies changes synchronously on older APIs
                editor.Apply();        // applies changes asynchronously on newer APIs
            }
            else
            {
                StartActivity(typeof(ExistingListActivity));
            }

            Button gobtn = FindViewById<Button>(Resource.Id.gobutton);
            gobtn.Click += delegate
            {
                //for now its buylistform, later we need to take them to the existing list
                StartActivity(typeof(ExistingListActivity));
            };
        }
    }
}

