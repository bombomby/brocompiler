using System;
using System.Windows;
using BroCollector;
using MahApps.Metro.Controls;
using BroCompiler.Models;

namespace BroCompiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        DataCollector Collector { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Collector = new DataCollector(new Config()
            {
                ProcessFilters = new String[] { "notepad.exe", "calc.exe", "CL.exe", "link.exe", "MSBuild.exe" }
                //ProcessFilters = new String[] { "link.exe" }
            });

            Collector.Start();

            ProcessList.DataContext = Collector;
        }

        private void ShowOnTimeline_Click(object sender, RoutedEventArgs e)
        {
            Timeline.Root = new ProcessGroupModel("GroupName", Collector.ProcessEvents);
        }
    }
}
