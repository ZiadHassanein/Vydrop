using System.ComponentModel;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Automation.Peers;
using System.Windows.Threading;
using System.Windows.Input;

namespace YouTubeDownloader;
public partial class MainWindow : Window
{
    internal static readonly RoutedCommand FocusLinkCommand = new();
    private readonly MainViewModel model;
    private bool closing, closed;
    private FlowDirection presentationDirection;
    internal AppHeaderView HeaderContent => ManagerContent.Header;
    private IInputElement editorReturnFocus;
    internal SystemThemeMonitor SystemTheme { get; }
    public MainWindow() : this(null, null)
    {
    }

    internal MainWindow(DownloadService service, SettingsRepository repository, IMediaThumbnailLoader thumbnailLoader = null)
    {
        InitializeComponent();

        Root.Children.Remove(EditorOverlay);
        ManagerContent.EditorHost.Children.Add(EditorOverlay);
        Root.Children.Remove(Notice);
        DownloadForm.FolderNoticeHost.Children.Add(Notice);
        var actions = new DesktopActions(this);
        model = new MainViewModel(service ?? new DownloadService(new YtDlpProvider(new DependencyManager()), new LocalPlaylistStorage()), repository ?? new SettingsRepository(), actions, thumbnailLoader);
        presentationDirection = model.ContentDirection;
        DataContext = model;
        ManagerContent.WorkspaceLayoutChanged += ApplyResponsiveLayout;
        CommandBindings.Add(new CommandBinding(FocusLinkCommand, (_, e) =>
        {
            model.NewDownloadCommand.Execute(null);
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                DownloadForm.LinkInput.BringIntoView();
                DownloadForm.LinkInput.Focus();
                DownloadForm.LinkInput.SelectAll();
            });
            e.Handled = true;
        }, (_, e) => { e.CanExecute = model.IsIdle; e.Handled = true; }));
        InputBindings.Add(new KeyBinding(FocusLinkCommand, Key.L, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(model.HistoryCommand, Key.J, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(model.QueueCommand, Key.Q, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(model.SettingsCommand, Key.OemComma, ModifierKeys.Control));
        SystemTheme = new SystemThemeMonitor(this, () => model.IsDark, model.RefreshThemeBindings);
        model.NoticeRequested += PlaceNotice;
        SizeChanged += (_, _) => ApplyResponsiveLayout();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && EditorOverlay.IsVisible) { CloseEditor(); e.Handled = true; } };
        StateChanged += (_, _) => ConstrainToWorkArea();
        DpiChanged += (_, _) => ConstrainToWorkArea();
        model.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrEmpty(args.PropertyName))
            {
                if (presentationDirection != model.ContentDirection)
                {
                    ResetDropDowns(Root);
                    SettingsPane.ResetHelp();
                    presentationDirection = model.ContentDirection;
                }
                ApplyTitleBar();
                UpdateEditor();
            }
            if (args.PropertyName is nameof(MainViewModel.SettingsVisible) or nameof(MainViewModel.HasMediaPreview) or nameof(MainViewModel.QueueRunning))
            {
                UpdateEditor();
                ApplyResponsiveLayout();
                if (args.PropertyName == nameof(MainViewModel.SettingsVisible))
                {
                    ContextViewport.ScrollToTop();
                    if (model.SettingsVisible) ViewMotion.Reveal(SettingsPane);
                }
                else if (args.PropertyName == nameof(MainViewModel.HasMediaPreview) && model.HasMediaPreview && !model.SettingsVisible)
                {
                    Viewport.ScrollToTop();
                    ContextViewport.ScrollToTop();
                    ViewMotion.Reveal(MediaPreview.PreviewContent);
                }
            }
            if (args.PropertyName is nameof(MainViewModel.AddDownloadVisible) or nameof(MainViewModel.HistoryVisible)) UpdateEditor();
            if (args.PropertyName == nameof(MainViewModel.HasFile) && model.HasFile && model.ShowInlineActivity)
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => DownloadForm.InlineActivity.BringIntoView());
            if (((args.PropertyName == nameof(MainViewModel.StatusHeading) || args.PropertyName == nameof(MainViewModel.IsCheckingMedia) && model.IsCheckingMedia) && !model.SettingsVisible)
                || (args.PropertyName == nameof(MainViewModel.NoticeHeading) && !model.OptionsNoticeVisible))
            {
                FrameworkElement element = args.PropertyName == nameof(MainViewModel.NoticeHeading) ? NoticeHeading :
                    model.IsCheckingMedia ? DownloadForm.DownloadAction.DownloadButton :
                    MediaPreview.IsVisible ? MediaPreview.Activity.StatusHeading : DownloadForm.InlineActivity.StatusHeading;
                (UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element))?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }
        };
        SourceInitialized += (_, _) =>
        {
            ApplyTitleBar();
            ConstrainToWorkArea();
        };
        ContentRendered += (_, _) =>
        {
            ConstrainToWorkArea();
            ApplyResponsiveLayout();
            ManagerContent.AddDownloadButton.Focus();
            AppUpdates.AcknowledgeStartup();
        };
        Closing += ClosingAsync;
        Activated += (_, _) => { if (model.HistoryVisible) model.History?.RefreshFiles(); };
    }

    private void ApplyResponsiveLayout()
    {
        bool split = model.HasMediaPreview && !model.SettingsVisible && ManagerContent.DetailsColumn.Width.Value > 0;
        bool inline = model.HasMediaPreview && !split && !model.SettingsVisible;
        var previewHost = inline ? (Panel)DownloadForm.InlinePreviewHost : ManagerContent.EditorDetailsHost;
        if (MediaPreview.Parent is Panel parent && parent != previewHost)
        {
            parent.Children.Remove(MediaPreview);
            previewHost.Children.Add(MediaPreview);
        }
        model.InlineMediaPreview = inline;
        ContextViewport.Visibility = model.SettingsVisible ? Visibility.Visible : Visibility.Collapsed;
        ContextPane.Visibility = ContextViewport.Visibility;
        Viewport.Visibility = model.SettingsVisible ? Visibility.Collapsed : Visibility.Visible;
        MediaPreview.SetCompact(inline);
        MediaPreview.Visibility = model.HasMediaPreview && !model.SettingsVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateEditor()
    {
        bool visible = model.SettingsVisible || model.AddDownloadVisible;
        bool opening = visible && !EditorOverlay.IsVisible;
        if (opening) editorReturnFocus = Keyboard.FocusedElement;
        EditorOverlay.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ManagerContent.RefreshWorkspace();
        EditorHeading.Text = model.SettingsVisible ? model.SettingsLabel : model.AddDownloadLabel;
        EditorHeading.FlowDirection = model.ContentDirection;
        ApplyResponsiveLayout();
        if (opening) Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (model.SettingsVisible) SettingsPane.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            else { DownloadForm.LinkInput.Focus(); DownloadForm.LinkInput.SelectAll(); }
        });
        else if (!visible && editorReturnFocus != null) { Keyboard.Focus(editorReturnFocus); editorReturnFocus = null; }
    }
    private void CloseEditorClicked(object sender, RoutedEventArgs e) => CloseEditor();
    private void CloseEditor()
    {
        model.SettingsVisible = false;
        model.CloseAddDownloadCommand.Execute(null);
        UpdateEditor();
    }

    private static void ResetDropDowns(DependencyObject parent)
    {
        if (parent is FrameworkElement { ContextMenu: { } menu }) menu.IsOpen = false;
        if (parent is ComboBox combo)
        {
            // WPF can retain the old popup HWND's height after inherited font/direction changes.
            // Recreate the template's popup; keep the ComboBox, selection and bindings intact.
            combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
            var template = combo.Template;
            combo.SetCurrentValue(Control.TemplateProperty, null);
            combo.SetCurrentValue(Control.TemplateProperty, template);
            combo.ApplyTemplate();
            return;
        }
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            ResetDropDowns(VisualTreeHelper.GetChild(parent, index));
    }

    private void ApplyTitleBar()
    {
        ThemeResources.ApplyTitleBar(this, model.IsDark);
    }

    private void ConstrainToWorkArea()
    {
        if (WindowState != WindowState.Normal) return;
        var area = CurrentWorkArea();
        if (Width > area.Width - 16) Width = Math.Max(MinWidth, area.Width - 16);
        if (Height > area.Height - 16) Height = Math.Max(MinHeight, area.Height - 16);
        if (Top + Height > area.Bottom) Top = area.Bottom - Height;
        if (Top < area.Top) Top = area.Top;
    }

    internal Rect CurrentWorkArea()
    {
        var source = PresentationSource.FromVisual(this);
        var handle = new WindowInteropHelper(this).Handle;
        var info = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };
        if (handle != IntPtr.Zero && GetMonitorInfo(MonitorFromWindow(handle, 2), ref info))
        {
            var matrix = source?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
            return new Rect(matrix.Transform(new Point(info.Work.Left, info.Work.Top)), matrix.Transform(new Point(info.Work.Right, info.Work.Bottom)));
        }

        return SystemParameters.WorkArea;
    }

    private void PlaceNotice(string kind)
    {
        if (kind == "options") return;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (kind == "folder") model.NewDownloadCommand.Execute(null);
            else if (kind != "library") model.SettingsVisible = true;
            Panel host = kind switch
            {
                "folder" => DownloadForm.FolderNoticeHost,
                "library" => LibraryNoticeHost,
                "tools" => SettingsPane.ToolNoticeHost,
                _ => SettingsPane.UpdateNoticeHost
            };
            if (Notice.Parent is Panel parent && parent != host)
            {
                parent.Children.Remove(Notice);
                host.Children.Add(Notice);
            }

            Notice.Margin = kind is "folder" or "tools" ? new Thickness(0, 12, 0, 0) : new Thickness(0, 0, 0, 10);
            Notice.HorizontalAlignment = HorizontalAlignment.Stretch;
            Notice.BringIntoView();
        });
    }

    private async void ClosingAsync(object sender, CancelEventArgs args)
    {
        if (closed)
            return;
        args.Cancel = true;
        if (closing)
            return;
        closing = true;
        IsEnabled = false;
        try
        {
            await model.CloseAsync();
            closed = true;
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => Close());
        }
        catch
        {
            closing = false;
            IsEnabled = true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor, Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
