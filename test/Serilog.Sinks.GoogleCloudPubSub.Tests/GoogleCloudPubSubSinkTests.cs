using System;
using Xunit;
using System.Collections.Generic;
using Google.Pubsub.V1;
using Google.Api.Gax;

namespace Serilog.Sinks.GoogleCloudPubSub.Tests
{

    [Collection(nameof(GoogleCloudPubsubFixture))]
    public class GoogleCloudPubSubSinkTests
    {

        //*****************************************************************************************
        //
        //  EXECUTION SETTINGS 
        //
        // Project: it has to be set on the environment variable "TEST_PROJECT".
        // Topic & Subscription: the fixture will create a temporary topic and subscription used only by the test. At the end
        //                       they will be deleted. Both will start with "xunit-test-".
        //
        // Authentication: it is used the Google-Cloud-Dotnet package to access PubSub so it has to be used the authentication
        //                 provided by this package: https://github.com/GoogleCloudPlatform/google-cloud-dotnet#authentication
        // 
        // Local buffer file (any path).
        private string _bufferPathLocal = "C:\\Tmp\\TestBufferPubSub\\test";
        //
        //  -It is necessary that the used subscription is empty so only testing messages will be contained: if not, if there
        //   are more messages than the testing ones, then the test result will be NOT OK.
        //  -If necessary it can be used the method CleanAllOnSubscription to clean the subscription.
        //  -Even if not cleaning the subscription at the begining, all recovered messages in the test will be erased.
        //
        //*****************************************************************************************

        private readonly GoogleCloudPubsubFixture _fixture;

        private readonly string _projectId;
        private readonly string _topicId;
        private readonly string _subscriptionId;
        private readonly string _subscriptionIdFull;

        private readonly SubscriberClient _subscriberClient = null;

        public GoogleCloudPubSubSinkTests(GoogleCloudPubsubFixture fixture)
        {
            this._fixture = fixture;

            //---

            this._projectId = this._fixture.ProjectId;
            this._topicId = this._fixture.CreateTopicId();
            this._subscriptionId = this._fixture.CreateSubscriptionId(this._topicId);
            this._subscriptionIdFull = this._fixture.GetProjectSubsFull(this._subscriptionId);

            //---

            // It is necessary to specify a correct Deadline time (in UTC). If not then it is thrown
            // the following error: Status(StatusCode=DeadlineExceeded, Detail="Deadline Exceeded")
            ServiceEndpoint sep = null;
            SubscriberSettings settings = new SubscriberSettings();
            settings.PullSettings = new CallSettings();
            settings.PullSettings.Timing = CallTiming.FromDeadline(DateTime.UtcNow.AddMinutes(5)); // 5 minutes deadline time.
            // Client to access PubSub for subscriptions.
            this._subscriberClient = SubscriberClient.Create(sep, settings);

        }


        //-----------------------


        /// <summary>
        /// This test uses the sink to send data to PubSub and then retrieves it to validate that are exactly the same.
        /// </summary>
        [Fact]
        public void BasicTest()
        {

            //-----------

            // If necessary it can be used the method CleanAllOnSubscription to clean the subscription.
            //this.CleanAllOnSubscription();

            //-----------

            // Initializing log...

            long bufferFileSizeLimitBytes = 10 * 1024 * 1024;

            ILogger testLogger = new LoggerConfiguration()
                .WriteTo.GoogleCloudPubSub(new GoogleCloudPubSubSinkOptions(this._projectId, this._topicId)
                {
                    BufferBaseFilename = _bufferPathLocal, // This means we will use a buffer file on disk instead of using memory.
                    BufferFileSizeLimitBytes = bufferFileSizeLimitBytes,
                    BufferLogShippingInterval = TimeSpan.FromMilliseconds(2000),       //Send to Google PubSub every 2 seconds. -> 2000
                    Period = TimeSpan.FromMilliseconds(2000),                          //Send to Google PubSub every 2 seconds. -> 2000
                })
                .CreateLogger();

            //----------

            // Creating logs (storing into PubSub in durable mode)...
            // We send two blocks of messages
            List<string> initialList = this.SendMessages(testLogger);

            //----------

            // Recovering from PubSub and comparing...
            HashSet<string> recoveredList = new HashSet<string>();
            this.AddMessagesFromPubSub(recoveredList);
            this.CompareLists(initialList, recoveredList);

        }



