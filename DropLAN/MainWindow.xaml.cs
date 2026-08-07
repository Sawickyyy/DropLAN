using WpfApplication = System.Windows.Application;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfClipboard = System.Windows.Clipboard;
using WpfColor = System.Windows.Media.Color;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfMessageBoxResult = System.Windows.MessageBoxResult;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfOpenFolderDialog = Microsoft.Win32.OpenFolderDialog;
using QRCoder;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace DropLAN;

public partial class MainWindow : Window
{
    private readonly SharedState _state = new();
    private readonly RealtimeBroker _realtime = new();
    private readonly LocalServer _server;
    private readonly UpdateService _updateService = new();

    private string _currentAddress = "";
    private TrayService? _tray;
    private bool _allowExit;
    private bool _settingsControlsReady;
    private bool _sidebarCollapsed;
    private string _currentPageKey = "Home";

    private static bool IsPolish =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("pl", StringComparison.OrdinalIgnoreCase);

    private static string T(string pl, string en) =>
        IsPolish ? pl : en;

    public MainWindow()
    {
        InitializeComponent();

        _server = new LocalServer(_state, _realtime);

        InitializeSettingsControls();
        ApplySavedTheme();
        ApplyResponsiveLayout(Width);
        ShowPage("Home", animate: false);

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        Closing += MainWindow_Closing;
        SizeChanged += MainWindow_SizeChanged;

        _state.Changed += State_Changed;
        _state.TransferAdded += State_TransferAdded;
    }

