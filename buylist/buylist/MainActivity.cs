using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace buylist
{
    [Activity(Label = "Buylist", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            Button gobtn = FindViewById<Button>(Resource.Id.gobutton);
            gobtn.Click += delegate
            {
                StartActivity(typeof(BuyListInputFormActivity));
                //StartActivity(typeof(ExistingList)); 
            };
        }
    }
}

