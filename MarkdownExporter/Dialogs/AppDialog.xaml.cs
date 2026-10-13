using System.Windows;
using System.Windows.Controls;
using MarkdownExporter.Localization;

namespace MarkdownExporter.Dialogs;

public enum AppDialogKind
{
    Question,
    Warning,
    Error
}

public partial class AppDialog : Window
{
    private AppDialog(string message, string heading, bool confirm, AppDialogKind kind)
    {
        InitializeComponent();
        Heading.Text = heading;
        Message.Text = message;
        SecondaryButton.Visibility = confirm ? Visibility.Visible : Visibility.Collapsed;
        SecondaryButton.Content = LanguageManager.Text("DialogNo");
        PrimaryButton.Content = LanguageManager.Text(confirm ? "DialogYes" : "DialogOk");
        ToneMark.SetResourceReference(Border.BackgroundProperty, kind switch
        {
            AppDialogKind.Warning => "UiBrush.Warning",
            AppDialogKind.Error => "UiBrush.Error",
            _ => "UiBrush.Accent"
        });
    }

    public static bool Confirm(Window owner, string message, string heading,
        AppDialogKind kind = AppDialogKind.Question) =>
        new AppDialog(message, heading, true, kind) { Owner = owner }.ShowDialog() == true;

    public static void ShowMessage(Window? owner, string message, string heading,
        AppDialogKind kind = AppDialogKind.Error)
    {
        var dialog = new AppDialog(message, heading, false, kind);
        if (owner is not null)
            dialog.Owner = owner;
        else
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        dialog.ShowDialog();
    }

    private void Window_SourceInitialized(object sender, EventArgs e) =>
        ((App)Application.Current).ThemeManager.ApplyTitleBar(this);

    private void PrimaryButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void SecondaryButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
