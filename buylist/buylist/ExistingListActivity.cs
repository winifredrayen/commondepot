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
using System.IO;

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

            //create the db helper class
            var dbhelper = new DBHelper(AppGlobal.DatabasebFilePath);
            //create or open shopitem database
            var result = dbhelper.create_database();
            var db_list = dbhelper.query_selected_values("select * from ShopItem");

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
            adapter.mOnItemCheck += Adapter_mOnItemCheck;
            mListview.Adapter = adapter;
        }

        private void Adapter_mOnItemCheck(object sender, onItemChecked e)
        {
            if (!e.checkedvalue)
                return;

            var builder = new AlertDialog.Builder(this)
                .SetTitle("You have selected this queued item !")
                .SetMessage("Do you want to remove this item from the list?")
                .SetNegativeButton("No", (EventHandler<DialogClickEventArgs>)null)
                .SetPositiveButton("Yes", (EventHandler<DialogClickEventArgs>)null); 

            var dialog = builder.Create();
            dialog.Show();

            // Get the buttons.
            var yesBtn = dialog.GetButton((int)DialogButtonType.Positive);
            var noBtn = dialog.GetButton((int)DialogButtonType.Negative);

            // Assign our handlers.
            yesBtn.Click += delegate
            {
                Console.WriteLine("Item about to be deleted : " + e.ID + "is the value checked? " + e.checkedvalue);
                //create the db helper class
                var dbhelper = new DBHelper(AppGlobal.DatabasebFilePath);
                //create or open shopitem database
                var result = dbhelper.create_database();
                dbhelper.delete_rows(e.ID);
                db_to_list_refresh();
                dialog.Dismiss();
            };
            noBtn.Click += delegate
            {
                // Dismiss dialog.
                Console.WriteLine("I will dismiss now!");
                db_to_list_refresh();
                dialog.Dismiss();
            };

        }
        protected override void OnResume()
        {
            base.OnResume();
            db_to_list_refresh();
        }
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.existinglistview);

            if ( !get_database_relocation() )
            {
                relocate_database();
                set_database_relocation();
            }
            db_to_list_refresh();

            mAddItem = FindViewById<Button>(Resource.Id.additem);
            mSaveBudget = FindViewById<Button>(Resource.Id.getbudget);
            mShowBuylist = FindViewById<Button>(Resource.Id.whattobuy);

            //quick dialog boxes
            mAddItem.Click += (object sender, EventArgs e) =>
            {
                showItemInputDlg();
            };

            mSaveBudget.Click += delegate
            {
                showBudgetDialog();
            };
            mShowBuylist.Click += delegate
            {
                if( 0 != get_budget() )
                {
                    StartActivity(typeof(DPfinallistActivity));
                }
                else
                {
                    var builder = new AlertDialog.Builder(this)
                                   .SetTitle("Sorry")
                                   .SetMessage("You need to set the monthly shopping-budget first!")
                                   .SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);

                    var dialog = builder.Create();
                    dialog.Show();

                    // Get the buttons.
                    var yesBtn = dialog.GetButton((int)DialogButtonType.Positive);
                    yesBtn.Click += delegate
                    {
                        dialog.Dismiss();
                    };
                }
            };
        }

        private float get_budget()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            float budget_set = prefs.GetFloat("monthly_shopping_budget", 0);
            return budget_set;
        }
        private void showItemInputDlg()
        {
            //Pull up input dialog
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            dialog_getitem_info input_dialog = new dialog_getitem_info();
            input_dialog.Show(transaction, "dialog_fragment");
            input_dialog.mOnShopItemAdded += onSaveShopItemdata;
            input_dialog.mOnError += OnErrorHandler;
        }

        private void showBudgetDialog()
        {
            
            //Pull up input dialog
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            dialog_input_budget budget_input_dialog = new dialog_input_budget();
            budget_input_dialog.budget = get_budget();
            budget_input_dialog.Show(transaction, "dialog_fragment");
            budget_input_dialog.mOnBudgetAdded += onBudgetValueChanged;
            budget_input_dialog.mOnError += OnErrorHandler;
        }

        //------------------------------------------------------------------------//
        //DB relocation and directory creation
        private void relocate_database()
        {
            try
            {
                // function call used fro creating a working directory
                create_directory();

                // checking is there a directory available in the same external storage, if not create one
                if (!File.Exists(AppGlobal.DatabasebFilePath))
                {
                    // creating Database folder and file
                    creatingworking_dbfolder();
                }
            }
            catch (Exception ex)
            {
                //Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
        //------------------------------------------------------------------------//
        public void create_directory()
        {
            bool isExists = false;
            try
            {
                // checking folder available or not
                isExists = System.IO.Directory.Exists(AppGlobal.ExternalAppFolder);

                // if not create the folder
                if (!isExists)
                    System.IO.Directory.CreateDirectory(AppGlobal.ExternalAppFolder);

                isExists = System.IO.Directory.Exists(AppGlobal.ExternalAppDBFolder);

                if (!isExists)
                    System.IO.Directory.CreateDirectory(AppGlobal.ExternalAppDBFolder);
            }
            catch (Exception ex)
            {
                //Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
        //------------------------------------------------------------------------//
        public void creatingworking_dbfolder()
        {
            try
            {
                //checking file exist in location or not
                if (!File.Exists(AppGlobal.DatabasebFilePath))
                {
                    {
                        // create DB path
                        var docs_folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                        var path_to_olddatabase = System.IO.Path.Combine(docs_folder, "shoplist.db");

                        using (var asset = File.Open(path_to_olddatabase,FileMode.Open))
                        using (var dest = File.Create(AppGlobal.DatabasebFilePath))
                        {
                            // copying database from applicationdata folder to external storage device
                            asset.CopyTo(dest);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                //Toast.MakeText(this, ex.ToString(), ToastLength.Long).Show();
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
        //one time task - db relocation check
        private bool get_database_relocation()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            bool retvalue = prefs.GetBoolean("database_relocated", false);
            return retvalue;
        }
        private void set_database_relocation()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutBoolean("database_relocated", true);
            // editor.Commit();    // applies changes synchronously on older APIs
            editor.Apply();        // applies changes asynchronously on newer APIs
        }
        //------------------------------------------------------------------------//
        //Event Handlers
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
            ShopItem item_info = new ShopItem { ItemBrief = e.item_brief, ItemCost = e.item_Cost,
                ItemDescription = e.item_description, ItemPriority = e.item_priority };

            //create the db helper class
            var dbhelper = new DBHelper(AppGlobal.DatabasebFilePath);
            var result = dbhelper.insert_update_data(item_info);
            var records = dbhelper.get_total_records();
            Console.WriteLine("DB Update :" + result + " Number of records : ", records);
            db_to_list_refresh();
        }
        //------------------------------------------------------------------------//
        private void OnErrorHandler(object sender, OnShopItemError e)
        {
            Toast.MakeText(this, e.error_msg, ToastLength.Long).Show();
        }
        //------------------------------------------------------------------------//
    }
}