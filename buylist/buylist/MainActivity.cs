using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Preferences;
using Android.Support.V4.App;
using Android.Support.V4.View;

namespace buylist
{
    [Activity(Label = "Shop off", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Android.Support.V4.App.FragmentActivity
    {
        private ViewPager mViewPager;
        private IntroAdapter mIntroAdapter;
        private bool mPageEnd;
        private int mSelectedIndex;
        private bool callHappened;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            //requesting features must be called before calling setcontentview
            RequestWindowFeature(WindowFeatures.NoTitle);
            SetContentView(Resource.Layout.Main);

            mViewPager = FindViewById<ViewPager>(Resource.Id.viewpager);
            //set adapter
            //set page transformer

            mIntroAdapter = new IntroAdapter(this.SupportFragmentManager);
            mViewPager.Adapter = mIntroAdapter;
            
            //event handlers help to identify the last page
            mViewPager.PageScrolled += MViewPager_PageScrolled;
            mViewPager.PageSelected += MViewPager_PageSelected;
            mViewPager.PageScrollStateChanged += MViewPager_PageScrollStateChanged;

            IntroPageTransformer transformer = new IntroPageTransformer();
            mViewPager.SetPageTransformer(false, transformer);
            
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
                this.OverridePendingTransition(Resource.Animation.slide_in_top, Resource.Animation.slide_out_bottom);
            }
        }
        //Save each selected page so that we can know which is the last one
        private void MViewPager_PageSelected(object sender, ViewPager.PageSelectedEventArgs e)
        {
            mSelectedIndex = e.Position;
        }
        //use the above selected to know if its the last page
        private void MViewPager_PageScrollStateChanged(object sender, ViewPager.PageScrollStateChangedEventArgs e)
        {
            if (mSelectedIndex == mIntroAdapter.Count - 1)
            {
                mPageEnd = true;
            }
        }
        private void MViewPager_PageScrolled(object sender, ViewPager.PageScrolledEventArgs e)
        {
            if (mPageEnd && e.Position == mSelectedIndex && !callHappened)
            {
                Toast.MakeText(this, "Go ahead .. begin queueing..", ToastLength.Long).Show();

                mPageEnd = false;
                callHappened = true; //To avoid multiple calls. 

                Finish();
                StartActivity(typeof(ExistingListActivity));
                this.OverridePendingTransition(Resource.Animation.slide_in_top, Resource.Animation.slide_out_bottom);
            }
            else
            {
                mPageEnd = false;
            }
        }
    }
}

