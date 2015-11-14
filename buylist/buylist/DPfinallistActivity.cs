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
using System.Collections.ObjectModel;

/*experimental Piechart*/
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Xamarin.Android;

namespace buylist
{
    [Activity(Label = "Best fitting cart", Icon = "@drawable/icon")]
    public class DPfinallistActivity : Activity
    {
        private ObservableCollection<ShopItem> m_sortedlist = new ObservableCollection<ShopItem>();

        /*experimental Piechart*/
        private PlotView mPlotView;
        private LinearLayout mLLayoutModel;

        public static string[] colors = new string[] { "#7DA137", "#3B8DA5", "#F0BA22", "#1E90FF",
            "#97B35E", "#00E5EE","#EC8542","#758FD4","#D475B4","#00FF83","#E066FF","#5B37A1" };

        protected override void OnResume()
        {
            base.OnResume();
            refreshUI();
        }
        private void refreshUI()
        {
            if (null == m_sortedlist)
                m_sortedlist = new ObservableCollection<ShopItem>();

            // Create your application here
            List<ShopItem> all_items = get_all_shopitems();
            float budget = get_monthly_budget();

            List<ShopItem> item_collection = sortandplace(all_items, budget);
            m_sortedlist.Clear();
            //this clearing part is very important 
            foreach (var item in item_collection)
            {
                m_sortedlist.Add(item);
                budget -= (float)item.ItemCost;
            }
            mPlotView.Model = CreatePlotModel(budget);
        }
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.piechart);

            /*experimental*/

            mPlotView = FindViewById<PlotView>(Resource.Id.plotViewModel);
            mLLayoutModel = FindViewById<LinearLayout>(Resource.Id.linearLayoutModel);

