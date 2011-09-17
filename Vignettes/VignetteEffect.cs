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
    public class VignetteEffect
    {
        private const int Dpi = 72;
        private const int BitsPerPixel = 24;
        private BitmapSource _image;

        public double OrientationInDegrees { private get; set; } // This parameter is not of relevance for the Circle vignette.

        public double CoveragePercent { private get; set; }

        public int BandWidthInPixels { get; set; }

        public int NumberOfGradationSteps { private get; set; }

        public double CenterXOffsetPercent { private get; set; } // with respect to half the width of the image.

        public double CenterYOffsetPercent { private get; set; } // with respect to half the height of the image.

        public Color BorderColor { private get; set; } // We consider only R, G, B values here. Alpha value is ignored.

        public VignetteFigure Figure { private get; set; }

        public double InnerWidth
        {
            get { return (_image.PixelWidth * CoverageRatio - BandWidthInPixels) / 2; }
        }

        public double InnerHeight
        {
            get { return (_image.PixelHeight * CoverageRatio - BandWidthInPixels) / 2; }
        }

        public double OuterWidth
        {
            get { return (_image.PixelWidth * CoverageRatio + BandWidthInPixels) / 2; }
        }

        public Size InnerSize
        {
            get { return Figure.Size(this); }
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
            get { return OrientationInDegrees * Math.PI / 180.0; }
        }

        private double CoverageRatio
        {
            get { return CoveragePercent / 100.0; }
        }

        private double BandWidthX
        {
            get { return Figure.BandWidthX(this); }
        }

        private double BandWidthY
        {
            get { return Figure.BandWidthY(this); }
        }

        private Color ColorAt(int i, Color originalColor)
        {
            var weight = WeightAt(i);
            return Color.Add(Color.Multiply(originalColor, weight), Color.Multiply(BorderColor, 1 - weight));
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
        private float WeightAt(int i)
        {
            return
                (float)
                (IsPixelInStep(i, NumberOfGradationSteps)
                     ? (1 + Math.Cos(Math.PI/BandWidthInPixels*Offset(StepContaining(i) + 0.5, 0, BandWidthX)))/2
                     : IsPixelInStep(i, 0) ? 1 : 0);
        }

        private int StepContaining(int i)
        {
            return Enumerable.Range(1, NumberOfGradationSteps).First(step => IsPixelInStep(i, step)) - 1;
        }

        private bool IsPixelInStep(int i, int step)
        {
            return Figure.IsPixelInStep(this, i, step);
        }

        public double YPrime(int i)
        {
            return Math.Abs(RowMinusHalfHeight(i)*CosOrientation - ColumnMinusHalfWidth(i)*SinOrientation);
        }

        public double XPrime(int i)
        {
            return Math.Abs(RowMinusHalfHeight(i)*SinOrientation + ColumnMinusHalfWidth(i)*CosOrientation);
        }

        private double RowMinusHalfHeight(int i)
        {
            int row = i/_image.PixelWidth;
            return row - (1 + CenterYOffsetPercent/100.0)*_image.PixelHeight*0.5;
        }

        private double ColumnMinusHalfWidth(int i)
        {
            int column = i%_image.PixelWidth;
            return column - (1 + CenterXOffsetPercent/100.0)*_image.PixelWidth*0.5;
        }

        public double YOffsetOf(int step)
        {
            return Offset(step, InnerSize.Height, BandWidthY);
        }

        public double XOffsetOf(int step)
        {
            return Offset(step, InnerSize.Width, BandWidthX);
        }

        private double Offset(double step, double length, double bandWidth)
        {
            return step*(bandWidth/NumberOfGradationSteps) + length;
        }

        public BitmapSource Transform(BitmapSource image)
        {
            _image = image;
            IList<Color> colors = image.Pixels().Select((color, i) => ColorAt(i, color)).ToList();
            int stride = (image.PixelWidth * BitsPerPixel + 7) / 8;
            var pixelsToWrite = new byte[stride * image.PixelHeight];

            for (int i = 0; i < pixelsToWrite.Count(); i += 3)
            {
                Color color = colors[i / 3];
                pixelsToWrite[i] = color.R;
                pixelsToWrite[i + 1] = color.G;
                pixelsToWrite[i + 2] = color.B;
            }

            return BitmapSource.Create(image.PixelWidth, image.PixelHeight, Dpi, Dpi, PixelFormats.Rgb24, null, pixelsToWrite, stride);
        }

        public static bool CanTransform(BitmapSource image)
        {
            PixelFormat format = image.Format;
            return (format == PixelFormats.Bgra32 || format == PixelFormats.Bgr32) &&
                   (format.BitsPerPixel == 24 || format.BitsPerPixel == 32);
        }
    }

    public interface IHasSize
    {
        Size Size(VignetteEffect effect);
    }

    class IdenticalAxes : IHasSize
    {
        public Size Size(VignetteEffect effect)
        {
            double dimension = Math.Min(effect.InnerWidth, effect.InnerHeight);
            return new Size(dimension, dimension);
        }
    }

    class DifferentAxes : IHasSize
    {
        public Size Size(VignetteEffect effect)
        {
            return new Size(effect.InnerWidth, effect.InnerHeight);
        }
    }

    public interface IHasBandWidth
    {
        double BandWidthX(VignetteEffect effect);
        double BandWidthY(VignetteEffect effect);
    }

    class UniformBandWidth : IHasBandWidth
    {
        public double BandWidthX(VignetteEffect effect)
        {
            return effect.BandWidthInPixels;
        }

        public double BandWidthY(VignetteEffect effect)
        {
            return effect.BandWidthInPixels;
        }
    }

    class DiamondBandWidth : IHasBandWidth
    {
        public double BandWidthX(VignetteEffect effect)
        {
            return effect.OuterWidth - effect.InnerSize.Width;
        }

        public double BandWidthY(VignetteEffect effect)
        {
            return effect.InnerSize.Height * (effect.OuterWidth / effect.InnerSize.Width - 1);
        }
    }

    public interface IHasSteps
    {
        bool IsPixelInStep(VignetteEffect effect, int pixel, int step);
    }

    class RectangularSteps : IHasSteps
    {
        public bool IsPixelInStep(VignetteEffect effect, int pixel, int step)
        {
            return
                new Rect(0, 0, effect.XOffsetOf(step), effect.YOffsetOf(step)).Contains(
                    new Point(effect.XPrime(pixel), effect.YPrime(pixel)));
        }
    }

    abstract class NonRectangularSteps : IHasSteps
    {
        private readonly double _power;

        protected NonRectangularSteps(double power)
        {
            _power = power;
        }

        public bool IsPixelInStep(VignetteEffect effect, int pixel, int step)
        {
            return
                Math.Pow(effect.XPrime(pixel) / effect.XOffsetOf(step), _power) +
                Math.Pow(effect.YPrime(pixel) / effect.YOffsetOf(step), _power) < 1;
        }
    }

    class EllipticalSteps : NonRectangularSteps
    {
        public EllipticalSteps() : base(2)
        {
        }
    }

    class DiamondSteps : NonRectangularSteps
    {
        public DiamondSteps() : base(1)
        {
        }
    }

    public class VignetteFigure : IHasSize, IHasBandWidth, IHasSteps
    {
        private readonly IHasSize _hasSize;
        private readonly IHasBandWidth _hasBandWidth;
        private readonly IHasSteps _hasSteps;

        protected VignetteFigure(IHasSize hasSize, IHasBandWidth hasBandWidth, IHasSteps hasSteps)
        {
            _hasSize = hasSize;
            _hasBandWidth = hasBandWidth;
            _hasSteps = hasSteps;
        }

        public Size Size(VignetteEffect effect)
        {
            return _hasSize.Size(effect);
        }

        public double BandWidthX(VignetteEffect effect)
        {
            return _hasBandWidth.BandWidthX(effect);
        }

        public double BandWidthY(VignetteEffect effect)
        {
            return _hasBandWidth.BandWidthY(effect);
        }

        public bool IsPixelInStep(VignetteEffect effect, int pixel, int step)
        {
            return _hasSteps.IsPixelInStep(effect, pixel, step);
        }
    }

    public class Circle : VignetteFigure
    {
        public Circle() : base(new IdenticalAxes(), new UniformBandWidth(), new EllipticalSteps())
        {
        }
    }

    public class Ellipse : VignetteFigure
    {
        public Ellipse() : base(new DifferentAxes(), new UniformBandWidth(), new EllipticalSteps())
        {
        }
    }

    public class Diamond : VignetteFigure
    {
        public Diamond() : base(new DifferentAxes(), new DiamondBandWidth(), new DiamondSteps())
        {
        }
    }

    public class Square : VignetteFigure
    {
        public Square() : base(new IdenticalAxes(), new UniformBandWidth(), new RectangularSteps())
        {
        }
    }

    public class Rectangle : VignetteFigure
    {
        public Rectangle() : base(new DifferentAxes(), new UniformBandWidth(), new RectangularSteps())
        {
        }
    }
}
