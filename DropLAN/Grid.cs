using System.Windows;

namespace DropLAN;

internal static class Grid
{
    public static void SetRow(UIElement element, int value) =>
        System.Windows.Controls.Grid.SetRow(element, value);

    public static void SetColumn(UIElement element, int value) =>
        System.Windows.Controls.Grid.SetColumn(element, value);

    public static void SetColumnSpan(UIElement element, int value) =>
        System.Windows.Controls.Grid.SetColumnSpan(element, value);
}

