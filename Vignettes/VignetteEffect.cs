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

        private List<Color> _pixels;
        private List<double> _majorAxisValues;
        private List<double> _minorAxisValues;
        private List<double> _midfigureMajorAxisValues;
        private List<double> _weights;
        private int _width;
        private int _height;

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

        private Color GetPixModified(int i)
        {
            return IsPixelInStep(i, 0)
                       ? _pixels[i]
                       : (IsPixelInStep(i, NumberOfGradationSteps) ? ColorAt(i) : BorderColor);
        }

        private Color ColorAt(int i)
        {
            var weight = (float) _weights[StepContaining(i)];
            return Color.Add(Color.Multiply(_pixels[i], weight),
                             Color.Multiply(BorderColor, 1 - weight));
        }

        private int StepContaining(int i)
        {
            return Enumerable.Range(1, NumberOfGradationSteps).First(step => IsPixelInStep(i, step)) - 1;
        }

        private bool IsPixelInStep(int i, int step)
        {
            return Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse || Shape == VignetteShape.Diamond
                        ? IsPixelInStepCircleEllipseDiamond(i, step)
                        : IsPixelInStepRectangleSquare(i, step);
        }

        private bool IsPixelInStepRectangleSquare(int i, int step)
        {
            return PointInRectAt(step, new Point(XPrime(i), YPrime(i)));
        }

        private bool IsPixelInStepCircleEllipseDiamond(int i, int step)
        {
            return Potential(XPrime(i)/_majorAxisValues[step], YPrime(i)/_minorAxisValues[step]) < 0;
        }

        private double YPrime(int i)
        {
            return Math.Abs(RowMinusHalfHeight(i)*CosOrientation - ColumnMinusHalfWidth(i)*SinOrientation);
        }

        private double XPrime(int i)
        {
            return Math.Abs(RowMinusHalfHeight(i)*SinOrientation + ColumnMinusHalfWidth(i)*CosOrientation);
        }

        private double RowMinusHalfHeight(int i)
        {
            int row = i/_width;
            return row - (1 + CenterYOffsetPercent/100.0)*_height*0.5;
        }

        private double ColumnMinusHalfWidth(int i)
        {
            int column = i%_width;
            return column - (1 + CenterXOffsetPercent/100.0)*_width*0.5;
        }

        private void SetupParameters()
        {
            double a0 = AxisValue(_width, -1);
            double b0 = AxisValue(_height, -1);

            // For a circle or square, both 'major' and 'minor' axes are identical
            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Square)
            {
                a0 = b0 = Math.Min(a0, b0);
            }

            InitAxisValues(a0, b0);
            _weights = new List<double>(Enumerable.Range(0, NumberOfGradationSteps).Select(i => Weight(i, a0)));
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
        private double Weight(int step, double a0)
        {
            return 0.5*(1.0 + Math.Cos(Math.PI/BandWidthInPixels*(_midfigureMajorAxisValues[step] - a0)));
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
            _majorAxisValues = AxisValues(i => a0 + i * stepX);
            _minorAxisValues = AxisValues(i => b0 + i * stepY);
            _midfigureMajorAxisValues = AxisValues(i => a0 + (i + 0.5) * stepX);
        }

        private List<double> AxisValues(Func<int, double> selector)
        {
            return new List<double>(Enumerable.Range(0, NumberOfGradationSteps + 1).Select(selector));
        }

        private double Potential(double factor1, double factor2)
        {
            return Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse
                       ? factor1*factor1 + factor2*factor2 - 1
                       : factor1 + factor2 - 1;
        }

        private bool PointInRectAt(int step, Point point)
        {
            return new Rect(0, 0, _majorAxisValues[step], _minorAxisValues[step]).Contains(point);
        }

        public BitmapSource CreateImage()
        {
            SetupParameters();
            int stride = (_width*BitsPerPixel + 7)/8;
            var pixelsToWrite = new byte[stride*_height];

            for (int i = 0; i < pixelsToWrite.Count(); i += 3)
            {
                Color color = GetPixModified(i/3);
                pixelsToWrite[i] = color.R;
                pixelsToWrite[i + 1] = color.G;
                pixelsToWrite[i + 2] = color.B;
            }

            return BitmapSource.Create(_width, _height, Dpi, Dpi, PixelFormats.Rgb24, null, pixelsToWrite, stride);
        }

        public void SetupParameters(List<Color> pixels, int width, int height)
        {
            _pixels = pixels;
            _width = width;
            _height = height;
        }
    }
}
