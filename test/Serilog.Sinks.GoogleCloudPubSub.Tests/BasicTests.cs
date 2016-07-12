using System;
using Xunit;

namespace Serilog.Sinks.GoogleCloudPubSub.Tests
{
    public class BasicTests
    {
         [Fact]
        public void TestOK()
        {
            Assert.True(true, $"Test true test");
        }

    }
}
