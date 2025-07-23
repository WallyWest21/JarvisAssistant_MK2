using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.MAUI.ViewModels
{
    /// <summary>
    /// Base view model class that provides common functionality for all view models in the application.
    /// </summary>
    public abstract partial class BaseViewModel : ObservableObject
    {
        private readonly IErrorHandlingService? _errorHandlingService;

        /// <summary>
        /// Gets or sets a value indicating whether the view model is currently performing a busy operation.
        /// </summary>
        [ObservableProperty]
        private bool isBusy;

        /// <summary>
        /// Gets or sets the title of the view model, typically used for page titles.
        /// </summary>
        [ObservableProperty]
        private string title = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the view model is currently loading data.
        /// </summary>
        [ObservableProperty]
        private bool isLoading;

        /// <summary>
        /// Gets or sets the current error message to display to the user.
        /// </summary>
        [ObservableProperty]
        private string? errorMessage;

        /// <summary>
        /// Gets or sets a value indicating whether an error is currently being displayed.
        /// </summary>
        [ObservableProperty]
        private bool hasError;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseViewModel"/> class.
        /// </summary>
        protected BaseViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseViewModel"/> class with error handling service.
        /// </summary>
        /// <param name="errorHandlingService">The error handling service for managing errors.</param>
        protected BaseViewModel(IErrorHandlingService errorHandlingService)
        {
            _errorHandlingService = errorHandlingService;
        }

        /// <summary>
        /// Called when the view model is appearing/activated.
        /// Override this method to perform initialization when the view appears.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual Task OnAppearingAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the view model is disappearing/deactivated.
        /// Override this method to perform cleanup when the view disappears.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual Task OnDisappearingAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes an async operation safely with error handling and busy state management.
        /// </summary>
        /// <param name="operation">The async operation to execute.</param>
        /// <param name="showLoading">Whether to show loading state during the operation.</param>
        /// <param name="errorMessage">Custom error message to display if the operation fails.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected async Task ExecuteSafelyAsync(Func<Task> operation, bool showLoading = true, string? errorMessage = null)
        {
            try
            {
                if (showLoading)
                {
                    IsLoading = true;
                }
                
                IsBusy = true;
                ClearError();

                await operation();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, errorMessage);
            }
            finally
            {
                IsBusy = false;
                IsLoading = false;
            }
        }

        /// <summary>
        /// Executes an async operation safely with error handling and busy state management, returning a result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="operation">The async operation to execute.</param>
        /// <param name="showLoading">Whether to show loading state during the operation.</param>
        /// <param name="errorMessage">Custom error message to display if the operation fails.</param>
        /// <returns>A task that represents the asynchronous operation with a result, or default(T) if an error occurs.</returns>
        protected async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> operation, bool showLoading = true, string? errorMessage = null)
        {
            try
            {
                if (showLoading)
                {
                    IsLoading = true;
                }
                
                IsBusy = true;
                ClearError();

                return await operation();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, errorMessage);
                return default;
            }
            finally
            {
                IsBusy = false;
                IsLoading = false;
            }
        }

        /// <summary>
        /// Handles errors that occur in the view model.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="customMessage">Optional custom error message to display.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected virtual async Task HandleErrorAsync(Exception exception, string? customMessage = null)
        {
            ErrorMessage = customMessage ?? "An unexpected error occurred. Please try again.";
            HasError = true;

            if (_errorHandlingService != null)
            {
                await _errorHandlingService.HandleErrorAsync(exception, GetType().Name, customMessage);
            }
        }

        /// <summary>
        /// Clears the current error state.
        /// </summary>
        [RelayCommand]
        protected virtual void ClearError()
        {
            ErrorMessage = null;
            HasError = false;
        }

        /// <summary>
        /// Refreshes the view model data.
        /// Override this method to implement refresh functionality.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [RelayCommand]
        public virtual async Task RefreshAsync()
        {
            await OnAppearingAsync();
        }
    }
}
