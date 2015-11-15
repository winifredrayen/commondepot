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
using Android.Support.V4.App;

namespace buylist
{
    class IntroAdapter : FragmentPagerAdapter
    {
        private int total_introscreens = 5;
        public IntroAdapter(Android.Support.V4.App.FragmentManager fm): base(fm)
        {
        }
        public override int Count
        {
            get
            {
                return total_introscreens;
            }
        }

        public override Android.Support.V4.App.Fragment GetItem(int position)
        {
            //add the introgramnets here
            switch(position)
            {
                case 0:
                    return IntroFragment.newInstance("#03A9F4", position);
                case 1:
                    return IntroFragment.newInstance("#4CAF50", position);
                case 2:
                    return IntroFragment.newInstance("#EEC900", position);
                case 3:
                    return IntroFragment.newInstance("#ED9121", position);
                default:
                case 4:
                    return IntroFragment.newInstance("#8E388E", position);
            }
        }
    }
}