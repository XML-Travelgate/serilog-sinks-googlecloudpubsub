using System;
using Xunit;
using Xunit.Abstractions;
using Google.Pubsub.V1;

namespace Serilog.Sinks.GoogleCloudPubSub.Tests
{

    [Collection(nameof(GoogleCloudPubsubFixture))]
    public class BasicTests
    {
        private readonly GoogleCloudPubsubFixture _fixture;
        private readonly ITestOutputHelper _output;
        
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
        public void ListTopics()
        {
            string projectId = _fixture.ProjectId;

            // Snippet: ListTopics 
            PublisherClient client = PublisherClient.Create();

            // Alternative: use a known project resource name:
            // "projects/{PROJECT_ID}"
            string projectName = PublisherClient.FormatProjectName(projectId);
            foreach (Topic topic in client.ListTopics(projectName))
            {
                _output.WriteLine(topic.Name);
            }
            // End snippet
        }
        
        
        //public void GooglePubsubCheck()
        //{
        // string projectId = this._fixture.Config["PubsubProjectId"];
        // string topicId = this._fixture.Config["PubsubTopicId"];
        // string subId = this._fixture.Config["PubsubSubId"];
        // _output.WriteLine($"Using [{projectId}],[{topicId}],[{subId}]]");

        //  //Test Google.Pubsub.V1 library  
        //  //PublisherClient publisher = PublisherClient.Create();
        //  //string topicName = PublisherClient.FormatTopicName(projectId,topicId);


        //  // Subscribe to the topic.
        //  SubscriberClient subscriber = SubscriberClient.Create();
        //  string subscriptionName = SubscriberClient.FormatSubscriptionName(projectId, subId);
        //  subscriber.CreateSubscription(subscriptionName, subId, pushConfig: null, ackDeadlineSeconds: 60);

        //  PullResponse response = subscriber.Pull(subscriptionName, returnImmediately: true, maxMessages: 10);
        //    foreach (ReceivedMessage received in response.ReceivedMessages)
        //    {
        //            PubsubMessage msg = received.Message;
        //            Console.WriteLine($"Received message {msg.MessageId} published at {msg.PublishTime.ToDateTime()}");
        //            Console.WriteLine($"Text: '{msg.Data.ToStringUtf8()}'");
        //    }


        //}

    }
}
