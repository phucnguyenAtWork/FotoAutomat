using System.Windows;

namespace FotoAutomat.Desktop;

public partial class UnsavedLayoutWindow : Window
{
    public bool SaveRequested { get; private set; }
    public UnsavedLayoutWindow() => InitializeComponent();
    private void OnSave(object sender, RoutedEventArgs e) { SaveRequested = true; DialogResult = true; }
    private void OnDiscard(object sender, RoutedEventArgs e) => DialogResult = true;
}
