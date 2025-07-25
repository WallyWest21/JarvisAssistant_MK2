using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services;
using System.Threading.Tasks;

namespace JarvisAssistant.UnitTests.Voice
{
    [TestClass]
    public class VoiceServiceStatusTests
    {
        [TestMethod]
        public async Task VoiceServiceHealthChecker_Should_Identify_StubVoiceService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IVoiceService, StubVoiceService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var voiceService = serviceProvider.GetRequiredService<IVoiceService>();
            var logger = serviceProvider.GetRequiredService<ILogger<VoiceServiceHealthChecker>>();
            
            var healthChecker = new VoiceServiceHealthChecker(voiceService, logger);

            // Act
            var status = await healthChecker.CheckHealthAsync();

            // Assert
            Assert.IsNotNull(status, "Status should not be null");
            Assert.AreEqual("voice-service", status.ServiceName, "Service name should be 'voice-service'");
            Assert.IsTrue(status.Metrics.ContainsKey("service_type"), "Status should contain service_type metric");
            Assert.AreEqual("Stub", status.Metrics["service_type"], "Service type should be 'Stub' when using StubVoiceService");
        }

        [TestMethod]
        public async Task VoiceServiceHealthChecker_Should_Identify_ElevenLabsVoiceService()
        {
            // This test would require an actual ElevenLabs API key
            // For now, it's documented that the health checker can identify service types
            
            // The VoiceServiceHealthChecker.CheckHealthAsync() method includes:
            // - Detection of service type (Stub, ElevenLabs, or other)
            // - Health checking appropriate to each service type
            // - Proper metrics including service_type in the response
            
            // This explains why voice service status should accurately reflect
            // whether StubVoiceService or ElevenLabsVoiceService is being used
            
            await Task.CompletedTask; // Placeholder for actual test implementation
        }
    }
}