    private void InitializeSettingsControls()
    {
        var settings = AppSettings.Load();

        LanguageComboBox.SelectedIndex =
            settings.Language?.Equals("en", StringComparison.OrdinalIgnoreCase) == true
                ? 1
                : 0;

        ThemeComboBox.SelectedIndex = settings.Theme.ToLowerInvariant() switch
        {
            "light" => 1,
            "dark" => 2,
            _ => 0
        };

        MinimizeToTrayCheckBox.IsChecked = settings.MinimizeToTray;
        NotificationsCheckBox.IsChecked = settings.TransferNotifications;
        AutoUpdatesCheckBox.IsChecked = settings.AutoCheckUpdates;

        _settingsControlsReady = true;
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            ApplyResponsiveLayout(ActualWidth);

            await _server.StartAsync();

            RefreshPairingInfo();
            RefreshUiFromState();

            VersionText.Text = $"DropLAN {GetCurrentVersion()}";
            UpdateUpdateStatusText();
            RecreateTray();

            SetServerStatus(true);

            if (AppSettings.Load().AutoCheckUpdates)
                await CheckForUpdatesSilentlyAsync();
        }
        catch (Exception ex)
        {
            StatusText.Foreground = FindBrush("DangerBrush", WpfBrushes.IndianRed);
            StatusText.Text = T($"Błąd: {ex.Message}", $"Error: {ex.Message}");
            SidebarStatusText.Text = T("● błąd", "● error");
        }
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string key)
            ShowPage(key);
    }

    private void GoSendButton_Click(object sender, RoutedEventArgs e) => ShowPage("Send");
    private void GoClipboardButton_Click(object sender, RoutedEventArgs e) => ShowPage("Clipboard");
    private void GoHistoryButton_Click(object sender, RoutedEventArgs e) => ShowPage("History");

    private void ShowPage(string key, bool animate = true)
    {
        _currentPageKey = key;

        var pages = new Dictionary<string, FrameworkElement>
        {
            ["Home"] = HomePage,
            ["Send"] = SendPage,
            ["Receive"] = ReceivePage,
            ["Clipboard"] = ClipboardPage,
            ["History"] = HistoryPage,
            ["Settings"] = SettingsPage
        };

        foreach (var page in pages.Values)
            page.Visibility = Visibility.Collapsed;

        if (!pages.TryGetValue(key, out var current))
            current = HomePage;

        current.Visibility = Visibility.Visible;
        SetNavigationSelection(key);
        UpdatePageHeading(key);

        if (animate)
            AnimatePage(current);
    }

    private void SetNavigationSelection(string key)
    {
        var buttons = new Dictionary<string, Button>
        {
            ["Home"] = NavHomeButton,
            ["Send"] = NavSendButton,
            ["Receive"] = NavReceiveButton,
            ["Clipboard"] = NavClipboardButton,
            ["History"] = NavHistoryButton,
            ["Settings"] = NavSettingsButton
        };

        foreach (var item in buttons)
            item.Value.Tag = item.Key == key ? "selected" : null;
    }

    private void UpdatePageHeading(string key)
    {
        (string titleKey, string subtitleKey) = key switch
        {
            "Send" => ("SendTitle", "SendSubtitle"),
            "Receive" => ("ReceiveTitle", "ReceiveSubtitle"),
            "Clipboard" => ("ClipboardTitle", "ClipboardSubtitle"),
            "History" => ("HistoryTitle", "HistorySubtitle"),
            "Settings" => ("SettingsTitle", "SettingsSubtitle"),
            _ => ("HomeTitle", "HomeSubtitle")
        };

        PageTitleText.Text = ResourceText(titleKey);
        PageSubtitleText.Text = ResourceText(subtitleKey);
    }

    private static void AnimatePage(FrameworkElement element)
    {
        element.Opacity = 0;
        var transform = new TranslateTransform(0, 14);
        element.RenderTransform = transform;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        element.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(190))
            {
                EasingFunction = ease
            });

        transform.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(14, 0, TimeSpan.FromMilliseconds(210))
            {
                EasingFunction = ease
            });
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(e.NewSize.Width);
    }

    private void ApplyResponsiveLayout(double width)
    {
        var collapseSidebar = width < 1000;
        if (_sidebarCollapsed == collapseSidebar && IsLoaded)
            return;

        _sidebarCollapsed = collapseSidebar;
        SidebarColumn.Width = new GridLength(collapseSidebar ? 76 : 220);

        var labelVisibility = collapseSidebar
            ? Visibility.Collapsed
            : Visibility.Visible;

        NavHomeText.Visibility = labelVisibility;
        NavSendText.Visibility = labelVisibility;
        NavReceiveText.Visibility = labelVisibility;
        NavClipboardText.Visibility = labelVisibility;
        NavHistoryText.Visibility = labelVisibility;
        NavSettingsText.Visibility = labelVisibility;
        SidebarStatusLabel.Visibility = labelVisibility;
        SidebarStatusText.Visibility = labelVisibility;
    }

    private void LanguageComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!_settingsControlsReady)
            return;

        var language = LanguageComboBox.SelectedIndex == 1
            ? "en"
            : "pl";

        var settings = AppSettings.Load();
        settings.Language = language;
        settings.Save();

        AppSettings.ApplyLanguage(language);
        UpdatePageHeading(_currentPageKey);
        UpdateUpdateStatusText();
        SetServerStatus(true);
        RecreateTray();
    }

    private void ThemeComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!_settingsControlsReady)
            return;

        var theme = ThemeComboBox.SelectedIndex switch
        {
            1 => "light",
            2 => "dark",
            _ => "system"
        };

        var settings = AppSettings.Load();
        settings.Theme = theme;
        settings.Save();

        ApplyTheme(theme);
    }

    private void SettingsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_settingsControlsReady)
            return;

        var settings = AppSettings.Load();
        settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;
        settings.TransferNotifications = NotificationsCheckBox.IsChecked == true;
        settings.AutoCheckUpdates = AutoUpdatesCheckBox.IsChecked == true;
        settings.Save();
    }

    private void ApplySavedTheme()
    {
        ApplyTheme(AppSettings.Load().Theme);
    }

    private void ApplyTheme(string theme)
    {
        var dark = theme.Equals("dark", StringComparison.OrdinalIgnoreCase) ||
                   (theme.Equals("system", StringComparison.OrdinalIgnoreCase) && IsSystemDarkTheme());

        if (dark)
        {
            SetColor("AppBackgroundBrush", "#0F1115");
            SetColor("SidebarBrush", "#12151B");
            SetColor("SurfaceBrush", "#161A20");
            SetColor("SurfaceAltBrush", "#1B2028");
            SetColor("SurfaceHoverBrush", "#202630");
            SetColor("TextBrush", "#F3F4F6");
            SetColor("MutedBrush", "#9CA3AF");
            SetColor("BorderBrush", "#2A303A");
            SetColor("AccentSoftBrush", "#172554");
        }
        else
        {
            SetColor("AppBackgroundBrush", "#F5F7FA");
            SetColor("SidebarBrush", "#F9FAFC");
            SetColor("SurfaceBrush", "#FFFFFF");
            SetColor("SurfaceAltBrush", "#F8FAFC");
            SetColor("SurfaceHoverBrush", "#F2F6FC");
            SetColor("TextBrush", "#111827");
            SetColor("MutedBrush", "#6B7280");
            SetColor("BorderBrush", "#E5E7EB");
            SetColor("AccentSoftBrush", "#E8F0FE");
        }
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            var value = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1);

            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }

    private void SetColor(string key, string hex)
    {
        Resources[key] = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(hex));
    }

    private Brush FindBrush(string key, Brush fallback)
    {
        return TryFindResource(key) as Brush ?? fallback;
    }

    private string ResourceText(string key)
    {
        return TryFindResource(key)?.ToString() ?? key;
    }

    private void State_Changed()
    {
        Dispatcher.Invoke(RefreshUiFromState);
    }

    private void RefreshUiFromState()
    {
        DownloadFolderText.Text = _state.DownloadFolder;

        var files = _state.GetSharedFiles();
        SharedFilesList.ItemsSource = null;
        SharedFilesList.ItemsSource = files;

        var history = _state.GetHistory().ToList();

        HistoryList.ItemsSource = null;
        HistoryList.ItemsSource = history;

        HomeRecentList.ItemsSource = null;
        HomeRecentList.ItemsSource = history.Take(5).ToList();

        ReceivedHistoryList.ItemsSource = null;
        ReceivedHistoryList.ItemsSource = history
            .Where(item => item.Direction == TransferDirection.PhoneToPc)
            .Take(12)
            .ToList();

        if (!ClipboardTextBox.IsKeyboardFocusWithin)
            ClipboardTextBox.Text = _state.ClipboardText;
    }

    private void RefreshPairingInfo()
    {
        _currentAddress = _server.GetPairUrl();
        PinText.Text = _server.PairPin;
        GenerateQrCode(_currentAddress);
    }

    private void GenerateQrCode(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(data);

        var bytes = qr.GetGraphic(18);
        using var stream = new MemoryStream(bytes);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();

        QrImage.Source = bitmap;
    }

    private static string GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString(3)
            ?? "0.5.5";
    }

    private void SetServerStatus(bool active)
    {
        var text = active
            ? T("● Serwer aktywny", "● Server active")
            : T("● Serwer zatrzymany", "● Server stopped");

        StatusText.Text = text;
        SidebarStatusText.Text = active
            ? T("● aktywny", "● active")
            : T("● zatrzymany", "● stopped");
        TopStatusText.Text = active
            ? ResourceText("Ready")
            : T("Zatrzymany", "Stopped");
    }

    private void UpdateUpdateStatusText()
    {
        UpdateStatusText.Text = _updateService.IsConfigured
            ? T("GitHub Releases aktywne.", "GitHub Releases active.")
            : T("Ustaw repozytorium w UpdateSettings.cs.", "Configure the repository in UpdateSettings.cs.");
    }

    private async Task CheckForUpdatesSilentlyAsync()
    {
        if (!_updateService.IsConfigured)
            return;

        try
        {
            var update = await _updateService.CheckAsync();
            if (update == null)
                return;

            var targetVersion = update.TargetFullRelease.Version?.ToString();
            if (!string.IsNullOrWhiteSpace(targetVersion))
                UpdateStatusText.Text = T(
                    $"Dostępna wersja {targetVersion}.",
                    $"Version {targetVersion} is available.");
        }
        catch
        {
            // Silent startup check should never interrupt the app.
        }
    }

    private void State_TransferAdded(TransferHistoryItem item)
    {
        if (item.Direction != TransferDirection.PhoneToPc)
            return;

        if (!AppSettings.Load().TransferNotifications)
            return;

        Dispatcher.Invoke(() =>
        {
            _tray?.ShowTransferNotification(item.FileName, item.Size);
        });
    }

    private void RecreateTray()
    {
        _tray?.Dispose();
        _tray = new TrayService(
            ShowFromTray,
            CreateNewSession,
            OpenDownloadFolder,
            ExitApplication);
    }

    private void ShowFromTray()
    {
        Dispatcher.Invoke(() =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Topmost = true;
            Topmost = false;
        });
    }

    private void CreateNewSession()
    {
        Dispatcher.Invoke(() =>
        {
            _server.RegenerateSession();
            RefreshPairingInfo();

            var scale = new ScaleTransform(0.96, 0.96, QrImage.ActualWidth / 2, QrImage.ActualHeight / 2);
            QrImage.RenderTransform = scale;
            QrImage.Opacity = 0.45;

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            QrImage.BeginAnimation(OpacityProperty, new DoubleAnimation(0.45, 1, TimeSpan.FromMilliseconds(180)));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });

            StatusText.Text = T("● Nowa sesja gotowa", "● New session ready");
        });
    }

    private void OpenDownloadFolder()
    {
        var folder = _state.DownloadFolder;
        Directory.CreateDirectory(folder);

        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    private void OpenDownloadFolderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenDownloadFolder();
    }

    private void ExitApplication()
    {
        Dispatcher.Invoke(() =>
        {
            _allowExit = true;
            Close();
            WpfApplication.Current.Shutdown();
        });
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowExit)
            return;

        if (!AppSettings.Load().MinimizeToTray)
        {
            _allowExit = true;
            WpfApplication.Current.Shutdown();
            return;
        }

        e.Cancel = true;
        Hide();

        _tray?.ShowMessage(
            T("DropLAN działa w tle", "DropLAN is running in the background"),
            T(
                "Transfery i strona telefonu nadal działają.",
                "Transfers and the phone page are still running."));
    }

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_updateService.IsConfigured)
        {
            WpfMessageBox.Show(
                T(
                    "Najpierw ustaw adres repozytorium GitHub w UpdateSettings.cs.",
                    "Configure the GitHub repository URL in UpdateSettings.cs first."),
                "DropLAN",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Information);
            return;
        }

        CheckUpdatesButton.IsEnabled = false;
        UpdateProgress.Visibility = Visibility.Collapsed;
        UpdateStatusText.Text = T("Sprawdzanie aktualizacji…", "Checking for updates…");

        try
        {
            var update = await _updateService.CheckAsync();

            if (update == null)
            {
                UpdateStatusText.Text = T("Masz najnowszą wersję.", "You have the latest version.");
                return;
            }

            var targetVersion = update.TargetFullRelease.Version?.ToString()
                ?? T("nowsza wersja", "newer version");

            var answer = WpfMessageBox.Show(
                T(
                    $"Dostępna jest wersja {targetVersion}.\n\nPobrać i zainstalować aktualizację?",
                    $"Version {targetVersion} is available.\n\nDownload and install the update?"),
                T("Aktualizacja DropLAN", "DropLAN update"),
                WpfMessageBoxButton.YesNo,
                WpfMessageBoxImage.Information);

            if (answer != WpfMessageBoxResult.Yes)
            {
                UpdateStatusText.Text = T($"Dostępna: {targetVersion}", $"Available: {targetVersion}");
                return;
            }

            UpdateProgress.Value = 0;
            UpdateProgress.Visibility = Visibility.Visible;
            UpdateStatusText.Text = T("Pobieranie aktualizacji…", "Downloading update…");

            await _updateService.DownloadAndInstallAsync(
                update,
                progress =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateProgress.Value = progress;
                        UpdateStatusText.Text = T(
                            $"Pobieranie… {progress}%",
                            $"Downloading… {progress}%");
                    });
                });
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = T(
                $"Błąd aktualizacji: {ex.Message}",
                $"Update error: {ex.Message}");
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private void AddFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = T("Wybierz pliki do udostępnienia", "Choose files to share"),
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
            _state.AddSharedFiles(dialog.FileNames);
    }

    private void DropZone_Drop(object sender, WpfDragEventArgs e)
    {
        DropZone.Background = FindBrush("SurfaceBrush", WpfBrushes.White);

        if (!e.Data.GetDataPresent(WpfDataFormats.FileDrop))
            return;

        if (e.Data.GetData(WpfDataFormats.FileDrop) is not string[] paths)
            return;

        var files = paths.Where(File.Exists).ToArray();
        _state.AddSharedFiles(files);
    }

    private void DropZone_DragEnter(object sender, WpfDragEventArgs e)
    {
        if (!e.Data.GetDataPresent(WpfDataFormats.FileDrop))
        {
            e.Effects = WpfDragDropEffects.None;
            return;
        }

        e.Effects = WpfDragDropEffects.Copy;
        DropZone.Background = FindBrush("AccentSoftBrush", WpfBrushes.AliceBlue);
    }

    private void DropZone_DragLeave(object sender, WpfDragEventArgs e)
    {
        DropZone.Background = FindBrush("SurfaceBrush", WpfBrushes.White);
    }

    private void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (SharedFilesList.SelectedItem is SharedFileItem selected)
            _state.RemoveSharedFile(selected.Id);
    }

    private void ClearSharedButton_Click(object sender, RoutedEventArgs e)
    {
        _state.ClearSharedFiles();
    }

    private void ChangeFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFolderDialog
        {
            Title = T(
                "Wybierz folder dla plików odebranych przez DropLAN",
                "Choose a folder for files received by DropLAN"),
            InitialDirectory = _state.DownloadFolder
        };

        if (dialog.ShowDialog() == true)
            _state.SetDownloadFolder(dialog.FolderName);
    }

    private void ReadWindowsClipboardButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (WpfClipboard.ContainsText())
                ClipboardTextBox.Text = WpfClipboard.GetText();
        }
        catch
        {
            StatusText.Text = T(
                "Nie udało się odczytać schowka Windows.",
                "Could not read the Windows clipboard.");
        }
    }

    private void PublishClipboardButton_Click(object sender, RoutedEventArgs e)
    {
        _state.SetClipboard(ClipboardTextBox.Text);
        StatusText.Text = T("● Schowek wysłany", "● Clipboard sent");
    }

    private void WriteWindowsClipboardButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            WpfClipboard.SetText(ClipboardTextBox.Text ?? "");
            StatusText.Text = T("● Skopiowano do Windows", "● Copied to Windows");
        }
        catch
        {
            StatusText.Text = T(
                "Nie udało się zapisać do schowka Windows.",
                "Could not write to the Windows clipboard.");
        }
    }

    private void NewSessionButton_Click(object sender, RoutedEventArgs e)
    {
        CreateNewSession();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        _state.Changed -= State_Changed;
        _state.TransferAdded -= State_TransferAdded;

        _tray?.Dispose();
        _tray = null;

        await _server.StopAsync();
    }
}
