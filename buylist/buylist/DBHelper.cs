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
using Android.Preferences;

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
        public Context mContext { get; set; }

        public DBHelper(string path,Context context)
        {
            db_path = path;
            mContext = context;
        }
        public string create_database()
        {
            try
            {
                if (!is_databasefolder_setup())
                {
                    setup_database_folder();
                    set_databasefolder_complete();

                    var connection = new SQLiteConnection(db_path);
                    connection.CreateTable<ShopItem>();
                }
                return "Database created";
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("exception handled while creating database:{0}", ex.Message);
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
                Console.WriteLine("exception handled while inserting data:{0}", ex.Message);
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
                Console.WriteLine("exception handled while inserting data:{0}", ex.Message);
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
                Console.WriteLine("exception handled while querying:{0}", ex.Message);
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
            catch (SQLiteException ex)
            {
                Console.WriteLine("get total records has failed, ex.msg :{0}",ex.Message);
                return -1;
            }
        }
        public double delete_rows(int ID)
        {
            double cost = 0;
            try
            {
                var db = new SQLiteConnection(db_path);
                var query = db.Table<ShopItem>().Where(item => item.ID == ID);
                if (query != null)
                {
                    foreach (var obj in query.ToList<ShopItem>())
                    {
                        cost = obj.ItemCost;
                        db.Delete<ShopItem>(obj.ID);
                    }
                }
                db.Commit();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("deleting row has failed, invalid parameters ID: {0} ex.msg :{1}", ID,ex.Message);
                return -1;
            }
            return cost;
        }

        //------------------------------------------------------------------------//
        //DB relocation and directory creation
        private void setup_database_folder()
        {
            try
            {
                // function call used fro creating a working directory
                create_directory();

                // checking is there a directory available in the same external storage, if not create one
                if (!File.Exists(AppGlobal.DatabasebFilePath))
                {
                    // creating Database folder and file
                    create_workind_dbfile();
                }
            }
            catch (Exception ex)
            {
                //Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
        //------------------------------------------------------------------------//
        public void create_directory()
        {
            bool isExists = false;
            try
            {
                // checking folder available or not
                isExists = System.IO.Directory.Exists(AppGlobal.ExternalAppFolder);

                // if not create the folder
                if (!isExists)
                    System.IO.Directory.CreateDirectory(AppGlobal.ExternalAppFolder);

                isExists = System.IO.Directory.Exists(AppGlobal.ExternalAppDBFolder);

                if (!isExists)
                    System.IO.Directory.CreateDirectory(AppGlobal.ExternalAppDBFolder);
            }
            catch (Exception ex)
            {
                //Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
        //------------------------------------------------------------------------//
        public void create_workind_dbfile()
        {
            try
            {
                //checking file exist in location or not
                if (!File.Exists(AppGlobal.DatabasebFilePath))
                {
                    {
                        using (var dest = File.Create(AppGlobal.DatabasebFilePath))
                        {
                            Console.WriteLine("Created the database filed:{0}", AppGlobal.DatabasebFilePath);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                //Toast.MakeText(this, ex.ToString(), ToastLength.Long).Show();
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
        //one time task - db relocation check
        private bool is_databasefolder_setup()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(mContext);
            bool retvalue = prefs.GetBoolean("database_relocated", false);
            return retvalue;
        }
        private void set_databasefolder_complete()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(mContext);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutBoolean("database_relocated", true);
            // editor.Commit();    // applies changes synchronously on older APIs
            editor.Apply();        // applies changes asynchronously on newer APIs
        }
        //------------------------------------------------------------------------//
    }
}