using System.Drawing;
using System.Globalization;
using Forms = System.Windows.Forms;

namespace DropLAN;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;

    private static bool IsPolish =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("pl", StringComparison.OrdinalIgnoreCase);

    private static string T(string pl, string en) =>
        IsPolish ? pl : en;

    public TrayService(
        Action openWindow,
        Action newSession,
        Action openDownloadFolder,
        Action exitApplication)
    {
        var menu = new Forms.ContextMenuStrip();

        menu.Items.Add(
            T("Otwórz DropLAN", "Open DropLAN"),
            null,
            (_, _) => openWindow());

        menu.Items.Add(new Forms.ToolStripSeparator());

        menu.Items.Add(
            T("Nowa sesja", "New session"),
            null,
            (_, _) => newSession());

        menu.Items.Add(
            T("Otwórz folder odbiorczy", "Open download folder"),
            null,
            (_, _) => openDownloadFolder());

        menu.Items.Add(new Forms.ToolStripSeparator());

        menu.Items.Add(
            T("Zakończ DropLAN", "Exit DropLAN"),
            null,
            (_, _) => exitApplication());

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "DropLAN",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => openWindow();
    }

    public void ShowTransferNotification(string fileName, long size)
    {
        _notifyIcon.BalloonTipTitle = "DropLAN";
        _notifyIcon.BalloonTipText = IsPolish
            ? $"Odebrano: {fileName} ({FormatBytes(size)})"
            : $"Received: {fileName} ({FormatBytes(size)})";
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(3500);
    }

    public void ShowMessage(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(3000);
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];

        double value = bytes;
        var index = 0;

        while (value >= 1024 && index < units.Length - 1)
        {
            value /= 1024;
            index++;
        }

        return index == 0
            ? $"{value:0} {units[index]}"
            : $"{value:0.#} {units[index]}";
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
