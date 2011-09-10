using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;
using System.Linq;
using System.Windows.Media.Imaging;

// Program to "vignettify" an image, for circular, elliptical, diamond, rectangular
//  and square-shaped vignettes.
// Written by Amarnath S, Bengaluru, India. Version 1.0, April 2011.
// amarnaths.codeproject@gmail.com

namespace Vignettes
{
    enum VignetteShape
    {
        Circle, 
        Ellipse,
        Diamond,
        Square,
        Rectangle
    };

    class VignetteEffect
    {
        private const int Dpi = 72;
        public const int BitsPerPixel = 24;

        List<Color> _pixOrig = new List<Color>();
        List<Color> _pixModified = new List<Color>();
        readonly List<double> _majorAxisValues = new List<double>();
        readonly List<double> _minorAxisValues = new List<double>();
        readonly List<double> _midfigureMajorAxisValues = new List<double>();
        readonly List<double> _midfigureMinorAxisValues = new List<double>();
        readonly List<double> _imageWeights = new List<double>();
        readonly List<double> _borderWeights = new List<double>();
        int _width;
        int _height;

        public double OrientationInDegrees { private get; set; } // This parameter is not of relevance for the Circle vignette.

        public double CoveragePercent { private get; set; }

        public int BandWidthInPixels { private get; set; }

        public int NumberOfGradationSteps { private get; set; }

        public int CenterXOffsetPercent { private get; set; } // with respect to half the width of the image.

        public int CenterYOffsetPercent { private get; set; } // with respect to half the height of the image.

        public Color BorderColor { private get; set; } // We consider only R, G, B values here. Alpha value is ignored.

        public VignetteShape Shape { private get; set; }

        private double SinOrientation
        {
            get { return Math.Sin(OrientationInRadians); }
        }

        private double CosOrientation
        {
            get { return Math.Cos(OrientationInRadians); }
        }

        private double OrientationInRadians
        {
            get { return OrientationInDegrees * Math.PI / 180.0; }
        }

        private double Hb2
        {
            get { return (1 + CenterYOffsetPercent / 100.0) * _height * 0.5; }
        }

        private double Wb2
        {
            get { return (1 + CenterXOffsetPercent / 100.0) * _width * 0.5; }
        }

        private Func<int, int, Color> PixModified
        {
            get
            {
                return Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse || Shape == VignetteShape.Diamond
                           ? (Func<int, int, Color>)PixModifiedCircleEllipseDiamond
                           : PixModifiedRectangleSquare;
            }
        }

        private void ModifyPixels()
        {
            for (int el = 0; el < _height; ++el)
            {
                for (int k = 0; k < _width; ++k)
                {
                    _pixModified[_width*el + k] = PixModified(el, k);
                }
            }
        }

        private void SetupParameters()
        {
            _majorAxisValues.Clear();
            _minorAxisValues.Clear();
            _midfigureMajorAxisValues.Clear();
            _midfigureMinorAxisValues.Clear();
            _imageWeights.Clear();
            _borderWeights.Clear();

            double a0 = AxisValue(_width, -1);
            double b0 = AxisValue(_height, -1);

            // For a circle or square, both 'major' and 'minor' axes are identical
            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Square)
            {
                a0 = b0 = Math.Min(a0, b0);
            }

            InitAxisValues(a0, b0);
            InitWeights(a0);
        }

        private void InitWeights(double a0)
        {
            for (int i = 0; i < NumberOfGradationSteps; ++i)
            {
                _imageWeights.Add(Weight(1, i, a0));
                _borderWeights.Add(Weight(-1, i, a0));
            }
        }

        // The weight functions given below form the crux of the code. It was a struggle after which 
        // I got these weighting functions. 
        // Initially, I tried linear interpolation, and the effect was not so pleasing. The 
        // linear interpolation function is C0-continuous at the boundary, and therefore shows 
        // a distinct border.
        // Later, upon searching, I found a paper by Burt and Adelson on Mosaics. Though I did 
        // not use the formulas given there, one of the initial figures in that paper set me thinking
        // on using the cosine function. This function is C1-continuous at the boundary, and therefore
        // the effect is pleasing on the eye. Yields quite a nice blending effect. The cosine 
        // functions are incorporated into the wei1 and wei2 definitions below.
        //
        // Reference: Burt and Adelson [Peter J Burt and Edward H Adelson, A Multiresolution Spline
        //  With Application to Image Mosaics, ACM Transactions on Graphics, Vol 2. No. 4,
        //  October 1983, Pages 217-236].
        private double Weight(int multiplier, int i, double a0)
        {
            return 0.5 * (1.0 + multiplier * Math.Cos(Math.PI / BandWidthInPixels * (_midfigureMajorAxisValues[i] - a0)));
        }

        private double AxisValue(int length, int multiplier)
        {
            return (length * CoveragePercent / 100.0 + multiplier * BandWidthInPixels) * 0.5;
        }

