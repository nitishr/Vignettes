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

        public double OrientationInDegrees { private get; set; } // This parameter is not of relevance for the Circle vignette.

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
                    // This is the usual rotation formula, along with translation.
                    // I could have perhaps used the Transform feature of WPF.
                    var xprime = XPrime(el, k);
                    var yprime = YPrime(el, k);

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
                    int w1 = _width * el + k;

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
                        r = BorderColor.R;
                        g = BorderColor.G;
                        b = BorderColor.B;
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
                        r = (byte)(_pixRedOrig[w1] * _imageWeights[j1] + BorderColor.R * _borderWeights[j1]);
                        g = (byte)(_pixGreenOrig[w1] * _imageWeights[j1] + BorderColor.G * _borderWeights[j1]);
                        b = (byte)(_pixBlueOrig[w1] * _imageWeights[j1] + BorderColor.B * _borderWeights[j1]);
                    }
                    _pixRedModified[w1] = r;
                    _pixGreenModified[w1] = g;
                    _pixBlueModified[w1] = b;
                }
            }
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
                    var modified = PixModified(el, k);
                    _pixRedModified[_width * el + k] = modified.R;
                    _pixGreenModified[_width * el + k] = modified.G;
                    _pixBlueModified[_width * el + k] = modified.B;
                }
            }
        }

        private Color PixModified(int el, int k)
        {
            var point = new Point(Math.Abs(XPrime(el, k)), Math.Abs(YPrime(el, k)));
            double potential = Potential(point);
            int w1 = _width*el + k;
            if (potential < -1.0) // Arbitrary negative number, greater than N1
            {
                // Point is within the inner square / rectangle,
                return Color.FromRgb(_pixRedOrig[w1], _pixGreenOrig[w1], _pixBlueOrig[w1]);
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
            return Color.FromRgb(ColorComponentAt(j - 1, _pixRedOrig[w1], BorderColor.R),
                                 ColorComponentAt(j - 1, _pixGreenOrig[w1], BorderColor.G),
                                 ColorComponentAt(j - 1, _pixBlueOrig[w1], BorderColor.B));
        }

        private byte ColorComponentAt(int j1, byte imagePixel, byte borderPixel)
        {
            return (byte) (imagePixel*_imageWeights[j1] + borderPixel*_borderWeights[j1]);
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
