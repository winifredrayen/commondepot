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
        private int m_cost;
        private bool m_checked;
        public int ID
        {
            get { return m_ID; }
            set { m_ID = value; }
        }
        public int Cost
        {
            get { return m_cost; }
            set { m_cost = value; }
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
        private ObservableCollection<ShopItem> mItemList = new ObservableCollection<ShopItem>();
        private Context mContext;

        private List<DataSetObserver> mObservers;
        public event EventHandler<onItemChecked> mOnItemCheck;

        public ListViewAdapter(Context context, ObservableCollection<ShopItem> items)
        {
            mContext = context;
            mItemList = items;
            mObservers = new List<DataSetObserver>();
        }
        public override int Count
        {
            get { return mItemList.Count; }
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override ShopItem this[int position]
        {
            get{ return mItemList[position]; }
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

            tview.Text = mItemList[position].ItemBrief;
            itemcost.Text = mItemList[position].ItemCost.ToString();

            chckbox.Tag = mItemList[position].ID.ToString();

            //fix for accumulating event handlers for every getview 
            chckbox.CheckedChange -= onCheckItem;
            chckbox.CheckedChange += onCheckItem;

            return row;
        }

        private void onCheckItem(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            CheckBox cbox = (CheckBox)sender;
            Console.WriteLine("Checked/Unchecked!!" + Int32.Parse(cbox.Tag.ToString()));
            mOnItemCheck.Invoke(this, new onItemChecked(Int32.Parse(cbox.Tag.ToString()), e.IsChecked));
            cbox.Checked = false;
        }

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

    }
}