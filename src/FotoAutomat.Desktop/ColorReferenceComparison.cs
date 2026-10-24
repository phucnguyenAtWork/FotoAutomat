using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FotoAutomat.Desktop;

public sealed partial class ColorEditorPanel
{
    private FrameworkElement BuildReferenceComparison(Grid editedView, Panel actions)
    {
        var compare = new CheckBox { Content = "So với reference", IsEnabled = false, MinHeight = EditorUi.ControlHeight, FontSize = EditorUi.TextSize, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 10, 6), VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(compare);
        var grid = new Grid(); Grid.SetRow(grid, 1);
        var referenceColumn = new ColumnDefinition { Width = new GridLength(0) };
        grid.ColumnDefinitions.Add(referenceColumn); grid.ColumnDefinitions.Add(new());
        var image = new Image { Stretch = Stretch.Uniform, Margin = new Thickness(4) };
        int referenceIndex = 0;
        var filename = new TextBlock { Foreground = Brushes.Silver, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 8, 0) };
        var counter = new TextBlock { Foreground = Brushes.Silver, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        var navigation = new StackPanel { Orientation = Orientation.Horizontal };
        Button Arrow(string text, string help, int step)
        {
            var button = new Button { Content = text, ToolTip = help, Width = 40, Height = 32, MinHeight = 32, FontSize = 18, Padding = new Thickness(0), Background = Brushes.Transparent, Foreground = Brushes.LightGray, BorderThickness = new Thickness(0) };
            System.Windows.Automation.AutomationProperties.SetName(button, help);
            button.Click += (_, _) => { referenceIndex = (referenceIndex + step + matchLook.References.Count) % matchLook.References.Count; ShowReference(); };
            return button;
        }
        navigation.Children.Add(Arrow("‹", "Reference trước", -1)); navigation.Children.Add(counter); navigation.Children.Add(Arrow("›", "Reference sau", 1));
        var referencePane = new DockPanel { Background = new SolidColorBrush(Color.FromRgb(28, 30, 31)), Margin = new Thickness(0, 0, 6, 0), Visibility = Visibility.Collapsed };
        var heading = new DockPanel { Height = 36, Margin = new Thickness(8, 0, 8, 0) };
        var title = new TextBlock { Text = "Reference", Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center };
        DockPanel.SetDock(title, Dock.Left); heading.Children.Add(title);
        DockPanel.SetDock(navigation, Dock.Right); heading.Children.Add(navigation); heading.Children.Add(filename);
        DockPanel.SetDock(heading, Dock.Top); referencePane.Children.Add(heading); referencePane.Children.Add(image);
        grid.Children.Add(referencePane);
        var resultPane = new Grid { ClipToBounds = true };
        Grid.SetRow(editedView, 0);
        resultPane.Children.Add(editedView); Grid.SetColumn(resultPane, 1); grid.Children.Add(resultPane);
        void Layout()
        {
            bool show = compare.IsChecked == true && matchLook.References.Count > 0;
            referencePane.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            referenceColumn.Width = show ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            ResetView();
        }
        compare.Click += (_, _) => Layout();
        void ShowReference()
        {
            var item = matchLook.References.ElementAtOrDefault(referenceIndex);
            image.Source = item?.Bitmap; filename.Text = item?.Name ?? ""; filename.ToolTip = item?.Name;
            counter.Text = $"{referenceIndex + 1} / {matchLook.References.Count}";
            navigation.Visibility = matchLook.References.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }
        matchLook.ReferencesChanged += () =>
        {
            referenceIndex = 0; ShowReference();
            compare.IsEnabled = matchLook.References.Count > 0;
            compare.IsChecked = compare.IsEnabled; Layout();
        };
        return grid;
    }
}
