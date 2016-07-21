using System;
using Xunit;
using Xunit.Abstractions;
using Google.Pubsub.V1;

namespace Serilog.Sinks.GoogleCloudPubSub.Tests
{
    public class BasicTests : IClassFixture<GoogleCloudPubsubFixture>
    {
        private readonly ITestOutputHelper _output;
        private GoogleCloudPubsubFixture _fixture;


        public BasicTests(ITestOutputHelper output, GoogleCloudPubsubFixture fixture)
        {
            this._output = output;
            this._fixture = fixture;
        }
         
         [Fact]
        public void TestOK()
        {
            Assert.True(true, $"Test true test");
        }

         [Fact]
        public void GoogleApisPubsubCheck()
        {
         //   _output.WriteLine("En PubSub!!!!");
          //Test Google.Pubsub.V1 library  
            //PublisherClient publisher = PublisherClient.Create();
            //string topicName = PublisherClient.FormatTopicName(this._fixture.Config["PubsubProjectId"], this._fixture.Config["PubsubTopicId"]);
          
          //Test Google.Apis.Pubsub library
        //  PubsubService pubSubservice = new PubSu

        }
        
        [Fact]
        public void GooglePubsubCheck()
        {
         //   _output.WriteLine("En PubSub!!!!");
          //Test Google.Pubsub.V1 library  
          PublisherClient publisher = PublisherClient.Create();
          string topicName = PublisherClient.FormatTopicName(this._fixture.Config["PubsubProjectId"], this._fixture.Config["PubsubTopicId"]);
          
        }

    }
}
