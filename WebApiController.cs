using System;
using System.Collections.Generic;
using System.Web.Http;


namespace Termors.Serivces.HippotronicsLedDaemon
{
    public class SetLampData
    {
        public SetLampData()
        {
            Red = Green = Blue = -1;    // Indicating not set
        }

        public string On { get; set; }
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
    }

    public class WebApiController : ApiController
    {
        [Route("webapi/refresh"), HttpGet]
        public void Refresh()
        {
            // No real function anymore
        }

        [Route("webapi/lamps"),HttpGet]
        public LampNode[] GetLamps()
        {
            lock (DatabaseClient.Synchronization)
            {
                using (var db = new DatabaseClient())
                {
                    var records = db.GetAll();
                    List<LampNode> retval = new List<LampNode>();

                    foreach (var r in records) retval.Add(r);
                    return retval.ToArray();
                }
            }
        }

        [Route("webapi/lamp/{id}"), HttpGet]
        public LampNode GetLamp(string id)
        {
            lock (DatabaseClient.Synchronization)
            {
                using (var db = new DatabaseClient())
                {
                    var record = db.GetOne(id);
                    return record;
                }
            }

        }

        [Route("webapi/lamp/{id}"), HttpPost]
        public LampNode SetLampState(string id, SetLampData data)
        {
            lock (DatabaseClient.Synchronization)
            {
                using (var db = new DatabaseClient())
                {
                    var record = db.GetOne(id);
                    if (record == null) return null;

                    // See what was set
                    bool changed = false;
                    if (data.Red >= 0 && data.Red <= 255)
                    {
                        record.Red = Convert.ToByte(data.Red);
                        changed = true;
                    }
                    if (data.Green >= 0 && data.Green <= 255)
                    {
                        record.Green = Convert.ToByte(data.Green);
                        changed = true;
                    }
                    if (data.Blue >= 0 && data.Blue <= 255)
                    {
                        record.Blue = Convert.ToByte(data.Blue);
                        changed = true;
                    }

                    bool dataOn = Convert.ToBoolean(data.On);
                    if (dataOn != record.On)
                    {
                        record.On = dataOn;
                        changed = true;
                    }

                    if (changed)
                    {
                        var client = new LampClient(record);

                        client.SetState().Wait();       // Synchronously set the state. It may throw an exception
                        db.AddOrUpdate(record);         // and if not, set the last seen time to update.
                    }

                    return record;
                }
            }
        }
    }
}
