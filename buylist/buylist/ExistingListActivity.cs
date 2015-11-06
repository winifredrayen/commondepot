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
        private ListView mListview;
        private Button mAddItem;
        private Button mSaveBudget;
        private Button mShowBuylist;
        ListViewAdapter m_adapter;
        private List<int> m_queue_for_deletion = new List<int>();
        private ObservableCollection<ShopItem> mItemList = new ObservableCollection<ShopItem>();

        protected override void OnResume()
        {
            base.OnResume();
            //inorder to refresh the list if you move back & forth this activity
            dbupdateUI();
        }
        //------------------------------------------------------------------------//
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.existinglistview);

            m_adapter = new ListViewAdapter(this, mItemList);
            mListview = FindViewById<ListView>(Resource.Id.existinglist);
            m_adapter.mOnItemCheck += OnCheckItemClick;
            mListview.Adapter = m_adapter;
            mListview.ItemClick += OnListViewItemClick;
            mListview.ItemLongClick += onLongItemClick;

            dbupdateUI();

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
                    //start this activity only if budget is already set
                    StartActivity(typeof(DPfinallistActivity));
                }
                else
                {
                    var builder = new AlertDialog.Builder(this)
                                   .SetTitle("Action Required")
                                   .SetMessage("You need to set the monthly shopping-budget first!")
                                   .SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);

                    var dialog = builder.Create();
                    dialog.Show();

                    // Get the buttons : inorder to dismiss the dialog from outside
                    var yesBtn = dialog.GetButton((int)DialogButtonType.Positive);
                    yesBtn.Click += delegate
                    {
                        dialog.Dismiss();
                    };
                }
            };
        }

        private void onLongItemClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            var builder = new AlertDialog.Builder(this)
                .SetTitle("Action Required")
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
                Console.WriteLine("Item about to be deleted : " + mItemList[e.Position].ID );
                //create the db helper class
                var dbhelper = new DBHelper(DBGlobal.DatabasebFilePath, this);
                //create or open shopitem database
                var result = dbhelper.create_database();
                if (!result)
                {
                    Toast.MakeText(this, "Failed to open / create the database ", ToastLength.Long).Show();
                    return;
                }
                dbhelper.delete_rows(mItemList[e.Position].ID);
                dbupdateUI();
                dialog.Dismiss();
            };
            noBtn.Click += delegate
            {
                // Dismiss dialog.
                Console.WriteLine("I will dismiss now!");
                dbupdateUI();
                dialog.Dismiss();
            };
        }

        //------------------------------------------------------------------------//
        private float get_budget()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            float budget_set = prefs.GetFloat("monthly_shopping_budget", 0);
            return budget_set;
        }
        //------------------------------------------------------------------------//
        //Dialog options - TBD common interface
        private void showItemInputDlg(ShopItem item = null)
        {
            //Pull up input dialog
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            dialog_getitem_info input_dialog = new dialog_getitem_info();
            if(item != null){ input_dialog.this_shopitem = item; }

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
            bool result;

            //create the db helper class
            var dbhelper = new DBHelper(DBGlobal.DatabasebFilePath,this);
            if( e.updatetable ) {
                ShopItem item_info = new ShopItem
                {
                    ID = e.ID,
                    ItemBrief = e.item_brief,
                    ItemCost = e.item_Cost,
                    ItemDescription = e.item_description,
                    ItemPriority = e.item_priority
                };
                result = dbhelper.update_data(item_info);
            }
            else
            {
                ShopItem item_info = new ShopItem
                {
                    ItemBrief = e.item_brief,
                    ItemCost = e.item_Cost,
                    ItemDescription = e.item_description,
                    ItemPriority = e.item_priority
                };
                result = dbhelper.insert_update_data(item_info);
            }
            
            var records = dbhelper.get_total_records();
            Console.WriteLine("DB Update :" + result + " Number of records : ", records);
            dbupdateUI();
        }
        //------------------------------------------------------------------------//
        private void OnErrorHandler(object sender, OnShopItemError e)
        {
            Toast.MakeText(this, e.error_msg, ToastLength.Long).Show();
        }
        //------------------------------------------------------------------------//
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.options, menu);
            return base.OnCreateOptionsMenu(menu);
        }
        //------------------------------------------------------------------------//
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.delete_items:
                    delete_selected_items();
                    return true;
                case Resource.Id.delete_all_items:
                    delete_selected_items(true);
                    return true;
                case Resource.Id.clear_items:
                    m_adapter.NotifyDataSetChanged();
                    return true;
                case Resource.Id.help:
                    //do something
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
            
        }
        //------------------------------------------------------------------------//
        private void OnCheckItemClick(object sender, onItemChecked e)
        {
            if( m_queue_for_deletion == null )
            {
                m_queue_for_deletion = new List<int>();
            }
            if ( e.checkedvalue)
            {
                m_queue_for_deletion.Add(e.ID);
            }
            else
            {
                m_queue_for_deletion.Remove(e.ID);
            }
        }
        private void delete_selected_items(bool all_items = false)
        {
            string represent_items = "";
            if( all_items )
            {
                represent_items = " all your items ";
            }
            else
            {
                if (0 == m_queue_for_deletion.Count)
                {
                    Toast.MakeText(this, "No items were selected", ToastLength.Long).Show();
                    return;
                }
                represent_items = " those selected items ";
            }
            var builder = new AlertDialog.Builder(this)
                .SetTitle("Action Required")
                .SetMessage("Do you want to remove" + represent_items +"from the list?")
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
                //create the db helper class
                var dbhelper = new DBHelper(DBGlobal.DatabasebFilePath, this);
                //create or open shopitem database
                var result = dbhelper.create_database();
                if (!result)
                {
                    Toast.MakeText(this, "Failed to open / create the database ", ToastLength.Long).Show();
                    return;
                }
                if ( !all_items )
                {
                    foreach (var item_id in m_queue_for_deletion)
                    {
                        dbhelper.delete_rows(item_id);
                    }
                }
                else
                {
                    dbhelper.deleteall();
                }
                dbupdateUI();
                dialog.Dismiss();
            };
            noBtn.Click += delegate
            {
                // Dismiss dialog.
                Console.WriteLine("I will dismiss now!");
                m_adapter.NotifyDataSetChanged();
                dialog.Dismiss();
            };
        }
        //------------------------------------------------------------------------//
        //UI operation: we need to find a neat way, as to do this operation async and tie this with UI thread later on
        private void dbupdateUI()
        {
            if( null == mItemList)
                mItemList = new ObservableCollection<ShopItem>();

            //create the db helper class
            var dbhelper = new DBHelper(DBGlobal.DatabasebFilePath, this);
            //create or open shopitem database
            var result = dbhelper.create_database();
            if (!result)
            {
                Toast.MakeText(this, "Failed to open / create the database ", ToastLength.Long).Show();
                return;
            }

            var db_list = dbhelper.query_selected_values("select * from ShopItem");

            //if there were no entries then we might hv got a null, in that case, take them to add a new entry
            if (db_list != null)
            {
                mItemList.Clear();
                foreach (var shopping_item in db_list)
                {
                    mItemList.Add(shopping_item);
                }
            }
            m_queue_for_deletion.Clear();
            m_adapter.NotifyDataSetChanged();
        }

        private void OnListViewItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Console.WriteLine("Item clicked priority:{0}", mItemList[e.Position].ItemPriority);
            showItemInputDlg(mItemList[e.Position]);
        }
        //------------------------------------------------------------------------//
    }
}