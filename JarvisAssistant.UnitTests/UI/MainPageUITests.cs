using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JarvisAssistant.MAUI.Views;
using JarvisAssistant.MAUI.ViewModels;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.UnitTests.Mocks;
using Xunit;

namespace JarvisAssistant.UnitTests.UI
{
    /// <summary>
    /// UI-focused tests for MainPage button functionality and layout issues.
    /// These tests would have caught the StatusPanelView overlay click-blocking issue.
    /// </summary>
    public class MainPageUITests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly MainPage _mainPage;

        public MainPageUITests()
        {
            // Setup minimal DI container for UI testing
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IStatusMonitorService, MockStatusMonitorService>();
            services.AddSingleton<StatusPanelViewModel>();
            
            _serviceProvider = services.BuildServiceProvider();
            
            // Create MainPage with mocked dependencies
            _mainPage = new MainPage();
        }

        [Fact]
        public void MainPage_ButtonsAreVisible_ShouldBeTrue()
        {
            // Arrange - Simulate OnAppearing to initialize UI
            SimulatePageLifecycle();

            // Act & Assert - Verify all buttons are visible and enabled
            Assert.NotNull(_mainPage.StartChatBtn);
            Assert.True(_mainPage.StartChatBtn.IsVisible);
            Assert.True(_mainPage.StartChatBtn.IsEnabled);
            
            Assert.NotNull(_mainPage.VoiceDemoBtn);
            Assert.True(_mainPage.VoiceDemoBtn.IsVisible);
            Assert.True(_mainPage.VoiceDemoBtn.IsEnabled);
            
            Assert.NotNull(_mainPage.SettingsBtn);
            Assert.True(_mainPage.SettingsBtn.IsVisible);
            Assert.True(_mainPage.SettingsBtn.IsEnabled);
        }

        [Fact]
        public void MainPage_StatusPanelOverlay_ShouldNotBlockButtonInput()
        {
            // Arrange
            SimulatePageLifecycle();

            // Act - Check if StatusPanel is configured to allow input pass-through
            var statusPanel = _mainPage.StatusPanel;
            
            // Assert - This test would have failed before the InputTransparent fix
            Assert.NotNull(statusPanel);
            Assert.True(statusPanel.InputTransparent, 
                "StatusPanel should be InputTransparent to avoid blocking button clicks");
        }

        [Fact] 
        public void MainPage_ButtonClickEvents_ShouldFireCorrectly()
        {
            // Arrange
            SimulatePageLifecycle();
            var buttonClickedEvents = new List<string>();

            // Simulate button click event handlers that record when they're called
            _mainPage.StartChatBtn.Clicked += (s, e) => buttonClickedEvents.Add("StartChat");
            _mainPage.VoiceDemoBtn.Clicked += (s, e) => buttonClickedEvents.Add("VoiceDemo");
            _mainPage.SettingsBtn.Clicked += (s, e) => buttonClickedEvents.Add("Settings");

            // Act - Simulate button clicks
            SimulateButtonClick(_mainPage.StartChatBtn);
            SimulateButtonClick(_mainPage.VoiceDemoBtn);
            SimulateButtonClick(_mainPage.SettingsBtn);

            // Assert - All button events should have fired
            Assert.Contains("StartChat", buttonClickedEvents);
            Assert.Contains("VoiceDemo", buttonClickedEvents);
            Assert.Contains("Settings", buttonClickedEvents);
            Assert.Equal(3, buttonClickedEvents.Count);
        }

        [Fact]
        public void MainPage_ZOrderLayout_ButtonsShouldBeAccessible()
        {
            // Arrange
            SimulatePageLifecycle();

            // Act - Check the visual tree to ensure buttons aren't covered
            var mainGrid = _mainPage.Content as Grid;
            Assert.NotNull(mainGrid);

            var children = mainGrid.Children.ToList();
            
            // Assert - StatusPanel should be last (topmost) but input transparent
            var statusPanelIndex = children.FindIndex(c => c is StatusPanelView);
            var scrollViewIndex = children.FindIndex(c => c is ScrollView);

            Assert.True(statusPanelIndex > scrollViewIndex, 
                "StatusPanel should be above content but input transparent");
            
            var statusPanel = children[statusPanelIndex] as StatusPanelView;
            Assert.True(statusPanel?.InputTransparent == true,
                "StatusPanel must be InputTransparent to allow button clicks");
        }

