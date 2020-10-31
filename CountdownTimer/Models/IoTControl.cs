using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Windows.Web.Http.Headers;
using System.Threading;
using Windows.Gaming.Input;
using System.Diagnostics;
using Windows.ApplicationModel.UserDataTasks.DataProvider;

namespace CountdownTimer.Models
{
    public class IoTControl
    {
        public int lightStatus;
        public static readonly HttpClient client = new HttpClient( );


        public async Task<string> SetLightAsync(int _lightStatus)
        {
            string response;

            try
            {
                lightStatus = _lightStatus;
                if ( _lightStatus == 0 )
                {
                    var cancellationToken = new CancellationTokenSource( );
                    response = await client.GetStringAsync(new Uri(@"http://192.168.1.252:5000/on/saturation/100"));
                    await Task.Factory.StartNew(async () => await NotSafeAsync(cancellationToken.Token), TaskCreationOptions.LongRunning);
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

        public async Task NotSafeAsync(CancellationToken _cancellationToken)
        {
            try
            {
                string msgResponse;
                while (lightStatus == 0)
                {
                    msgResponse = await client.GetStringAsync(new Uri(@"http://192.168.1.162:5000/msg/notsafe"));
                    Debug.WriteLine($"msgRepsonse = {msgResponse}");
                }
                
                msgResponse = await client.GetStringAsync(new Uri(@"http://192.168.1.162:5000/random"));
                Debug.WriteLine($"msgRepsonse = {msgResponse}");

            }
            catch (Exception e)
            {
                Debug.WriteLine("Something bad happened when printing NOTSAFE :(");
            }
        }
    }
}
