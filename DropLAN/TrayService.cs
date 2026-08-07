using System.Drawing;
using Forms = System.Windows.Forms;

namespace DropLAN;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayService(
        Action openWindow,
        Action copyAddress,
        Action newSession,
        Action openDownloadFolder,
        Action exitApplication)
    {
        var menu = new Forms.ContextMenuStrip();

        menu.Items.Add(
            "Otwórz DropLAN",
            null,
            (_, _) => openWindow());

        menu.Items.Add(new Forms.ToolStripSeparator());

        menu.Items.Add(
            "Kopiuj adres",
            null,
            (_, _) => copyAddress());

        menu.Items.Add(
            "Nowa sesja",
            null,
            (_, _) => newSession());

        menu.Items.Add(
            "Otwórz folder odbiorczy",
            null,
            (_, _) => openDownloadFolder());

        menu.Items.Add(new Forms.ToolStripSeparator());

        menu.Items.Add(
            "Zakończ DropLAN",
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

    public void ShowTransferNotification(
        string fileName,
        long size)
    {
        _notifyIcon.BalloonTipTitle = "DropLAN";
        _notifyIcon.BalloonTipText =
            $"Odebrano: {fileName} ({FormatBytes(size)})";
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(3500);
    }

    public void ShowMessage(
        string title,
        string message)
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

        while (value >= 1024 &&
               index < units.Length - 1)
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
