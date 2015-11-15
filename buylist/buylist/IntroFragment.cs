using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace buylist
{
    class IntroFragment : Android.Support.V4.App.Fragment
    {
        private static string BACKGROUND_COLOR = "backgroundColor";
        private static string PAGE = "page";

        private int mPage;
        private string mBackgroundColor;
        public static IntroFragment newInstance(string bgcolor, int page)
        {
            IntroFragment frag = new IntroFragment();
            Bundle b = new Bundle();
            b.PutString(BACKGROUND_COLOR, bgcolor);
            b.PutInt(PAGE, page);
            frag.Arguments = b;
            return frag;
        }
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Bundle b = this.Arguments;
            if ( !b.ContainsKey(BACKGROUND_COLOR) )
            {
                throw new Exception("Fragment must contain a\"" + BACKGROUND_COLOR + "\" argument!");
            }
            mBackgroundColor = b.GetString(BACKGROUND_COLOR);
            if (!b.ContainsKey(PAGE))
            {
                throw new Exception("Fragment must contain a\"" + PAGE + "\" argument!");
            }
            mPage = b.GetInt(PAGE);
        }
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            int layout_resid;
            switch(mPage)
            {
                default:
                case 0:
                    layout_resid = Resource.Layout.intro_screen1;
                    break;
                case 1:
                    layout_resid = Resource.Layout.intro_screen2;
                    break;
                case 2:
                    layout_resid = Resource.Layout.intro_screen3;
                    break;
                case 3:
                    layout_resid = Resource.Layout.intro_screen4;
                    break;
                case 4:
                    layout_resid = Resource.Layout.intro_screen5;
                    break;
            }
            View view = inflater.Inflate(layout_resid, container, false);
            view.Tag =  mPage ;
            return view;
        }
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            View background = view.FindViewById<FrameLayout>(Resource.Id.intro_background);
            background.SetBackgroundColor(Android.Graphics.Color.ParseColor(mBackgroundColor));
        }
    }
}