using System;
using System.Windows;
using BroCollector;
using MahApps.Metro.Controls;
using BroCompiler.Models;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;

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
            Timeline.Root = new ProcessGroupModel("GroupName", Collector.Group);
        }

        private String SelectFileDialog()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = "Capture";
            dlg.DefaultExt = ".bro";
            dlg.Filter = "Bro Capture (.bro)|*.bro";
            bool? result = dlg.ShowDialog();
            return (result == true) ? dlg.FileName : null;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            String file = SelectFileDialog();
            if (file != null)
            {
                using (Stream stream = File.Create(file))
                {
                    Serializer.Save(stream, Collector.Group);
                }
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            String file = SelectFileDialog();
            if (file != null)
            {
                using (Stream stream = File.OpenRead(file))
                {
                    ProcessGroup group = Serializer.Load<ProcessGroup>(stream);
                    Collector.Group = group;

                    ProcessList.DataContext = null;
                    ProcessList.DataContext = Collector;
                }
            }
        }
    }
}
