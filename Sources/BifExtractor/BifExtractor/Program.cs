using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace BifExtractor
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("\nBifExtractor.exe InputFile [OutputFolder]\n");
                return 0;
            }

            String Input = args[0];
            String Output = Path.GetDirectoryName(Input);
            if (args.Length > 1)
                Output = args[1];

            byte[] Buffer;

            Buffer = File.ReadAllBytes(Input).Skip(12).Take(4).ToArray();
            int NbImages = BitConverter.ToInt32(Buffer, 0);
            Console.WriteLine("Nombre d'images : " + NbImages);

            Buffer = File.ReadAllBytes(Input).Skip(16).Take(4).ToArray();
            int Interval = BitConverter.ToInt32(Buffer, 0) / 1000;
            Console.WriteLine("Intervalle entre les images (secondes) : " + Interval );

            int i = 64;
            for ( int j = 0; j < NbImages; j++ )
            {
                byte[] Index = File.ReadAllBytes(Input).Skip(i).Take(4).ToArray();
                byte[] ByteOffsetCurrent = File.ReadAllBytes(Input).Skip(i + 4).Take(4).ToArray();
                byte[] ByteOffsetNext = File.ReadAllBytes(Input).Skip(i + 12).Take(4).ToArray();
                i += 8;

                int ByteRangeIn = BitConverter.ToInt32(ByteOffsetCurrent, 0);
                int ByteRangeOut = (BitConverter.ToInt32(ByteOffsetNext, 0) - 1);
                int ByteRangeSize = ByteRangeOut - ByteRangeIn;
                Console.WriteLine("#" + j + ": ByteStart=" + ByteRangeIn + " ByteEnd=" + ByteRangeOut );
                File.WriteAllBytes(Path.Combine(Output, j + ".jpg"), File.ReadAllBytes(Input).Skip(ByteRangeIn).Take(ByteRangeSize).ToArray());
            }
            return 0;
        }
    }
}
