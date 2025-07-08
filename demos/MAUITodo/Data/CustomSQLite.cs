using PowerSync.Maui.SQLite;
using PowerSync.Common.MDSQLite;
using Microsoft.Data.Sqlite;
using PowerSync.Common.Client;
using PowerSync.Common.DB;

namespace MAUITodo.Data
{
    public class CustomSQLiteAdapter : MAUISQLiteAdapter
    {
        public CustomSQLiteAdapter(MDSQLiteAdapterOptions options) : base(options) { }

        protected override void LoadExtension(SqliteConnection db)
        {
            base.LoadExtension(db);

            try
            {
                db.EnableExtensions(true);
                db.LoadExtension(System.IO.Path.Combine(AppContext.BaseDirectory, "vec0.dll"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load vec0.dll: {ex}");
            }
        }
    }

    public class CustomSQLiteDBOpenFactory : ISQLOpenFactory
    {
        private readonly MDSQLiteOpenFactoryOptions options;

        public CustomSQLiteDBOpenFactory(MDSQLiteOpenFactoryOptions options)
        {
            this.options = options;
        }

        public IDBAdapter OpenDatabase()
        {
            return new CustomSQLiteAdapter(new MDSQLiteAdapterOptions
            {
                Name = options.DbFilename,
                SqliteOptions = options.SqliteOptions
            });
        }
    }

}
