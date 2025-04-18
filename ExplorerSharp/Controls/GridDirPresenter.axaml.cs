using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace ExplorerSharp.Controls
{
    public partial class GridDirPresenter : UserControl, IDirectoryContentPresenter
    {
        public MainWindow Main;
        public GridDirPresenter(MainWindow main)
        {
            InitializeComponent();
            Main = main;
        }

        public GridDirPresenter()
        {
            InitializeComponent();
        }

        public void ListBox_DoubleTapped(object sender, TappedEventArgs args)
        {
            FileInfo selected = ((DataGrid)sender).SelectedItem as FileInfo;
            Main.DoubleClicked(selected);

        }

        private void DataGrid_PointerReleased(object sender, PointerReleasedEventArgs args)
        {
            FileInfo selected = ((DataGrid)sender).SelectedItem as FileInfo;
            Main.MouseReleased(selected, args);
        }


        private void DataGrid_CellPointerPressed(object sender, DataGridCellPointerPressedEventArgs e)
        {
            if (e.PointerPressedEventArgs.GetCurrentPoint(sender as DataGridCell).Properties.IsMiddleButtonPressed)
            {
                DirectoryContentGrid.SelectedIndex = e.Row.Index;
            }
        }

        void IDirectoryContentPresenter.SetContent(System.Collections.Generic.List<FileInfo> files)
        {
            DirectoryContentGrid.ItemsSource = files;
        }

        void IDirectoryContentPresenter.SelectFile(int index) { DirectoryContentGrid.SelectedIndex = index; }
    }
}

