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
using Android.Graphics;

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
        public onItemChecked(int in_ID, bool isChecked)
        {
            m_ID = in_ID;
            m_checked = isChecked;
        }
    }
    class ListViewAdapter : BaseAdapter<ShopItem>
    {
        private Context mContext;
        Dictionary<int, bool> check_position;
        private ObservableCollection<ShopItem> mItemList;

        private List<DataSetObserver> mObservers;
        public event EventHandler<onItemChecked> mOnItemCheck;

        public ListViewAdapter(Context context, ObservableCollection<ShopItem> items, Dictionary<int, bool> markeditems)
        {
            mContext = context;
            mItemList = items;
            if( markeditems != null )
            {
                check_position = markeditems;
            }
            else
            {
                check_position = new Dictionary<int, bool>();
            }
            
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
            get { return mItemList[position]; }
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            //reuse if available
            View row = convertView;
            if (row == null)
            {
                //if not create one
                row = LayoutInflater.From(mContext).Inflate(Resource.Layout.row, null, false);
            }
            //Android.Graphics.Color bgcolor = getRowColor(mItemList[position].ItemPriority);

            TextView tview = row.FindViewById<TextView>(Resource.Id.txtTitle);
            TextView itemcost = row.FindViewById<TextView>(Resource.Id.txtCost);
            TextView dview = row.FindViewById<TextView>(Resource.Id.txtDescription);
            CheckBox chckbox = row.FindViewById<CheckBox>(Resource.Id.cbxStart);

            tview.Text = mItemList[position].ItemBrief;
            itemcost.Text = mItemList[position].ItemCost.ToString();
            dview.Text = mItemList[position].ItemDescription.ToString();

            chckbox.Tag = position;

            if (check_position.ContainsKey(mItemList[position].ID))
            {
                chckbox.Checked = check_position[mItemList[position].ID];
            }
            else
            {
                chckbox.Checked = false;
            }

            //fix for accumulating event handlers for every getview 
            chckbox.CheckedChange -= onCheckItem;
            chckbox.CheckedChange += onCheckItem;

            return row;
        }
        private void onCheckItem(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            CheckBox chckbox = sender as CheckBox;
            check_position[mItemList[Int32.Parse(chckbox.Tag.ToString())].ID] = e.IsChecked;

            Console.WriteLine("Checked/Unchecked!!" + mItemList[Int32.Parse(chckbox.Tag.ToString())].ID);
            mOnItemCheck.Invoke(this,
                new onItemChecked(mItemList[Int32.Parse(chckbox.Tag.ToString())].ID, e.IsChecked));
        }

        public override void RegisterDataSetObserver(DataSetObserver observer)
        {
           base.RegisterDataSetObserver(observer);
           mObservers.Add(observer);
        }
        public override void NotifyDataSetChanged()
        {
           base.NotifyDataSetChanged();
           check_position.Clear();
           foreach ( DataSetObserver observer in mObservers )
           {
               observer.OnChanged();
           }
        }
    }
}