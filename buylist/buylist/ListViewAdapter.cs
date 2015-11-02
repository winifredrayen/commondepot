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
    public class onItemChecked : EventArgs
    {
        private int m_ID;
        private bool m_checked;
        public int ID
        {
            get { return m_ID; }
            set { m_ID = value; }
        }
        public bool checkedvalue
        {
            get { return m_checked; }
            set { m_checked = value; }
        }
        public onItemChecked(int in_ID,bool isChecked)
        {
            m_ID = in_ID;
            m_checked = isChecked;
        }
    }
    class ListViewAdapter : BaseAdapter<ShopItem>
    {
        private List<ShopItem> mItems;
        private Context mContext;

        private List<DataSetObserver> mObservers;
        public event EventHandler<onItemChecked> mOnItemCheck;
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
            CheckBox chckbox = row.FindViewById<CheckBox>(Resource.Id.itemcheck);

            tview.Text = mItems[position].ItemBrief;
            itemcost.Text = mItems[position].ItemCost.ToString();

            chckbox.Tag = mItems[position].ID.ToString();
            chckbox.CheckedChange += (sender, e) =>
            {
                Console.WriteLine("Checked/Unchecked!!"+ Int32.Parse(chckbox.Tag.ToString()));
                mOnItemCheck.Invoke(this, new onItemChecked(Int32.Parse(chckbox.Tag.ToString()),e.IsChecked));
            };
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