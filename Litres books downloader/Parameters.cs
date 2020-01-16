using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litres_books_downloader
{
    /// <summary>
    /// Публичный класс для хранения параметров
    /// </summary>
    public class Parameters
    {
        private static string _url = $"https://sch.litres.ru/pages/get_pdf_page/?file=$BOOK_ID$&page=$PAGENUM$&rt=w1900&ft=gif";
        public static string Url => BookID != "" ? _url.Replace("$BOOK_ID", BookID) : throw new Exception("Невозможно получить URl без иницилизации BookID");

        public static string BookID { get; set; }

        public static string Login { get; set; }
        public static string Password { get; set; }

        public static string Header { get; set; }
        public static string Cookies { get; set; }
        public static string Parameter { get; set; }
    }
}
