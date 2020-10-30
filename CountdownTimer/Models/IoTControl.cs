using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Windows.Web.Http.Headers;

namespace CountdownTimer.Models
{
    public static class IoTControl
    {
        public static readonly HttpClient client = new HttpClient( );

        public static async Task<string> SetLightAsync(int lightStatus)
        {
            string response;

            try
            {
                if ( lightStatus == 0 )
                {
                    response = await client.GetStringAsync(new Uri(@"http://192.168.1.252:5000/on/saturation/100"));
                }
                else
                {
                    response = await client.GetStringAsync(new Uri(@"http://192.168.1.252:5000/on/saturation/0"));
                }
            } 
            catch (HttpRequestException http_e) {
                response = $"FAILED: {http_e}";
            }
            return response;
        }
    }
}
