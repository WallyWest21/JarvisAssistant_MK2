using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JarvisAssistant.UnitTests.Mocks
{
    public class MockMainPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _platform;

        public MockMainPage(IServiceProvider serviceProvider, string platform = "Windows")
        {
            _serviceProvider = serviceProvider;
            _platform = platform;
        }

        public Task<InputResult> SimulateTapAsync(string buttonName)
        {
            var navigationService = _serviceProvider.GetService<INavigationService>();
            var dialogService = _serviceProvider.GetService<IDialogService>();

            switch (buttonName)
            {
                case "StartChatBtn":
                    navigationService.NavigateToAsync("ChatPage");
                    return Task.FromResult(new InputResult { Success = true });
                case "VoiceDemoBtn":
                    navigationService.NavigateToAsync("VoiceDemoPage");
                    return Task.FromResult(new InputResult { Success = true });
                case "SettingsBtn":
                    dialogService.DisplayAlertAsync("Settings", "Settings page coming soon!", "OK");
                    return Task.FromResult(new InputResult { Success = true });
                default:
                    return Task.FromResult(new InputResult { Success = false, ErrorMessage = "Button not found" });
            }
        }

        public MockStatusPanelView GetStatusPanel()
        {
            return new MockStatusPanelView(_platform);
        }
    }

    public class MockStatusPanelView
    {
        private readonly string _platform;

        public MockStatusPanelView(string platform = "Windows")
        {
            _platform = platform;
        }

        public Task<InputResult> SimulateStatusBarTapAsync()
        {
            return Task.FromResult(new InputResult { Success = true });
        }

        public Task<InputResult> SimulateBackdropTapAsync()
        {
            return Task.FromResult(new InputResult { Success = true });
        }

        public Task<InputResult> SimulateStatusBarTouchAsync()
        {
            return Task.FromResult(new InputResult { Success = true });
        }

        public Task<InputResult> SimulateBackdropTouchAsync()
        {
            return Task.FromResult(new InputResult { Success = true });
        }

        public (double Width, double Height) GetStatusBarTouchTarget() => (100, 44);
        public (double Width, double Height) GetCloseButtonTouchTarget() => (44, 44);
    }

    public class InputResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
