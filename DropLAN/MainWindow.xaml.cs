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
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using QRCoder;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DropLAN;

public partial class MainWindow : Window
{
    private readonly SharedState _state = new();
    private readonly RealtimeBroker _realtime = new();
    private readonly LocalServer _server;

    private string _currentAddress = "";
    private readonly UpdateService _updateService = new();
    private TrayService? _tray;
    private bool _allowExit;

    private static bool IsPolish =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("pl", StringComparison.OrdinalIgnoreCase);

    private static string T(string pl, string en) =>
        IsPolish ? pl : en;

    public MainWindow()
    {
        InitializeComponent();

        _server = new LocalServer(_state, _realtime);

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        Closing += MainWindow_Closing;

        _state.Changed += State_Changed;
        _state.TransferAdded += State_TransferAdded;
    }

    private async void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _server.StartAsync();

            RefreshPairingInfo();
            RefreshUiFromState();

            VersionText.Text =
                $"DropLAN {GetCurrentVersion()}";

            UpdateStatusText.Text =
                _updateService.IsConfigured
                    ? T("GitHub Releases aktywne.", "GitHub Releases active.")
                    : T("Ustaw repozytorium w UpdateSettings.cs.", "Configure the repository in UpdateSettings.cs.");

            _tray = new TrayService(
                ShowFromTray,
                CopyCurrentAddress,
                CreateNewSession,
                OpenDownloadFolder,
                ExitApplication);

            StatusText.Text = T("● Serwer aktywny", "● Server active");
        }
        catch (Exception ex)
        {
            StatusText.Foreground = WpfBrushes.IndianRed;
            StatusText.Text = T($"Błąd: {ex.Message}", $"Error: {ex.Message}");
        }
    }

    private void State_Changed()
    {
        Dispatcher.Invoke(RefreshUiFromState);
    }

    private void RefreshUiFromState()
    {
        DownloadFolderText.Text = _state.DownloadFolder;

        SharedFilesList.ItemsSource = null;
        SharedFilesList.ItemsSource = _state.GetSharedFiles();

        HistoryList.ItemsSource = null;
        HistoryList.ItemsSource = _state.GetHistory();

        if (!ClipboardTextBox.IsKeyboardFocusWithin)
            ClipboardTextBox.Text = _state.ClipboardText;
    }

    private void RefreshPairingInfo()
    {
        _currentAddress = _server.GetPairUrl();

        AddressText.Text = _currentAddress;
        PinText.Text = _server.PairPin;

        GenerateQrCode(_currentAddress);
    }

    private void GenerateQrCode(string text)
    {
        using var generator = new QRCodeGenerator();

        using var data = generator.CreateQrCode(
            text,
            QRCodeGenerator.ECCLevel.Q);

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
            ?? "0.5.2";
    }

    private void State_TransferAdded(
        TransferHistoryItem item)
    {
        if (item.Direction != TransferDirection.PhoneToPc ||
            item.Status != "Zakończono")
            return;

        Dispatcher.Invoke(() =>
        {
            _tray?.ShowTransferNotification(
                item.FileName,
                item.Size);
        });
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

    private void CopyCurrentAddress()
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                WpfClipboard.SetText(_currentAddress);
                StatusText.Text = T("● Adres skopiowany", "● Address copied");
            }
            catch
            {
                StatusText.Text = T("Nie udało się skopiować adresu.", "Could not copy the address.");
            }
        });
    }

    private void CreateNewSession()
    {
        Dispatcher.Invoke(() =>
        {
            _server.RegenerateSession();
            RefreshPairingInfo();

            _tray?.ShowMessage(
                "DropLAN",
                T("Utworzono nową sesję parowania.", "A new pairing session was created."));
        });
    }

    private void OpenDownloadFolder()
    {
        var folder = _state.DownloadFolder;
        Directory.CreateDirectory(folder);

        Process.Start(
            new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
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

    private void MainWindow_Closing(
        object? sender,
        CancelEventArgs e)
    {
        if (_allowExit)
            return;

        e.Cancel = true;
        Hide();

        _tray?.ShowMessage(
            T("DropLAN działa w tle", "DropLAN is running in the background"),
            T(
                "Serwer nadal działa. Otwórz aplikację z ikony przy zegarku.",
                "The server is still running. Open the app from the tray icon."));
    }

    private async void CheckUpdatesButton_Click(
        object sender,
        RoutedEventArgs e)
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

            var targetVersion =
                update.TargetFullRelease.Version?.ToString()
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
                UpdateStatusText.Text = T(
                    $"Dostępna: {targetVersion}",
                    $"Available: {targetVersion}");
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

    private void AddFilesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = T("Wybierz pliki do udostępnienia", "Choose files to share"),
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
            _state.AddSharedFiles(dialog.FileNames);
    }

    private void DropZone_Drop(
        object sender,
        WpfDragEventArgs e)
    {
        DropZone.Background = new SolidColorBrush(
            WpfColor.FromRgb(17, 21, 31));

        if (!e.Data.GetDataPresent(WpfDataFormats.FileDrop))
            return;

        if (e.Data.GetData(WpfDataFormats.FileDrop) is not string[] paths)
            return;

        var files = paths
            .Where(File.Exists)
            .ToArray();

        _state.AddSharedFiles(files);
    }

    private void DropZone_DragEnter(
        object sender,
        WpfDragEventArgs e)
    {
        if (!e.Data.GetDataPresent(WpfDataFormats.FileDrop))
        {
            e.Effects = WpfDragDropEffects.None;
            return;
        }

        e.Effects = WpfDragDropEffects.Copy;

        DropZone.Background = new SolidColorBrush(
            WpfColor.FromRgb(28, 31, 48));
    }

    private void DropZone_DragLeave(
        object sender,
        WpfDragEventArgs e)
    {
        DropZone.Background = new SolidColorBrush(
            WpfColor.FromRgb(17, 21, 31));
    }

    private void RemoveSelectedButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (SharedFilesList.SelectedItem is SharedFileItem selected)
            _state.RemoveSharedFile(selected.Id);
    }

    private void ClearSharedButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _state.ClearSharedFiles();
    }

    private void ChangeFolderButton_Click(
        object sender,
        RoutedEventArgs e)
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

    private void ReadWindowsClipboardButton_Click(
        object sender,
        RoutedEventArgs e)
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

    private void PublishClipboardButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _state.SetClipboard(ClipboardTextBox.Text);
        StatusText.Text = T("● Schowek udostępniony", "● Clipboard shared");
    }

    private void WriteWindowsClipboardButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            WpfClipboard.SetText(ClipboardTextBox.Text ?? "");
            StatusText.Text = T(
                "● Skopiowano do schowka Windows",
                "● Copied to Windows clipboard");
        }
        catch
        {
            StatusText.Text = T(
                "Nie udało się zapisać do schowka Windows.",
                "Could not write to the Windows clipboard.");
        }
    }

    private void CopyAddressButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        CopyCurrentAddress();
    }

    private void NewSessionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        CreateNewSession();
        StatusText.Text = T("● Utworzono nową sesję", "● New session created");
    }

    private void TitleBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Hide();

        _tray?.ShowMessage(
            T("DropLAN działa w tle", "DropLAN is running in the background"),
            T(
                "Transfery i strona telefonu nadal działają.",
                "Transfers and the phone page are still running."));
    }

    private async void MainWindow_Closed(
        object? sender,
        EventArgs e)
    {
        _state.Changed -= State_Changed;
        _state.TransferAdded -= State_TransferAdded;

        _tray?.Dispose();
        _tray = null;

        await _server.StopAsync();
    }
}