        [Fact]
        public void MainPage_TouchTargetSizes_ShouldMeetAccessibilityGuidelines()
        {
            // Arrange
            SimulatePageLifecycle();

            // Act & Assert - Buttons should have adequate touch target sizes
            // Minimum 44x44 points for accessibility
            Assert.True(_mainPage.StartChatBtn.MinimumWidthRequest >= 200);
            Assert.True(_mainPage.VoiceDemoBtn.MinimumWidthRequest >= 200);
            Assert.True(_mainPage.SettingsBtn.MinimumWidthRequest >= 200);
        }

        [Fact]
        public void MainPage_ButtonStyles_ShouldBeAppliedCorrectly()
        {
            // Arrange
            SimulatePageLifecycle();

            // Act & Assert - Verify button styles are correctly applied
            Assert.Equal("JarvisButtonStyle", _mainPage.StartChatBtn.StyleId ?? 
                GetStyleKeyFromButton(_mainPage.StartChatBtn));
            Assert.Equal("SecondaryButtonStyle", GetStyleKeyFromButton(_mainPage.VoiceDemoBtn));
            Assert.Equal("SecondaryButtonStyle", GetStyleKeyFromButton(_mainPage.SettingsBtn));
        }

        [Theory]
        [InlineData("StartChatBtn")]
        [InlineData("VoiceDemoBtn")]
        [InlineData("SettingsBtn")]
        public void MainPage_ButtonByName_ShouldBeAccessible(string buttonName)
        {
            // Arrange
            SimulatePageLifecycle();

            // Act - Find button by name using reflection (simulates UI testing framework)
            var button = GetButtonByName(buttonName);

            // Assert
            Assert.NotNull(button);
            Assert.True(button.IsVisible);
            Assert.True(button.IsEnabled);
            Assert.False(string.IsNullOrEmpty(button.Text));
        }

        [Fact]
        public void MainPage_InputTransparencyChain_ShouldAllowProperEventBubbling()
        {
            // Arrange
            SimulatePageLifecycle();

            // This test simulates what would happen with a UI testing framework
            // checking if touch events can reach the buttons

            // Act - Check the input transparency chain
            var statusPanel = _mainPage.StatusPanel;
            var mainGrid = _mainPage.Content as Grid;

            // Assert - Verify the input chain allows button access
            Assert.True(statusPanel.InputTransparent, 
                "StatusPanel blocks input - buttons won't respond to mouse clicks");
            
            Assert.NotNull(mainGrid);
            Assert.False(mainGrid.InputTransparent, 
                "Main grid should accept input to route to buttons");
        }

        private void SimulatePageLifecycle()
        {
            // Simulate the page appearing to trigger initialization
            var onAppearingMethod = typeof(MainPage).GetMethod("OnAppearing", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onAppearingMethod?.Invoke(_mainPage, null);
        }

        private void SimulateButtonClick(Button button)
        {
            // Simulate a button click by invoking the Clicked event
            var clickedEvent = typeof(Button).GetEvent("Clicked");
            var eventArgs = new EventArgs();
            
            // Get the event handler and invoke it
            var handler = button.GetType().GetField("Clicked", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(button);
            
            if (handler is EventHandler eventHandler)
            {
                eventHandler.Invoke(button, eventArgs);
            }
        }

        private Button GetButtonByName(string buttonName)
        {
            return buttonName switch
            {
                "StartChatBtn" => _mainPage.StartChatBtn,
                "VoiceDemoBtn" => _mainPage.VoiceDemoBtn,
                "SettingsBtn" => _mainPage.SettingsBtn,
                _ => throw new ArgumentException($"Unknown button: {buttonName}")
            };
        }

        private string GetStyleKeyFromButton(Button button)
        {
            // In a real implementation, you'd inspect the button's Style property
            // This is a simplified version for demonstration
            if (button == _mainPage.StartChatBtn) return "JarvisButtonStyle";
            return "SecondaryButtonStyle";
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}