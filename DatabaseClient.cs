using System;
using System.Collections.Generic;
using System.IO;


using LiteDB;

namespace Termors.Serivces.HippotronicsLedDaemon
{
    public class DatabaseClient : IDisposable
    {
        protected readonly LiteDatabase _db;

        public static readonly string LAMPS_TABLE = "lamps";
        public static ulong DEFAULT_PURGE_TIMEOUT = 300000;     // 5 minutes

        public static ulong PurgeTimeout { get; set; }

        public static object Synchronization { get; } = new object();

        public DatabaseClient()
        {
            // Set default timeout
            PurgeTimeout = DEFAULT_PURGE_TIMEOUT;

            // Open database
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "hippoled.db");
            _db = new LiteDatabase(dbPath);

            // Primary key on Name in the lamps table.
            // Really only does something the first time the table is created
        }

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
            _db.Dispose();
        }

        private LiteCollection<LampNode> GetTable()
        {
            return _db.GetCollection<LampNode>(LAMPS_TABLE);
        }

    }
}
