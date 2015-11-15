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
using Android.Views.InputMethods;
using System.Linq;
using System.Threading.Tasks;

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
        ListViewAdapter m_adapter;
        private Dictionary<int,bool> m_queue_for_deletion = new Dictionary<int, bool>();
        private ObservableCollection<ShopItem> mItemList = new ObservableCollection<ShopItem>();
        ObservableCollection<ShopItem> mfiltered_list = new ObservableCollection<ShopItem>();
        private ProgressDialog mProgressDialog;
        private LinearLayout mContainer;
        private EditText mSearch;
        private bool mAnimatedDown;
        private bool misAnimating;

        private enum scrolloptions
        {
            scroll_none,
            scroll_to_top,
            scroll_to_bottom
        }
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
            dbupdateUI(scrolloptions.scroll_to_bottom);
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

            m_adapter = new ListViewAdapter(this, mItemList,m_queue_for_deletion);
            mListview = FindViewById<ListView>(Resource.Id.existinglist);

            mListview.SmoothScrollbarEnabled = true;
            m_adapter.mOnItemCheck += OnCheckItemClick;
            mListview.Adapter = m_adapter;
            mListview.ItemClick += OnListViewItemClick;
            mListview.ItemLongClick += onLongItemClick;
            mListview.TextFilterEnabled = true;

            dbupdateUI(scrolloptions.scroll_to_bottom);

            /**Search bar implementation **/
            mSearch = FindViewById<EditText>(Resource.Id.etSearch);
            mContainer = FindViewById<LinearLayout>(Resource.Id.llcontainer);
            mSearch.Alpha = 0;
            mSearch.TextChanged += mSearch_TextChanged;

        }

        private void mSearch_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            mfiltered_list.Clear();
            //mListview.SetFilterText(mSearch.Text.ToString());
            List<ShopItem> searchedItems = (from item in mItemList
                                           where item.ItemBrief.Contains(mSearch.Text,StringComparison.OrdinalIgnoreCase) ||
                                           item.ItemDescription.Contains(mSearch.Text,StringComparison.OrdinalIgnoreCase) select item).ToList<ShopItem>();

            foreach(var filtereditem in searchedItems)
            {
                mfiltered_list.Add(filtereditem);
            }
            m_adapter = new ListViewAdapter(this, mfiltered_list, m_queue_for_deletion);
            m_adapter.mOnItemCheck -= OnCheckItemClick;
            m_adapter.mOnItemCheck += OnCheckItemClick;

            mListview.Adapter = m_adapter;
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

                webClient.DownloadStringCompleted += (s, e) =>
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
                    if( temp_item.ItemBrief.Contains(temp_item.ItemDescription) )
                        {
                            temp_item.ItemDescription = "";
                        }
                        ThreadPool.QueueUserWorkItem(o => SlowMethod(temp_item));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ExceptionHandled:{0}" + ex.Message);
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Crashed / ExceptionHandled:{0}" + ex.Message);
            }
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
            if (mfiltered_list.Count > 0)
            {
                delete_item(deleteoptions.delete_this_item, mfiltered_list[e.Position].ID);
            }
            else
            {
                delete_item(deleteoptions.delete_this_item, mItemList[e.Position].ID);
            }
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
                        foreach(var kvc in m_queue_for_deletion)
                        {
                            if( kvc.Value == true)
                                ids_to_delete.Add(kvc.Key);
                        }
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

                m_queue_for_deletion.Clear();

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

                    dbupdateUI(scrolloptions.scroll_to_top);
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

            dbupdateUI(scrolloptions.scroll_to_bottom);
        }
        //------------------------------------------------------------------------//
        private void OnErrorHandler(object sender, OnShopItemError e)
        {
            Toast.MakeText(this, e.error_msg, ToastLength.Long).Show();
        }
        //------------------------------------------------------------------------//
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.actionbar, menu);
            return base.OnCreateOptionsMenu(menu);
        }
        //------------------------------------------------------------------------//
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.search:
                    //Search icon has been clicked
                    if(misAnimating)
                    {
                        //if it is animating then exit.
                        return true;
                    }
                    if(!mAnimatedDown)
                    {
                        //List view is up
                        MyAnimation anim = new MyAnimation(mListview, mListview.Height - mSearch.Height);
                        anim.Duration = 500;
                        mListview.StartAnimation(anim);
                        anim.AnimationStart += animationStartDown;
                        anim.AnimationEnd += animationEndDown;
                        mContainer.Animate().TranslationYBy(mSearch.Height).SetDuration(500).Start();
                    }
                    else
                    {
                        //Listview is down
                        MyAnimation anim = new MyAnimation(mListview, mListview.Height + mSearch.Height);
                        anim.Duration = 500;
                        mListview.StartAnimation(anim);
                        anim.AnimationStart += animationStartUp;
                        anim.AnimationEnd += animationEndUp;
                        mContainer.Animate().TranslationYBy(-mSearch.Height).SetDuration(500).Start();
                    }
                    //toggle value
                    mAnimatedDown = !mAnimatedDown;
                    return true;
                case Resource.Id.show_cart:
                    if (0 != get_budget())
                    {
                        //start this activity only if budget is already set
                        StartActivity(typeof(DPfinallistActivity));
                        this.OverridePendingTransition(Resource.Animation.slide_in_top, Resource.Animation.slide_out_bottom);
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
                    return true;
                case Resource.Id.setbudget:
                    showBudgetDialog();
                    return true;
                case Resource.Id.add_items:
                    showItemInputDlg(dboperations.insert_manually);
                    return true;
                case Resource.Id.delete_items:
                    //delete_selected_items();
                    delete_item(deleteoptions.delete_selected_items, 0);
                    return true;
                case Resource.Id.about:
                    Context context = this.ApplicationContext;
                    var Version = context.PackageManager.GetPackageInfo(context.PackageName, 0);

                    String ver_info = String.Format("Shop off {0}.0.{1}",
                         Version.VersionName, Version.VersionCode);
                    
                    Toast.MakeText(this, ver_info, ToastLength.Long).Show();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private void animationEndDown(object sender, Android.Views.Animations.Animation.AnimationEndEventArgs e)
        {
            misAnimating = false; //animation lock
        }
        private void animationEndUp(object sender, Android.Views.Animations.Animation.AnimationEndEventArgs e)
        {
            misAnimating = false; //animation lock
            mSearch.Text = string.Empty;
            mSearch.ClearFocus();
            InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
            inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);
        }

        private void animationStartDown(object sender, Android.Views.Animations.Animation.AnimationStartEventArgs e)
        {
            misAnimating = true; //animation lock
            mSearch.Animate().AlphaBy(1.0f).SetDuration(500).Start();
        }

        private void animationStartUp(object sender, Android.Views.Animations.Animation.AnimationStartEventArgs e)
        {
            misAnimating = true; //animation lock
            mSearch.Animate().AlphaBy(-1.0f).SetDuration(300).Start();
        }

        //------------------------------------------------------------------------//
        //Based on checked state of the item, we add the items to the pending queue
        private void OnCheckItemClick(object sender, onItemChecked e)
        {
            if( m_queue_for_deletion == null ) return;
            m_queue_for_deletion[e.ID] = e.checkedvalue;
        }
        //------------------------------------------------------------------------//
        //UI operation: we need to find a neat way, as to do this operation async and tie this with UI thread later on
        async void dbupdateUI(scrolloptions sclopt)
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
            if( mfiltered_list.Count > 0 )
            {
                mfiltered_list.Clear();
                foreach(var newitem in mItemList)
                {
                    mfiltered_list.Add(newitem);
                }
            }
            m_adapter.NotifyDataSetChanged();

            switch (sclopt)
            {
                case scrolloptions.scroll_to_bottom:
                    await Task.Delay(1000);
                    mListview.SmoothScrollToPosition(m_adapter.Count - 1);
                    break;
                case scrolloptions.scroll_to_top:
                    await Task.Delay(1000);
                    mListview.SmoothScrollToPosition(0);
                    break;
                default:
                case scrolloptions.scroll_none:
                    break;
            }
            
        }
        public override void OnBackPressed()
        {
            base.OnBackPressed();
            this.OverridePendingTransition(Resource.Animation.slide_in_top, Resource.Animation.slide_out_bottom);
        }
        private void OnListViewItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            if(mfiltered_list.Count > 0)
            {
                Console.WriteLine("Item clicked priority:{0}", mfiltered_list[e.Position].ItemPriority);
                showItemInputDlg(dboperations.update_table, mfiltered_list[e.Position]);
            }
            else
            {
                Console.WriteLine("Item clicked priority:{0}", mItemList[e.Position].ItemPriority);
                showItemInputDlg(dboperations.update_table, mItemList[e.Position]);
            }
        }
        //------------------------------------------------------------------------//
    }
}