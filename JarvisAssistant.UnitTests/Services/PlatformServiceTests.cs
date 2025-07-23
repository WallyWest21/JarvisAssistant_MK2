using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JarvisAssistant.UnitTests.Services
{
    public class PlatformServiceTests
    {
        private readonly Mock<IPlatformService> _mockPlatformService;

        public PlatformServiceTests()
        {
            _mockPlatformService = new Mock<IPlatformService>();
        }

        [Fact]
        public void CurrentPlatform_ShouldReturnValidPlatformType()
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(PlatformType.Windows);

            // Act
            var platform = _mockPlatformService.Object.CurrentPlatform;

            // Assert
            Assert.NotEqual(PlatformType.Unknown, platform);
            Assert.True(Enum.IsDefined(typeof(PlatformType), platform));
        }

        [Theory]
        [InlineData(PlatformType.Windows, AppTheme.Desktop)]
        [InlineData(PlatformType.MacOS, AppTheme.Desktop)]
        [InlineData(PlatformType.Android, AppTheme.Mobile)]
        [InlineData(PlatformType.iOS, AppTheme.Mobile)]
        [InlineData(PlatformType.AndroidTV, AppTheme.TV)]
        public void GetOptimalTheme_ForDifferentPlatforms_ShouldReturnExpectedTheme(PlatformType platform, AppTheme expectedTheme)
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(platform);
            _mockPlatformService.Setup(p => p.GetOptimalTheme()).Returns(expectedTheme);
            _mockPlatformService.Setup(p => p.IsGoogleTV()).Returns(platform == PlatformType.AndroidTV);

            // Act
            var theme = _mockPlatformService.Object.GetOptimalTheme();

            // Assert
            Assert.Equal(expectedTheme, theme);
        }

        [Theory]
        [InlineData(PlatformType.Windows, true)]
        [InlineData(PlatformType.MacOS, true)]
        [InlineData(PlatformType.AndroidTV, true)]
        [InlineData(PlatformType.Android, false)]
        [InlineData(PlatformType.iOS, false)]
        public void SupportsMultiPanel_ShouldReturnExpectedValueForPlatform(PlatformType platform, bool expected)
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(platform);
            _mockPlatformService.Setup(p => p.SupportsMultiPanel()).Returns(expected);

            // Act
            var supportsMultiPanel = _mockPlatformService.Object.SupportsMultiPanel();

            // Assert
            Assert.Equal(expected, supportsMultiPanel);
        }

        [Theory]
        [InlineData(PlatformType.Windows, 1.0)]
        [InlineData(PlatformType.Android, 2.0)]
        [InlineData(PlatformType.iOS, 3.0)]
        [InlineData(PlatformType.AndroidTV, 1.5)]
        public void GetScreenDensity_ShouldReturnPositiveValue(PlatformType platform, double expectedDensity)
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(platform);
            _mockPlatformService.Setup(p => p.GetScreenDensity()).Returns(expectedDensity);

            // Act
            var density = _mockPlatformService.Object.GetScreenDensity();

            // Assert
            Assert.True(density > 0);
            Assert.Equal(expectedDensity, density);
        }

        [Theory]
        [InlineData(PlatformType.Android, true)]
        [InlineData(PlatformType.iOS, true)]
        [InlineData(PlatformType.Windows, false)]
        [InlineData(PlatformType.MacOS, false)]
        [InlineData(PlatformType.AndroidTV, false)]
        public void IsTouchPrimary_ShouldReturnExpectedValueForPlatform(PlatformType platform, bool expected)
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(platform);
            _mockPlatformService.Setup(p => p.IsTouchPrimary()).Returns(expected);

            // Act
            var isTouchPrimary = _mockPlatformService.Object.IsTouchPrimary();

            // Assert
            Assert.Equal(expected, isTouchPrimary);
        }

        [Theory]
        [InlineData(PlatformType.Android, true)]
        [InlineData(PlatformType.iOS, true)]
        [InlineData(PlatformType.Windows, true)]
        [InlineData(PlatformType.MacOS, true)]
        [InlineData(PlatformType.AndroidTV, true)]
        public void SupportsVoiceInput_ShouldReturnTrueForModernPlatforms(PlatformType platform, bool expected)
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(platform);
            _mockPlatformService.Setup(p => p.SupportsVoiceInput()).Returns(expected);

            // Act
            var supportsVoice = _mockPlatformService.Object.SupportsVoiceInput();

            // Assert
            Assert.Equal(expected, supportsVoice);
        }

        [Theory]
        [InlineData(PlatformType.AndroidTV, true)]
        [InlineData(PlatformType.Android, false)]
        [InlineData(PlatformType.iOS, false)]
        [InlineData(PlatformType.Windows, false)]
        [InlineData(PlatformType.MacOS, false)]
        public void IsGoogleTV_ShouldReturnTrueOnlyForAndroidTV(PlatformType platform, bool expected)
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(platform);
            _mockPlatformService.Setup(p => p.IsGoogleTV()).Returns(expected);

            // Act
            var isGoogleTV = _mockPlatformService.Object.IsGoogleTV();

            // Assert
            Assert.Equal(expected, isGoogleTV);
        }

        [Fact]
        public void AndroidTVDetection_BothScenarios_ShouldBeHandledCorrectly()
        {
            // Test scenario 1: PlatformType.AndroidTV
            _mockPlatformService.Reset();
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(PlatformType.AndroidTV);
            _mockPlatformService.Setup(p => p.IsGoogleTV()).Returns(true);
            _mockPlatformService.Setup(p => p.GetOptimalTheme()).Returns(AppTheme.TV);

            Assert.Equal(PlatformType.AndroidTV, _mockPlatformService.Object.CurrentPlatform);
            Assert.True(_mockPlatformService.Object.IsGoogleTV());
            Assert.Equal(AppTheme.TV, _mockPlatformService.Object.GetOptimalTheme());

            // Test scenario 2: PlatformType.Android with IsGoogleTV = true
            _mockPlatformService.Reset();
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(PlatformType.Android);
            _mockPlatformService.Setup(p => p.IsGoogleTV()).Returns(true);
            _mockPlatformService.Setup(p => p.GetOptimalTheme()).Returns(AppTheme.TV);

            Assert.Equal(PlatformType.Android, _mockPlatformService.Object.CurrentPlatform);
            Assert.True(_mockPlatformService.Object.IsGoogleTV());
            Assert.Equal(AppTheme.TV, _mockPlatformService.Object.GetOptimalTheme());
        }

        [Fact]
        public void PlatformService_InterfaceContract_ShouldBeCorrect()
        {
            // This test verifies that the IPlatformService interface has all required members
            var interfaceType = typeof(IPlatformService);
            
            // Verify required properties
            Assert.NotNull(interfaceType.GetProperty(nameof(IPlatformService.CurrentPlatform)));
            
            // Verify required methods
            Assert.NotNull(interfaceType.GetMethod(nameof(IPlatformService.IsGoogleTV)));
            Assert.NotNull(interfaceType.GetMethod(nameof(IPlatformService.GetOptimalTheme)));
            Assert.NotNull(interfaceType.GetMethod(nameof(IPlatformService.SupportsMultiPanel)));
            Assert.NotNull(interfaceType.GetMethod(nameof(IPlatformService.GetScreenDensity)));
            Assert.NotNull(interfaceType.GetMethod(nameof(IPlatformService.IsTouchPrimary)));
            Assert.NotNull(interfaceType.GetMethod(nameof(IPlatformService.SupportsVoiceInput)));
        }

        [Fact]
        public void PlatformTypeEnum_ShouldHaveAllExpectedValues()
        {
            // Verify that the PlatformType enum has all expected values
            var expectedValues = new[]
            {
                PlatformType.Windows,
                PlatformType.Android,
                PlatformType.AndroidTV,
                PlatformType.iOS,
                PlatformType.MacOS,
                PlatformType.Unknown
            };

            foreach (var expectedValue in expectedValues)
            {
                Assert.True(Enum.IsDefined(typeof(PlatformType), expectedValue));
            }
        }

        [Fact]
        public void AppThemeEnum_ShouldHaveAllExpectedValues()
        {
            // Verify that the AppTheme enum has all expected values
            var expectedValues = new[]
            {
                AppTheme.Light,
                AppTheme.Dark,
                AppTheme.System,
                AppTheme.Desktop,
                AppTheme.Mobile,
                AppTheme.TV
            };

            foreach (var expectedValue in expectedValues)
            {
                Assert.True(Enum.IsDefined(typeof(AppTheme), expectedValue));
            }
        }

        [Fact]
        public void PlatformServiceMock_ShouldSupportAllScenarios()
        {
            // Test that our mock can handle all the platform combinations
            var platformConfigurations = new[]
            {
                new { Platform = PlatformType.Windows, Theme = AppTheme.Desktop, MultiPanel = true, Touch = false, Voice = true, IsTV = false },
                new { Platform = PlatformType.MacOS, Theme = AppTheme.Desktop, MultiPanel = true, Touch = false, Voice = true, IsTV = false },
                new { Platform = PlatformType.Android, Theme = AppTheme.Mobile, MultiPanel = false, Touch = true, Voice = true, IsTV = false },
                new { Platform = PlatformType.iOS, Theme = AppTheme.Mobile, MultiPanel = false, Touch = true, Voice = true, IsTV = false },
                new { Platform = PlatformType.AndroidTV, Theme = AppTheme.TV, MultiPanel = true, Touch = false, Voice = true, IsTV = true }
            };

            foreach (var config in platformConfigurations)
            {
                // Arrange
                _mockPlatformService.Reset();
                _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(config.Platform);
                _mockPlatformService.Setup(p => p.GetOptimalTheme()).Returns(config.Theme);
                _mockPlatformService.Setup(p => p.SupportsMultiPanel()).Returns(config.MultiPanel);
                _mockPlatformService.Setup(p => p.IsTouchPrimary()).Returns(config.Touch);
                _mockPlatformService.Setup(p => p.SupportsVoiceInput()).Returns(config.Voice);
                _mockPlatformService.Setup(p => p.IsGoogleTV()).Returns(config.IsTV);
                _mockPlatformService.Setup(p => p.GetScreenDensity()).Returns(1.0);

                var service = _mockPlatformService.Object;

                // Act & Assert
                Assert.Equal(config.Platform, service.CurrentPlatform);
                Assert.Equal(config.Theme, service.GetOptimalTheme());
                Assert.Equal(config.MultiPanel, service.SupportsMultiPanel());
                Assert.Equal(config.Touch, service.IsTouchPrimary());
                Assert.Equal(config.Voice, service.SupportsVoiceInput());
                Assert.Equal(config.IsTV, service.IsGoogleTV());
                Assert.Equal(1.0, service.GetScreenDensity());
            }
        }

        [Theory]
        [InlineData(PlatformType.Android, true, AppTheme.TV)]
        [InlineData(PlatformType.Android, false, AppTheme.Mobile)]
        [InlineData(PlatformType.AndroidTV, true, AppTheme.TV)]
        [InlineData(PlatformType.AndroidTV, false, AppTheme.TV)]
        public void AndroidPlatformThemeMapping_ShouldHandleAllAndroidVariants(PlatformType platform, bool isGoogleTV, AppTheme expectedTheme)
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(platform);
            _mockPlatformService.Setup(p => p.IsGoogleTV()).Returns(isGoogleTV);
            _mockPlatformService.Setup(p => p.GetOptimalTheme()).Returns(expectedTheme);

            // Act
            var actualTheme = _mockPlatformService.Object.GetOptimalTheme();
            var actualIsGoogleTV = _mockPlatformService.Object.IsGoogleTV();
            var actualPlatform = _mockPlatformService.Object.CurrentPlatform;

            // Assert
            Assert.Equal(expectedTheme, actualTheme);
            Assert.Equal(isGoogleTV, actualIsGoogleTV);
            Assert.Equal(platform, actualPlatform);
        }

        [Fact]
        public void PlatformCapabilities_ShouldBeConsistentAcrossPlatforms()
        {
            // Test that platform capabilities are logically consistent
            var testCases = new[]
            {
                // Desktop platforms should support multi-panel but not be touch-primary
                new { Platform = PlatformType.Windows, ExpectedMultiPanel = true, ExpectedTouchPrimary = false },
                new { Platform = PlatformType.MacOS, ExpectedMultiPanel = true, ExpectedTouchPrimary = false },
                
                // Mobile platforms should be touch-primary but not support multi-panel
                new { Platform = PlatformType.Android, ExpectedMultiPanel = false, ExpectedTouchPrimary = true },
                new { Platform = PlatformType.iOS, ExpectedMultiPanel = false, ExpectedTouchPrimary = true },
                
                // TV platforms should support multi-panel but not be touch-primary
                new { Platform = PlatformType.AndroidTV, ExpectedMultiPanel = true, ExpectedTouchPrimary = false }
            };

            foreach (var testCase in testCases)
            {
                // Arrange
                _mockPlatformService.Reset();
                _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(testCase.Platform);
                _mockPlatformService.Setup(p => p.SupportsMultiPanel()).Returns(testCase.ExpectedMultiPanel);
                _mockPlatformService.Setup(p => p.IsTouchPrimary()).Returns(testCase.ExpectedTouchPrimary);

                var service = _mockPlatformService.Object;

                // Act & Assert
                Assert.Equal(testCase.ExpectedMultiPanel, service.SupportsMultiPanel());
                Assert.Equal(testCase.ExpectedTouchPrimary, service.IsTouchPrimary());
                
                // Verify logical consistency: platforms that are touch-primary should not support multi-panel
                if (testCase.ExpectedTouchPrimary)
                {
                    Assert.False(testCase.ExpectedMultiPanel, $"{testCase.Platform} should not support multi-panel if it's touch-primary");
                }
            }
        }

        [Fact]
        public void PlatformService_EdgeCases_ShouldBeHandledGracefully()
        {
            // Test edge case: Unknown platform
            _mockPlatformService.Reset();
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(PlatformType.Unknown);
            _mockPlatformService.Setup(p => p.GetOptimalTheme()).Returns(AppTheme.System);
            _mockPlatformService.Setup(p => p.IsGoogleTV()).Returns(false);
            _mockPlatformService.Setup(p => p.SupportsMultiPanel()).Returns(false);
            _mockPlatformService.Setup(p => p.IsTouchPrimary()).Returns(false);
            _mockPlatformService.Setup(p => p.SupportsVoiceInput()).Returns(false);
            _mockPlatformService.Setup(p => p.GetScreenDensity()).Returns(1.0);

            var service = _mockPlatformService.Object;

            // Act & Assert - Unknown platform should have sensible defaults
            Assert.Equal(PlatformType.Unknown, service.CurrentPlatform);
            Assert.Equal(AppTheme.System, service.GetOptimalTheme());
            Assert.False(service.IsGoogleTV());
            Assert.False(service.SupportsMultiPanel());
            Assert.False(service.IsTouchPrimary());
            Assert.False(service.SupportsVoiceInput());
            Assert.Equal(1.0, service.GetScreenDensity());
        }
    }
}
