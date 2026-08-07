using System.Windows;

namespace DropLAN;

public partial class LanguageWindow : Window
{
    public string? SelectedLanguage { get; private set; }

    public LanguageWindow()
    {
        InitializeComponent();
    }

    private void PolishButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SelectLanguage("pl");
    }

    private void EnglishButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SelectLanguage("en");
    }

    private void SelectLanguage(string language)
    {
        SelectedLanguage = language;
        DialogResult = true;
        Close();
    }
}
