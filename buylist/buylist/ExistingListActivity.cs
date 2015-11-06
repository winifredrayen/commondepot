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

        private enum deleteoptions
        {
            delete_this_item,
            delete_selected_items,
            delete_all_items
        }
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
        //------------------------------------------------------------------------//
        //Convenience : give a long item click for deleting each item
        private void onLongItemClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            delete_item(deleteoptions.delete_this_item, mItemList[e.Position].ID);
        }
        //------------------------------------------------------------------------//
        //bundled all the delete options together
        private void delete_item(deleteoptions dopt,int ID)
        {
            string represent_items = "";
            List<int> ids_to_delete = new List<int>();

            switch (dopt)
            {
                case deleteoptions.delete_this_item:
                    represent_items = " this item ";
                    ids_to_delete.Add(ID);
                    break;
                case deleteoptions.delete_selected_items:
                    represent_items = " those selected items ";
                    if (0 == m_queue_for_deletion.Count)
                    {
                        Toast.MakeText(this, "No items were selected", ToastLength.Long).Show();
                        return;
                    }
                    ids_to_delete = m_queue_for_deletion;
                    break;
                case deleteoptions.delete_all_items:
                    represent_items = " all your items ";
                    if (0 == mItemList.Count)
                    {
                        Toast.MakeText(this, "No items are present", ToastLength.Long).Show();
                        return;
                    }
                    foreach (var i in mItemList)
                        ids_to_delete.Add(i.ID);
                    break;
                default:
                    break;
            }
            var builder = new AlertDialog.Builder(this)
                .SetTitle("Action Required")
                .SetMessage("Do you want to remove" + represent_items + "from the list?")
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

                foreach (var item_id in ids_to_delete)
                {
                    dbhelper.delete_rows(item_id);
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
        //helper : reading the sharedprefs
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
                    //delete_selected_items();
                    delete_item(deleteoptions.delete_selected_items, 0);
                    return true;
                case Resource.Id.delete_all_items:
                    //delete_selected_items(true);
                    delete_item(deleteoptions.delete_all_items, 0);
                    return true;
                case Resource.Id.clear_items:
                    m_adapter.NotifyDataSetChanged();
                    return true;
                case Resource.Id.about:
                    Context context = this.ApplicationContext;
                    var Version = context.PackageManager.GetPackageInfo(context.PackageName, 0);

                    String ver_info = String.Format("Smart-Shop {0}.0.{1}",
                         Version.VersionName, Version.VersionCode);
                    
                    Toast.MakeText(this, ver_info, ToastLength.Long).Show();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }
        //------------------------------------------------------------------------//
        //Based on checked state of the item, we add the items to the pending queue
        private void OnCheckItemClick(object sender, onItemChecked e)
        {
            if( m_queue_for_deletion == null )
            {
                m_queue_for_deletion = new List<int>();
            }
            if ( e.checkedvalue )
            {
                m_queue_for_deletion.Add(e.ID);
            }
            else
            {
                m_queue_for_deletion.Remove(e.ID);
            }
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