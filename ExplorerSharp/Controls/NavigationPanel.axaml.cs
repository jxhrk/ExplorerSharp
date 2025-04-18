using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace ExplorerSharp;

struct NavPathBtnData
{
    public string Name {  get; set; }
    public string PathPart;
    public int Index = -1;

    public NavPathBtnData()
    {
    }
}

public partial class NavigationPanel : UserControl, INotifyPropertyChanged
{
    public MainWindow Main;

    List<NavPathBtnData> CurrentPath = new List<NavPathBtnData>();

    private string currentDirectoryDisplayName;


    public string CurrentDirectoryDisplayName
    {
        set
        {
            currentDirectoryDisplayName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentDirectoryDisplayName)));
        }
        get { return currentDirectoryDisplayName; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public NavigationPanel(MainWindow main)
    {
        InitializeComponent();
        Main = main;
    }

    public NavigationPanel()
    {
        InitializeComponent();
    }

    public void ChangePath(string path, string currentDirDisplayName)
    {

        string[] parts = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);

        PathListBox.ItemsSource = null;
        CurrentPath.Clear();

        int i = 0;
        foreach (string part in parts)
        {
            CurrentPath.Add(new NavPathBtnData() { Name = part, PathPart = part, Index = i });
            i++;
        }

        //TODO: do something with this
        NavPathBtnData first = CurrentPath.First();
        first.Name = Utils.GetDirDisplayName(parts[0]+"\\");
        CurrentPath[0] = first;

        PathListBox.ItemsSource = CurrentPath;
        CurrentDirectoryDisplayName = currentDirDisplayName;
    }

    public void BackButton_Click(object sender, RoutedEventArgs args)
    {
        Main.HistoryBack();
    }

    public void PathButton_Click(object sender, RoutedEventArgs args)
    {
        NavPathBtnData data = (NavPathBtnData)(sender as Button).DataContext;
        string path = GetPathFromData(data);

        NavPathBtnData next = CurrentPath.Find(o => o.Index == data.Index + 1);
        string select = next.Index == -1 ? "" : next.PathPart;


        Main.NavigateToDir(path, select);
    }

    private string GetPathFromData(NavPathBtnData data)
    {
        string path = "";
        foreach (NavPathBtnData navPathBtnData in CurrentPath)
        {
            path += navPathBtnData.PathPart;
            if (navPathBtnData.Index == data.Index)
            {
                break;
            }
            else
            {
                path += "\\";
            }
        }

        return path;
    }

    public void ListBox_PointerPressed(object sender, PointerPressedEventArgs args)
    {
        string path = GetPathFromData(new NavPathBtnData());
        PathTextBox.IsVisible = true;
        PathTextBox.IsHitTestVisible = true;
        PathTextBox.Focus();
        PathTextBox.Text = path;
        PathTextBox.SelectAll();
        PathListBox.IsVisible = false;
    }
    public void TextBox_LostFocus(object sender, RoutedEventArgs args)
    {
        PathTextBox.IsVisible = false;
        PathTextBox.IsHitTestVisible = false;
        PathListBox.IsVisible = true;
    }

    private void TextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Main.NavigateToDir(PathTextBox.Text);
            Main.FocusManager.ClearFocus();
        }
        
    }
}