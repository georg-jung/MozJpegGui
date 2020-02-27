using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using Path = System.IO.Path;

namespace MozJpegGui
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /*private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "JPEGs|*.jpg;*.jpeg"
            };
            if (ofd.ShowDialog() != true)
                return;
            var file = ofd.FileName;
            using var bmp = new Bitmap(file);
            using var tjc = new MozJpegSharp.TJCompressor();
            var compressed = tjc.Compress(bmp, MozJpegSharp.TJSubsamplingOption.Chrominance420, 75, MozJpegSharp.TJFlags.None);
            var outFolder = Path.GetDirectoryName(file);
            var outFile = Path.GetFileNameWithoutExtension(file) + "_mozjpeg";
            string outPath() => Path.Combine(outFolder, $"{outFile}.jpg");
            while (File.Exists(outPath()))
                outFile += Guid.NewGuid().ToString("n").Substring(0, 10);
            File.WriteAllBytes(outPath(), compressed);
        }*/

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;
            var trg = Path.Combine(dialog.FileName, "originals");
            Directory.CreateDirectory(trg);
            var files = Directory.GetFiles(dialog.FileName, "*.jpg");
            progressBar.Value = 0;
            progressBar.Maximum = files.Length;
            compressFolderButton.IsEnabled = false;
            await Compress(files, trg, new Progress<(int current, int count)>(UpdateProgress)).ConfigureAwait(true);
            compressFolderButton.IsEnabled = true;
        }

        private void UpdateProgress((int current, int count) progress)
        {
            progressBar.Value = progress.current;
            statusLabel.Content = progress.current == progress.count ? "Finished!" : $"{progress.current} / {progress.count}";
        }

        private async Task Compress(string[] files, string moveTarget, IProgress<(int current, int count)> progress = null)
        {
            var cnt = 0;
            await Task.Run(() =>
                Parallel.ForEach(files, file =>
                {
                    CompressSingle(file, moveTarget);
                    Interlocked.Increment(ref cnt);
                    progress?.Report((cnt, files.Length));
                })).ConfigureAwait(false);
        }

        private static void CompressSingle(string file, string moveTarget)
        {
            using var tjc = new MozJpegSharp.TJCompressor();
            byte[] compressed;
            PropertyItem[] exif;
            using (var bmp = new Bitmap(file))
            {
                exif = bmp.PropertyItems;
                compressed = tjc.Compress(bmp, MozJpegSharp.TJSubsamplingOption.Chrominance420, 75, MozJpegSharp.TJFlags.None);
            }
            using var ms = new MemoryStream(compressed);
            using var img = System.Drawing.Image.FromStream(ms, false, false);
            foreach (var item in exif)
                img.SetPropertyItem(item);
            var fileName = Path.GetFileName(file);
            File.Move(file, Path.Combine(moveTarget, fileName));
            img.Save(file, ImageFormat.Jpeg);
        }
    }
}
