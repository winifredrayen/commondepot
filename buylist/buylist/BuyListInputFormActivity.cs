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
            var docsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var pathToDatabase = System.IO.Path.Combine(docsFolder, "shoplist.db");
            var dbhanlder = new DBHelper(pathToDatabase);

            var cost = FindViewById<EditText>(Resource.Id.itemCost);
            var describe = FindViewById<EditText>(Resource.Id.item_desc);
            var brief = FindViewById<EditText>(Resource.Id.item_brief);
            RatingBar priority = FindViewById<RatingBar>(Resource.Id.priority);

            ShopItem sitem;
            Button savebtn = FindViewById<Button>(Resource.Id.savebtn);
            savebtn.Enabled = false;

            var result = dbhanlder.createDatabase();
            if (result == "Database created")
            {
                savebtn.Enabled = true;
             
            }
            

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

                    sitem = new ShopItem { ItemBrief = brief.Text, ItemCost = costvalue, ItemDescription = describe.Text, ItemPriority = priority_star };
                    result = dbhanlder.insertUpdateData(sitem);

                    var records = dbhanlder.findNumberRecords();
                    Finish();
                }
            };
        }

        private bool evaluateInput(string cost, float rating, string brief)
        {
            if (cost.Equals(String.Empty))
            {
                return false;
            }
            if(rating == 0)
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
        
