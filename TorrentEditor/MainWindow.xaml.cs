using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TorrentMod.Torrent.Log += LogText;
        }

        private void textBox1_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void textBox1_PreviewDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null) return;

            for (int i = 0; i < files.Length; i++)
            {
                string extension = System.IO.Path.GetExtension(files[i]);
                if (string.Compare(extension, ".torrent", true) == 0)
                {
                    EditTorrent(files[i]);
                }
            }
        }

        private void selectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "(Torrent 文件)|*.torrent";
            if (dialog.ShowDialog(this) == false) return;
            for (int i = 0; i < dialog.FileNames.Length; i++)
            {
                EditTorrent(dialog.FileNames[i]);
            }
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            textBox1.Text = "";
        }

        private string GetTorrentName(TorrentMod.ItemBase item)
        {
            if (item.ItemType == TorrentMod.ItemType.String)
            {
                return (item as TorrentMod.StringItem).StringData;
            }
            else if (item.ItemType == TorrentMod.ItemType.Dictionary)
            {
                TorrentMod.ItemBase subItem;
                (item as TorrentMod.DictionaryItem).DictionaryData.TryGetValue("name", out subItem);
                if (subItem == null)
                {
                    (item as TorrentMod.DictionaryItem).DictionaryData.TryGetValue("info", out subItem);
                }

                if (subItem != null)
                {
                    return GetTorrentName(subItem);
                }
            }

            return null;
        }

        int index = 0;
        private void EditTorrent(string filename)
        {
            var fi = new FileInfo(filename);
            if (fi.Exists)
            {
                LogText($"------------------------------ {++index} ------------------------------");
                LogText($"开始解析Torrent文件：{filename}");
                var t3 = TorrentMod.Torrent.DecodeFile(filename);

                string name = GetTorrentName(t3);

                if (string.IsNullOrEmpty(name))
                {
                    Regex reg = new Regex(@"^\s*\[.*?\][\.\s]*", RegexOptions.IgnoreCase);
                    name = fi.Name;
                    while (reg.IsMatch(name))
                    {
                        name = reg.Replace(name, "");
                    }
                }
                else
                {
                    name += ".torrent";
                }

                string dir = $"{fi.Directory}\\torrents";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string path = $"{dir}\\{name}";
                TorrentMod.Torrent.WriteToFile(path, t3);
                LogText($"已保存为新文件：{path}");

                if ((bool)checkBox.IsChecked)
                {
                    try
                    {
                        File.Delete(fi.FullName);
                        LogText("源文件已删除");
                    }
                    catch (System.Exception e)
                    {

                        LogText($"源文件删除失败：{e.Message}");
                    }
                }
            }
        }

        private void LogText(string str)
        {
            textBox1.AppendText($"[{DateTime.Now}] {str}\n");
            textBox1.ScrollToEnd();
        }
    }
}
