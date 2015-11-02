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

namespace buylist
{
    public class OnBudgetEvtArgs : EventArgs
    {
        private double mMonthlyBudget;
        public double budget
        {
            get { return mMonthlyBudget; }
            set { mMonthlyBudget = value; }
        }
        public OnBudgetEvtArgs(double _givenbudget)
        {
            mMonthlyBudget = _givenbudget;
        }
    }
    class dialog_input_budget : DialogFragment
    {
        public event EventHandler<OnBudgetEvtArgs> mOnBudgetAdded;
        public event EventHandler<OnShopItemError> mOnError;
        private Button mSavebtn;
        private EditText mBudgetInput;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.inputdialogbox,container, false);

            mSavebtn = view.FindViewById<Button>(Resource.Id.savebudget);
            mBudgetInput = view.FindViewById<EditText>(Resource.Id.budgetvalue);

            mSavebtn.Click += onSaveButtonClicked;
            return view;
        }

        private void onSaveButtonClicked(object sender, EventArgs e)
        {
            //evaluation is pending here. 
            if( mBudgetInput.Text.Equals(string.Empty))
            {
                Console.WriteLine("ERROR: Budget cannot be empty");
                mOnError.Invoke(this, new OnShopItemError("error: Budget value cannot be empty"));
            }
            else
            {
                //convert the string to double
                double budgetvalue = Double.Parse(mBudgetInput.Text);

                //make an event and broadcast it. --> mOnBudgetAdded
                mOnBudgetAdded.Invoke(this, new OnBudgetEvtArgs(budgetvalue));

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