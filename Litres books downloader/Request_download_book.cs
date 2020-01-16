using System;
using RestSharp;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litres_books_downloader
{
    class Request_download_book
    {
        private RestClient restClient;
        private RestRequest request;
        private string url;

        private enum RequestObjectType{ 
            Header,
            Cookie
        }

        public Request_download_book(string url_, string Header_, string Cookies_)
        {
            //Исключения
            if (url_ == "")
                throw new Exception("Url can't be empty!");
            else if (!url_.Contains("$PAGENUM$"))
                throw new Exception("Url must contains \'$PAGENUM$\' parameter!");
            else if (Header_ == "")
                throw new Exception("Request header can't be empty!");


            //Создание запроса
            this.url = url_;

            restClient = new RestClient();
            request = new RestRequest(url, Method.GET);

            this.addRequestObject(Header_,RequestObjectType.Header);
            this.addRequestObject(Cookies_,RequestObjectType.Cookie);
        }

        /// <summary>
        /// Функция для добавления обьектов к запросу
        /// </summary>
        /// <param name="requestObj">Строка с параметрами например var=1;test=2;</param>
        /// <param name="type">Тип запроса</param>
        private void addRequestObject(string requestObj, RequestObjectType type)
        {
            foreach (string str in requestObj.Split(';'))
            {
                if (str.Contains("="))
                {
                    var v = str.Replace(" ", "").Split('=');

                    if(type == RequestObjectType.Cookie)
                        request.AddCookie(v[0], v[1]);
                    else if(type == RequestObjectType.Header)
                        request.AddHeader(v[0], v[1]);
                }
            }
        }


        /// <summary>
        /// Запрос на получение количества страниц в документе
        /// </summary>
        /// <param name="book_ID">Индификатор книги</param>
        /// <returns>Количество страниц</returns>
        public int GetCountPage(string book_ID)
        {
            request.Resource = $"https://sch.litres.ru/pages/get_pdf_js/?file=" + book_ID;

            IRestResponse result = restClient.Execute(request);
            if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new Exception($"Книга с ID={book_ID} не найдена");
            
            return result.Content.Substring(result.Content.IndexOf("p:[")).Split('{').Length-2;
        }
        /// <summary>
        /// Запрос на загрузку страницы
        /// </summary>
        /// <param name="pageNum">Номер страницы</param>
        /// <returns>Массив байт содержащий страницу</returns>
        public byte[] Download(int pageNum)
        {
            request.Resource = url.Replace("$PAGENUM$", pageNum.ToString());
            //request.Co

            byte[] fileBytes = restClient.DownloadData(request);
            if (fileBytes.Length == 0)
                throw new Exception("Server returned NULL bytes");

            return fileBytes;
        }

        public static string GetSID_Cookie(string login, string password, string bookID)
        {
            RestClient restClient = new RestClient();
            RestRequest request = new RestRequest();

            request.Resource = $"https://sch.litres.ru/pages/ajax_empty2/";

            request.AddQueryParameter("pre_action", "login");
            request.AddQueryParameter("ref_url", $"get_pdf_page/?file={bookID}&page=1&rt=w1900&ft=gif");
            request.AddQueryParameter("login", login);
            request.AddQueryParameter("pwd", password);
            request.AddQueryParameter("showpwd", "on");
            request.AddQueryParameter("utc_offset_min", "-480");
            request.AddQueryParameter("timestamp", "1578814237619");
            request.AddQueryParameter("csrf", "804144:1578861037:174629741e05485e1d7d4b1b52b26cecad44140cda46ab8d72ba9892cf87e303");
            request.AddQueryParameter("gu_ajax", "true");

            IRestResponse result = restClient.Execute(request);

            var cookie = "";
            if (result.StatusCode != System.Net.HttpStatusCode.OK ||
                            cookie == null ||
                            result.Cookies.Count() == 1)
            {
                throw new Exception("Не верный логин или пароль");
            }
            else
            {
                foreach (var c in result.Cookies)
                    cookie += $"{c.Name}={c.Value}; ";
            }

            return cookie;
        }
    }
}
