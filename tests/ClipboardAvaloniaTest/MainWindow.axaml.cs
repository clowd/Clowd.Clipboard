using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Clowd.Clipboard;
using System;

namespace ClipboardAvaloniaTest
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            btn.Click += Clicked;
        }

        private void Clicked(object? sender, RoutedEventArgs e)
        {
            var bmp = ClipboardAvalonia.GetImage();
            img.Source = bmp;
        }
    }
}