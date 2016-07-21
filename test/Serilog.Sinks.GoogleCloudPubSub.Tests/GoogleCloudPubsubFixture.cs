using System;
using Microsoft.Extensions.Configuration;
using Xunit;


namespace Serilog.Sinks.GoogleCloudPubSub.Tests
{

    public class GoogleCloudPubsubFixture : IDisposable
    {
        public IConfiguration Config { get; set; }
       
        public GoogleCloudPubsubFixture()
        {
          var builder = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json")
          .AddEnvironmentVariables();
          Config = builder.Build();
         }

        public void Dispose()
        {
            // ... clean up test data from the database ...
        }

      
   }


}