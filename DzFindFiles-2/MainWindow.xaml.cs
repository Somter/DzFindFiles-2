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

namespace DzFindFiles_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread searchThread;
        private ManualResetEvent stopEvent = new ManualResetEvent(false);
       
        public MainWindow()
        {
            InitializeComponent();
            LoadDrives();
            buttonStop.IsEnabled = false; 
        }

        private void FindFile()
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

                stopEvent.Reset(); 
                if (!string.IsNullOrEmpty(phrase))
                {
                    SearchFilesWithPhrase(searchPath, filesType, searchOption, phrase);
                }
                else
                {
                    SearchFilesSafe(searchPath, filesType, searchOption);
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

        private void SearchFilesWithPhrase(string searchPath, string searchPattern, System.IO.SearchOption searchOption, string phrase)
        {
            try
            {
                foreach (string filePath in Directory.GetFiles(searchPath, searchPattern))
                {
                    if (stopEvent.WaitOne(0)) return; 

                    try
                    {
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader reader = new StreamReader(fs))
                        {
                            string fileContent = reader.ReadToEnd();
                            if (fileContent.Contains(phrase))
                            {
                                FileInfo file = new FileInfo(filePath);
                                FileInfoModel fileInfoModel = new FileInfoModel(
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
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show("Ошибка при чтении файла: " + ex.Message));
                    }
                }

                if (searchOption == System.IO.SearchOption.AllDirectories)
                {
                    foreach (string dir in Directory.GetDirectories(searchPath))
                    {
                        if (stopEvent.WaitOne(0)) return; 

                        try
                        {
                            SearchFilesWithPhrase(dir, searchPattern, searchOption, phrase);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Игнорируем защищенные папки
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Пропускаем папки без доступа
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Ошибка при поиске: " + ex.Message));
            }
        }


        void SearchFilesSafe(string searchPath, string searchPattern, System.IO.SearchOption searchOption)
        {
            try
            {
                foreach (string filePath in Directory.GetFiles(searchPath, searchPattern))
                {
                    if (stopEvent.WaitOne(0)) return; 

                    FileInfo file = new FileInfo(filePath);
                    FileInfoModel fileInfoModel = new FileInfoModel(
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
                        if (stopEvent.WaitOne(0)) return; 

                        try
                        {
                            SearchFilesSafe(dir, searchPattern, searchOption);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Игнорируем защищенные папки
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Пропускаем папки без доступа
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Ошибка при поиске: " + ex.Message));
            }
        }


        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            if (searchThread != null && searchThread.IsAlive)
            {
                stopEvent.Set(); 
            }

            stopEvent.Reset(); 

            searchThread = new Thread(FindFile);
            searchThread.IsBackground = true;
            searchThread.Start();

            buttonStop.IsEnabled = true;
            buttonStop.Content = "Остановить"; 
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            if (searchThread != null && searchThread.IsAlive)
            {
                stopEvent.Set(); 
            }

            Dispatcher.Invoke(() =>
            {
                buttonStop.IsEnabled = false; 
                buttonStop.Content = "Остановить"; 
            });
        }

        private void LoadDrives()
        {
            comboBoxDrives.Items.Clear();

            if (Directory.Exists("C:\\")) comboBoxDrives.Items.Add("C:");
            if (Directory.Exists("D:\\")) comboBoxDrives.Items.Add("D:");

            if (comboBoxDrives.Items.Count > 0)
                comboBoxDrives.SelectedIndex = 0;
        }

        private void textBoxMask_TextChanged(object sender, TextChangedEventArgs e) { }
    }
}