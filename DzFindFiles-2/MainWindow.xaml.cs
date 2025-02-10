using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Threading.Tasks;

namespace DzFindFiles_2
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            LoadDrives();
            buttonStop.IsEnabled = false;
        }

        private void FindFile(CancellationToken token)
        {
            try
            {
                string filesType = "";
                string disk = "";
                bool searchInSubDirs = false;
                string phrase = "";

                Dispatcher.Invoke(() =>
                {
                    filesType = textBoxMask.Text;
                    disk = comboBoxDrives.Text;
                    searchInSubDirs = checkBoxSubDirs.IsChecked == true;
                    phrase = textBoxPhrase.Text;
                });

                string searchPath = disk + "\\";
                if (!Directory.Exists(searchPath))
                {
                    MessageBox.Show("Указанный диск или каталог не существует.");
                    Dispatcher.Invoke(() => buttonStop.IsEnabled = false);
                    return;
                }

                System.IO.SearchOption searchOption = searchInSubDirs ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;
                Dispatcher.Invoke(() => listViewResults.Items.Clear());

                if (!string.IsNullOrEmpty(phrase))
                {
                    SearchFilesWithPhrase(searchPath, filesType, searchOption, phrase, token);
                }
                else
                {
                    SearchFilesSafe(searchPath, filesType, searchOption, token);
                }

                Dispatcher.Invoke(() =>
                {
                    buttonStop.IsEnabled = false;
                    buttonStop.Content = "Остановить";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Ошибка: " + ex.Message));
            }
        }

        private void SearchFilesWithPhrase(string searchPath, string searchPattern, System.IO.SearchOption searchOption, string phrase, CancellationToken token)
        {
            try
            {
                foreach (string filePath in Directory.GetFiles(searchPath, searchPattern))
                {
                    if (token.IsCancellationRequested) return;

                    try
                    {
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader reader = new StreamReader(fs))
                        {
                            string fileContent = reader.ReadToEnd();
                            if (fileContent.Contains(phrase))
                            {
                                FileInfo file = new FileInfo(filePath);
                                var fileInfoModel = new FileInfoModel(
                                    file.Name,
                                    file.DirectoryName,
                                    file.Length.ToString(),
                                    file.LastWriteTime.ToString()
                                );

                                Dispatcher.Invoke(() =>
                                {
                                    listViewResults.Items.Add(fileInfoModel);
                                    textBlockResultsCount.Text = $"Результаты поиска: Количество найденных файлов: {listViewResults.Items.Count}";
                                });
                            }
                        }
                    }
                    catch { }
                }

                if (searchOption == System.IO.SearchOption.AllDirectories)
                {
                    foreach (string dir in Directory.GetDirectories(searchPath))
                    {
                        if (token.IsCancellationRequested) return;
                        try
                        {
                            SearchFilesWithPhrase(dir, searchPattern, searchOption, phrase, token);
                        }
                        catch (UnauthorizedAccessException) { }
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        private void SearchFilesSafe(string searchPath, string searchPattern, System.IO.SearchOption searchOption, CancellationToken token)
        {
            try
            {
                foreach (string filePath in Directory.GetFiles(searchPath, searchPattern))
                {
                    if (token.IsCancellationRequested) return;

                    FileInfo file = new FileInfo(filePath);
                    var fileInfoModel = new FileInfoModel(
                        file.Name,
                        file.DirectoryName,
                        file.Length.ToString(),
                        file.LastWriteTime.ToString()
                    );

                    Dispatcher.Invoke(() =>
                    {
                        listViewResults.Items.Add(fileInfoModel);
                        textBlockResultsCount.Text = $"Результаты поиска: Количество найденных файлов: {listViewResults.Items.Count}";
                    });
                }

                if (searchOption == System.IO.SearchOption.AllDirectories)
                {
                    foreach (string dir in Directory.GetDirectories(searchPath))
                    {
                        if (token.IsCancellationRequested) return;
                        try
                        {
                            SearchFilesSafe(dir, searchPattern, searchOption, token);
                        }
                        catch (UnauthorizedAccessException) { }
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            Task.Factory.StartNew(() => FindFile(token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            buttonStop.IsEnabled = true;
            buttonStop.Content = "Остановить";
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
            Dispatcher.Invoke(() =>
            {
                buttonStop.IsEnabled = false;
                buttonStop.Content = "Остановить";
            });
        }

        private void LoadDrives()
        {
            comboBoxDrives.Items.Clear();

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    comboBoxDrives.Items.Add(drive.Name);
                }
            }

            if (comboBoxDrives.Items.Count > 0)
                comboBoxDrives.SelectedIndex = 0;
        }

        private void textBoxMask_TextChanged(object sender, TextChangedEventArgs e) { }
    }
}
