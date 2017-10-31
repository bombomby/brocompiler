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
using BroCollector;
using MahApps.Metro.Controls;

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
            //ProcessGroupModel group = new ProcessGroupModel("GroupName", Collector.ProcessEvents);
            Timeline.Root = null;
        }
    }
}
