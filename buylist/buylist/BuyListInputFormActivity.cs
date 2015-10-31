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

using SQLite;

namespace buylist
{
    [Activity(Label = "BuyListInputFormActivity")]
    public class BuyListInputFormActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Create your application here
            SetContentView(Resource.Layout.ItemInputForm);

            // create DB path
            var docs_folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var path_to_database = System.IO.Path.Combine(docs_folder, "shoplist.db");

            //create the db helper class
            var dbhelper = new DBHelper(path_to_database);

            //get the cost, priority, item description from the user
            EditText cost = FindViewById<EditText>(Resource.Id.itemCost);
            EditText describe = FindViewById<EditText>(Resource.Id.item_desc);
            EditText brief = FindViewById<EditText>(Resource.Id.item_brief);
            RatingBar priority = FindViewById<RatingBar>(Resource.Id.priority);

            var result = dbhelper.create_database();
            if (result == "Database created")
            {
                Toast.MakeText(this, "Sorry, database creation has failed. Try clearing the data in application settings.", ToastLength.Long).Show();
                Finish();
            }

            Button savebtn = FindViewById<Button>(Resource.Id.savebtn);
            savebtn.Click += delegate
            {
                if( !evaluateInput(cost.Text,priority.Rating,brief.Text) )
                {
                    Toast.MakeText(this, "Sorry, cost - brief description - rating all are mandatory", ToastLength.Long).Show();
                }
                else
                {
                    double costvalue = Double.Parse(cost.Text);
                    double priority_star = Double.Parse(priority.Rating.ToString());

                    ShopItem shopping_item = new ShopItem { ItemBrief = brief.Text, ItemCost = costvalue, ItemDescription = describe.Text, ItemPriority = priority_star };
                    result = dbhelper.insert_update_data(shopping_item);

                    var records = dbhelper.get_total_records();
                    Finish();
                }
            };
        }

        private bool evaluateInput(string cost, float rating, string brief)
        {
            if ( cost.Equals(String.Empty) )
            {
                return false;
            }
            if( 0 == rating )
            {
                return false;
            }
            if( brief.Equals(String.Empty))
            {
                return false;
            }
            return true;
        }

    }
}
        
