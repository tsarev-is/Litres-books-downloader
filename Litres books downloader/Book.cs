using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Litres_books_downloader
{
    class Book
    {
        //список ссылок и размеов файла
        public static List<(string, long)> pages = new List<(string, long)>();

        /// <summary>
        /// Очистить коллекцию изображений
        /// </summary>
        /// <param name="flagDeleteFiles">Флаг нужно ли удалять файлы</param>
        public static void ClearImageCollection(bool flagDeleteFiles)
        {
            if (flagDeleteFiles)
            {
                foreach (var pth in Book.pages.Select(x => x.Item1).ToList())
                    File.Delete(pth);
            }

            pages.Clear();
        }
        /// <summary>
        /// Загрузить книгу
        /// </summary>
        /// <param name="beginIndex">номер первой страницы</param>
        /// <param name="endIndex">номер последней страницы</param>
        /// <param name="request"></param>
        /// <param name="pathToImage"></param>
        /// <param name="isCompress"></param>
        public static void Download(int beginIndex, int endIndex, Request_download_book request, string pathToImage, bool isCompress)
        {
            for (int i = beginIndex; i < endIndex; i++)
            {
                if (i + 3 <= endIndex)  //загружаем по две страницы, пока в очереди не останется одна
                    Parallel.Invoke(
                        () => downloadPage(request, i++, pathToImage, isCompress),
                        () => downloadPage(request, i++, pathToImage, isCompress),
                        () => downloadPage(request, i, pathToImage, isCompress));
                else downloadPage(request, i, pathToImage, isCompress);
            }
        }
        /// <summary>
        /// Обьединть все изображения в один файл
        /// </summary>
        /// <param name="pathToPDF"></param>
        /// <param name="list"></param>
        public static void SavePDFfromImages(string pathToPDF, List<string> list) => Helpers.ImgsToPdf(pathToPDF, Book.pages.Select(x => x.Item1).ToList());


        private static void downloadPage(Request_download_book request, int pageNum, string path, bool isCompressed)
        {
            //Запрос на загрузку
            var fileBytes = request.Download(pageNum);
            if (fileBytes.Length == 0)
                throw new Exception("Server returned NULL bytes");

            string fileName = "";
            if (isCompressed)//Сжатие
            {
                fileBytes = Helpers.imageConvertToJpeg(fileBytes);
                fileName = $"page_{pageNum}.jpeg";
            }
            else fileName = $"page_{pageNum}.gif";

            //запись в файл
            File.WriteAllBytes(System.IO.Path.Combine(path, fileName), fileBytes);
            pages.Add((path + fileName, fileBytes.Length));
        }

    }
}


