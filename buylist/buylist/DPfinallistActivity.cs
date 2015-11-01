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
using Android.Preferences;

namespace buylist
{
    [Activity(Label = "Happy shopping !")]
    public class DPfinallistActivity : Activity
    {
        private ListView mListview;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.dpoutput);

            mListview = FindViewById<ListView>(Resource.Id.finallist);
            // Create your application here
        }

        private float get_monthly_budget()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            float given_budget = prefs.GetFloat("monthly_shopping_budget", 0);
            return given_budget;
        }
        private List<ShopItem> get_all_shopitems()
        {
            List<ShopItem> shop_items = new List<ShopItem>();

            // create DB path
            var docs_folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var path_to_database = System.IO.Path.Combine(docs_folder, "shoplist.db");

            //create the db helper class
            var dbhelper = new DBHelper(path_to_database);
            //create or open shopitem database
            var result = dbhelper.create_database();
            var db_list = dbhelper.query_selected_values("select ItemBrief,ItemCost,ItemPriority,ItemDescription from ShopItem");

            //if there were no entries then we might hv got a null, in that case, take them to add a new entry
            if (db_list != null)
            {
                shop_items.Clear();
                foreach (var shopping_item in db_list)
                {
                    shop_items.Add(shopping_item);
                }
            }
            return shop_items;
        }
    }
}