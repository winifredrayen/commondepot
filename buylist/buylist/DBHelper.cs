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
    class DBHelper
    {
        public string db_path { get; set; }

        public DBHelper(string path)
        {
            db_path = path;
        }
        public string create_database()
        {
            try
            {
                var connection = new SQLiteConnection(db_path);
                connection.CreateTable<ShopItem>();
                return "Database created";
            }
            catch (SQLiteException ex)
            {
                return ex.Message;
            }
        }

        public string insert_update_data(ShopItem data)
        {
            try
            {
                var db = new SQLiteConnection(db_path);
                if (db.Insert(data) != 0)
                    db.Update(data);
                return "Single data file inserted or updated";
            }
            catch (SQLiteException ex)
            {
                return ex.Message;
            }
        }

        public string insert_update_all(IEnumerable<ShopItem> data)
        {
            try
            {
                var db = new SQLiteConnection(db_path);
                if (db.InsertAll(data) != 0)
                    db.UpdateAll(data);
                return "List of data inserted or updated";
            }
            catch (SQLiteException ex)
            {
                return ex.Message;
            }
        }

        public int get_total_records()
        {
            try
            {
                var db = new SQLiteConnection(db_path);
                // this counts all records in the database, it can be slow depending on the size of the database
                var count = db.ExecuteScalar<int>("SELECT Count(*) FROM ShopItem");

                // for a non-parameterless query
                // var count = db.ExecuteScalar<int>("SELECT Count(*) FROM ShopItem WHERE FirstName="Amy");

                return count;
            }
            catch (SQLiteException)
            {
                return -1;
            }
        }
    }
}