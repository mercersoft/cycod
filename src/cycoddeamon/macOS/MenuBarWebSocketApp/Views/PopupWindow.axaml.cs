using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MenuBarWebSocketApp.ViewModels;
using System;

namespace MenuBarWebSocketApp.Views;

public partial class PopupWindow : Window
{
    public PopupWindow()
    {
        InitializeComponent();
        
        Deactivated += (s, e) => Close();
        
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
                Close();
        };
    }
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is PopupViewModel vm)
        {
            vm.CloseRequested += () => Close();
        }
    }
}