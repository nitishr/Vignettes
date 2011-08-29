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
    /// <summary>
    /// Enum for the vignette shape. There are five possible vignette shapes.
    /// </summary>
    enum VignetteShape
    {
        Circle, 
        Ellipse,
        Diamond,
        Square,
        Rectangle
    };

    /// <summary>
    /// Enum for the mode of operation of the vignette. There are two modes:
    ///  Display Mode - To improve the application response, we do all visible vignette operations 
    ///  on a scaled image (max dimensions 600 x 600).
    ///  Save Mode - Here, we perform the vignette operations on the original image (with original 
    ///  dimensions), and save the image. 
    /// </summary>
    internal enum ModeOfOperation
    {
        DisplayMode,
        SaveMode
    };

    /// <summary>
    /// Class to implement the vignette effect. 
    /// </summary>
    class VignetteEffect
    {
        List<byte> pixRedOrig;       // List of red pixels in original image.
        List<byte> pixGreenOrig;     // List of green pixels in original image.
        List<byte> pixBlueOrig;      // List of blue pixels in original image.
        List<byte> pixRedModified;   // List of red pixels in modified image.
        List<byte> pixGreenModified; // List of green pixels in modified image.
        List<byte> pixBlueModified;  // List of blue pixels in modified image.
        List<double> aVals;          // Major axis value of the vignette shape.
        List<double> bVals;          // Minor axis value of the vignette shape.
        List<double> aValsMidPoints; // Major axis of mid-figures of the vignette shape.
        List<double> bValsMidPoints; // Minor axis of mid-figures of the vignette shape.
        List<double> weight1;        // Weights for the original image.
        List<double> weight2;        // Weights for the border colour.
        int dpi;                     // DPI for the image for saving.
        int width;                   // Width of image.
        int height;                  // Height of image.
        double geometryFactor;       // See note below, in the constructor.
        ModeOfOperation mode;        // Either display mode or save mode.        
        MainWindow mainWin;          // Main Window object.

        public VignetteEffect(MainWindow main)
        {
            pixRedOrig = new List<byte>();
            pixGreenOrig = new List<byte>();
            pixBlueOrig = new List<byte>();
            pixRedModified = new List<byte>();
            pixGreenModified = new List<byte>();
            pixBlueModified = new List<byte>();

            mainWin = main;

            aVals = new List<double>();
            bVals = new List<double>();
            aValsMidPoints = new List<double>();
            bValsMidPoints = new List<double>();
            weight1 = new List<double>();
            weight2 = new List<double>();

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
            geometryFactor = 0.5/100.0;

            dpi = 72; // I guess this suffices
        }

        /// <summary>
        /// Orientation of the Ellipse, Diamond, Square or Rectangle in degrees. 
        /// This parameter is not of relevance for the Circle vignette.
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// Coverage of the vignette in percentage of the image dimension (width or height).
        /// </summary>
        public double Coverage { get; set; }

        /// <summary>
        /// Width of the "band" between the inner "original image" region and the outer
        /// "border" region. This width is measured in pixels.
        /// </summary>
        public int BandPixels { get; set; }

        /// <summary>
        /// Number of steps of "gradation" to be accommodated within the above parameter BandPixels.
        /// This is just a number, and has no units.
        /// </summary>
        public int NumberSteps { get; set; }

        /// <summary>
        /// X Offset of the centre of rotation in terms of percentage with respect to half the 
        /// width of the image.
        /// </summary>
        public int Xcentre { get; set; }

        /// <summary>
        /// Y Offset of the centre of rotation in terms of percentage with respect to half the 
        /// height of the image.
        /// </summary>
        public int Ycentre { get; set; }

        /// <summary>
        /// Border Colour of the vignette. We consider only R, G, B values here. Alpha value is ignored.
        /// </summary>
        public Color BorderColour { get; set; }

        /// <summary>
        /// Shape of the Vignette - one of Circle, Ellipse, Diamond, Rectangle or Square.
        /// </summary>
        public VignetteShape Shape { get; set; }

        /// <summary>
        /// Name of the file for saving.
        /// </summary>
        public string FileNameToSave { get; set; }

        /// <summary>
        /// Method to transfer pixels from the main window to the vignette class.
        /// </summary>
        public void TransferImagePixels(
            ref List<byte> redOrig, ref List<byte> greenOrig, ref List<byte> blueOrig,
            int wid, int hei,
            ref List<byte> redModified, ref List<byte> greenModified, ref List<byte> blueModified, 
            ModeOfOperation modeOfOperation)
        {
            pixRedOrig = redOrig;
            pixGreenOrig = greenOrig;
            pixBlueOrig = blueOrig;
            width = wid;
            height = hei;
            pixRedModified = redModified;
            pixGreenModified = greenModified;
            pixBlueModified = blueModified;
            mode = modeOfOperation;
        }

        /// <summary>
        /// Method to apply the vignette.
        /// </summary>
        public void ApplyEffect()
        {
            SetupParameters();
            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse ||
                Shape == VignetteShape.Diamond)
                ApplyEffectCircleEllipseDiamond();
            else // if (Shape == VignetteShape.Rectangle || Shape == VignetteShape.Square)
                ApplyEffectRectangleSquare();

            if (mode == ModeOfOperation.DisplayMode) // Send back the pixels to display the image.
            {                
                mainWin.UpdateImage(ref pixRedModified, ref pixGreenModified, ref pixBlueModified);
            }
            else // if (mode == ModeOfOperation.SaveMode) // Save the image onto the specified file.
            {
                SaveImage();
            }
        }

        /// <summary>
        /// Set up the different parameters.
        /// </summary>
        private void SetupParameters()
        {
            aVals.Clear();
            bVals.Clear();
            aValsMidPoints.Clear();
            bValsMidPoints.Clear();
            weight1.Clear();
            weight2.Clear();

            double a0, b0, aLast, bLast, aEll, bEll;
            double stepSize = BandPixels * 1.0 / NumberSteps;
            double bandPixelsBy2 = 0.5 * BandPixels;
            double arguFactor = Math.PI / BandPixels;
            double vignetteWidth = width * Coverage / 100.0;
            double vignetteHeight = height * Coverage / 100.0;
            double vwb2 = vignetteWidth * 0.5;
            double vhb2 = vignetteHeight * 0.5;
            a0 = vwb2 - bandPixelsBy2;
            b0 = vhb2 - bandPixelsBy2;

            // For a circle or square, both 'major' and 'minor' axes are identical
            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Square)
            {
                a0 = Math.Min(a0, b0);
                b0 = a0;
            }

            if (Shape == VignetteShape.Circle || Shape == VignetteShape.Ellipse ||
                Shape == VignetteShape.Rectangle || Shape == VignetteShape.Square)
            {
                for (int i = 0; i <= NumberSteps; ++i)
                {
                    aEll = a0 + stepSize * i;
                    bEll = b0 + stepSize * i;
                    aVals.Add(aEll);
                    bVals.Add(bEll);
                }
                for (int i = 0; i < NumberSteps; ++i)
                {
                    aEll = a0 + stepSize * (i + 0.5);
                    bEll = b0 + stepSize * (i + 0.5);
                    aValsMidPoints.Add(aEll);
                    bValsMidPoints.Add(bEll);
                }
            }
            else// if (Shape == VignetteShape.Diamond)
            {
                aLast = vwb2 + bandPixelsBy2;
                bLast = b0 * aLast / a0;
                double stepXdiamond = (aLast - a0) / NumberSteps;
                double stepYdiamond = (bLast - b0) / NumberSteps;

                for (int i = 0; i <= NumberSteps; ++i)
                {
                    aEll = a0 + stepXdiamond * i;
                    bEll = b0 + stepYdiamond * i;
                    aVals.Add(aEll);
                    bVals.Add(bEll);
                }
                for (int i = 0; i <= NumberSteps; ++i)
                {
                    aEll = a0 + stepXdiamond * (i + 0.5);
                    bEll = b0 + stepYdiamond * (i + 0.5);
                    aValsMidPoints.Add(aEll);
                    bValsMidPoints.Add(bEll);
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
            double wei1, wei2, arg, argCosVal;
            for (int i = 0; i < NumberSteps; ++i)
            {
                arg = arguFactor * (aValsMidPoints[i] - a0);
                argCosVal = Math.Cos(arg);
                wei1 = 0.5 * (1.0 + argCosVal);
                wei2 = 0.5 * (1.0 - argCosVal);
                weight1.Add(wei1);
                weight2.Add(wei2);
            }
        }

        /// <summary>
        /// Method to apply the Circular, Elliptical or Diamond-shaped vignette on an image.
        /// </summary>
        private void ApplyEffectCircleEllipseDiamond()
        {
            int k, el, w1, w2;
            byte r, g, b;
            double wb2 = width * 0.5 + Xcentre * width * geometryFactor;
            double hb2 = height * 0.5 + Ycentre * height * geometryFactor;
            double thetaRadians = Angle * Math.PI / 180.0;
            double cos = Math.Cos(thetaRadians);
            double sin = Math.Sin(thetaRadians);
            double xprime, yprime, potential1, potential2, potential;
            double factor1, factor2, factor3, factor4;
            byte redBorder = BorderColour.R;
            byte greenBorder = BorderColour.G;
            byte blueBorder = BorderColour.B;

            // Loop over the number of pixels
            for (el = 0; el < height; ++el)
            {
                w2 = width * el;
                for (k = 0; k < width; ++k)
                {
                    // This is the usual rotation formula, along with translation.
                    // I could have perhaps used the Transform feature of WPF.
                    xprime = (k - wb2) * cos + (el - hb2) * sin;
                    yprime = -(k - wb2) * sin + (el - hb2) * cos;

                    factor1 = 1.0 * Math.Abs(xprime) / aVals[0];
                    factor2 = 1.0 * Math.Abs(yprime) / bVals[0];
                    factor3 = 1.0 * Math.Abs(xprime) / aVals[NumberSteps];
                    factor4 = 1.0 * Math.Abs(yprime) / bVals[NumberSteps];

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
                    w1 = w2 + k;

                    if (potential1 <= 0.0)
                    {
                        // Point is within the inner circle / ellipse / diamond
                        r = pixRedOrig[w1];
                        g = pixGreenOrig[w1];
                        b = pixBlueOrig[w1];
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
                        int j, j1;

                        for (j = 1; j < NumberSteps; ++j)
                        {
                            factor1 = Math.Abs(xprime) / aVals[j];
                            factor2 = Math.Abs(yprime) / bVals[j];

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
                        j1 = j - 1;
                        // The formulas where the weights are applied to the image, and border.
                        r = (byte)(pixRedOrig[w1] * weight1[j1] + redBorder * weight2[j1]);
                        g = (byte)(pixGreenOrig[w1] * weight1[j1] + greenBorder * weight2[j1]);
                        b = (byte)(pixBlueOrig[w1] * weight1[j1] + blueBorder * weight2[j1]);
                    }
                    pixRedModified[w1] = r;
                    pixGreenModified[w1] = g;
                    pixBlueModified[w1] = b;
                }
            }
        }

        /// <summary>
        /// Method to apply the Rectangular or Square-shaped vignette on an image.
        /// </summary>
        private void ApplyEffectRectangleSquare()
        {
            Rect rect1 = new Rect(), rect2 = new Rect(), rect3 = new Rect();
            Point point = new Point();
            int k, el, w1, w2;
            byte r, g, b;

            double wb2 = width * 0.5 + Xcentre * width * geometryFactor;
            double hb2 = height * 0.5 + Ycentre * height * geometryFactor;
            double thetaRadians = Angle * Math.PI / 180.0;
            double cos = Math.Cos(thetaRadians);
            double sin = Math.Sin(thetaRadians);
            double xprime, yprime, potential;
            byte redBorder = BorderColour.R;
            byte greenBorder = BorderColour.G;
            byte blueBorder = BorderColour.B;

            rect1.X = 0.0;
            rect1.Y = 0.0;
            rect1.Width = aVals[0];
            rect1.Height = bVals[0];
            rect2.X = 0.0;
            rect2.Y = 0.0;
            rect2.Width = aVals[NumberSteps];
            rect2.Height = bVals[NumberSteps];

            for (el = 0; el < height; ++el)
            {
                w2 = width * el;
                for (k = 0; k < width; ++k)
                {
                    // The usual rotation-translation formula
                    xprime = (k - wb2) * cos + (el - hb2) * sin;
                    yprime = -(k - wb2) * sin + (el - hb2) * cos;

                    potential = 0.0;
                    point.X = Math.Abs(xprime);
                    point.Y = Math.Abs(yprime);

                    // For a rectangle, we can use the Rect.Contains(Point) method to determine
                    //  whether the point is in the rectangle or not
                    if (rect1.Contains(point))
                        potential = -2.0; // Arbitrary negative number N1

                    if (!rect2.Contains(point))
                        potential = 2.0; // Arbitrary positive number = - N1

                    w1 = w2 + k;

                    if (potential < -1.0) // Arbitrary negative number, greater than N1
                    {
                        // Point is within the inner square / rectangle,
                        r = pixRedOrig[w1];
                        g = pixGreenOrig[w1];
                        b = pixBlueOrig[w1];
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
                        int j, j1;

                        for (j = 1; j < NumberSteps; ++j)
                        {
                            rect3.X = 0.0;
                            rect3.Y = 0.0;
                            rect3.Width = aVals[j];
                            rect3.Height = bVals[j];

                            if (rect3.Contains(point))
                                break;
                        }
                        j1 = j - 1;
                        r = (byte)(pixRedOrig[w1] * weight1[j1] + redBorder * weight2[j1]);
                        g = (byte)(pixGreenOrig[w1] * weight1[j1] + greenBorder * weight2[j1]);
                        b = (byte)(pixBlueOrig[w1] * weight1[j1] + blueBorder * weight2[j1]);
                    }
                    pixRedModified[w1] = r;
                    pixGreenModified[w1] = g;
                    pixBlueModified[w1] = b;
                }
            }
        }

        /// <summary>
        /// Function to save the modified image onto a file
        /// </summary>
        private void SaveImage()
        {
            // First, create the image to be saved
            int bitsPerPixel = 24, i1;
            int stride = (width * bitsPerPixel + 7) / 8;
            byte[] pixelsToWrite = new byte[stride * height];

            for (int i = 0; i < pixelsToWrite.Count(); i += 3)
            {
                i1 = i / 3;
                pixelsToWrite[i] = pixRedModified[i1];
                pixelsToWrite[i + 1] = pixGreenModified[i1];
                pixelsToWrite[i + 2] = pixBlueModified[i1];
            }

            BitmapSource imageToSave = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Rgb24,
                null, pixelsToWrite, stride);

            // Then, save the image
            string extn = Path.GetExtension(FileNameToSave);
            FileStream fs = new FileStream(FileNameToSave, FileMode.Create);
            if (extn == ".png")
            {
                // Save as PNG
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(imageToSave));
                encoder.Save(fs);
            }
            else if (extn == ".jpg")
            {
                // Save as JPG
                BitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(imageToSave));
                encoder.Save(fs);
            }
            else // if (extn == "bmp")
            {
                // Save as BMP
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(imageToSave));
                encoder.Save(fs);
            }
            fs.Close();
        }
    }
}
