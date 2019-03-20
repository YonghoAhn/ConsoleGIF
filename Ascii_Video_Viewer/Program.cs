using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using NReco.VideoConverter;

namespace Ascii_Video_Viewer
{
    class Program
    {
        private static readonly string[] asciiChars = { "#", "#", "@", "%", "=", "+", "*", ":", "-", ".", " " };
        private static string content;
        private static string path;
        private static List<byte[]> imageList;
        private static int frameCount;
        static Task<MemoryStream> firstFrameTask;
        static Task<MemoryStream> secondFrameTask;

        static async Task<MemoryStream> getFrameFromVideo(float startTime)
        {
            Stream rawBmpOutputStream = new MemoryStream();
            var ffProbe = new NReco.VideoInfo.FFProbe();
            var videoInfo = ffProbe.GetMediaInfo(path);
            var convertSettings = new ConvertSettings()
            {
                VideoFrameSize = "1280*720",  // lets resize to exact frame size
                VideoFrameRate = 24, // lets consume 24 frames per second
                MaxDuration = 1 // lets consume live stream for first 5 seconds
            };
            convertSettings.Seek = startTime;
            var videoConv = new FFMpegConverter();
            var ffMpegTask = videoConv.ConvertLiveMedia(
                        path,
                        null, // autodetect live stream format
                        rawBmpOutputStream,  // this is your special stream that will capture bitmaps
                        Format.gif,
                        convertSettings );
            ffMpegTask.Start();
            ffMpegTask.Wait();
            return (MemoryStream)rawBmpOutputStream;
        }

        static void Main(string[] args)
        {
            
            GifImage a;

            Console.CursorSize = 8;
            path = Console.ReadLine();
            imageList = new List<byte[]> { };
            frameCount = 0;
            //check if file is gif or video
            string ext = Path.GetExtension(path);
            if(ext.Equals(".gif"))
            {
                try
                {
                    a = new GifImage(path)
                    {
                        ReverseAtEnd = false
                    };
                    Timer timer = new Timer((object o) => TimerTick(o, ref a), null, 0, 60);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            //gif
            else
            {
                Stream rawBmpOutputStream = new MemoryStream();
                var ffProbe = new NReco.VideoInfo.FFProbe();
                var videoInfo = ffProbe.GetMediaInfo(path);

                var videoConv = new FFMpegConverter();
                var ffMpegTask = videoConv.ConvertLiveMedia(
                            path,
                            null, // autodetect live stream format
                            rawBmpOutputStream,  // this is your special stream that will capture bitmaps
                            Format.gif,
                            new ConvertSettings()
                            {
                                VideoFrameSize = "1280*720",  // lets resize to exact frame size
                                VideoFrameRate = 24, // lets consume 24 frames per second
                                MaxDuration = 10 // lets consume live stream for first 5 seconds
                        });
                ffMpegTask.Start();
                ffMpegTask.Wait();
                //var sr = new BinaryReader(rawBmpOutputStream);
                //sr.BaseStream.Position = 0;

                //using (var fileStream = File.Create(@"D:\test.gif"))
                //{
                //    rawBmpOutputStream.Seek(0, SeekOrigin.Begin);
                //    rawBmpOutputStream.CopyTo(fileStream);
                //}
                
                try
                {
                    a = new GifImage(rawBmpOutputStream)
                    {
                        ReverseAtEnd = false
                    };
                    Timer timer = new Timer((object o) => TimerTick(o, ref a), null, 0, 60);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
            //video
            Console.WindowWidth = Console.LargestWindowWidth;
            Console.WindowHeight = Console.LargestWindowHeight;
            // var t = new Timer((object o) => TimerTick(o, ref a), null, 0, 100);

            Console.ReadKey();
        }
        private static void TimerTick(Object o, ref GifImage a)
        {
            if(frameCount%2 == 0)
            {
                //0, 2, 4, 6, ...
                if(secondFrameTask.IsCompleted)
                {
                    var stream = secondFrameTask.Result;
                    try
                    {
                        a = new GifImage(stream)
                        {
                            ReverseAtEnd = false
                        };
                        Timer timer = new Timer((object obj) => FrameTimerTick(obj, ref a), null, 0, 60);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else
            {
                //1, 3, 5, 7, ...
            }

            GenerateAscii(a.GetNextFrame());
            //timeCount++;
            // if (imageList.Count <= timeCount)
            //    timeCount = 1;

            //File.WriteAllBytes(@"D:\Test\" + timeCount.ToString() + ".bmp", imageList[timeCount]);
            //var ms = new MemoryStream(imageList[timeCount]);
            //Image i = Image.FromStream(ms);
            GC.Collect();
        }

        private static void FrameTimerTick(object o, ref GifImage a)
        {

        }


        private static string ConvertToAscii(Bitmap image)
        {
            bool toggle = false;
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
                        sb.Append(asciiChars[index]);
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
            image = GetReSizedImage(image, 200);
            content = ConvertToAscii(image);
            Console.Write(content);
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
