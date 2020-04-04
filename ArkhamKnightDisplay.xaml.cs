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
using System.Threading;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace ArkhamKnightDisplay
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker updateGridWorker;
        private const string SAVE_FILE_PREFIX = "BAK1Save";
        private const string SAVE_FILE_SUFFIX = ".sgd";
        private const string DEFAULT_ROUTE_PATH = "Arkham Knight 100% Route - Route.tsv";
        private const string DEFAULT_ROUTE_PATH_FIRST = "Arkham Knight 100% Route - First Ending.tsv";
        private const string DEFAULT_ROUTE_PATH_NG_PLUS = "Arkham Knight 100% Route - NG+ Route.tsv";
        private const string DEFAULT_SAVE_PREFIX = "C:\\Program Files (x86)\\Steam\\userdata\\134351627\\208650\\remote";
        string[] routeLines = null;
        private const int ROW_HEIGHT = 40;

        public MainWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            StopButton.IsEnabled = false;
            StartButton.IsEnabled = true;
            updateGridWorker.CancelAsync();
        }

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            updateGridWorker = new BackgroundWorker();
            updateGridWorker.WorkerSupportsCancellation = true;
            updateGridWorker.WorkerReportsProgress = true;
            updateGridWorker.DoWork += backgroundWorkerOnDoWork;
            updateGridWorker.ProgressChanged += BackgroundWorkerOnProgressChanged;

            updateGridWorker.RunWorkerAsync();

            StopButton.IsEnabled = true;
            StartButton.IsEnabled = false;
            

        }

        private void backgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                worker.ReportProgress(0, "Dummy");
                Thread.Sleep(1000);
            }
        }

        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                string routepath = RoutePathBox.Text;

                bool isNewGamePlus = NGPlusBox.IsChecked.Value;
                bool isFirstEnding = FirstKnightfallBox.IsChecked.Value;
                if (String.IsNullOrEmpty(routepath))
                {
                    if (isFirstEnding)
                    {
                        routepath = DEFAULT_ROUTE_PATH_FIRST;
                    }
                    else if (isNewGamePlus)
                    {
                        routepath = DEFAULT_ROUTE_PATH_NG_PLUS;
                    }
                    else
                    {
                        routepath = DEFAULT_ROUTE_PATH;
                    }
                }

                string savepath = SavePathBox.Text;
                if (String.IsNullOrEmpty(savepath))
                {
                    savepath = DEFAULT_SAVE_PREFIX;
                }

                routeLines = System.IO.File.ReadAllLines(routepath);
                updateGrid(savepath, GetSaveFileIndex());
            }
            catch (Exception ex)
            {
                StopButton.IsEnabled = false;
                StartButton.IsEnabled = true;
                updateGridWorker.CancelAsync();
                System.Windows.MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void updateGrid(string savepath, string saveindex)
        {
            DisplayGrid.Children.Clear();
            DisplayGrid.RowDefinitions.Clear();
            DisplayGrid.ColumnDefinitions.Clear();

            DisplayGrid.ShowGridLines = true;
            DisplayGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(200) });
            DisplayGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });

            String saveFileText = System.IO.File.ReadAllText(GetSaveFileFullPath(savepath, saveindex));
            int firstNotDoneIndex = -1;
            int secondNotDoneIndex = -1;
            bool isNewGamePlus = NGPlusBox.IsChecked.Value;
            bool is120 = OneTwentyBox.IsChecked.Value;
            int minRequiredMatches = isNewGamePlus ? 1 : 0;
            int lineCount = 1;
            for (int index = 0; index < routeLines.Length; index++)
            {
                if (index == 0)
                {
                    continue;
                }
                string line = routeLines[index];

                string[] lineComponents = line.Split('\t');

                if (isNewGamePlus && (isRiddlerCollectible(lineComponents[1]) || isUpgrade(lineComponents[1]) || isCornerCase(lineComponents[0])))
                {
                    continue;
                }
                if (!is120 && isSeasonOfInfamy(lineComponents[1]))
                {
                    continue;
                }

                DisplayGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(ROW_HEIGHT) });
                TextBlock txt0 = new TextBlock();
                txt0.Text = lineComponents[0];
                txt0.TextWrapping = TextWrapping.Wrap;
                Grid.SetColumn(txt0, 0);
                Grid.SetRow(txt0, lineCount-1);

                DisplayGrid.Children.Add(txt0);

                string saveFileKey = lineComponents[2].Trim();
                Regex rx = new Regex(@"\b" + saveFileKey + @"\b");
                MatchCollection matches = rx.Matches(saveFileText);
                if (!String.IsNullOrEmpty(saveFileKey) && matches.Count > minRequiredMatches)
                {
                    TextBlock txt1 = new TextBlock();
                    txt1.Text = "Done";
                    Grid.SetColumn(txt1, 1);
                    Grid.SetRow(txt1, lineCount-1);
                    DisplayGrid.Children.Add(txt1);
                } 
                else if (firstNotDoneIndex < 0) 
                {
                    firstNotDoneIndex = lineCount;
                }
                else if (secondNotDoneIndex < 0)
                {
                    secondNotDoneIndex = lineCount;
                }
                lineCount++;
            }

            if (firstNotDoneIndex > -1 && IgnoreFirst.IsChecked.Value == false)
            {
                double numInViewport = GridScroll.Height / ROW_HEIGHT;
                int scrollHeight = (firstNotDoneIndex - 6) * (ROW_HEIGHT);
                GridScroll.ScrollToVerticalOffset(scrollHeight);
            }
            else if (secondNotDoneIndex > -1 && IgnoreFirst.IsChecked.Value == true)
            {
                double numInViewport = GridScroll.Height / ROW_HEIGHT;
                int scrollHeight = (secondNotDoneIndex - 6) * (ROW_HEIGHT);
                GridScroll.ScrollToVerticalOffset(scrollHeight);
            }
        }

        private bool isSeasonOfInfamy(String categoryName)
        {
            return
                string.Equals("Mad Hatter", categoryName) ||
                string.Equals("Freeze", categoryName) ||
                string.Equals("Killer Croc", categoryName) ||
                string.Equals("League of Assassins", categoryName);
        }

        private bool isUpgrade(String checkpointName)
        {
            return string.Equals("Upgrades", checkpointName);
        }

        private bool isCornerCase(String checkpointName)
        {
            return string.Equals("Diagnostics", checkpointName);
        }

        private bool isRiddlerCollectible(String categoryName)
        {
            return
                string.Equals("Riddles", categoryName) ||
                string.Equals("Riddler Trophies", categoryName) ||
                string.Equals("Breakable Objects", categoryName) ||
                string.Equals("Riddler Bomb", categoryName);
        }

        private String GetSaveFileFullPath(string savefileprefix, string saveindex)
        {
            string filename0 = System.IO.Path.Combine(savefileprefix, SAVE_FILE_PREFIX + saveindex + "x0" + SAVE_FILE_SUFFIX);
            string filename1 = System.IO.Path.Combine(savefileprefix, SAVE_FILE_PREFIX + saveindex + "x1" + SAVE_FILE_SUFFIX);
            string filename2 = System.IO.Path.Combine(savefileprefix, SAVE_FILE_PREFIX + saveindex + "x2" + SAVE_FILE_SUFFIX);
            DateTime writetime0 = DateTime.MinValue;
            DateTime writetime1 = DateTime.MinValue;
            DateTime writetime2 = DateTime.MinValue;
            if (System.IO.File.Exists(filename0))
            {
                writetime0 = System.IO.File.GetLastWriteTimeUtc(filename0);
            }
            if (System.IO.File.Exists(filename1))
            {
                writetime1 = System.IO.File.GetLastWriteTimeUtc(filename1);
            }
            if (System.IO.File.Exists(filename2))
            {
                writetime2 = System.IO.File.GetLastWriteTimeUtc(filename2);
            }

            string currentfile = filename0;
            DateTime currentwritetime = writetime0;
            if (currentwritetime < writetime1)
            {
                currentfile = filename1;
            }
            if (currentwritetime < writetime2)
            {
                currentfile = filename2;
            }
            return currentfile;
        }

        private String GetSaveFileIndex()
        {
            if (Save0.IsChecked == true)
            {
                return "0";
            }
            if (Save1.IsChecked == true)
            {
                return "1";
            }
            if (Save2.IsChecked == true)
            {
                return "2";
            }
            return "3";
        }
    }
}
