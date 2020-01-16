using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Litres_books_downloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            #if DEBUG
                tbLogin.Text = "628912840";
                tbPassword.Password = "";
                tb_BookID.Text = "";
            #endif

            Parameters.Header = "Accept-Encoding=gzip,deflate,br;" +
                "Accept-Language=en-US,en;" +
                "Connection=keep-alive;" +
                "Host=sch.litres.ru;" +
                "Sec-Fetch-Mode=navigate;" +
                "Sec-Fetch-Site=none;" +
                "Sec-Fetch-User=?1;" +
                "Upgrade-Insecure-Requests=1;" +
                "User-Agent=Safari/537.37;";

        }

        private void tb_PreviewTextInput_DigitOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void btn_SaveBook(object sender, RoutedEventArgs e)
        {
            //проверка введеных данных
            if (!checkInputData())
                return;

            string bookID = tb_BookID.Text;
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Title = "Сохранить книгу";
            saveFile.FileName = $"Book_{bookID}";
            saveFile.Filter = "PDF document |*.pdf";

            var result = saveFile.ShowDialog();

            if (result == true)
            {
                string bookName = System.IO.Path.GetFileName(saveFile.FileName);
                string pathToImage = chbSaveImages.IsChecked == true ?  //определение дерриктории хранения картинок
                    saveFile.FileName.Replace(bookName, "") : System.IO.Path.GetTempPath();


                startDownload(bookID, pathToImage, saveFile.FileName);
            }

        }
        private void btn_OpenFirstPage(object sender, RoutedEventArgs e)
        {
            //проверка введеных данных
            if (!checkInputData())
                return;

            //подготовка url для скачивания
            Request_download_book request = PreparationURL();

            Book.Download(1, 2, request, System.IO.Path.GetTempPath(), false);
            Process.Start(Book.pages[0].Item1);
        }

        private Request_download_book PreparationURL()
        {
            Parameters.Login = tbLogin.Text;
            Parameters.Password = tbPassword.Password;
            Parameters.BookID = getBookID();
            Parameters.Cookies = getAuthCookie();

            return new Request_download_book(Parameters.Url, Parameters.Header, Parameters.Cookies);
        }
        private string getBookID()
        {
            string bookID = "";
            var regex = new Regex(@"file=\d+&");
            if (regex.IsMatch(tb_BookID.Text))
            {
                string temp = regex.Match(tb_BookID.Text).Value;
                bookID = temp.Substring(5, (temp.Length - 5 - 1));
            }
            else bookID = tb_BookID.Text;

            return bookID;
        }
        private bool checkInputData()
        {
            try
            {
                string bookID = tb_BookID.Text;
                if (bookID == "")
                {
                    throw new Exception("ID книги не указан");
                }
                else if (chbSaveImages.IsChecked == false && chbSavePdf.IsChecked == false)
                {
                    throw new Exception("Не выбран не один из режимов скачивания");
                }
                else if (tbLogin.Text == "" || tbPassword.Password == "")
                {
                    throw new Exception("Логин или пароль не корректны");
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        private void startDownload(string bookID, string pathToImage, string pathToPDF)
        {
            try
            {
                tbCurrentStatus.Text = "Подготовка";

                //подготовка url для скачивания
                Request_download_book request = PreparationURL();

                #region создание границ(номеров страниц) для скачивания
                int beginIndex = -1, endIndex = -1, maxIndex = -1;
                try
                {
                    maxIndex = request.GetCountPage(bookID);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки книги.\nПодробнее: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (chbSaveInParts.IsChecked == true)
                {
                    try
                    {
                        beginIndex = Convert.ToInt32(tbBeginWith.Text);
                        endIndex = Convert.ToInt32(tbEndOn.Text) + 1;

                        if (beginIndex > endIndex)
                            throw new Exception("Номер последней страницы не может быть больше номера первой");
                        if (beginIndex <= 0)
                            throw new Exception("Номер первой страницы не может быть меньше единицы");
                        if (endIndex > maxIndex)
                            throw new Exception($"Номер последней страницы не может быть больше количества страниц в книге({maxIndex}).");

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка чтения диапозонов.\nПодробнее {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    beginIndex = 1;
                    endIndex = maxIndex;
                }
                #endregion

                btnSave.IsEnabled = false;
                btnOpenFirstPage.IsEnabled = false;
                //bool IsCompress = chbUseCompress.IsChecked == true;
                bool IsSavePdf = chbSavePdf.IsChecked == true;
                bool IsSaveImages = chbSaveImages.IsChecked == true;
                tbCurrentStatus.Text = "Скачивание страниц";

                //отдельный поток на обновление интерфейса и создание документа
                new Thread(() =>
                {
                    Parallel.Invoke(
                        async () =>   //асинхронная задача для обновления прогресс бара
                        {
                            int countPages = endIndex - beginIndex;
                            while (countPages > Book.pages.Count)
                            {
                                Dispatcher.Invoke(() => pbDownloadProgress.Value = (int)(((double)(Book.pages.Count + 1) / countPages) * 100));
                                await Task.Delay(500);
                            }
                        },
                        async () =>   //ассинхронная задача для оценки скорости скачивания
                        {
                            int countPages = endIndex - beginIndex;
                            DateTime startDate = DateTime.Now;

                            while (countPages > Book.pages.Count)
                            {
                                await Task.Delay(1000);

                                TimeSpan ts = DateTime.Now - startDate;

                                long countByteDownloaded = Book.pages.Sum(x => x.Item2);
                                Dispatcher.Invoke(() => tbDownloadSpeed.Text = Helpers.BytesToString(Convert.ToInt64((countByteDownloaded) / ts.TotalSeconds)) + "/s");
                            }
                        },
                        () =>   //загрузка книги
                        {
                            Book.Download(beginIndex, endIndex, request, pathToImage, false);
                        });


                    if (IsSavePdf)
                    {
                        Dispatcher.Invoke(() => tbCurrentStatus.Text = "Создание документа");
                        Book.SavePDFfromImages(pathToPDF, Book.pages.Select(x => x.Item1).ToList());
                        Dispatcher.Invoke(() => tbCurrentStatus.Text = "Успешно");
                    }

                    Book.ClearImageCollection(IsSaveImages);

                    MessageBox.Show("Загрузка завершена успешно", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    Dispatcher.Invoke(() =>
                    {
                        pbDownloadProgress.Value = 0;
                        tbCurrentStatus.Text = "";
                        tbDownloadSpeed.Text = "";
                        btnSave.IsEnabled = true;
                        btnOpenFirstPage.IsEnabled = true;
                    });

                    Process.Start(System.IO.Path.GetDirectoryName(pathToPDF));

                }).Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Dispatcher.Invoke(() =>
                {
                    pbDownloadProgress.Value = 0;
                    tbCurrentStatus.Text = "";
                    tbDownloadSpeed.Text = "";
                    btnSave.IsEnabled = true;
                    btnOpenFirstPage.IsEnabled = true;
                });
                return;
            }

        }

        private string getAuthCookie() => Request_download_book.GetSID_Cookie(Parameters.Login, Parameters.Password, Parameters.BookID);

    }
}
