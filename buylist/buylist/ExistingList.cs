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
    [Activity(Label = "ExistingList")]

    public class ExistingList : Activity
    {
        private List<string> mItems;
        private ListView mListview;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.existinglistview);

            // create DB path
            var docs_folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var path_to_database = System.IO.Path.Combine(docs_folder, "shoplist.db");

            //create the db helper class
            var dbhelper = new DBHelper(path_to_database);

            mItems = new List<string>();
            //it returns a value of the IEnumerable type "selected_table" from selected_table.cs
            var db_list = dbhelper.query_selected_values("select ID,ItemBrief from ShopItem");

            //if there were no entries then we might hv got a null, in that case, take them to add a new entry
            if (db_list != null)
            {
                foreach (var shopping_item in db_list)
                {
                    mItems.Add(shopping_item.ItemBrief);
                }
            }


            mListview = FindViewById<ListView>(Resource.Id.existinglist);

            ListViewAdapter adapter = new ListViewAdapter(this, mItems);
            mListview.Adapter = adapter;

            Button additem = FindViewById<Button>(Resource.Id.additem);
            Button getbudget = FindViewById<Button>(Resource.Id.getbudget);

            additem.Click += delegate
            {
                //for now its buylistform, later we need to take them to the existing list
                StartActivity(typeof(BuyListInputFormActivity));
            };
            getbudget.Click += delegate
            {

            };
        }
    }
}