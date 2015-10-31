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
    class ShopItem
    {
        [PrimaryKey,AutoIncrement]
        public int ID { get; set; }

        public string ItemBrief { get; set; }

        public double ItemCost { get; set; }

        public double ItemPriority { get; set; }

        public string ItemDescription { get; set; }

        public override string ToString()
        {
            return string.Format("[SHOPITEM: ID={0}, ItemBrief={1}, ItemCost={2}, ItemPriority={3},ItemDescription={4}]", ID, ItemBrief, ItemCost, ItemPriority, ItemDescription);
        }
    }
}