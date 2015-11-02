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

namespace buylist
{
    [Activity(Label = "Happy shopping !")]
    public class DPfinallistActivity : Activity
    {
        private ListView mListview;
        private List<ShopItem> sortedlist;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.dpoutput);

            mListview = FindViewById<ListView>(Resource.Id.finallist);
            // Create your application here
            List<ShopItem> all_items = get_all_shopitems();
            float budget = get_monthly_budget();

            sortedlist = sortandplace(all_items, budget);

            ListViewAdapter adapter = new ListViewAdapter(this, sortedlist);
            mListview.Adapter = adapter;
        }

        private float get_monthly_budget()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            float given_budget = prefs.GetFloat("monthly_shopping_budget", 30);
            return given_budget;
        }
        private List<ShopItem> get_all_shopitems()
        {
            List<ShopItem> shop_items = new List<ShopItem>();

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
            List<ShopItem> toshowlist = new List<ShopItem>();
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
                    toshowlist.Add(_item[idx-1]);
                    budget_index = budget_index - cost[idx-1];
                    Console.WriteLine("Item : " + _item[idx-1].ItemBrief + " is in the sack!");
                }
                idx = idx - 1;
            }
            return toshowlist;
        }
    }
}