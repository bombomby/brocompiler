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
            ProcessList.ProcessDataGrid.SelectionChanged += ProcessDataGrid_SelectionChanged;
        }

        private void ProcessDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ProcessData process = ProcessList.ProcessDataGrid.SelectedItem as ProcessData;
            if (process != null)
            {
                ThreadView.Board = new ThreadGroupModel(process);
            }
        }

        private void ShowOnTimeline_Click(object sender, RoutedEventArgs e)
        {
            Timeline.Board = new ProcessGroupModel("GroupName", Collector.Group);
        }

        enum FileOperation
        {
            Save,
            Load,
        }

        private String SelectFileDialog(FileOperation op)
        {
            FileDialog dlg = null;

            if (op == FileOperation.Save)
                dlg = new SaveFileDialog();
            else
                dlg = new OpenFileDialog();

            dlg.FileName = "Capture";
            dlg.DefaultExt = ".bro";
            dlg.Filter = "Bro Capture (.bro)|*.bro";
            bool? result = dlg.ShowDialog();
            return (result == true) ? dlg.FileName : null;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            String file = SelectFileDialog(FileOperation.Save);
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
            String file = SelectFileDialog(FileOperation.Load);
            LoadCapture(file);
        }

        public void LoadCapture(String file)
        {
            if (file != null && File.Exists(file))
            {
                using (Stream stream = File.OpenRead(file))
                {
                    ProcessGroup group = Serializer.Load<ProcessGroup>(stream);
                    Collector.Group = group;

                    ProcessList.DataContext = null;
                    ProcessList.DataContext = Collector;

                    Timeline.Board = new ProcessGroupModel(file, group);
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Collector.Group?.Processes.Clear();
        }
    }
}
