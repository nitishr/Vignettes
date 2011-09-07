﻿using System;
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

    internal enum ModeOfOperation
    {
        DisplayMode,
        SaveMode
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
        ModeOfOperation _mode;
        readonly MainWindow _mainWin;

        public VignetteEffect(MainWindow main)
        {
            _mainWin = main;
        }

        public double OrientationInDegrees { private get; set; } // This parameter is not of relevance for the Circle vignette.

        public double CoveragePercent { private get; set; }

        public int BandWidthInPixels { get; set; }

        public int NumberOfGradationSteps { get; set; }

        public int CenterXOffsetPercent { get; set; } // with respect to half the width of the image.

        public int CenterYOffsetPercent { get; set; } // with respect to half the height of the image.

        public Color BorderColor { get; set; } // We consider only R, G, B values here. Alpha value is ignored.

        public VignetteShape Shape { get; set; }

        public void ApplyEffect()
        {
            SetupParameters();
            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse ||
                Shape == VignetteShape.Diamond)
                ApplyEffectCircleEllipseDiamond();
            else // if (Shape == VignetteShape.Rectangle || Shape == VignetteShape.Square)
                ApplyEffectRectangleSquare();

            if (_mode == ModeOfOperation.DisplayMode) // Send back the pixels to display the image.
            {                
                _mainWin.UpdateImage();
            }
            else // if (mode == ModeOfOperation.SaveMode) // Save the image onto the specified file.
            {
                _mainWin.SaveImage();
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
                a0 = Math.Min(a0, b0);
                b0 = a0;
            }

            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse ||
                Shape == VignetteShape.Rectangle || Shape == VignetteShape.Square)
            {
                double step = ((double)BandWidthInPixels/NumberOfGradationSteps);
                AddAxisValues(a0, step, b0, step);
            }
            else// if (Shape == VignetteShape.Diamond)
            {
                double aLast = AxisValue(_width, 1);
                double bLast = b0 * aLast / a0;
                AddAxisValues(a0, (aLast - a0) / NumberOfGradationSteps, b0, (bLast - b0) / NumberOfGradationSteps);
            }

            for (int i = 0; i < NumberOfGradationSteps; ++i)
            {
                _imageWeights.Add(Weight(1, i, a0));
                _borderWeights.Add(Weight(-1, i, a0));
            }
        }

        private double AxisValue(int length, int multiplier)
        {
            return (length*CoveragePercent/100.0 + multiplier*BandWidthInPixels)*0.5;
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
            return 0.5 * (1.0 + multiplier*Math.Cos(Math.PI/BandWidthInPixels*(_midfigureMajorAxisValues[i] - a0)));
        }

        private void AddAxisValues(double a0, double stepX, double b0, double stepY)
        {
            for (int i = 0; i <= NumberOfGradationSteps; ++i)
            {
                _majorAxisValues.Add(a0 + i*stepX);
                _minorAxisValues.Add(b0 + i*stepY);
                _midfigureMajorAxisValues.Add(a0 + (i + 0.5)*stepX);
                _midfigureMinorAxisValues.Add(b0 + (i + 0.5)*stepY);
            }
        }

        private void ApplyEffectCircleEllipseDiamond()
        {
            // Loop over the number of pixels
            for (int el = 0; el < _height; ++el)
            {
                for (int k = 0; k < _width; ++k)
                {
                    _pixModified[_width*el + k] = PixModifiedCircleEllipseDiamond(el, k);
                }
            }
        }

        private Color PixModifiedCircleEllipseDiamond(int el, int k)
        {
            var xprime = XPrime(el, k);
            var yprime = YPrime(el, k);

            double factor1 = 1.0*Math.Abs(xprime)/_majorAxisValues[0];
            double factor2 = 1.0*Math.Abs(yprime)/_minorAxisValues[0];
            double factor3 = 1.0*Math.Abs(xprime)/_majorAxisValues[NumberOfGradationSteps];
            double factor4 = 1.0*Math.Abs(yprime)/_minorAxisValues[NumberOfGradationSteps];

            double potential1;
            double potential2;
            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse)
            {
                // Equations for the circle / ellipse. 
                // "Potentials" are analogous to distances from the inner and outer boundaries
                // of the two ellipses.
                potential1 = factor1*factor1 + factor2*factor2 - 1.0;
                potential2 = factor3*factor3 + factor4*factor4 - 1.0;
            }
            else //if (Shape == VignetteShape.Diamond)
            {
                // Equations for the diamond. 
                potential1 = factor1 + factor2 - 1.0;
                potential2 = factor3 + factor4 - 1.0;
            }

            int w1 = _width*el + k;
            if (potential1 <= 0.0)
            {
                // Point is within the inner circle / ellipse / diamond
                return _pixOrig[w1];
            }
            if (potential2 >= 0.0)
            {
                // Point is outside the outer circle / ellipse / diamond
                return BorderColor;
            }
            // Point is in between the outermost and innermost circles / ellipses / diamonds
            int j;
            for (j = 1; j < NumberOfGradationSteps; ++j)
            {
                factor1 = Math.Abs(xprime)/_majorAxisValues[j];
                factor2 = Math.Abs(yprime)/_minorAxisValues[j];

                double potential;
                if (Shape == VignetteShape.Circle ||
                    Shape == VignetteShape.Ellipse)
                {
                    potential = factor1*factor1 + factor2*factor2 - 1.0;
                }
                else // if (Shape == VignetteShape.Diamond)
                {
                    potential = factor1 + factor2 - 1.0;
                }
                if (potential < 0.0) break;
            }
            return ColorAt(j - 1, w1);
        }

        private double YPrime(int el, int k)
        {
            return -(k - Wb2)*SinOrientation + (el - Hb2)*CosOrientation;
        }

        private double XPrime(int el, int k)
        {
            return (k - Wb2)*CosOrientation + (el - Hb2)*SinOrientation;
        }

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
            get { return OrientationInDegrees*Math.PI/180.0; }
        }

        private double Hb2
        {
            get { return (1 + CenterYOffsetPercent/100.0)*_height*0.5; }
        }

        private double Wb2
        {
            get { return (1 + CenterXOffsetPercent/100.0)*_width*0.5; }
        }

        private void ApplyEffectRectangleSquare()
        {
            for (int el = 0; el < _height; ++el)
            {
                for (int k = 0; k < _width; ++k)
                {
                    _pixModified[_width*el + k] = PixModifiedRectangleSquare(el, k);
                }
            }
        }

        private Color PixModifiedRectangleSquare(int el, int k)
        {
            var point = new Point(Math.Abs(XPrime(el, k)), Math.Abs(YPrime(el, k)));
            double potential = Potential(point);
            int w1 = _width*el + k;
            if (potential < -1.0) // Arbitrary negative number, greater than N1
            {
                // Point is within the inner square / rectangle,
                return _pixOrig[w1];
            }
            if (potential > 1.0) // Arbitrary positive number lesser than - N1
            {
                // Point is outside the outer square / rectangle
                return BorderColor;
            }
            // Point is in between outermost and innermost squares / rectangles
            int j;
            for (j = 1; j < NumberOfGradationSteps; ++j)
            {
                if (new Rect(0, 0, _majorAxisValues[j], _minorAxisValues[j]).Contains(point))
                    break;
            }
            return ColorAt(j - 1, w1);
        }

        private Color ColorAt(int j1, int w1)
        {
            return Color.Add(Color.Multiply(_pixOrig[w1], (float) _imageWeights[j1]),
                             Color.Multiply(BorderColor, (float) _borderWeights[j1]));
        }

        private double Potential(Point point)
        {
            double potential = 0.0;
            if (new Rect(0, 0, _majorAxisValues[0], _minorAxisValues[0]).Contains(point))
                potential = -2.0; // Arbitrary negative number N1

            if (new Rect(0, 0, _majorAxisValues[NumberOfGradationSteps],
                         _minorAxisValues[NumberOfGradationSteps]).Contains(point)) return potential;
            potential = 2.0; // Arbitrary positive number = - N1
            return potential;
        }

        public static BitmapSource CreateImage(List<Color> pix, int pixelWidth, int pixelHeight)
        {
            int stride = (pixelWidth*BitsPerPixel + 7)/8;
            var pixelsToWrite = new byte[stride*pixelHeight];

            for (int i = 0; i < pixelsToWrite.Count(); i += 3)
            {
                int i1 = i/3;
                pixelsToWrite[i] = pix[i1].R;
                pixelsToWrite[i + 1] = pix[i1].G;
                pixelsToWrite[i + 2] = pix[i1].B;
            }

            return BitmapSource.Create(pixelWidth, pixelHeight, Dpi, Dpi, PixelFormats.Rgb24,
                                       null, pixelsToWrite, stride);
        }

        public void ApplyEffect(List<Color> pixels, List<Color> pixelsModified, int width, int height, ModeOfOperation modeOfOperation)
        {
            _pixOrig = pixels;
            _width = width;
            _height = height;
            _pixModified = pixelsModified;
            _mode = modeOfOperation;
            ApplyEffect();
        }
    }
}
