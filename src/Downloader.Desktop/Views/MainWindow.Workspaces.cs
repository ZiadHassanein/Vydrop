using System.Windows;

namespace YouTubeDownloader;

public partial class MainWindow
{
    internal FrameworkElement WorkspaceView => ManagerContent.WorkspaceHost.Content as FrameworkElement;

    internal void ShowWorkspace(WorkspacePage page, FrameworkElement content)
    {
        content.FlowDirection = model.ContentDirection;
        content.SetValue(FontFamilyProperty, model.ContentFont);
        model.OpenWorkspace(page);
        ManagerContent.WorkspaceHost.Content = content;
        UpdateEditor();
    }

    private void RefreshWorkspacePresentation()
    {
        if (WorkspaceView is not { } view) return;
        view.FlowDirection = model.ContentDirection;
        view.SetValue(FontFamilyProperty, model.ContentFont);
        if (view.DataContext is BrowserSetupViewModel setup) setup.RefreshLanguage(model.IsArabic);
        if (view is PlaylistSelectionView selection) selection.RefreshLanguage();
    }
}
