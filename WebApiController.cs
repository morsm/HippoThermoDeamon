using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;


namespace Termors.Serivces.HippotronicsThermoDaemon
{
    public class WebApiController : ApiController
    {
        [Route("webapi/html"), HttpGet]
        public HttpResponseMessage GetWebPage()
        {
            var result = WebPageGenerator.GenerateWebPage();

            var res = Request.CreateResponse(HttpStatusCode.OK);
            res.Content = new StringContent(result.ToString(), System.Text.Encoding.UTF8, "text/html");

            return res;
        }


        [Route("webapi/roomtemp"), HttpGet]
        public async Task<Temperature> GetRoomTemperature()
        {
            Temperature temp = new Temperature();

            temp.CelsiusValue = await ThermostatDaemon.Instance.ReadRoomTemperatureCelsius();

            return temp;
        }

        [Route("webapi/targettemp"), HttpGet]
        public Temperature GetTargetTemperature()
        {
            return GetThermostatState().TargetTemperature;
        }

        [Route("webapi/state"), HttpGet]
        public ThermostatState GetThermostatState()
        {
            return ThermostatDaemon.Instance.InternalState;
        }

        [Route("webapi/targettemp"), HttpPost]
        public ThermostatState SetTargetTemp(Temperature temp)
        {
            // Set temp
            ThermostatDaemon.Instance.SetTargetTemperature(temp);

            // Return total thermostat state
            return GetThermostatState();
        }

        [Route("webapi/tempdelta"), HttpPost]
        public ThermostatState SetTempDelta(Temperature tempDelta)
        {
            // Set temp
            ThermostatDaemon.Instance.SetTargetTemperature(ThermostatDaemon.Instance.InternalState.TargetTemperature + tempDelta);

            // Return total thermostat state
            return GetThermostatState();
        }

    }
}