        private void InitAxisValues(double a0, double b0)
        {
            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse ||
                Shape == VignetteShape.Rectangle || Shape == VignetteShape.Square)
            {
                double step = ((double) BandWidthInPixels/NumberOfGradationSteps);
                InitAxisValues(a0, step, b0, step);
            }
            else // if (Shape == VignetteShape.Diamond)
            {
                double aLast = AxisValue(_width, 1);
                double bLast = b0*aLast/a0;
                InitAxisValues(a0, (aLast - a0)/NumberOfGradationSteps, b0, (bLast - b0)/NumberOfGradationSteps);
            }
        }

        private void InitAxisValues(double a0, double stepX, double b0, double stepY)
        {
            for (int i = 0; i <= NumberOfGradationSteps; ++i)
            {
                _majorAxisValues.Add(a0 + i*stepX);
                _minorAxisValues.Add(b0 + i*stepY);
                _midfigureMajorAxisValues.Add(a0 + (i + 0.5)*stepX);
                _midfigureMinorAxisValues.Add(b0 + (i + 0.5)*stepY);
            }
        }

        private Color PixModifiedCircleEllipseDiamond(int el, int k)
        {
            return GetPixModified(el, k, PixelInStepAtCircleEllipseDiamond, StepCircleEllipseDiamond);
        }

        private int StepCircleEllipseDiamond(int el, int k)
        {
            return Step(i => PotentialAt(i, el, k) < 0);
        }

        private bool PixelInStepAtCircleEllipseDiamond(int step, int el, int k)
        {
            return PotentialAt(step, el, k) < 0;
        }

        private double PotentialAt(int i, int el, int k)
        {
            return Potential(Math.Abs(XPrime(el, k))/_majorAxisValues[i], Math.Abs(YPrime(el, k))/_minorAxisValues[i]);
        }

        private int Step(Func<int, bool> isInStep)
        {
            return Enumerable.Range(1, NumberOfGradationSteps).First(isInStep) - 1;
        }

        private double Potential(double factor1, double factor2)
        {
            return Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse
                       ? factor1*factor1 + factor2*factor2 - 1
                       : factor1 + factor2 - 1;
        }

        private double YPrime(int el, int k)
        {
            return -(k - Wb2)*SinOrientation + (el - Hb2)*CosOrientation;
        }

        private double XPrime(int el, int k)
        {
            return (k - Wb2)*CosOrientation + (el - Hb2)*SinOrientation;
        }

        private Color PixModifiedRectangleSquare(int el, int k)
        {
            return GetPixModified(el, k, PixelInStepAtRectangleSquare, StepRectangleSquare);
        }

        private Color GetPixModified(int el, int k, Func<int, int, int, bool> pixelInStepAt, Func<int, int, int> step)
        {
            int w1 = _width*el + k;
            return pixelInStepAt(0, el, k)
                       ? _pixOrig[w1]
                       : (pixelInStepAt(NumberOfGradationSteps, el, k) ? ColorAt(step(el, k), w1) : BorderColor);
        }

        private int StepRectangleSquare(int el, int k)
        {
            return Step(new Point(Math.Abs(XPrime(el, k)), Math.Abs(YPrime(el, k))));
        }

        private bool PixelInStepAtRectangleSquare(int step, int el, int k)
        {
            return PointInRectAt(step, new Point(Math.Abs(XPrime(el, k)), Math.Abs(YPrime(el, k))));
        }

        private int Step(Point point)
        {
            return Step(i => PointInRectAt(i, point));
        }

        private bool PointInRectAt(int i, Point point)
        {
            return new Rect(0, 0, _majorAxisValues[i], _minorAxisValues[i]).Contains(point);
        }

        private Color ColorAt(int j1, int w1)
        {
            return Color.Add(Color.Multiply(_pixOrig[w1], (float) _imageWeights[j1]),
                             Color.Multiply(BorderColor, (float) _borderWeights[j1]));
        }

        public BitmapSource CreateImage()
        {
            SetupParameters();
            ModifyPixels();
            int stride = (_width*BitsPerPixel + 7)/8;
            var pixelsToWrite = new byte[stride*_height];

            for (int i = 0; i < pixelsToWrite.Count(); i += 3)
            {
                int i1 = i/3;
                pixelsToWrite[i] = _pixModified[i1].R;
                pixelsToWrite[i + 1] = _pixModified[i1].G;
                pixelsToWrite[i + 2] = _pixModified[i1].B;
            }

            return BitmapSource.Create(_width, _height, Dpi, Dpi, PixelFormats.Rgb24, null, pixelsToWrite, stride);
        }

        public void SetupParameters(List<Color> pixels, List<Color> pixelsModified, int width, int height)
        {
            _pixOrig = pixels;
            _width = width;
            _height = height;
            _pixModified = pixelsModified;
        }
    }
}
