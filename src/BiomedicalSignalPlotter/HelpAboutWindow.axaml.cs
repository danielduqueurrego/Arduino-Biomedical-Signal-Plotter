using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BiomedicalSignalPlotter;

public partial class HelpAboutWindow : Window
{
    public HelpAboutWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
