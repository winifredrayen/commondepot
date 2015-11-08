using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Threading;
using Android.Preferences;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Net;
using HtmlAgilityPack;


namespace buylist
{

    [Activity(Label = "Shop off", Icon = "@drawable/icon")]
    [IntentFilter(new[] { Intent.ActionSend }, 
        Categories = new[] {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
    DataMimeType = "text/plain")]
    public class ExistingListActivity : Activity
    {
        private ListView mListview;
        private Button mAddItem;
        private Button mSaveBudget;
        private Button mShowBuylist;
        ListViewAdapter m_adapter;
        private List<int> m_queue_for_deletion = new List<int>();
        private ObservableCollection<ShopItem> mItemList = new ObservableCollection<ShopItem>();
        private ProgressDialog mProgressDialog;
        private enum deleteoptions
        {
            delete_this_item,
            delete_selected_items,
            delete_all_items
        }
        public enum dboperations
        {
            update_table,
            insert_manually,
            insert_from_src,
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

            string text = Intent.GetStringExtra(Intent.ExtraText);
            string subject = Intent.GetStringExtra(Intent.ExtraSubject);
            string html = Intent.GetStringExtra(Intent.ExtraHtmlText);
            Console.WriteLine("text:{0},subject:{1}", text, subject);


            if( text != null ) do_url_processing(text);

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
                showItemInputDlg(dboperations.insert_manually);
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
        private List<string> findPrice(string url,HtmlDocument doc)
        {
            List<string> cost_collection = new List<string>();
            try
            {
                if (url.Contains("amazon.com"))
                {
                    var trnodes = doc.DocumentNode.Descendants("tr")
                        .Where(nd => nd.Id == "priceblock_dealprice_row" ||
                        nd.Id == "priceblock_ourprice_row" ||
                        nd.InnerText.Contains("price"));

                    foreach (var tnode in trnodes)
                    {
                        string target_string = tnode.InnerText;
                        foreach (Match m in Regex.Matches(target_string, @"\$\d*\.?,?\d*"))
                        {
                            //Console.WriteLine("PRICE:" + m.ToString());
                            cost_collection.Add(m.ToString());
                        }
                    }
                }
                else
                {
                    Regex linkParser = new Regex(@"/\s*\$\s*\d+.*\d/", RegexOptions.Compiled);
                    foreach (HtmlNode tnode in doc.DocumentNode.Descendants().Where(n => n.Id.Contains("price") ||
                       n.GetAttributeValue("class", "").Contains("price") ||
                       n.Attributes.Contains("price")))
                    {
                        string target_string = tnode.InnerText;

                        foreach (Match m in Regex.Matches(target_string, @"\$\d*\.?,?\d*"))
                        {
                            //Console.WriteLine("PRICE:" + m.ToString());
                            cost_collection.Add(m.ToString());
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Crashed / ExceptionHandled:{0}" + ex.Message);
            }
            return cost_collection;
        }
        private void do_url_processing(string rawString)
        {
            mProgressDialog = ProgressDialog.Show(this, "Please wait...", "Loading this item's info...", true);
            WebClient webClient = new WebClient();
            string url_match = "";
            try
            {
                Regex linkParser = new Regex(@"\b(?:https?://)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                foreach (Match m in linkParser.Matches(rawString))
                {
                    Console.WriteLine("URL : {0}", m);
                    url_match = m.ToString();
                }
                var url = new Uri(url_match); // Html home page
                webClient.Encoding = Encoding.UTF8;
                webClient.DownloadStringAsync(url);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Crashed / ExceptionHandled:{0}" + ex.Message);
            }

            webClient.DownloadStringCompleted += (s, e)  =>
            {
                var text = e.Result; // get the downloaded text
                HtmlDocument doc = new HtmlDocument();
                try
                {
                    doc.LoadHtml(text);
                    double minvalue = double.PositiveInfinity;
                    ShopItem temp_item = new ShopItem();

                    var all_prices = findPrice(url_match, doc);
                    int maxcount = all_prices.Count > 2 ? 2 : all_prices.Count;

                    for (int i = 0; i < maxcount; i++)
                    {
                        Console.WriteLine("PRICE:" + all_prices[i]);
                        string tp = all_prices[i].Replace("$", "");
                        if (!string.IsNullOrEmpty(tp))
                        {
                            double temp = Double.Parse(tp.ToString());
                            if (minvalue > temp)
                            {
                                minvalue = temp;
                            }
                        }
                    }
                    if (!double.IsInfinity(minvalue))
                        temp_item.ItemCost = minvalue;
                    else
                        temp_item.ItemCost = 0;

                    foreach (var node in doc.DocumentNode.Descendants("meta"))
                    {
                        if (node != null &&
                        node.Attributes["name"] != null &&
                        node.Attributes["name"].Value == "description")
                        {
                            HtmlAttribute desc;
                            desc = node.Attributes["content"];
                            string fulldescription = desc.Value;
                            temp_item.ItemDescription = fulldescription;
                            Console.WriteLine("DESCRIPTION:{0}", fulldescription.ToString());
                        }
                        if (node != null &&
                        node.Attributes["name"] != null &&
                        node.Attributes["name"].Value == "title")
                        {
                            HtmlAttribute desc;
                            desc = node.Attributes["content"];
                            string title = desc.Value;
                            Console.WriteLine("TITLE:{0}", title.ToString());
                            temp_item.ItemBrief = title;
                        }
                        else if (node != null &&
                            node.Attributes["property"] != null &&
                            node.Attributes["property"].Value.Contains("title"))
                        {
                            HtmlAttribute desc;
                            desc = node.Attributes["content"];
                            string title = desc.Value;
                            Console.WriteLine("TITLE:{0}", title.ToString());
                            temp_item.ItemBrief = title;
                        }
                    }
                    temp_item.ItemPriority = 2.5; //average

                    if (null == temp_item.ItemBrief)
                    {
                        temp_item.ItemBrief = temp_item.ItemDescription;
                        temp_item.ItemDescription = "";
                    }
                    ThreadPool.QueueUserWorkItem(o => SlowMethod(temp_item));
                }
                catch(Exception ex)
                {
                    Console.WriteLine("ExceptionHandled:{0}" + ex.Message);
                }
            };
        }

        private void SlowMethod(ShopItem item)
        {
            try
            {
                RunOnUiThread(() =>
                {
                    //Toast.MakeText(this, "Toast within progress dialog.", ToastLength.Long).Show();
                    mProgressDialog.Hide();

                    showItemInputDlg(dboperations.insert_from_src, item);
                    Console.WriteLine("UI thread execution :)");
                });
            }
            catch(Exception ex)
            {
                Console.WriteLine("ExceptioHandled:{0}" + ex.Message);
            }

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
            try
            {
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
            catch(Exception ex)
            {
                Console.WriteLine("ExceptioHandled:{0}" + ex.Message);
            }

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
        private void showItemInputDlg(dboperations opt, ShopItem item = null)
        {
            //Pull up input dialog
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            dialog_getitem_info input_dialog = new dialog_getitem_info();

            input_dialog.this_shopitem = item;
            input_dialog.this_operation = opt;

            input_dialog.Show(transaction, "dialog_fragment");
            input_dialog.mOnShopItemAdded += onSaveShopItemdata;
            input_dialog.mOnError += OnErrorHandler;
            input_dialog.mOnDismissEvt += OnDlgDismiss;
        }

        private void OnDlgDismiss(object sender, OnDismissListener e)
        {
            if( e.operate == dboperations.insert_from_src )
            {
                this.Finish();
            }
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
            bool result = false;
            //create the db helper class
            var dbhelper = new DBHelper(DBGlobal.DatabasebFilePath,this);
            switch( e.operate )
            {
                case dboperations.insert_from_src:
                case dboperations.insert_manually:
                    ShopItem item_info1 = new ShopItem
                    {
                        ItemBrief = e.item_brief,
                        ItemCost = e.item_Cost,
                        ItemDescription = e.item_description,
                        ItemPriority = e.item_priority
                    };
                    result = dbhelper.insert_update_data(item_info1);
                    break;
                case dboperations.update_table:
                    ShopItem item_info2 = new ShopItem
                    {
                        ID = e.ID,
                        ItemBrief = e.item_brief,
                        ItemCost = e.item_Cost,
                        ItemDescription = e.item_description,
                        ItemPriority = e.item_priority
                    };
                    result = dbhelper.update_data(item_info2);
                    break;
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
            showItemInputDlg(dboperations.update_table, mItemList[e.Position]);
        }
        //------------------------------------------------------------------------//
    }
}