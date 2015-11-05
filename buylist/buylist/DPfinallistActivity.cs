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
    [Activity(Label = "Happy shopping!")]
    public class DPfinallistActivity : Activity
    {
        private ListView mListview;
        private List<ShopItem> sortedlist;

        private void refreshUI()
        {
            // Create your application here
            List<ShopItem> all_items = get_all_shopitems();
            float budget = get_monthly_budget();

            sortedlist = sortandplace(all_items, budget);

            ListViewAdapter adapter = new ListViewAdapter(this, sortedlist);
            adapter.mOnItemCheck += Adapter_mOnItemCheck;
            mListview.Adapter = adapter;

        }
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.dpoutput);

            mListview = FindViewById<ListView>(Resource.Id.finallist);
            refreshUI();
        }

        private void Adapter_mOnItemCheck(object sender, onItemChecked e)
        {
            if (!e.checkedvalue)
                return;

            var builder = new AlertDialog.Builder(this)
                .SetTitle("Shopping Complete")
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
                var dbhelper = new DBHelper(DBGlobal.DatabasebFilePath,this);
                //create or open shopitem database
                var result = dbhelper.create_database();
                double cost = dbhelper.delete_rows(e.ID);
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