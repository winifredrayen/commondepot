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
    public static class ExtensionMethods
    {
        public static bool Contains(this string src,string toCheck,StringComparison comparisonType)
        {
            return (src.IndexOf(toCheck, comparisonType) >= 0);
        }
    }
}