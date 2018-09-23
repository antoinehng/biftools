using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BifCreator
{
    public class ImageTargetConfiguration
    {
        public enum Formats
        {
            jpg,
            bmp,
            png,
        }
        public Formats ImageFormat;
        public int Width;
        public int Height;
        public int AspectRatio;
        public int TimeInterval;
        public int QuantificationFactor;
    }
}
