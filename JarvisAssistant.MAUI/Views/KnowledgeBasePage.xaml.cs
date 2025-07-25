using JarvisAssistant.MAUI.ViewModels;

namespace JarvisAssistant.MAUI.Views;

/// <summary>
/// Knowledge Base page for document management and search.
/// </summary>
public partial class KnowledgeBasePage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeBasePage"/> class.
    /// </summary>
    /// <param name="viewModel">The knowledge base view model.</param>
    public KnowledgeBasePage(KnowledgeBaseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Called when the page appears.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is KnowledgeBaseViewModel viewModel)
        {
            await viewModel.OnAppearingAsync();
        }
    }

    /// <summary>
    /// Called when the page disappears.
    /// </summary>
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        
        if (BindingContext is KnowledgeBaseViewModel viewModel)
        {
            await viewModel.OnDisappearingAsync();
        }
    }
}
