using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JarvisAssistant.UnitTests.Services
{
    public class PlatformServiceInterfaceTests
    {
        [Fact]
        public void PlatformType_EnumValues_ShouldBeValid()
        {
            // Arrange & Act
            var values = Enum.GetValues<PlatformType>();

            // Assert
            Assert.Contains(PlatformType.Windows, values);
            Assert.Contains(PlatformType.Android, values);
            Assert.Contains(PlatformType.iOS, values);
            Assert.Contains(PlatformType.MacOS, values);
        }

        [Fact]
        public void AppTheme_EnumValues_ShouldContainPlatformSpecificThemes()
        {
            // Arrange & Act
            var values = Enum.GetValues<AppTheme>();

            // Assert
            Assert.Contains(AppTheme.Desktop, values);
            Assert.Contains(AppTheme.Mobile, values);
            Assert.Contains(AppTheme.TV, values);
            Assert.Contains(AppTheme.Light, values);
            Assert.Contains(AppTheme.Dark, values);
            Assert.Contains(AppTheme.System, values);
        }

        [Theory]
        [InlineData(PlatformType.Windows, AppTheme.Desktop)]
        [InlineData(PlatformType.MacOS, AppTheme.Desktop)]
        [InlineData(PlatformType.iOS, AppTheme.Mobile)]
        public void GetOptimalTheme_Logic_ShouldReturnExpectedThemes(PlatformType platform, AppTheme expectedTheme)
        {
            // This tests the logic that would be in a platform service implementation
            // Arrange & Act
            var result = platform switch
            {
                PlatformType.Windows or PlatformType.MacOS => AppTheme.Desktop,
                PlatformType.iOS => AppTheme.Mobile,
                PlatformType.Android => AppTheme.Mobile, // Default for Android
                _ => AppTheme.System
            };

            // Assert
            Assert.Equal(expectedTheme, result);
        }
    }
}
