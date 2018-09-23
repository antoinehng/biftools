using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace BifCreator
{
    class Worker
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("\nBifCreator.exe InputFile [TimeInterval] [Width] [Height] [AspectRatio] [QuantificationFactor]\n");
                Console.WriteLine(" => TimeInterval in seconds");
                Console.WriteLine("    defautl = 10s\n");
                Console.WriteLine(" => AspectRatio = '43' or '169'");
                Console.WriteLine("    defautl =  16:9 (240x135)\n");
                Console.WriteLine(" => QuantificationFactor starting at 0 (best quality) and up for lower quality images");
                Console.WriteLine("    defautl =  5\n");

                return 0;
            }

            String inputFilePath = args[0];
            
            String timeInterval = "10";
            if (args.Length > 1)
                timeInterval = args[1];

            String width = "240";
            if (args.Length > 2)
                width = args[2];

            String height = "135";
            if (args.Length > 3)
                width = args[3];

            String aspectRatio = "169";
            if (args.Length > 4)
                aspectRatio = args[4];

            String quantificationFactor = "5";
            if (args.Length > 5)
                quantificationFactor = args[5];

            ImageTargetConfiguration configuration = new ImageTargetConfiguration();
            configuration.ImageFormat = (ImageTargetConfiguration.Formats)Enum.Parse(typeof(ImageTargetConfiguration.Formats), "jpg", true);
            configuration.Width = Int32.Parse(width);
            configuration.Height = Int32.Parse(height);
            configuration.AspectRatio = Int32.Parse(aspectRatio);
            configuration.TimeInterval = Int32.Parse(timeInterval);
            configuration.QuantificationFactor = Int32.Parse(quantificationFactor);

            String outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath), Path.GetFileNameWithoutExtension(inputFilePath)+".bif");
            
            GenerateThumbnails(inputFilePath, outputFilePath, configuration);
            return 0;
        }

        static void GenerateThumbnails(string inputFilePath, string outputFilePath, ImageTargetConfiguration configuration)
        {
            // Get output directory
            String outputDirectoryPath = Path.GetDirectoryName(outputFilePath);

            //Prepare the ffmpeg process to run
            int exitCode;
            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"dependencies\ffmpeg.exe");
            string arguments = "-ss 0 -i \"" + inputFilePath + "\"  -filter:v \"fps=1/" + configuration.TimeInterval.ToString() + ", scale=" + configuration.Width.ToString() + ":" + configuration.Height.ToString() + "\" -q:v " + configuration.QuantificationFactor.ToString() + " -hide_banner -copyinkf -y \"" + outputDirectoryPath + "\\" + Path.GetFileNameWithoutExtension(outputFilePath) + "_%08d." + configuration.ImageFormat.ToString().ToLower() + "\"";
            string stdOut, stdErr;

            Console.WriteLine(String.Format(" => Génération des imagettes pour le  fichier : {0}", inputFilePath));
            Console.WriteLine(String.Format("\n => TimeInterval : {0}", configuration.TimeInterval.ToString()));
            Console.WriteLine(String.Format(" => Width : {0}", configuration.Width.ToString()));
            Console.WriteLine(String.Format(" => Height : {0}", configuration.Height.ToString()));
            Console.WriteLine(String.Format(" => AspectRatio : {0}", configuration.AspectRatio.ToString()));
            Console.WriteLine(String.Format(" => QuantificationFactor : {0}", configuration.QuantificationFactor.ToString()));
            Console.WriteLine(String.Format("\n => Command line : {0} {1}", ffmpegPath, arguments));

            //Run external process & wait for it to finish
            exitCode = ProcessHelper.ExecuteProcess(ffmpegPath, arguments, 3600 * 1000, out stdOut, out stdErr);
            if (exitCode != 0)
            {
                throw new Exception(string.Format("Error {0} : Ffmpeg returned non-zero value while generating thumbnails", exitCode));
            }

            // Get the number of images
            string[] ImageFiles = Directory.GetFiles(outputDirectoryPath, "*." + configuration.ImageFormat.ToString().ToLower());
            List<string> ImageList = ImageFiles.ToList();
            ImageList.Sort();
            ImageFiles = ImageList.ToArray();
            Console.WriteLine(String.Format(" => {0} imagettes générées pour le  fichier : {1}", ImageFiles.Length.ToString(), inputFilePath));


            // set static BIF file informations
            byte[] MagicNumber = new byte[8] { 0x89, 0x42, 0x49, 0x46, 0x0d, 0x0a, 0x1a, 0x0a };
            byte[] BifVersion = new byte[4] { 0x00, 0x00, 0x00, 0x00 };


            // set Image Format
            byte[] ImageFormat;
            Dictionary<String, byte[]> FileFormat = new Dictionary<string, byte[]>();
            FileFormat.Add("jpg", new byte[4] { 0x4a, 0x50, 0x45, 0x47 });
            FileFormat.Add("jpeg", new byte[4] { 0x4a, 0x50, 0x45, 0x47 });
            FileFormat.Add("png", new byte[4] { 0x00, 0x50, 0x4e, 0x47 });
            FileFormat.Add("bmp", new byte[4] { 0x00, 0x42, 0x4d, 0x50 });
            FileFormat.TryGetValue(configuration.ImageFormat.ToString().ToLower(), out ImageFormat);


            // set Resolution
            int w = configuration.Width;
            byte[] Width = BitConverter.GetBytes(w);
            var Buffer = Enumerable.Repeat((byte)0x00, 2).ToArray(); // Limiting to 2 bytes
            for (int i = 0; i < Buffer.Length; i++)
            {
                if (i + 1 < Width.Length)
                    Buffer[i] = Width[i];
            }
            Width = Buffer;

            int h = configuration.Height;
            byte[] Height = BitConverter.GetBytes(h);
            Buffer = Enumerable.Repeat((byte)0x00, 2).ToArray(); // Limiting to 2 bytes
            for (int i = 0; i < Buffer.Length; i++)
            {
                if (i + 1 < Height.Length)
                    Buffer[i] = Height[i];
            }
            Height = Buffer;

            byte[] Resolution = new byte[4] { Width[0], Width[1], Height[0], Height[1] };


            // set Aspect Ratio
            byte[] AspectRatio;
            if (configuration.AspectRatio == 169)
            {
                AspectRatio = new byte[2] { 0x10, 0x09 };
            }
            else if (configuration.AspectRatio == 43)
            {
                AspectRatio = new byte[2] { 0x04, 0x09 };
            }
            else
            {
                AspectRatio = new byte[2] { 0x10, 0x09 };
            }


            // set Time Interval
            byte[] TimeInterval = BitConverter.GetBytes(configuration.TimeInterval * 1000); // Secondes to milliseconds


            // Set Vod/Live Tag
            byte[] VodTag = new byte[1] { 0x01 };
            byte[] LiveTag = new byte[1] { 0x02 };


            Console.WriteLine(String.Format(" => Création du fichier {0}", outputFilePath));
            using (var _FileStream = new System.IO.FileStream(outputFilePath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                // Binary File Format Indicator
                _FileStream.Write(MagicNumber, 0, MagicNumber.Length);

                // Version of the BIF File
                _FileStream.Write(BifVersion, 0, BifVersion.Length);

                // Write number of images
                byte[] NumberImages = BitConverter.GetBytes(ImageFiles.Length);
                Buffer = Enumerable.Repeat((byte)0x00, 4).ToArray(); // Limiting to 4 bytes
                for (int i = 0; i < Buffer.Length; i++)
                {
                    if (i + 1 < NumberImages.Length)
                        Buffer[i] = NumberImages[i];
                }
                _FileStream.Write(Buffer, 0, Buffer.Length);

                // Interval between images in milliseconds
                _FileStream.Write(TimeInterval, 0, TimeInterval.Length);

                // Image format indicator (FOURCC)
                _FileStream.Write(ImageFormat, 0, ImageFormat.Length);

                // Resolution
                _FileStream.Write(Resolution, 0, Resolution.Length);

                // AspectRatio
                _FileStream.Write(AspectRatio, 0, AspectRatio.Length);

                // Insert VOD Tag
                _FileStream.Write(VodTag, 0, VodTag.Length);

                // Bytes to spare
                int NbBlankBytes = 33;
                Buffer = Enumerable.Repeat((byte)0x00, NbBlankBytes).ToArray();
                _FileStream.Write(Buffer, 0, Buffer.Length);

                // set variables before looping
                byte[] Index;
                long offset;
                byte[] byteOffset;
                long previousOffset = 0;
                FileInfo previousFile;

                // Loop to create index image table
                for (var i = 0; i < ImageFiles.Length; i++)
                {
                    // Image index
                    Index = BitConverter.GetBytes(i);
                    _FileStream.Write(Index, 0, 4);

                    // Byte offset for image data
                    if (i == 0)
                    {
                        offset = previousOffset = 64 + (ImageFiles.Length + 1) * 8;
                    }
                    else
                    {
                        previousFile = new FileInfo(ImageFiles[i - 1]);
                        offset = previousOffset = previousOffset + previousFile.Length;
                    }
                    byteOffset = BitConverter.GetBytes(offset);
                    _FileStream.Write(byteOffset, 0, 4);
                }

                // Write end of index image table
                Buffer = Enumerable.Repeat((byte)0xff, 4).ToArray();
                _FileStream.Write(Buffer, 0, Buffer.Length);
                // Offset for last byte of image data + 1
                previousFile = new FileInfo(ImageFiles[ImageFiles.Length - 1]);
                offset = previousOffset + previousFile.Length;
                byteOffset = BitConverter.GetBytes(offset);
                _FileStream.Write(byteOffset, 0, 4);

                // Write image data bytes
                foreach (String Image in ImageFiles)
                {
                    byte[] ImageBytes = File.ReadAllBytes(Image);
                    _FileStream.Write(ImageBytes, 0, ImageBytes.Length);

                    File.Delete(Image);
                }

                _FileStream.Close(); //Closing File
            }

        }

    }
   
}
