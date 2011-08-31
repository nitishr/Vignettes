using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;
using System.Linq;
using System.Windows.Media.Imaging;
using System.IO;

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
        // Regarding the variable "geometryFactor": 
        // - This is not a magic number. This is the factor by which the x-Centre and 
        //   y-Centre slider values are to be multiplied so as to cause the following:
        //   When the x-Centre slider is at its midpoint, the vignette should be centred 
        //   at the midpoint of the image. When the x-Centre slider is at its extreme right
        //   point, the vignette centre should be along the right edge of the image. When the
        //   x-Centre slider is at its extreme left point, the vignette centre should be 
        //   along the left edge of the image. 
        //   Similarly with the y-Centre slider.
        //   So, the factor of multiplication is (Half-of the width)/(Maximum value of slider)
        //    = 0.5 / 100.0
        private const double GeometryFactor = 0.5 / 100.0;
        private const int Dpi = 72;
        public const int BitsPerPixel = 24;

        List<byte> _pixRedOrig = new List<byte>();
        List<byte> _pixGreenOrig = new List<byte>();
        List<byte> _pixBlueOrig = new List<byte>();
        List<byte> _pixRedModified = new List<byte>();
        List<byte> _pixGreenModified = new List<byte>();
        List<byte> _pixBlueModified = new List<byte>();
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

        public double OrientationInDegrees { get; set; } // This parameter is not of relevance for the Circle vignette.

        public double CoveragePercent { private get; set; }

        public int BandWidthInPixels { get; set; }

        public int NumberOfGradationSteps { get; set; }

        public int CenterXOffsetPercent { get; set; } // with respect to half the width of the image.

        public int CenterYOffsetPercent { get; set; } // with respect to half the height of the image.

        public Color BorderColor { get; set; } // We consider only R, G, B values here. Alpha value is ignored.

        public VignetteShape Shape { get; set; }

        public string FileNameToSave { get; set; }

        public void TransferImagePixels(
            ref List<byte> redOrig, ref List<byte> greenOrig, ref List<byte> blueOrig,
            int wid, int hei,
            ref List<byte> redModified, ref List<byte> greenModified, ref List<byte> blueModified, 
            ModeOfOperation modeOfOperation)
        {
            _pixRedOrig = redOrig;
            _pixGreenOrig = greenOrig;
            _pixBlueOrig = blueOrig;
            _width = wid;
            _height = hei;
            _pixRedModified = redModified;
            _pixGreenModified = greenModified;
            _pixBlueModified = blueModified;
            _mode = modeOfOperation;
        }

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
                _mainWin.UpdateImage(ref _pixRedModified, ref _pixGreenModified, ref _pixBlueModified);
            }
            else // if (mode == ModeOfOperation.SaveMode) // Save the image onto the specified file.
            {
                SaveImage();
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

            double a0 = (_width*CoverageRatio - BandWidthInPixels)*0.5;
            double b0 = (_height*CoverageRatio - BandWidthInPixels)*0.5;

            // For a circle or square, both 'major' and 'minor' axes are identical
            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Square)
            {
                a0 = Math.Min(a0, b0);
                b0 = a0;
            }

            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse ||
                Shape == VignetteShape.Rectangle || Shape == VignetteShape.Square)
            {
                for (int i = 0; i <= NumberOfGradationSteps; ++i)
                {
                    _majorAxisValues.Add(AxisValue(a0, i));
                    _minorAxisValues.Add(AxisValue(b0, i));
                }
                for (int i = 0; i < NumberOfGradationSteps; ++i)
                {
                    _midfigureMajorAxisValues.Add(AxisValue(a0, i + 0.5));
                    _midfigureMinorAxisValues.Add(AxisValue(b0, i + 0.5));
                }
            }
            else// if (Shape == VignetteShape.Diamond)
            {
                double aLast = (_width*CoverageRatio + BandWidthInPixels)*0.5;
                double bLast = b0 * aLast / a0;
                double stepXdiamond = (aLast - a0) / NumberOfGradationSteps;
                double stepYdiamond = (bLast - b0) / NumberOfGradationSteps;

                for (int i = 0; i <= NumberOfGradationSteps; ++i)
                {
                    _majorAxisValues.Add(a0 + stepXdiamond * i);
                    _minorAxisValues.Add(b0 + stepYdiamond * i);
                }
                for (int i = 0; i <= NumberOfGradationSteps; ++i)
                {
                    _midfigureMajorAxisValues.Add(a0 + stepXdiamond * (i + 0.5));
                    _midfigureMinorAxisValues.Add(b0 + stepYdiamond * (i + 0.5));
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
            for (int i = 0; i < NumberOfGradationSteps; ++i)
            {
                _imageWeights.Add(0.5 * (1.0 + ArgCosVal(a0, i)));
                _borderWeights.Add(0.5 * (1.0 - ArgCosVal(a0, i)));
            }
        }

        private double AxisValue(double axisValue, double stepSizeMultiplier)
        {
            return axisValue + stepSizeMultiplier*BandWidthInPixels/NumberOfGradationSteps;
        }

        private double CoverageRatio
        {
            get { return (CoveragePercent/100.0); }
        }

        private double ArgCosVal(double a0, int i)
        {
            return Math.Cos(Math.PI/BandWidthInPixels*(_midfigureMajorAxisValues[i] - a0));
        }

        private void ApplyEffectCircleEllipseDiamond()
        {
            int el;
            double wb2 = _width * 0.5 + CenterXOffsetPercent * _width * GeometryFactor;
            double hb2 = _height * 0.5 + CenterYOffsetPercent * _height * GeometryFactor;
            double thetaRadians = OrientationInDegrees * Math.PI / 180.0;
            double cos = Math.Cos(thetaRadians);
            double sin = Math.Sin(thetaRadians);
            byte redBorder = BorderColor.R;
            byte greenBorder = BorderColor.G;
            byte blueBorder = BorderColor.B;

            // Loop over the number of pixels
            for (el = 0; el < _height; ++el)
            {
                int w2 = _width * el;
                int k;
                for (k = 0; k < _width; ++k)
                {
                    // This is the usual rotation formula, along with translation.
                    // I could have perhaps used the Transform feature of WPF.
                    double xprime = (k - wb2) * cos + (el - hb2) * sin;
                    double yprime = -(k - wb2) * sin + (el - hb2) * cos;

                    double factor1 = 1.0 * Math.Abs(xprime) / _majorAxisValues[0];
                    double factor2 = 1.0 * Math.Abs(yprime) / _minorAxisValues[0];
                    double factor3 = 1.0 * Math.Abs(xprime) / _majorAxisValues[NumberOfGradationSteps];
                    double factor4 = 1.0 * Math.Abs(yprime) / _minorAxisValues[NumberOfGradationSteps];

                    double potential1;
                    double potential2;
                    if (Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse)
                    {
                        // Equations for the circle / ellipse. 
                        // "Potentials" are analogous to distances from the inner and outer boundaries
                        // of the two ellipses.
                        potential1 = factor1 * factor1 + factor2 * factor2 - 1.0;
                        potential2 = factor3 * factor3 + factor4 * factor4 - 1.0;
                    }
                    else //if (Shape == VignetteShape.Diamond)
                    {
                        // Equations for the diamond. 
                        potential1 = factor1 + factor2 - 1.0;
                        potential2 = factor3 + factor4 - 1.0;
                    }
                    int w1 = w2 + k;

                    byte r;
                    byte g;
                    byte b;
                    if (potential1 <= 0.0)
                    {
                        // Point is within the inner circle / ellipse / diamond
                        r = _pixRedOrig[w1];
                        g = _pixGreenOrig[w1];
                        b = _pixBlueOrig[w1];
                    }
                    else if (potential2 >= 0.0)
                    {
                        // Point is outside the outer circle / ellipse / diamond
                        r = redBorder;
                        g = greenBorder;
                        b = blueBorder;
                    }
                    else
                    {
                        // Point is in between the outermost and innermost circles / ellipses / diamonds
                        int j;

                        for (j = 1; j < NumberOfGradationSteps; ++j)
                        {
                            factor1 = Math.Abs(xprime) / _majorAxisValues[j];
                            factor2 = Math.Abs(yprime) / _minorAxisValues[j];

                            double potential;
                            if (Shape == VignetteShape.Circle ||
                                Shape == VignetteShape.Ellipse)
                            {
                                potential = factor1 * factor1 + factor2 * factor2 - 1.0;
                            }
                            else // if (Shape == VignetteShape.Diamond)
                            {
                                potential = factor1 + factor2 - 1.0;
                            }
                            if (potential < 0.0) break;
                        }
                        int j1 = j - 1;
                        // The formulas where the weights are applied to the image, and border.
                        r = (byte)(_pixRedOrig[w1] * _imageWeights[j1] + redBorder * _borderWeights[j1]);
                        g = (byte)(_pixGreenOrig[w1] * _imageWeights[j1] + greenBorder * _borderWeights[j1]);
                        b = (byte)(_pixBlueOrig[w1] * _imageWeights[j1] + blueBorder * _borderWeights[j1]);
                    }
                    _pixRedModified[w1] = r;
                    _pixGreenModified[w1] = g;
                    _pixBlueModified[w1] = b;
                }
            }
        }

        private void ApplyEffectRectangleSquare()
        {
            Rect rect1 = new Rect(), rect2 = new Rect(), rect3 = new Rect();
            var point = new Point();
            int el;

            double wb2 = _width * 0.5 + CenterXOffsetPercent * _width * GeometryFactor;
            double hb2 = _height * 0.5 + CenterYOffsetPercent * _height * GeometryFactor;
            double thetaRadians = OrientationInDegrees * Math.PI / 180.0;
            double cos = Math.Cos(thetaRadians);
            double sin = Math.Sin(thetaRadians);
            byte redBorder = BorderColor.R;
            byte greenBorder = BorderColor.G;
            byte blueBorder = BorderColor.B;

            rect1.X = 0.0;
            rect1.Y = 0.0;
            rect1.Width = _majorAxisValues[0];
            rect1.Height = _minorAxisValues[0];
            rect2.X = 0.0;
            rect2.Y = 0.0;
            rect2.Width = _majorAxisValues[NumberOfGradationSteps];
            rect2.Height = _minorAxisValues[NumberOfGradationSteps];

            for (el = 0; el < _height; ++el)
            {
                int w2 = _width * el;
                int k;
                for (k = 0; k < _width; ++k)
                {
                    // The usual rotation-translation formula
                    double xprime = (k - wb2) * cos + (el - hb2) * sin;
                    double yprime = -(k - wb2) * sin + (el - hb2) * cos;

                    double potential = 0.0;
                    point.X = Math.Abs(xprime);
                    point.Y = Math.Abs(yprime);

                    // For a rectangle, we can use the Rect.Contains(Point) method to determine
                    //  whether the point is in the rectangle or not
                    if (rect1.Contains(point))
                        potential = -2.0; // Arbitrary negative number N1

                    if (!rect2.Contains(point))
                        potential = 2.0; // Arbitrary positive number = - N1

                    int w1 = w2 + k;

                    byte r;
                    byte g;
                    byte b;
                    if (potential < -1.0) // Arbitrary negative number, greater than N1
                    {
                        // Point is within the inner square / rectangle,
                        r = _pixRedOrig[w1];
                        g = _pixGreenOrig[w1];
                        b = _pixBlueOrig[w1];
                    }
                    else if (potential > 1.0) // Arbitrary positive number lesser than - N1
                    {
                        // Point is outside the outer square / rectangle
                        r = redBorder;
                        g = greenBorder;
                        b = blueBorder;
                    }
                    else
                    {
                        // Point is in between outermost and innermost squares / rectangles
                        int j;

                        for (j = 1; j < NumberOfGradationSteps; ++j)
                        {
                            rect3.X = 0.0;
                            rect3.Y = 0.0;
                            rect3.Width = _majorAxisValues[j];
                            rect3.Height = _minorAxisValues[j];

                            if (rect3.Contains(point))
                                break;
                        }
                        int j1 = j - 1;
                        r = (byte)(_pixRedOrig[w1] * _imageWeights[j1] + redBorder * _borderWeights[j1]);
                        g = (byte)(_pixGreenOrig[w1] * _imageWeights[j1] + greenBorder * _borderWeights[j1]);
                        b = (byte)(_pixBlueOrig[w1] * _imageWeights[j1] + blueBorder * _borderWeights[j1]);
                    }
                    _pixRedModified[w1] = r;
                    _pixGreenModified[w1] = g;
                    _pixBlueModified[w1] = b;
                }
            }
        }

        private void SaveImage()
        {
            // First, create the image to be saved
            int stride = (_width * BitsPerPixel + 7) / 8;
            var pixelsToWrite = new byte[stride * _height];

            for (int i = 0; i < pixelsToWrite.Count(); i += 3)
            {
                int i1 = i / 3;
                pixelsToWrite[i] = _pixRedModified[i1];
                pixelsToWrite[i + 1] = _pixGreenModified[i1];
                pixelsToWrite[i + 2] = _pixBlueModified[i1];
            }

            BitmapSource imageToSave = BitmapSource.Create(_width, _height, Dpi, Dpi, PixelFormats.Rgb24,
                null, pixelsToWrite, stride);

            // Then, save the image
            string extn = Path.GetExtension(FileNameToSave);
            var fs = new FileStream(FileNameToSave, FileMode.Create);
            switch (extn)
            {
                case ".png":
                    {
                        // Save as PNG
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(imageToSave));
                        encoder.Save(fs);
                    }
                    break;
                case ".jpg":
                    {
                        // Save as JPG
                        BitmapEncoder encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(imageToSave));
                        encoder.Save(fs);
                    }
                    break;
                default:
                    {
                        // Save as BMP
                        BitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(imageToSave));
                        encoder.Save(fs);
                    }
                    break;
            }
            fs.Close();
        }
    }
}
