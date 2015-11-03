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
using System.IO;

namespace buylist
{
    public class AppGlobal
    {
        public static string DatabaseFileName = "shoplist.db";
        public static string ExternalAppFolder = Android.OS.Environment.ExternalStorageDirectory+"/buylist";
        public static string ExternalAppDBFolder = ExternalAppFolder +"/WorkingDB";
        public static string DatabasebFilePath = System.IO.Path.Combine(AppGlobal.ExternalAppDBFolder, AppGlobal.DatabaseFileName);
    }
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
        public IEnumerable<ShopItem> query_selected_values(string cmd)
        {
            try
            {
                var db = new SQLiteConnection(db_path);
                return db.Query<ShopItem>(cmd);
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("exception handled", ex.Message);
            }
            return null;
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
        public double delete_rows(int ID)
        {
            var db = new SQLiteConnection(db_path);
            var query = db.Table<ShopItem>().Where(item => item.ID == ID);
            double cost = 0;
            if (query != null)
            {
                foreach (var obj in query.ToList<ShopItem>()) {
                    cost = obj.ItemCost;
                    db.Delete<ShopItem>(obj.ID);
                }
            }
            db.Commit();
            return cost;
        }
    }
}