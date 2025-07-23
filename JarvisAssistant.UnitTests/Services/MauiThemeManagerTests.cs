using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JarvisAssistant.UnitTests.Services
{
    public class MauiThemeManagerTests
    {
        private readonly Mock<ILogger<ThemeManager>> _mockLogger;
        private readonly Mock<IPlatformService> _mockPlatformService;
        private readonly ThemeManager _themeManager;

        public MauiThemeManagerTests()
        {
            _mockLogger = new Mock<ILogger<ThemeManager>>();
            _mockPlatformService = new Mock<IPlatformService>();
            _themeManager = new ThemeManager(_mockLogger.Object, _mockPlatformService.Object);
        }

        [Fact]
        public void CurrentTheme_InitialValue_ShouldBeSystem()
        {
            // Act
            var currentTheme = _themeManager.CurrentTheme;

            // Assert
            Assert.Equal(AppTheme.System, currentTheme);
        }

        [Fact]
        public void AvailableThemes_ShouldContainBaseThemes()
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(PlatformType.Windows);

            // Act
            var availableThemes = _themeManager.AvailableThemes;

            // Assert
            Assert.Contains(AppTheme.Light, availableThemes);
            Assert.Contains(AppTheme.Dark, availableThemes);
            Assert.Contains(AppTheme.System, availableThemes);
        }

        [Fact]
        public void AvailableThemes_ForWindows_ShouldIncludeDesktopTheme()
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(PlatformType.Windows);

            // Act
            var availableThemes = _themeManager.AvailableThemes;

            // Assert
            Assert.Contains(AppTheme.Desktop, availableThemes);
        }

        [Fact]
        public void AvailableThemes_ForAndroid_ShouldIncludeMobileTheme()
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(PlatformType.Android);
            _mockPlatformService.Setup(p => p.IsGoogleTV()).Returns(false);

            // Act
            var availableThemes = _themeManager.AvailableThemes;

            // Assert
            Assert.Contains(AppTheme.Mobile, availableThemes);
        }

        [Fact]
        public void AvailableThemes_ForAndroidTV_ShouldIncludeTVTheme()
        {
            // Arrange
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(PlatformType.AndroidTV);

            // Act
            var availableThemes = _themeManager.AvailableThemes;

            // Assert
            Assert.Contains(AppTheme.TV, availableThemes);
        }

        [Fact]
        public async Task SwitchThemeAsync_SameTheme_ShouldNotTriggerChange()
        {
            // Arrange
            var themeChanged = false;
            _themeManager.ThemeChanged += (sender, theme) => themeChanged = true;

            // Act
            await _themeManager.SwitchThemeAsync(AppTheme.System);

            // Assert
            Assert.False(themeChanged);
        }

        [Fact]
        public async Task SwitchThemeAsync_DifferentTheme_ShouldTriggerThemeChangedEvent()
        {
            // Arrange
            AppTheme? changedTheme = null;
            _themeManager.ThemeChanged += (sender, theme) => changedTheme = theme;

            // Act
            await _themeManager.SwitchThemeAsync(AppTheme.Light);

            // Assert
            Assert.Equal(AppTheme.Light, changedTheme);
            Assert.Equal(AppTheme.Light, _themeManager.CurrentTheme);
        }

        [Fact]
        public async Task AutoSelectThemeAsync_WithPlatformService_ShouldUseOptimalTheme()
        {
            // Arrange
            _mockPlatformService.Setup(p => p.GetOptimalTheme()).Returns(AppTheme.Desktop);
            _mockPlatformService.Setup(p => p.CurrentPlatform).Returns(PlatformType.Windows);

            // Act
            await _themeManager.AutoSelectThemeAsync();

            // Assert
            Assert.Equal(AppTheme.Desktop, _themeManager.CurrentTheme);
        }

        [Fact]
        public async Task AutoSelectThemeAsync_WithoutPlatformService_ShouldUseSystemTheme()
        {
            // Arrange
            var themeManagerWithoutPlatform = new ThemeManager(_mockLogger.Object, null);

            // Act
            await themeManagerWithoutPlatform.AutoSelectThemeAsync();

            // Assert
            Assert.Equal(AppTheme.System, themeManagerWithoutPlatform.CurrentTheme);
        }

        [Fact]
        public async Task LoadThemeResourcesAsync_ShouldLoadResourcesOnce()
        {
            // Act
            await _themeManager.LoadThemeResourcesAsync(AppTheme.Light);
            await _themeManager.LoadThemeResourcesAsync(AppTheme.Light); // Second call

            // Assert
            // Verify through logs that resources were only loaded once
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loading theme resources for Light")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetSystemThemeAsync_ShouldReturnValidTheme()
        {
            // Act
            var systemTheme = await _themeManager.GetSystemThemeAsync();

            // Assert
            Assert.True(Enum.IsDefined(typeof(AppTheme), systemTheme));
        }

        [Fact]
        public async Task LoadThemePreferenceAsync_ShouldReturnSystemThemeByDefault()
        {
            // Act
            var preference = await _themeManager.LoadThemePreferenceAsync();

            // Assert
            Assert.Equal(AppTheme.System, preference); // Should return System by default as no preference is saved in tests
        }

        [Fact]
        public async Task SaveThemePreferenceAsync_ShouldNotThrow()
        {
            // Act & Assert
            await _themeManager.SaveThemePreferenceAsync(AppTheme.Dark);
            
            // Verify it was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Saved theme preference: Dark")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(AppTheme.Light)]
        [InlineData(AppTheme.Dark)]
        [InlineData(AppTheme.Desktop)]
        [InlineData(AppTheme.Mobile)]
        [InlineData(AppTheme.TV)]
        public async Task SwitchThemeAsync_AllThemes_ShouldSucceed(AppTheme theme)
        {
            // Act & Assert
            await _themeManager.SwitchThemeAsync(theme);
            Assert.Equal(theme, _themeManager.CurrentTheme);
        }

        [Fact]
        public async Task SwitchThemeAsync_ExceptionDuringSwitch_ShouldRevertTheme()
        {
            // Arrange
            var originalTheme = _themeManager.CurrentTheme;
            
            // Create a theme manager that will throw during SaveThemePreferenceAsync
            var mockThrowingLogger = new Mock<ILogger<ThemeManager>>();
            var throwingThemeManager = new TestableThemeManager(mockThrowingLogger.Object, _mockPlatformService.Object);
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => throwingThemeManager.SwitchThemeAsync(AppTheme.Light));
            Assert.Equal(AppTheme.System, throwingThemeManager.CurrentTheme); // Should revert to original
        }

        // Helper class to test exception handling during theme switching
        private class TestableThemeManager : ThemeManager
        {
            public TestableThemeManager(ILogger<ThemeManager> logger, IPlatformService? platformService) 
                : base(logger, platformService)
            {
            }

            public override async Task SaveThemePreferenceAsync(AppTheme theme)
            {
                if (theme == AppTheme.Light)
                {
                    throw new InvalidOperationException("Test exception");
                }
                await base.SaveThemePreferenceAsync(theme);
            }
        }
    }
}
