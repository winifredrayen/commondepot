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
using Android.Support.V4.View;

namespace buylist
{
    public class OnSlideComplete : EventArgs
    {
        private bool mSlideComplete;
        public bool slideComplete
        {
            get { return mSlideComplete; }
            set { mSlideComplete = value; }
        }
        public OnSlideComplete(bool isComplete)
        {
            slideComplete = isComplete;
        }
    }
    class IntroPageTransformer : Java.Lang.Object, Android.Support.V4.View.ViewPager.IPageTransformer
    {
        private bool mSlideCompleted;
        public event EventHandler<OnSlideComplete> mSlideCompletionEvt;

        public bool slideCompleted
        {
            get { return mSlideCompleted; }
            set { mSlideCompleted = value; }
        }
        public void TransformPage(View page, float position)
        {
            int pagePosition = Int32.Parse(page.Tag.ToString());

            // Get the page index from the tag. This makes
            // it possible to know which page index you're
            // currently transforming - and that can be used
            // to make some important performance improvements.

            // Here you can do all kinds of stuff, like get the
            // width of the page and perform calculations based
            // on how far the user has swiped the page.
            int pageWidth = page.Width;
            float pageWidthTimesPosition = pageWidth * position;
            float absPosition = Math.Abs(position);

            //Console.WriteLine("ABS position:" + absPosition);
            //Console.WriteLine("Page position:" + pagePosition);
            // Now it's time for the effects
            if (position <= -1.0f || position >= 1.0f)
            {

                // The page is not visible. This is a good place to stop
                // any potential work / animations you may have running.

            }
            else if (position == 0.0f)
            {

                // The page is selected. This is a good time to reset Views
                // after animations as you can't always count on the PageTransformer
                // callbacks to match up perfectly.

            }
            else
            {

                // The page is currently being scrolled / swiped. This is
                // a good place to show animations that react to the user's
                // swiping as it provides a good user experience.

                // Let's start by animating the title.
                // We want it to fade as it scrolls out
                View title = page.FindViewById<TextView>(Resource.Id.intro_title);
                title.Alpha = 1.0f - absPosition;

                // Now the description. We also want this one to
                // fade, but the animation should also slowly move
                // down and out of the screen
                View description = page.FindViewById<TextView>(Resource.Id.intro_description);

                description.TranslationY = (-pageWidthTimesPosition / 2f);
                
                description.Alpha = (1.0f - absPosition);

                // Now, we want the image to move to the right,
                // i.e. in the opposite direction of the rest of the
                // content while fading out

                View imgview;
                switch (pagePosition)
                {
                    case 0:
                        imgview = page.FindViewById<ImageView>(Resource.Id.intro_list);
                        break;
                    case 1:
                        imgview = page.FindViewById<ImageView>(Resource.Id.intro_queue);
                        break;
                    case 2:
                        imgview = page.FindViewById<ImageView>(Resource.Id.intro_priority);
                        break;
                    case 3:
                        imgview = page.FindViewById<ImageView>(Resource.Id.intro_deadline);
                        break;
                    default:
                    case 4:
                        imgview = page.FindViewById<ImageView>(Resource.Id.intro_cart);
                        break;
                }

                // We're attempting to create an effect for a View
                // specific to one of the pages in our ViewPager.
                // In other words, we need to check that we're on
                // the correct page and that the View in question
                // isn't null.
                if ( imgview != null )
                {
                    imgview.Alpha = (1.0f - absPosition);
                    imgview.TranslationX = (-pageWidthTimesPosition * 1.5f);
                }

                // Finally, it can be useful to know the direction
                // of the user's swipe - if we're entering or exiting.
                // This is quite simple:
                if (position < 0)
                {
                    // Create your out animation here
                }
                else
                {
                    // Create your in animation here
                }
            }
        }
    }
}