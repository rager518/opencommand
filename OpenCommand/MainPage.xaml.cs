using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace OpenCommand
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            //var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            //appView.Title = "Open Command";
        }

        private void ButtonHamburger_Click(object sender, RoutedEventArgs e)
        {
            SplitViewContent.IsPaneOpen = !SplitViewContent.IsPaneOpen;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Setting.IsSelected)
            {
                TextBlockTitle.Text = "Setting";
                MyFrame.Navigate(typeof(Setting));
            }
            else if (About.IsSelected)
            {
                TextBlockTitle.Text = "About";
                MyFrame.Navigate(typeof(More));
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Setting.IsSelected = true;
        }
    }
}
