using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode;
using System.Drawing;
using System.Drawing.Imaging;

namespace YoutubeDownloader2
{
    class VideoList
    {
        public List<AudioOnlyStreamInfo> aList = new List<AudioOnlyStreamInfo>();
        public List<VideoOnlyStreamInfo> vList = new List<VideoOnlyStreamInfo>();
        public List<MuxedStreamInfo> avList = new List<MuxedStreamInfo>();
        public string Video_Author;
        public string Video_Title;
        public string FFAPATH;
        public string FFVPATH;
        public string FFEXT;

        public VideoList(List<AudioOnlyStreamInfo> _aList = null, List<VideoOnlyStreamInfo> _vList = null, List<MuxedStreamInfo> _avList = null)
        {
            aList = _aList;
            vList = _vList;
            avList = _avList;
        }
    }

    class Program
    {
        // yeni : sorun çözüldü indirilen videolar ".mp4" olarak ineceği için ffmpeg hatası kalmadı...
        public static string VIDEOPATH = AppDomain.CurrentDomain.BaseDirectory + @"\Output";
        public static string CACHEPATH = AppDomain.CurrentDomain.BaseDirectory + @"\Cache";
        public static string IMAGEPATH = AppDomain.CurrentDomain.BaseDirectory + @"\Images";
        public static VideoList videoDatas = new VideoList();
        public static char qte = (char)34;
        public static string[] blacklist = {@"\", "/", "[", "]", "{", "}", "|", ">", "<", ":", "*", "=", "-", "-","?","*",qte.ToString()}; 

        async static Task PrintVideoInfo(string link, int mode)
        {
            YoutubeClient youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(link);    
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(link);  

            string pr;
            int sel_index = 0;

            string video_info_title = string.Format("{0} {1, -15} {2, -15} {3, -15} {4, -15}", "* -", "VideoCodec'i", "VideoKalitesi", "VideoCozunurlugu", "Boyutu");
            string audio_info_title = string.Format("{0} {1, -15} {2, -15} {3, -15} {4, -15}", "* -", "AudioCodec", "Bitrate", "Container", "Size");
            string video_infos = $"\nVideo Adi:        {video.Title}\nVideo Uzunlugu:   {video.Duration}\nVideo Yapimcisi:  {video.Author}\nVideo Tarihi:     {video.UploadDate}";
            Console.WriteLine(video_infos+"\n");

            videoDatas.Video_Author = video.Author.ToString();
            string titlefixed = video.Title;
            foreach(string b in blacklist)
            {
                titlefixed = titlefixed.Replace(b, " ");
            }
            videoDatas.Video_Title = titlefixed;


            switch (mode)
            {
                case 0: // ses dahil yayınlar
                    Console.WriteLine(video_info_title+"\n");
                    var av = streamManifest.GetMuxedStreams();
                    List<MuxedStreamInfo> buffer = new List<MuxedStreamInfo>();
                    foreach (var vid in av)
                    {
                        pr = string.Format("{0, -15} {1, -15} {2, -15} {3, -15}", vid.VideoCodec, vid.VideoQuality, vid.VideoResolution, vid.Size);
                        Console.WriteLine($"{sel_index} - {pr}");
                        sel_index++;
                        buffer.Add(vid);
                    }
                    videoDatas.avList = buffer;
                    break;
                case 1: // ayrıklar
                    Console.WriteLine(audio_info_title + "\n");
                    var a = streamManifest.GetAudioOnlyStreams();
                    var v = streamManifest.GetVideoOnlyStreams();
                    List<AudioOnlyStreamInfo> bufferA = new List<AudioOnlyStreamInfo>();
                    List<VideoOnlyStreamInfo> bufferB = new List<VideoOnlyStreamInfo>();
                    foreach(var audio in a)
                    {
                        pr = string.Format("{0, -15} {1, -15} {2, -15} {3, -15}", audio.AudioCodec, audio.Bitrate, audio.Container, audio.Size);
                        Console.WriteLine($"{sel_index} - {pr}");
                        sel_index++;
                        bufferA.Add(audio);
                    }
                    sel_index = 0;
                    Console.WriteLine("\n" +video_info_title + "\n");
                    foreach(var _video in v)
                    {
                        pr = string.Format("{0, -15} {1, -15} {2, -15} {3, -15}", _video.VideoCodec, _video.VideoQuality, _video.VideoResolution, _video.Size);
                        Console.WriteLine($"{sel_index} - {pr}");
                        sel_index++;
                        bufferB.Add(_video);
                    }
                    videoDatas.aList = bufferA;
                    videoDatas.vList = bufferB;
                    break;
            }
        }

        public static async Task DownloadVideo(int Index)
        {
            YoutubeClient youtube = new YoutubeClient();
            string pathToVideo = VIDEOPATH + "\\" + videoDatas.Video_Title + "." + videoDatas.avList[Index].Container;
            if (File.Exists(pathToVideo))
            {
                Console.WriteLine("Bu video ZATEN var, enter'e basiniz");
                Console.ReadLine();
                Environment.Exit(0);
            }
            await youtube.Videos.Streams.DownloadAsync(videoDatas.avList[Index], pathToVideo);
        }

        public static async Task DownloadVideoAndAuido(int Index1A, int Index2V)
        {
            YoutubeClient youtube = new YoutubeClient();
            string pathToAudio = CACHEPATH + "\\" +"A"+videoDatas.Video_Title + "." + videoDatas.aList[Index1A].Container;
            string pathToVideo = CACHEPATH + "\\" +"V"+videoDatas.Video_Title + "." + videoDatas.vList[Index2V].Container;
            if (File.Exists(pathToAudio) || File.Exists(pathToAudio))
            {
                Console.WriteLine("Bu video ZATEN var, enter'e basiniz");
                Console.ReadLine();
                Environment.Exit(0);
            }
            videoDatas.FFAPATH = pathToAudio;
            videoDatas.FFVPATH = pathToVideo;
            videoDatas.FFEXT = videoDatas.vList[Index2V].Container.ToString();
            await youtube.Videos.Streams.DownloadAsync(videoDatas.aList[Index1A], pathToAudio);
            await youtube.Videos.Streams.DownloadAsync(videoDatas.vList[Index2V], pathToVideo);
        }

        public static void ffmpegWork()
        {
            string finalLocation = VIDEOPATH + "\\" + "F"+videoDatas.Video_Title +".mp4"; //+ videoDatas.FFEXT;
            if (File.Exists(finalLocation))
            {
                Console.WriteLine("Bu video ZATEN var, enter'e basiniz");
                Console.ReadLine();
                Environment.Exit(0);
            }
            var prcs = System.Diagnostics.Process.Start("ffmpeg.exe", $" -i {qte}{videoDatas.FFVPATH}{qte} -i {qte}{videoDatas.FFAPATH}{qte} -c:v copy -c:a aac {qte}{finalLocation}{qte}");
            prcs.WaitForExit();
        }

        public static void WithAudio(string link)
        {
            Console.WriteLine("---------------");
            var prcs = PrintVideoInfo(link, 0);
            prcs.Wait();
            Console.WriteLine($"\nSecim Yapiniz 0 - {videoDatas.avList.Count - 1}");
            string selection = Console.ReadLine();
            var downprcs = DownloadVideo(int.Parse(selection));
            downprcs.Wait();
            // bitti
            Console.WriteLine($"Video Kaydedildi\nherhangi bir tusa basin...");
            Console.ReadLine();
        }

        public static void WithoutAudio(string link)
        {
            Console.WriteLine("---------------");
            var prcs = PrintVideoInfo(link, 1);
            prcs.Wait();
            Console.WriteLine($"\nSes Secimi Yapiniz 0 - {videoDatas.aList.Count - 1}");
            string IndexA = Console.ReadLine();
            Console.WriteLine($"\nVideo Secimi Yapiniz 0 - {videoDatas.vList.Count - 1}");
            string IndexV = Console.ReadLine();
            var prcsother = DownloadVideoAndAuido(int.Parse(IndexA), int.Parse(IndexV));
            Console.WriteLine("Lutfen bekleyin...");
            prcsother.Wait();
            Console.WriteLine("ffmpeg baslatildi...");
            ffmpegWork();
            int c = GetCacheFileCount();
            Console.Write($"Bitti\nCache'de {c} video var. silinsin mi ? [y/n]");
            string sel = Console.ReadLine();
            if (sel == "y")
            {
                DeleteCache();
            }
            else
            {
                Console.WriteLine("iptal edildi...");
            }
            Console.WriteLine("cikis icin enter'e basiniz");
            Console.ReadLine();
        }

        public static void Abort()
        {
            Console.WriteLine("1 ila 3 arasinda secim yapabilirsiniz,\nenter'e basin");
            Console.ReadLine();
            Environment.Exit(0);
        }

        public static int GetCacheFileCount()
        {
            int index = 0;
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(CACHEPATH);
            foreach (System.IO.FileInfo file in di.GetFiles())
            {
                index++;
            }
            return index;
        }

        public static void DeleteCache()
        {
            DirectoryInfo di = new DirectoryInfo(CACHEPATH);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            Console.WriteLine("Cache silindi");
        }

        async static Task<string[]> GetVideoID(string link)
        {
            YoutubeClient youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(link);
            string titlefixed = video.Title;
            foreach (string b in blacklist)
            {
                titlefixed = titlefixed.Replace(b, " ");
            }
            string[] x = { video.Id, titlefixed };
            return x;
        }


        public static void DownloadVideoThumbnail(string link)
        {
            string header = "https://i1.ytimg.com/vi/";
            string[] ends = { "/default.jpg" , "/mqdefault.jpg" , "/hqdefault.jpg", "/sddefault.jpg", "/maxresdefault.jpg" };
            var x = GetVideoID(link);
            x.Wait();
            Console.WriteLine($"Video id degeri = {x.Result[0]}\nVideo Adi = {x.Result[1]}\nFotograf boyutu secin:\n");
            Console.WriteLine("0: default\n1:medium\n2:high\n3:standard\n4:max res");
            string select = Console.ReadLine();
            if (int.TryParse(select, out int result))
            {
                if (result <= 4)
                {
                    using (WebClient webClient = new WebClient())
                    {
                        byte[] data = webClient.DownloadData(header + x.Result[0] + ends[result]);
                        using (MemoryStream mem = new MemoryStream(data))
                        {
                            using (var yourImage = Image.FromStream(mem))
                            {
                                if (File.Exists(IMAGEPATH + "\\" + x.Result[1] + ".jpeg"))
                                {
                                    Console.WriteLine("Bu fotograf ZATEN var, enter'e basiniz");
                                    Console.ReadLine();
                                    Environment.Exit(0);
                                }
                                yourImage.Save(IMAGEPATH + "\\" + x.Result[1] + ".jpeg", ImageFormat.Jpeg);
                                Console.WriteLine("Fotograf indirildi, enter'e basiniz.");
                                Console.ReadLine();
                                Environment.Exit(0);
                            }
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Hatali secim, enter'e basin");
                    Environment.Exit(0);
                }
            }
            else
            {
                Console.WriteLine("Hatali secim, enter'e basin");
                Environment.Exit(0);
            }
        }

        static void Main(params string[] args)
        {
            Console.Clear();
            if (args.Length == 0)   // argümansız
            {
                Console.WriteLine("Youtube Video Indirme Aracina Hos Geldiniz!\n" +
                    "Surum: 1.1\n\nIndirme modunu seciniz;\n1: Birlesik Video  [720P en fazla ]\n" +
                    "2: Ayrilmis Video  [1080P en fazla]\n3: iptal"+
                    "\n4: Video Fotografini indir");
                string selection = Console.ReadLine();
                Console.WriteLine("Video linki: ");
                string link = Console.ReadLine();
                switch (selection)
                {
                    case "1":
                        WithAudio(link);
                        break;
                    case "2":
                        WithoutAudio(link);
                        break;
                    case "3":
                        Environment.Exit(0);
                        break;
                    case "4":
                        DownloadVideoThumbnail(link);
                        break;
                    default:
                        Abort();
                        break;
                }
            }
            else   // argüman ile
            {

            }
        }
    }
}
