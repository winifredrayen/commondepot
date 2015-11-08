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

using SQLite;

namespace buylist
{
    public class OnShopItemError : EventArgs
    {
        private string mErrorMsg;
        public string error_msg
        {
            get { return mErrorMsg;  }
            set { mErrorMsg = value; }
        }
        public OnShopItemError(string _error)
        {
            mErrorMsg = _error;
        }
    }
    public class OnShopItemSaveEvtArgs : EventArgs
    {
        private ExistingListActivity.dboperations m_operation;
        private string mItem_Brief;
        private double mItem_Cost;
        private double mItem_Priority;
        private string mItem_Desc;
        private int m_ID;

        public ExistingListActivity.dboperations operate
        {
            get { return m_operation; }
            set { m_operation = value; }
        }
        public int ID
        {
            get { return m_ID; }
            set { m_ID = value; }
        }
        public string item_brief
        {
            get { return mItem_Brief;  }
            set { mItem_Brief = value; }
        }
        public string item_description
        {
            get { return mItem_Desc; }
            set { mItem_Desc = value; }
        }
        public double item_Cost
        {
            get { return mItem_Cost; }
            set { mItem_Cost = value; }
        }
        public double item_priority
        {
            get { return mItem_Priority; }
            set { mItem_Priority = value; }
        }
        public OnShopItemSaveEvtArgs(string _brief, 
            string _desc, double _priority, double _cost,
            ExistingListActivity.dboperations _operation, int _ID = 0) : base()
        {
            ID = _ID;
            item_brief = _brief;
            item_Cost = _cost;
            item_priority = _priority;
            item_description = _desc;
            operate = _operation;
        }
    }
    public class dialog_getitem_info : DialogFragment
    {
        //UI fill-data
        private ShopItem mShopItem;
        private ExistingListActivity.dboperations mOperation;
        public ShopItem this_shopitem
        {
            get { return mShopItem; }
            set { mShopItem = value; }
        }

        public ExistingListActivity.dboperations this_operation
        {
            get { return mOperation; }
            set { mOperation = value; }
        }

        //UI elements
        private EditText mitem_cost;
        private EditText mitem_brief;
        private EditText mitem_description;
        private RatingBar mitem_priority;

        public event EventHandler<OnShopItemSaveEvtArgs> mOnShopItemAdded;
        public event EventHandler<OnShopItemError> mOnError;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.ItemInputForm, container, false);

            mitem_cost = view.FindViewById<EditText>(Resource.Id.item_cost);
            mitem_brief = view.FindViewById<EditText>(Resource.Id.item_brief);
            mitem_description = view.FindViewById<EditText>(Resource.Id.item_desc);
            mitem_priority = view.FindViewById<RatingBar>(Resource.Id.item_priority);

            Button savebtn = view.FindViewById<Button>(Resource.Id.savebtn);
            //user has clicked the save button
            savebtn.Click += btn_onSaveShopItems;

            if(mShopItem != null)
                setitemcontent();

            return view; 
        }

        private void setitemcontent()
        {
            //initialize with the existing values if available
            mitem_brief.Text = mShopItem.ItemBrief;
            mitem_cost.Text = mShopItem.ItemCost.ToString();
            mitem_description.Text = mShopItem.ItemDescription;
            mitem_priority.Rating = (float)mShopItem.ItemPriority;

            //Console.WriteLine("What was the previously set priority? :{0}", mShopItem.ItemPriority);
        }

        void btn_onSaveShopItems(object sender, EventArgs e)
        {
            //evaluation is pending here. 
            if( mitem_brief.Text.Equals(string.Empty) || 
                mitem_cost.Text.Equals(string.Empty) ||
                mitem_priority.Rating == 0 )
            {
                Console.WriteLine("item description, cost and priority are mandatory");
                mOnError.Invoke(this, new OnShopItemError("error - item description, cost and priority are mandatory"));
            }
            else
            {
                //convert the string to double
                double costvalue = Double.Parse(mitem_cost.Text);
                double priorityvalue = Double.Parse(mitem_priority.Rating.ToString());

                //make an event and broadcast it. --> mOnShopItemAdded
                mOnShopItemAdded.Invoke(this, new OnShopItemSaveEvtArgs(mitem_brief.Text, mitem_description.Text,
                    priorityvalue, costvalue, mOperation , (mShopItem != null) ? mShopItem.ID : 0));

                this.Dismiss();
            }

        }
        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            base.OnActivityCreated(savedInstanceState);
            Dialog.Window.Attributes.WindowAnimations = Resource.Style.dialog_animation;
        }
    }
}
