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
using System.Threading;
using Android.Preferences;
using System.Collections.ObjectModel;

namespace buylist
{
    [Activity(Label = "Shopping list options")]

    public class ExistingListActivity : Activity
    {
        private List<ShopItem> mItems;
        private ListView mListview;
        private Button mAddItem;
        private Button mSaveBudget;
        private Button mShowBuylist;

        private void db_to_list_refresh()
        {
            mItems = new List<ShopItem>();
            mListview = FindViewById<ListView>(Resource.Id.existinglist);

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
                mItems.Clear();
                foreach (var shopping_item in db_list)
                {
                    mItems.Add(shopping_item);
                }
            }
            //if this was handled in another thread, then the below needs to be run on UI thread
            ListViewAdapter adapter = new ListViewAdapter(this, mItems);
            mListview.Adapter = adapter;
        }
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.existinglistview);

            db_to_list_refresh();

            mAddItem = FindViewById<Button>(Resource.Id.additem);
            mSaveBudget = FindViewById<Button>(Resource.Id.getbudget);
            mShowBuylist = FindViewById<Button>(Resource.Id.whattobuy);

            mAddItem.Click += (object sender, EventArgs e) =>
            {
                //for now its buylistform, later we need to take them to the existing list
                //StartActivity(typeof(get_iteminfo_dialog));
                //Pull up input dialog
                FragmentTransaction transaction = FragmentManager.BeginTransaction();
                dialog_getitem_info input_dialog = new dialog_getitem_info();
                input_dialog.Show(transaction, "dialog_fragment");
                input_dialog.mOnShopItemAdded += onSaveShopItemdata;
            };

            mSaveBudget.Click += delegate
            {
                //Pull up input dialog
                FragmentTransaction transaction = FragmentManager.BeginTransaction();
                dialog_input_budget budget_input_dialog = new dialog_input_budget();
                budget_input_dialog.Show(transaction, "dialog_fragment");
                budget_input_dialog.mOnBudgetAdded += onBudgetValueChanged;
            };
            mShowBuylist.Click += delegate
            {
                //startactivity->dpfinallistactivity
            };
        }

        private void onBudgetValueChanged(object sender, OnBudgetEvtArgs e)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutFloat("monthly_shopping_budget", (float)e.budget);
            // editor.Commit();    // applies changes synchronously on older APIs
            editor.Apply();        // applies changes asynchronously on newer APIs
        }

        private void onSaveShopItemdata(object sender, OnShopItemSaveEvtArgs e)
        {
            // create DB path
            var docs_folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var path_to_database = System.IO.Path.Combine(docs_folder, "shoplist.db");

            ShopItem item_info = new ShopItem { ItemBrief = e.item_brief, ItemCost = e.item_Cost,
                ItemDescription = e.item_description, ItemPriority = e.item_priority };

            //create the db helper class
            var dbhelper = new DBHelper(path_to_database);
            var result = dbhelper.insert_update_data(item_info);
            var records = dbhelper.get_total_records();
            Console.WriteLine("DB Update :" + result + " Number of records : ", records);
            db_to_list_refresh();
        }
    }
}