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
    public class DBGlobal
    {
        public static string DatabaseFileName = "shoplist.db";
        public static string ExternalAppFolder = Android.OS.Environment.ExternalStorageDirectory+"/buylist";
        public static string ExternalAppDBFolder = ExternalAppFolder +"/WorkingDB";
        public static string DatabasebFilePath = System.IO.Path.Combine(DBGlobal.ExternalAppDBFolder, DBGlobal.DatabaseFileName);
    }
    class DBHelper
    {
        public string m_db_path { get; set; }
        public Context m_context { get; set; }

        //------------------------------------------------------------------------//
        //c'tor - context is needed to access shared preferences
        public DBHelper(string path,Context context)
        {
            m_db_path = path;
            m_context = context;
        }
        //------------------------------------------------------------------------//
        //base function where we create the target db residing folder structure and 
        //where we create the db file.
        public bool create_database()
        {
            try
            {
                if (!is_databasefolder_setup())
                {
                    setup_database_folder();
                    set_databasefolder_complete();

                    var connection = new SQLiteConnection(m_db_path);
                    connection.CreateTable<ShopItem>();
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("exception handled while creating database:{0}", ex.Message);
                return false;
            }
        }
        //------------------------------------------------------------------------//
        //all addition / insertion are performed using this function
        public bool insert_update_data(ShopItem data)
        {
            try
            {
                var db = new SQLiteConnection(m_db_path);
                    if (0 != db.Insert(data))
                        db.Update(data);
                return true;
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("exception handled while inserting data:{0}", ex.Message);
                return false;
            }
        }
        //------------------------------------------------------------------------//
        //used to replace an existing row
        public bool update_data(ShopItem data)
        {
            try
            {
                var db = new SQLiteConnection(m_db_path);
                //returns the number of rows affected
                if ( 0 == db.InsertOrReplace(data) )
                {
                    return false;
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("exception handled while replacing data:{0}", ex.Message);
                return false;
            }
        }
        //------------------------------------------------------------------------//
        //not used yet
        public bool insert_update_all(IEnumerable<ShopItem> data)
        {
            try
            {
                var db = new SQLiteConnection(m_db_path);
                if (db.InsertAll(data) != 0)
                    db.UpdateAll(data);
                return true;
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("exception handled while inserting data:{0}", ex.Message);
                return false;
            }
        }
        //------------------------------------------------------------------------//
        //returns an iteratable object <ShopItem> based on the query
        //values not queried for will be empty in that object
        public IEnumerable<ShopItem> query_selected_values(string cmd)
        {
            try
            {
                var db = new SQLiteConnection(m_db_path);
                return db.Query<ShopItem>(cmd);
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("exception handled while querying:{0}", ex.Message);
            }
            return null;
        }
        //------------------------------------------------------------------------//
        //used for debugging purposes
        public int get_total_records()
        {
            try
            {
                var db = new SQLiteConnection(m_db_path);
                // this counts all records in the database, it can be slow depending on the size of the database
                var count = db.ExecuteScalar<int>("SELECT Count(*) FROM ShopItem");
                return count;
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("get total records has failed, ex.msg :{0}",ex.Message);
                return -1;
            }
        }
        //------------------------------------------------------------------------//
        //returns the cost of the deleted item for deducting standard balance
        public double delete_rows(int ID)
        {
            double cost = 0;
            try
            {
                var db = new SQLiteConnection(m_db_path);
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
                if (!File.Exists(DBGlobal.DatabasebFilePath))
                {
                    // creating Database folder and file
                    create_workind_dbfile();
                }
            }
            catch (Exception ex)
            {
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
                isExists = System.IO.Directory.Exists(DBGlobal.ExternalAppFolder);

                // if not create the folder
                if (!isExists)
                    System.IO.Directory.CreateDirectory(DBGlobal.ExternalAppFolder);

                isExists = System.IO.Directory.Exists(DBGlobal.ExternalAppDBFolder);

                if (!isExists)
                    System.IO.Directory.CreateDirectory(DBGlobal.ExternalAppDBFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
        //------------------------------------------------------------------------//
        public void create_workind_dbfile()
        {
            try
            {
                //checking file exist in location or not
                if (!File.Exists(DBGlobal.DatabasebFilePath))
                {
                    using (var dest = File.Create(DBGlobal.DatabasebFilePath))
                    {
                        Console.WriteLine("Created the database filed:{0}", DBGlobal.DatabasebFilePath);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
        //one time task - db relocation check
        private bool is_databasefolder_setup()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(m_context);
            bool retvalue = prefs.GetBoolean("database_relocated", false);
            return retvalue;
        }
        private void set_databasefolder_complete()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(m_context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutBoolean("database_relocated", true);
            editor.Apply();        // applies changes asynchronously on newer APIs
        }
        //------------------------------------------------------------------------//
    }
}