        //----------------------------------------------------------------------------

        private List<string> SendMessages(ILogger testLogger)
        {
            // We send two blocks of messages, validating so the internal rolling file.

            Random r = new Random();
            string currentMsg = null;
            List<string> initialList = new List<string>();

            //----------

            currentMsg = "Message A-" + r.Next().ToString();
            initialList.Add(currentMsg);
            testLogger.Warning(currentMsg);

            currentMsg = "Message B-" + r.Next().ToString();
            initialList.Add(currentMsg);
            testLogger.Warning(currentMsg);

            //----------

            // Waiting to give time to send...
            System.Threading.Thread.Sleep(10000);

            //----------

            currentMsg = "Message C-" + r.Next().ToString();
            initialList.Add(currentMsg);
            testLogger.Warning(currentMsg);

            currentMsg = "Message D-" + r.Next().ToString();
            initialList.Add(currentMsg);
            testLogger.Warning(currentMsg);

            //----------

            // Waiting to give time to send...
            System.Threading.Thread.Sleep(10000);

            //----------

            return initialList;
        }


        //----------------------------------------------------------------------------

        private void AddMessagesFromPubSub(HashSet<string> resultList)
        {
            try
            {
                //====================
                PullResponse response = this._subscriberClient.Pull(this._subscriptionIdFull, false, 100);
                //====================

                if (response.ReceivedMessages == null || response.ReceivedMessages.Count == 0)
                {
                    // No messages retrieved.
                    Assert.True(false);
                    return;
                }

                string str = null;

                foreach (var message in response.ReceivedMessages)
                {
                    // Unpack the message.
                    str = message.Message.Data.ToStringUtf8();
                    resultList.Add(str);
                }

                // Acknowledge the message so we don't see it again.
                this.AcknowledgeMessages(response);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                Assert.True(false);
            }

        }

        //----------------------------------------------------------------------------

        private void AcknowledgeMessages(PullResponse response)
        {
            // Acknowledge the messages so we don't see them again.
            var ackIds = new string[response.ReceivedMessages.Count];
            for (int i = 0; i < response.ReceivedMessages.Count; ++i)
            {
                ackIds[i] = response.ReceivedMessages[i].AckId;
            }

            //====================
            this._subscriberClient.Acknowledge(this._subscriptionIdFull, ackIds);
            //====================
        }

        //----------------------------------------------------------------------------

        private void CleanAllOnSubscription()
        {
            try
            {
                bool continueCleaning = true;

                while (continueCleaning)
                {
                    //====================
                    PullResponse response = this._subscriberClient.Pull(this._subscriptionIdFull, false, 100);
                    //====================

                    if (response.ReceivedMessages == null || response.ReceivedMessages.Count == 0)
                    {
                        // No messages retrieved. Finished
                        continueCleaning = false;
                        return;
                    }
                    else
                    {
                        // Acknowledge the message so we don't see it again.
                        this.AcknowledgeMessages(response);
                    }
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }

        }

        //----------------------------------------------------------------------------

        private void CompareLists(List<string> initialList, HashSet<string> recoveredList)
        {
            if (recoveredList == null || recoveredList.Count != initialList.Count)
            {
                Assert.True(false);
            }

            foreach (string str in initialList)
            {
                if (!recoveredList.Contains(str))
                {
                    Assert.True(false);
                }
            }

            // All ok.
            Assert.True(true);
        }

        //----------------------------------------------------------------------------
    }

}