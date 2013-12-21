using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeImageAPI;
using FreeImageAPI.IO;
using FreeImageAPI.Metadata;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace SteamImageConverterConsole
{
    public class Program
    {
        private static string currentDir;
        private static string userSteamID;
        private static string steamGameID;
        private static string steamPath;
        private static string[] supportedImageTypes;
        private static string steamScreenshotDir;

        private static string steamReg = @"HKEY_CURRENT_USER\Software\Valve\Steam\";
        private static string steamRegKey = @"SteamPath";

        public static void Main(string[] args)
        {
            currentDir = Environment.CurrentDirectory;
            supportedImageTypes = new string[] { ".png" };
            steamPath = Registry.GetValue(steamReg, steamRegKey, null).ToString();

            if (args.Length >= 1)
                userSteamID = args[0];
            else
                userSteamID = "10658906";
            if (args.Length >= 2)
                steamGameID = args[1];
            else
                steamGameID = "39140";

            if (userSteamID != null && steamGameID != null)
            {
                steamScreenshotDir = @"\userdata\" + userSteamID + @"\760\remote\" + steamGameID + @"\screenshots\";

                string[] images = GetImagesFromDirectory();

                int count = 1;
                foreach (var image in images)
                {
                    Console.WriteLine("Writing image: {0}.  Completed: {1}%", count, (count * 100.0) / images.Length);
                    ProcessImage(image);
                    count++;
                }

                RestartSteam();
            }
        }

        protected static string[] GetImagesFromDirectory()
        {
            List<string> foundImages = new List<string>();

            for (int i = 0; i < supportedImageTypes.Length; i++)
            {
                foundImages = System.IO.Directory.GetFiles(currentDir, "*" + supportedImageTypes[i]).ToList<string>();
            }

            return foundImages.ToArray<string>();
        }

        protected static void ProcessImage(string imagePath)
        {
            FIBITMAP image = new FIBITMAP();
            FIBITMAP imageToResize = new FIBITMAP();
            string imageName = string.Empty;

            try
            {
                image = FreeImage.Load(FreeImage.GetFileType(imagePath, 0), imagePath, FREE_IMAGE_LOAD_FLAGS.PNG_IGNOREGAMMA);
                imageToResize = FreeImage.Load(FreeImage.GetFileType(imagePath, 0), imagePath, FREE_IMAGE_LOAD_FLAGS.PNG_IGNOREGAMMA);
            }
            catch (Exception)
            { }

            imageName = imagePath.Substring(imagePath.LastIndexOf(@"\") + 1, imagePath.LastIndexOf(@".") - (imagePath.LastIndexOf(@"\") + 1));

            if (imageToResize != null)
            {
                ResizeImage(ref imageToResize);
                SaveImage(imageToResize, true, imageName);
            }
            if (image != null)
            {
                SaveImage(image, false, imageName);
            }
        }

        protected static void ResizeImage(ref FIBITMAP image)
        {
            uint dimensionX = FreeImage.GetHeight(image);
            uint dimensionY = FreeImage.GetWidth(image);
            int scaleFactor = 4;

            FIBITMAP rescaledImage = FreeImage.Rescale(image, (int)dimensionY / scaleFactor, (int)dimensionX / scaleFactor, FREE_IMAGE_FILTER.FILTER_BILINEAR);

            image = rescaledImage;
        }

        protected static void SaveImage(FIBITMAP image, bool isThumb, string fileName)
        {
            string outputPath = steamPath + steamScreenshotDir;

            if (CheckAndMakeDir(outputPath))
            {
                if (isThumb)
                {
                    outputPath = outputPath + @"thumbnails\";
                }
                MemoryStream imageTemp = new MemoryStream();
                try
                {
                    //requires some jumping through hoops to guarantee the output image is not corrupt
                    //for some reason the input PNG files would not convert cleanly with just a call to FreeImage.Save
                    //so save them to a MemoryStream first, reload from MemoryStream then save to disk
                    FreeImage.SaveToStream(image, imageTemp, FREE_IMAGE_FORMAT.FIF_PNG);
                    FIBITMAP temp = FreeImage.LoadFromStream(imageTemp);
                    FreeImage.Save(FREE_IMAGE_FORMAT.FIF_JPEG, temp, outputPath + fileName + ".jpg", FREE_IMAGE_SAVE_FLAGS.JPEG_BASELINE);
                }
                catch (Exception)
                { }
            }
        }

        protected static bool CheckAndMakeDir(string path)
        {
            bool parentExists = Directory.Exists(path);
            bool thumbExists = Directory.Exists(path + @"thumbnails\");

            if (!parentExists)
            {
                Directory.CreateDirectory(path);
                parentExists = true;
            }
            if (!thumbExists)
            {
                Directory.CreateDirectory(path + @"thumbnails\");
                thumbExists = true;
            }

            return (parentExists && thumbExists);

        }

        private static void RestartSteam()
        {
            Process[] processes = Process.GetProcessesByName("Steam");
            Process process = null;

            if (processes.Length != 0)
            {
                process = processes[0];
                string processPath = process.MainModule.FileName;

                process.Kill();
                Process.Start(processPath);
            }
            
        }
    }

}
