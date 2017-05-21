using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

namespace Ascii_Video_Viewer
{
    class Program
    {
        private static string[] _AsciiChars = { "#", "#", "@", "%", "=", "+", "*", ":", "-", ".", " " };
        private static string _Content;
        private static string Path;
        private static GifImage a;
        static void Main(string[] args)
        {

            Console.CursorSize = 8;
            Path = Console.ReadLine();
            Console.WindowWidth = Console.LargestWindowWidth;
            Console.WindowHeight = Console.LargestWindowHeight;
            try
            {
                a = new GifImage(Path)
                {
                    ReverseAtEnd = false
                };
                var t = new Timer(TimerTick, null, 0, 60);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }
        private static void TimerTick(Object o)
        {
            GenerateAscii(a.GetNextFrame());
            GC.Collect();
        }
        private static string ConvertToAscii(Bitmap image)
        {
            Boolean toggle = false;
            StringBuilder sb = new StringBuilder();

            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    Color pixelColor = image.GetPixel(w, h);
                    //Average out the RGB components to find the Gray Color
                    int red = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                    int green = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                    int blue = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                    Color grayColor = Color.FromArgb(red, green, blue);

                    //Use the toggle flag to minimize height-wise stretch
                    if (!toggle)
                    {
                        int index = (grayColor.R * 10) / 255;
                        sb.Append(_AsciiChars[index]);
                    }
                }
                if (!toggle)
                {
                    sb.Append("\r\n");
                    toggle = true;
                }
                else
                {
                    toggle = false;
                }
            }

            return sb.ToString();
        }

        private static void GenerateAscii(Image i)
        {
            //Console.Clear();
            Console.SetCursorPosition(0, 0);
            Bitmap image = new Bitmap(i);
            i.Dispose();
            image = GetReSizedImage(image, 180);
            _Content = ConvertToAscii(image);
            Console.Write(_Content);
            image.Dispose();
        }

        private static Bitmap GetReSizedImage(Bitmap inputBitmap, int asciiWidth)
        {
            int asciiHeight = 0;
            //폭을 줄였으니 높이도 계산한다.
            asciiHeight = (int)Math.Ceiling((double)inputBitmap.Height * asciiWidth / inputBitmap.Width);

            //폭*높이에 맞는 새 Bitmap 생성
            Bitmap result = new Bitmap(asciiWidth, asciiHeight);
            Graphics g = Graphics.FromImage((Image)result);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(inputBitmap, 0, 0, asciiWidth, asciiHeight);
            g.Dispose();
            return result;
        }
    }
}
