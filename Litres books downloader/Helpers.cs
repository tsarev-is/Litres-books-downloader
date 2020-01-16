using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Litres_books_downloader
{
    /// <summary>
    /// This class include helpers function
    /// </summary>
    public class Helpers
    {
        /// <summary>
        /// This function convert image whis every format to jpeg image
        /// </summary>
        /// <param name="imageIn"></param>
        /// <returns></returns>
        public static byte[] imageConvertToJpeg(byte[] image)
        {
            if (image == null)
                throw new Exception("image can't be null!");

            byte[] imagebuffer;

            //convert array of byte to image
            using (MemoryStream ms = new MemoryStream(image))
            using (System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms))
            {
                using (var mstmp = new MemoryStream())
                {
                    //get encoder
                    ImageCodecInfo jpegEncoder = null;
                    var codecs = ImageCodecInfo.GetImageDecoders();
                    foreach (ImageCodecInfo codec in codecs)
                    {
                        if (codec.FormatID == ImageFormat.Gif.Guid)
                        {
                            jpegEncoder = codec;
                        }
                    }

                    var encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);
                    returnImage.Save(mstmp, jpegEncoder, encoderParameters);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// This function merge image list to pdf file
        /// </summary>
        /// <param name="bookName">Result book name</param>
        /// <param name="path">Path to result pdf file</param>
        /// <param name="images">List image paths</param>
        public static void ImgsToPdf(string path, List<string> images)
        {
            //создание документа
            var document = new Document(iTextSharp.text.PageSize.A4, 25, 25, 25, 25);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                PdfWriter.GetInstance(document, stream);
                document.Open();

                foreach (var image in images)
                {
                    using (var imageStream = new FileStream(image, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        Image page = null;
                        try
                        {
                            page = iTextSharp.text.Image.GetInstance(imageStream);
                        }
                        catch(IOException ex)
                        {
                            continue;
                        }

                        //размер страницы
                        float width = page.Width;
                        float height = page.Height;

                        //настройка ориентации страницы
                        if (width < height)
                            document.SetPageSize(iTextSharp.text.PageSize.A4);
                        else
                            document.SetPageSize(iTextSharp.text.PageSize.A4.Rotate());

                        document.NewPage();

                        // Масштабируем размеры изображения под параметры страницы
                        if (width < height)
                        {
                            //вертикально
                            if (page.Height > iTextSharp.text.PageSize.A4.Height - 25)
                                page.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                            else if (page.Width > iTextSharp.text.PageSize.A4.Width - 25)
                                page.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                        }
                        else
                        {
                            //горизонтально
                            if (page.Height > iTextSharp.text.PageSize.A4.Height - 25)
                                page.ScaleToFit(iTextSharp.text.PageSize.A4.Height - 25, iTextSharp.text.PageSize.A4.Width - 25);
                            else if (page.Width > iTextSharp.text.PageSize.A4.Width - 25)
                                page.ScaleToFit(iTextSharp.text.PageSize.A4.Height - 25, iTextSharp.text.PageSize.A4.Width - 25);
                        }

                        //добавили в документ
                        page.Alignment = iTextSharp.text.Image.ALIGN_MIDDLE;
                        document.Add(page);
                    }
                }

                document.Close();
            }
        }

        /// <summary>
        /// This function convert byte count to string like 1024=1KB
        /// </summary>
        /// <param name="byteCount"></param>
        /// <returns></returns>
        public static String BytesToString(long byteCount)
        {
            string[] suf = { "Byt", "KB", "MB", "GB", "TB", "PB", "EB" }; //
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

    
    }
}
