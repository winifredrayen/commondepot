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
using Java.Lang;
using System.Collections.ObjectModel;
using Android.Database;

namespace buylist
{
    class ListViewAdapter : BaseAdapter<ShopItem>
    {
        private List<ShopItem> mItems;
        private Context mContext;

        private List<DataSetObserver> mObservers;
        public ListViewAdapter(Context context, List<ShopItem> items)
        {
            mItems = items;
            mContext = context;
            mObservers = new List<DataSetObserver>();
        }
        public override int Count
        {
            get{ return mItems.Count; }
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override ShopItem this[int position]
        {
            get{ return mItems[position]; }
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            //reuse if available
            View row = convertView;
            if( row == null )
            {
                //if not create one
                row = LayoutInflater.From(mContext).Inflate(Resource.Layout.listview_row, null, false);
            }
            TextView tview = row.FindViewById<TextView>(Resource.Id.textitem);
            TextView itemcost = row.FindViewById<TextView>(Resource.Id.itemcost);
            tview.Text = mItems[position].ItemBrief;
            itemcost.Text = mItems[position].ItemCost.ToString();
            return row;
        }
        /*
        public override void RegisterDataSetObserver(DataSetObserver observer)
        {
            base.RegisterDataSetObserver(observer);
            mObservers.Add(observer);
        }
        public override void NotifyDataSetChanged()
        {
            base.NotifyDataSetChanged();
            foreach( DataSetObserver observer in mObservers )
            {
                observer.OnChanged();
            }
        }
        */
    }
}