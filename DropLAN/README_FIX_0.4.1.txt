DropLAN 0.4.1 - poprawka konfliktów WPF / WinForms

Powód:
<UseWindowsForms>true</UseWindowsForms> wprowadza typy o takich samych nazwach jak w WPF.
MainWindow korzysta z WPF, a tray z WinForms.

Naprawiono przez jawne aliasy:
- System.Windows.Application
- System.Windows.Media.Brushes
- System.Windows.Clipboard
- System.Windows.Media.Color
- System.Windows.DataFormats
- System.Windows.DragDropEffects
- System.Windows.DragEventArgs
- System.Windows.MessageBox
- Microsoft.Win32.OpenFileDialog
- Microsoft.Win32.OpenFolderDialog

TrayService nadal używa:
using Forms = System.Windows.Forms;

Po podmianie:
Build -> Clean Solution
usuń bin/obj
Build -> Rebuild Solution
