using System.Windows;

namespace DropLAN;

internal static class Grid
{
    public static void SetRow(DependencyObject element, int value) =>
        System.Windows.Controls.Grid.SetRow(element, value);

    public static void SetColumn(DependencyObject element, int value) =>
        System.Windows.Controls.Grid.SetColumn(element, value);

    public static void SetColumnSpan(DependencyObject element, int value) =>
        System.Windows.Controls.Grid.SetColumnSpan(element, value);
}
