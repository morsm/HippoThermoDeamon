using System;
using System.Collections.Generic;
using System.IO;


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
            // Open database
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "hippoled.db");
            Database = new LiteDatabase(dbPath);
        }

        internal static LiteDatabase Database
        {
            get;
        }
    }

    public class DatabaseClient : IDisposable
    {
        public static readonly string LAMPS_TABLE = "lamps";
        public static ulong DEFAULT_PURGE_TIMEOUT = 300000;     // 5 minutes

        public static ulong PurgeTimeout { get; set; } = DEFAULT_PURGE_TIMEOUT;

        public static object Synchronization { get; } = new object();


        public void AddOrUpdate(LampNode node)
        {
            var table = GetTable();

            // Name exists?
            var currentLamp = table.FindOne(rec => rec.Name.Equals(node.Name));
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

        public IEnumerable<LampNode> GetAll()
        {
            var table = GetTable();

            return table.FindAll();
        }

        public LampNode GetOne(string id)
        {
            var table = GetTable();
            var record = table.FindOne(x => x.Name.ToLower() == id.ToLower());

            return record;
        }

        public void PurgeExpired()
        {
            var table = GetTable();
            var now = DateTime.Now;

            table.Delete(x => x.LastSeen.AddMilliseconds(PurgeTimeout) < now);
        }

        public void RemoveByName(string id)
        {
            GetTable().Delete(x => x.Name.ToLower() == id.ToLower());
        }

        public void Dispose()
        {
            // TODO: perhaps remove
            //_db.Dispose();
        }

        private LiteCollection<LampNode> GetTable()
        {
            return DatabaseSingleton.Database.GetCollection<LampNode>(LAMPS_TABLE);
        }

    }
}
