using Clowd.Clipboard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfTests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string ReferenceImagePath => System.IO.Path.GetFullPath("rgba32.png");

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            var bs = new BitmapImage(new Uri(ReferenceImagePath));
            imgRef.Source = bs;

            var tzoom = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
            imgRef.Width = bs.PixelWidth / tzoom;
            imgRef.Height = bs.PixelHeight / tzoom;
        }

        private void LoadClipImg_Click(object sender, RoutedEventArgs e)
        {
            using (var c = new ClipboardHandle())
            {
                c.Open();
                var img = c.GetImage();
                imgClip.Source = img;

                if (img != null)
                {
                    // undo wpf dpi to show image at actual size
                    var tzoom = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
                    imgClip.Width = img.PixelWidth / tzoom;
                    imgClip.Height = img.PixelHeight / tzoom;
                    Canvas.SetLeft(imgClip, (int)((imgCanvas.ActualWidth / 2) - (imgClip.Width / 2)));
                    Canvas.SetTop(imgClip, (int)((imgCanvas.ActualHeight / 2) - (imgClip.Height / 2)));
                }
            }
        }

        private void ShowRefImg_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", $"/select,\"{ReferenceImagePath}\"");
        }

        private void CopyRefImg_Click(object sender, RoutedEventArgs e)
        {
            using (var c = new ClipboardHandle())
            {
                c.Open();
                c.SetImage(new BitmapImage(new Uri(ReferenceImagePath)));
            }
        }
    }
}
