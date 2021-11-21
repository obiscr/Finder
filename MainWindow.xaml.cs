using System;
using System.Collections.Generic;
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

using System.IO;

namespace Finder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int UIindex = 0;
        
        public MainWindow()
        {
            InitializeComponent();
            DriveInfo[] drivers = DriveInfo.GetDrives();
            List<DiskInfo> items = new List<DiskInfo>();
            foreach (DriveInfo driver in drivers)
            {
                DiskInfo diskInfo = new DiskInfo();
                diskInfo.Name = driver.Name;
                diskInfo.Volumn = driver.VolumeLabel;
                diskInfo.DisplayName = driver.VolumeLabel + " (" + driver.Name + ")";
                diskInfo.TotalSize = driver.TotalSize;
                diskInfo.AvailableFreeSpace = driver.AvailableFreeSpace;
                items.Add(diskInfo);
            }
            volumes.ItemsSource = items;
        }

        private void volumes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            DiskInfo item = (DiskInfo)listBox.SelectedItem;
            string path = item.Name;

            // add list component
            createSubDir(path,0);

            // StatusBar Status
            string useInfo = item.getAvailableFreeSpaceConverted() + "可用，共" + item.getTotalSizeConverted();
            usageLabel.Content = useInfo;
        }

        private void createSubDir(string path,int index)
        {
            List<FileInfo> listItems = new List<FileInfo>();
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            ListBox list = new ListBox();
            list.Style = FindResource("ListBoxStyle") as Style;
            list.ItemTemplate = FindResource("fileTemplate") as DataTemplate;
            Array.Sort(files, string.CompareOrdinal);
            foreach (var file in files)
            {
                // filter
                if (file.EndsWith("System Volume Information") || file.EndsWith("$RECYCLE.BIN"))
                {
                    continue;
                }
                ListBoxItem item = new ListBoxItem();
                //item.Content = "- | " + MyHelper.lastFileName(file);
                //list.Items.Add(item);
                item.Style = FindResource("ListBoxItemStyle") as Style;

                FileInfo tmp = new FileInfo();
                tmp.Name = file;
                tmp.DisplayName = "- | " + MyHelper.lastFileName(file);
                tmp.isDir = false;
                tmp.index = index + 1;
                listItems.Add(tmp);
            }

            string[] dirs = Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly);
            Array.Sort(dirs, string.CompareOrdinal);
            list.ItemTemplate = FindResource("dirTemplate") as DataTemplate;
            foreach (var dir in dirs)
            {
                // filter
                if (dir.EndsWith("System Volume Information") || 
                    dir.EndsWith("$RECYCLE.BIN") || 
                    dir.EndsWith("Config.Msi"))
                {
                    continue;
                }
                ListBoxItem item = new ListBoxItem();
                //item.Content = "d | "+MyHelper.lastFileName(dir);
                //list.Items.Add(item);

                item.Style = FindResource("ListBoxItemStyle") as Style;

                FileInfo tmp = new FileInfo();
                tmp.Name = dir;
                tmp.DisplayName = "d | " + MyHelper.lastFileName(dir);
                tmp.isDir = true;
                tmp.index = index + 1;
                listItems.Add(tmp);
            }
            list.ItemsSource = listItems;
            
            // add selection listener
            list.SelectionChanged += (se, ee) =>
            {
                ListBox listBox = (ListBox)se;
                FileInfo item = (FileInfo)listBox.SelectedItem;
                if (item.isDir)
                {
                    try
                    {
                        createSubDir(item.Name, item.index);
                    } catch (Exception e)
                    {
                        MessageBox.Show(mainWindow, "权限不足，访问出错", "权限不足", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("is file: " + item.Name);
                }
            };
            centerGrid.HorizontalAlignment = HorizontalAlignment.Left;

            // if index=0，it must choose the root dir，
            // so clear all column definitions，and
            // make UIIndex = 0
            if(index == 0)
            {
                centerGrid.ColumnDefinitions.Clear();
                UIindex = 0;
                // add a default column definitions after clear
                ColumnDefinition c = new ColumnDefinition();
                centerGrid.ColumnDefinitions.Add(c);
            }
            else
            {
                // if the index == UIIndex means open dir operation is all
                // go to next level dir,and not exists "back prev level" situation
                //
                // else, it's means exists "back prev level" operation. So 
                // we need to clean the dirs after "index" and make UIIndex equals index
                if (index == UIindex)
                {
                    ColumnDefinition c = new ColumnDefinition();
                    centerGrid.ColumnDefinitions.Add(c);
                }
                else
                {
                    for(int i = UIindex; i > index; i--)
                    {
                        centerGrid.ColumnDefinitions.RemoveAt(i-1);
                    }
                    ColumnDefinition c = new ColumnDefinition();
                    centerGrid.ColumnDefinitions.Add(c);
                    // reset UIIndex to index
                    UIindex = index;
                }
            }
            
            //centerGrid.set(UIindex, list);
            Grid.SetColumn(list, UIindex);
            centerGrid.Children.Add(list);
            UIindex++;
        }
    }

    public class DiskInfo
    {
        public string Name { get; set; }
        public string Volumn { get; set; }
        public string DisplayName { get; set; }
        public long TotalSize { get; set; }
        public long AvailableFreeSpace { get; set; }

        public string FormatByte(long bytes)
        {
            string[] Suffix = { "Byte", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            if (bytes > 1024)
            {
                for (i = 0; (bytes / 1024) > 0; i++, bytes /= 1024)
                {
                    dblSByte = bytes / 1024.0;
                }
            }

            return string.Format("{0:0.##}{1}", dblSByte, Suffix[i]);
        }

        public string getTotalSizeConverted()
        {
            return FormatByte(TotalSize);
        }

        public string getAvailableFreeSpaceConverted()
        {
            return FormatByte(AvailableFreeSpace);
        }
    }

    public class FileInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool isDir { get; set; }
        public int index { get; set; }
    }

    public class MyHelper
    {
        public static string lastFileName(string path)
        {
            if (!path.Contains("\\"))
            {
                return path;
            }
            else
            {
                int index = path.LastIndexOf("\\");
                return path.Substring(index+1, path.Length - index - 1);
            }
        }
    }
}
