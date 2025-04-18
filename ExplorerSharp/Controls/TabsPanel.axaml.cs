using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using System.Linq;

namespace ExplorerSharp;

public partial class TabsPanel : UserControl
{
    public TabsPanel()
    {
        InitializeComponent();
    }

    public void ItemsPresenter_DoubleTapped(object sender, TappedEventArgs args)
    {
        Control border = ((Grid)(sender as Control).Parent.Parent).Children.ToList().Find(o => o is Border b && b.Name == "PART_ContentBorder");
        border.IsVisible = !border.IsVisible;
    }
}