using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace TortoiseQueue
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process anaconda = null;
        private StreamWriter inputPipe = null;

        private const string _configFile = "queue.cfg";
        private string tortoisePath;

        Queue<TortoiseJob> queue;

        public MainWindow()
        {
            // Setup WPF
            InitializeComponent();

            // Get path to Tortoise Repo
            tortoisePath = GetTortoisePath();

            // If it doesn't exist, save the tortoise path
            if(!File.Exists(_configFile))
            {
                File.WriteAllText(_configFile, tortoisePath);
            }
        }

        private string GetTortoisePath()
        {
            // Do we have a config file?
            if(File.Exists(_configFile))
            {
                return File.ReadAllText(_configFile);
            }
            // Otherwise create one, and ask the user for it
            else
            {
                // Show open file dialog box
                var dialog = new OpenFileDialog();
                bool? result = dialog.ShowDialog();

                // Process open file dialog box results
                if (result == true)
                {
                    // Get Folder name
                    return System.IO.Path.GetDirectoryName(dialog.FileName);
                }
                // Shutdown if invalid
                else
                {
                    Application.Current.Shutdown();
                }
            }
            return "";
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            // Request the file
            var dialog = new OpenFileDialog();
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                CreateQueue(dialog.FileName);
            }
        }

        private void CreateQueue(string path)
        {
            // Create a queue
            queue = new Queue<TortoiseJob>();
            QueueListBox.Items.Clear();

            // Load each line of the CSV
            var lines = File.ReadAllLines(path);

            foreach(var line in lines)
            {
                // Split the line by commas
                var parts = line.Split(',');

                // Get voice
                var voice = parts[0];

                // Get emotion
                var emotion = parts[1];

                // Combine remaining parts into the "base message"
                var baseMessage = string.Join(',', parts.Skip(2).ToArray()).Replace("\"", "");

                // But also, we're going to break it apart by sentence
                List<string> sentences = new List<string>();
                var sb = new StringBuilder();
                foreach(char c in baseMessage)
                {
                    sb.Append(c);
                    if(c == '.' || c == '?' || c == '!')
                    {
                        sentences.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                if(sb.Length > 1)
                {
                    sentences.Add(sb.ToString());
                }

                // Go through each sentence
                foreach(var sentence in sentences)
                {
                    // Is there an emotion?
                    if(!string.IsNullOrWhiteSpace(emotion))
                    {
                        // Add version with emotion
                        queue.Enqueue(new TortoiseJob
                        {
                            Voice = voice,
                            Message = $"{emotion}{sentence}"
                        });
                    }

                    // Add version without emotion
                    queue.Enqueue(new TortoiseJob
                    {
                        Voice = voice,
                        Message = sentence
                    });
                }
            }

            // Display all jobs in the listbox
            foreach(var job in queue)
            {
                QueueListBox.Items.Add($"{job.Voice}: {job.Message}");
            }
        }

        private async void runButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the Button
            runButton.IsEnabled = false;

            // Launch Command Prompt
            ProcessStartInfo PSInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k",
                CreateNoWindow = false,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
            anaconda = new Process
            {
                StartInfo = PSInfo
            };
            anaconda.Start();
            inputPipe = anaconda.StandardInput;
            inputPipe.AutoFlush = true;

            // Activate Anaconda
            inputPipe.WriteLine("conda activate tortoise");
            inputPipe.WriteLine($"cd { tortoisePath.Replace('\\', '/') }");
            inputPipe.WriteLine("echo hello");
        }
    }
}
