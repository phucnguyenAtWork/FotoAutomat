using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public partial class MainWindow : Window
{
    private readonly CustomerPreviewFlow flow = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private CustomerScreen? displayedScreen;
    private WindowState previousWindowState;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = flow;
        flow.PropertyChanged += OnFlowChanged;
        timer.Tick += OnTick;
        Closed += OnClosed;
        RefreshScreen();
    }

    private void OnTick(object? sender, EventArgs e) => flow.Tick();
    private void OnFlowChanged(object? sender, PropertyChangedEventArgs e) => RefreshScreen();

    private void RefreshScreen()
    {
        if (flow.Screen == CustomerScreen.Capture && !flow.CanSelectPhotos ||
            flow.Screen == CustomerScreen.Printing && !flow.CanFinishPrint)
            timer.Start();
        else
            timer.Stop();
        if (displayedScreen == flow.Screen) return;
        displayedScreen = flow.Screen;
        ScreenContent.ContentTemplate = (DataTemplate)FindResource(flow.Screen.ToString());
        var dark = flow.Screen == CustomerScreen.Capture;
        Canvas.Background = (Brush)FindResource(dark ? "Ink" : "Cream");
        Brand.Foreground = (Brush)FindResource(dark ? "Cream" : "Ink");
        Brand.Visibility = flow.Screen == CustomerScreen.Welcome ? Visibility.Hidden : Visibility.Visible;
        ProgressMarks.Children.Clear();
        for (var i = 0; i < 9 && flow.ProgressStep >= 0; i++)
        {
            ProgressMarks.Children.Add(new Border
            {
                Width = i == flow.ProgressStep ? 23 : 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(4),
                Background = (Brush)FindResource(i == flow.ProgressStep ? "Rose" : "Line")
            });
        }
        Title = $"FotoAutomat | UI preview | {flow.Screen}";
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            ScreenContent.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))));
    }

    private void OnChoice(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag } || tag.Length < 2 ||
            !int.TryParse(tag.AsSpan(1), out var index)) return;
        switch (tag[0])
        {
            case 'f': flow.SelectFilter(index); break;
            case 'l': flow.SelectLayout(index); break;
            case 'q': flow.SelectPackage(index); break;
            case 'p': flow.SelectPhoto(index); break;
        }
    }

    private void OnNext(object sender, RoutedEventArgs e) => flow.Next();
    private void OnBack(object sender, RoutedEventArgs e) => flow.Back();
    private void OnRestart(object sender, RoutedEventArgs e) => flow.Restart();
    private void OnRetake(object sender, RoutedEventArgs e) => flow.Retake();
    private void OnPaymentChoice(object sender, RoutedEventArgs e) =>
        flow.ChoosePayment((sender as FrameworkElement)?.Tag as string == "cash");
    private void OnSlot(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: int index }) flow.AssignSlot(index);
    }

    private void OnWindowKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            if (WindowStyle == WindowStyle.None) ExitFullScreen();
            else
            {
                previousWindowState = WindowState;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && WindowStyle == WindowStyle.None)
        {
            ExitFullScreen();
            e.Handled = true;
        }
    }

    private void ExitFullScreen()
    {
        WindowStyle = WindowStyle.SingleBorderWindow;
        WindowState = previousWindowState;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        timer.Stop();
        timer.Tick -= OnTick;
        flow.PropertyChanged -= OnFlowChanged;
    }
}

public sealed class IndexEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int index && int.TryParse(parameter?.ToString(), out var target) && index == target;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
