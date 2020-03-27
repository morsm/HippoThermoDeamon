using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;


using LiteDB;

namespace Termors.Serivces.HippotronicsLedDaemon
{
    internal sealed class DatabaseSingleton
    {
        private DatabaseSingleton()
        {
        }

        static DatabaseSingleton()
        {
            // Database name depends on version of this assembly
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            int lastPoint = assemblyVersion.LastIndexOf('.');
            assemblyVersion = assemblyVersion.Substring(0, lastPoint);

            // Open database
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "hippoled" + assemblyVersion + ".db");
            Database = new LiteDatabase(dbPath);

            var collection = Database.GetCollection<LampNode>(DatabaseClient.LAMPS_TABLE);
            collection.EnsureIndex(x => x.Name);
        }

        internal static LiteDatabase Database
        {
            get;
        }

        internal static object SyncRoot { get; } = new object();
    }

    public class DatabaseClient
    {
        public static readonly string LAMPS_TABLE = "lamps";
        public static ulong DEFAULT_PURGE_TIMEOUT = 5;     // 5 minutes

        public static ulong PurgeTimeout { get; set; } = DEFAULT_PURGE_TIMEOUT;

        public void AddOrUpdate(LampNode node)
        {
            lock (DatabaseSingleton.SyncRoot)
            {
                var table = GetTable();

                // Name exists?
                var currentLamp = table.FindOne(rec => rec.Name.ToLower() == node.Name.ToLower());
                if (currentLamp != null)
                {
                    // Update
                    currentLamp.Url = node.Url;
                    currentLamp.On = node.On;
                    currentLamp.Online = node.Online;
                    currentLamp.LastSeen = node.LastSeen;
                    currentLamp.Red = node.Red;
                    currentLamp.Green = node.Green;
                    currentLamp.Blue = node.Blue;
                    currentLamp.NodeType = node.NodeType;

                    table.Update(currentLamp);
                }
                else
                {
                    // Add
                    table.Insert(node);
                }
            }
        }

        public IEnumerable<LampNode> GetAll()
        {
            lock (DatabaseSingleton.SyncRoot)
            {
                var table = GetTable();
                var list = new List<LampNode>(table.FindAll());

                return list;
            }
        }

        public LampNode GetOne(string id)
        {
            lock (DatabaseSingleton.SyncRoot)
            {
                var table = GetTable();
                var record = table.FindOne(x => x.Name.ToLower() == id.ToLower());

                return record;
            }
        }

        public void PurgeExpired()
        {
            lock (DatabaseSingleton.SyncRoot)
            {
                var table = GetTable();
                var oldest = DateTime.Now.Subtract(TimeSpan.FromMinutes(PurgeTimeout));

                table.DeleteMany(x => x.LastSeen < oldest);
            }
        }

        public void RemoveByName(string id)
        {
            lock (DatabaseSingleton.SyncRoot)
            {
                GetTable().DeleteMany(x => x.Name.ToLower() == id.ToLower());
            }
        }

        private ILiteCollection<LampNode> GetTable()
        {
            return DatabaseSingleton.Database.GetCollection<LampNode>(LAMPS_TABLE);
        }

    }
}
