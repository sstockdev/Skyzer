using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace SkyzerSync
{
    public class AuctionsEndedWorker(ILogger<ActiveAuctionsWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


        }
    }
}