            refreshUI();
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.options, menu);
            return base.OnCreateOptionsMenu(menu);
        }
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.about:
                    Context context = this.ApplicationContext;
                    var Version = context.PackageManager.GetPackageInfo(context.PackageName, 0);

                    String ver_info = String.Format("Smart-Shop {0}.0.{1}",
                         Version.VersionName, Version.VersionCode);

                    Toast.MakeText(this, ver_info, ToastLength.Long).Show();
                    return true;
                case Resource.Id.apphelp:
                    {
                        var uri = Android.Net.Uri.Parse("https://majochristo.wordpress.com/");
                        var intent = new Intent(Intent.ActionView, uri);
                        StartActivity(intent);
                    }
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }
        public PlotModel CreatePlotModel(float unallocated)
        {
            var plotModel = new PlotModel { Title = "This Month's Split up" };

            var pieSeries = new PieSeries();
            pieSeries.InsideLabelPosition = 0.0;
            pieSeries.InsideLabelFormat = null;
            pieSeries.OutsideLabelFormat = null;
            /*we need the slice data from the list */
            pieSeries.Slices = GetPieSlices(unallocated);
            plotModel.Series.Add(pieSeries);
            return plotModel;
        }

        private List<PieSlice> GetPieSlices( float unallocated)
        {
            int i = 0;
            mLLayoutModel.RemoveAllViews();

            List<PieSlice> return_slices = new List<PieSlice>();
            /*This needs to be corrected*/
            TextView mainlabel = new TextView(this);
            mainlabel.TextSize = 14;
            LinearLayout.LayoutParams llp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            llp.SetMargins(5, 0, 0, 5);
            mainlabel.LayoutParameters = llp;
            mainlabel.SetTextColor(Android.Graphics.Color.Black);
            mainlabel.Text = "Legends for reference:";

            mLLayoutModel.AddView(mainlabel);

            foreach (var item in m_sortedlist)
            {
                if (i >= colors.Length)
                {
                    i = 0;
                }
                return_slices.Add(new PieSlice(item.ItemBrief, item.ItemCost) { Fill = OxyColor.Parse(colors[i]), IsExploded = true });

                LinearLayout hLayot = new LinearLayout(this);
                hLayot.Orientation = Android.Widget.Orientation.Horizontal;
                LinearLayout.LayoutParams param = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent);
                hLayot.LayoutParameters = param;

                //Add views with colors
                LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(30, 30);

                View mView = new View(this);

                lp.TopMargin = 7;
                mView.LayoutParameters = lp;
                mView.SetBackgroundColor(Android.Graphics.Color.ParseColor(colors[i++]));

                //Add titles
                TextView label = new TextView(this);
                label.TextSize = 14;
                label.SetTextColor(Android.Graphics.Color.Black);
                string textlabel = item.ItemBrief + ": $" + item.ItemCost;
                label.Text = string.Join(" ", textlabel);
                param.LeftMargin = 10;
                label.LayoutParameters = param;

                hLayot.Tag = item.ID.ToString();
                hLayot.AddView(mView);
                hLayot.AddView(label);

                hLayot.Click += HLayot_Click;
                mLLayoutModel.AddView(hLayot);
            }
            return return_slices;
        }

        private void HLayot_Click(object sender, EventArgs e)
        {
            var layout = sender as LinearLayout;
            Console.WriteLine("selected:{0}", layout.Tag);
            var builder = new AlertDialog.Builder(this)
                .SetTitle("Shopping Completed")
                .SetMessage("Have you purchased this item already??")
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
                double cost = dbhelper.delete_rows(Int32.Parse(layout.Tag.ToString()));
                refreshUI();
                reduce_budget((int)cost);
                dialog.Dismiss();
            };
            noBtn.Click += delegate
            {
                // Dismiss dialog.
                Console.WriteLine("I will dismiss now!");
                refreshUI();
                dialog.Dismiss();
            };
        }

        private void reduce_budget(int cost)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            ISharedPreferencesEditor editor = prefs.Edit();
            float existingvalue = prefs.GetFloat("monthly_shopping_budget", 0);
            if( (existingvalue - cost) > 0.0 )
            {
                existingvalue -= cost;
            }
            else
            {
                existingvalue = 0;
            }
            editor.PutFloat("monthly_shopping_budget", existingvalue);
            // editor.Commit();    // applies changes synchronously on older APIs
            editor.Apply();        // applies changes asynchronously on newer APIs
            Toast.MakeText(this, "Your shopping budget is adjusted based on this purchase, re-adjust when needed", ToastLength.Long).Show();
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

            //create the db helper class
            var dbhelper = new DBHelper(DBGlobal.DatabasebFilePath,this);
            //create or open shopitem database
            var result = dbhelper.create_database();
            var db_list = dbhelper.query_selected_values("select * from ShopItem");

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
        private List<ShopItem> sortandplace(List<ShopItem> _item, float B)
        {
            List<ShopItem> filtered_list = new List<ShopItem>();
            //total capacity = B
            //cost : _item.cost & priority: _item.priority
            int total_budgets = (int)B;
            int total_items = _item.Count;

            int[] cost = new int[total_items];
            int[] priority = new int[total_items];
            for (int i = 0; i < total_items; i++)
            {
                cost[i] = (int)_item[i].ItemCost;
                priority[i] = (int)_item[i].ItemPriority;
            }

            int[,] final_matrix = new int[total_items+1, total_budgets+1];  

            for( int i = 0; i <= total_items; i++ )
            {
                for( int b = 0; b <= total_budgets; b++ )
                {
                    if ( i == 0 || b == 0)
                    {
                        final_matrix[i, b] = 0;
                    }
                    else if(cost[i-1] <= b )
                    {
                        if( priority[i-1] + final_matrix[i-1,b-cost[i-1]] > final_matrix[i - 1, b] )
                        {
                            final_matrix[i, b] = priority[i-1] + final_matrix[i - 1, b - cost[i-1]];
                        }
                        else
                        {
                            final_matrix[i, b] = final_matrix[i - 1, b];
                        }
                    }
                    else
                    {
                        final_matrix[i, b] = final_matrix[i - 1, b];
                    }
                }
            }
            int idx = total_items; int budget_index = total_budgets;
            while ( idx > 0 )
            {
                if(final_matrix[idx,budget_index] != final_matrix[idx-1,budget_index])
                {
                    filtered_list.Add(_item[idx-1]);
                    budget_index = budget_index - cost[idx-1];
                    Console.WriteLine("Item : " + _item[idx-1].ItemBrief + " is in the sack!");
                }
                idx = idx - 1;
            }
            return filtered_list;
        }
    }
}