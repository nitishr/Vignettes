using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Samples.CustomControls;
using System.IO;
using Microsoft.Win32;

// Program to "vignettify" an image, for circular, elliptical, diamond, rectangular
//  and square-shaped vignettes.
// Written by Amarnath S, Bengaluru, India. April 2011.
// Version 1.0, April 2011.

namespace Vignettes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Lists of red, green and blue pixels in original image (unscaled).
        List<byte> pixels8Red;
        List<byte> pixels8Green;
        List<byte> pixels8Blue;

        // Lists of red, green and blue pixels in modified image (unscaled).
        List<byte> pixels8RedModified;
        List<byte> pixels8GreenModified;
        List<byte> pixels8BlueModified;

        // Lists of red, green and blue pixels in original image (scaled).
        List<byte> pixels8RedScaled;
        List<byte> pixels8GreenScaled;
        List<byte> pixels8BlueScaled;

        // Lists of red, green and blue pixels in modified image (scaled).
        List<byte> pixels8RedScaledModified;
        List<byte> pixels8GreenScaledModified;
        List<byte> pixels8BlueScaledModified;

        BitmapSource originalImage;      // Bitmap for the original image.
        BitmapSource newImage;           // Bitmap for the scaled image.
        TransformedBitmap scaledImage;   // Bitmap for the scaled image.

        // Tried to use a List<byte> for this, but found that BitmapSource.CopyPixels does not take 
        // this data type as one of its arguments.
        byte[] originalPixels; 
        byte[] scaledPixels;

        int originalWidth, originalHeight;
        int viewportWidthHeight = 600;
        int scaledWidth;
        int scaledHeight;
        string fileName;

        VignetteEffect vignette;
        VignetteShape shape;
        Color colour;              // Border colour
        byte red, green, blue;
        double scaleFactor;

        public MainWindow()
        {
            InitializeComponent();
            pixels8Red = new List<byte>();
            pixels8Green = new List<byte>();
            pixels8Blue = new List<byte>();

            pixels8RedModified = new List<byte>();
            pixels8GreenModified = new List<byte>();
            pixels8BlueModified = new List<byte>();

            pixels8RedScaled = new List<byte>();
            pixels8GreenScaled = new List<byte>();
            pixels8BlueScaled = new List<byte>();

            pixels8RedScaledModified = new List<byte>();
            pixels8GreenScaledModified = new List<byte>();
            pixels8BlueScaledModified = new List<byte>();

             scaleFactor = 1.0;

           // Magic numbers to represent the starting colour - predominantly blue
            red = 20; 
            green = 20;
            blue = 240;
            colour = new Color();
            colour = Color.FromRgb(red, green, blue);
            bnColour.Background = new SolidColorBrush(colour);

            comboTechnique.SelectedIndex = 1; // Select the ellipse shape
            vignette = null;
        }

        /// <summary>
        /// Method to read in an image.
        /// </summary>
        private bool ReadImage(string fn, string fileNameOnly)
        {
            bool retVal = false;
            // Open the image
            Uri imageUri = new Uri(fn, UriKind.RelativeOrAbsolute);
            originalImage = new BitmapImage(imageUri);
            int stride = (originalImage.PixelWidth * originalImage.Format.BitsPerPixel + 7) / 8;
            originalWidth = originalImage.PixelWidth;
            originalHeight = originalImage.PixelHeight;

            if ((originalImage.Format == PixelFormats.Bgra32) ||
                (originalImage.Format == PixelFormats.Bgr32))
            {
                originalPixels = new byte[stride * originalHeight];
                // Read in pixel values from the image
                originalImage.CopyPixels(Int32Rect.Empty, originalPixels, stride, 0);
                Title = "Vignette Effect: " + fileNameOnly;
                retVal = true;
            }
            else
            {
                MessageBox.Show("Sorry, I don't support this image format.");
            }

            return retVal;
        }

        /// <summary>
        /// Method to scale the original image to 600 x 600.
        /// This should preserve the aspect ratio of the original image.
        /// </summary>
        void ScaleImage()
        {
            double fac1 = Convert.ToDouble(scaledWidth / (1.0 * originalWidth));
            double fac2 = Convert.ToDouble(scaledHeight / (1.0 * originalHeight));
            scaleFactor = Math.Min(fac1, fac2);
            var scale = new ScaleTransform(fac1, fac2);
            scaledImage = new TransformedBitmap(originalImage, scale);

            img.Source = scaledImage;

            int stride = (scaledImage.PixelWidth * scaledImage.Format.BitsPerPixel + 7) / 8;
            scaledPixels = new byte[stride * scaledHeight];

            // Update the array scaledPixels from the scaled image
            scaledImage.CopyPixels(Int32Rect.Empty, scaledPixels, stride, 0);
        }

        /// <summary>
        /// Computes the scaled width and height of the image, so as to 
        /// maintain the aspect ratio of original image.
        /// </summary>
        private void ComputeScaledWidthAndHeight()
        {
            if (originalWidth > originalHeight)
            {
                scaledWidth = viewportWidthHeight;
                scaledHeight = originalHeight * viewportWidthHeight / originalWidth;
            }
            else
            {
                scaledHeight = viewportWidthHeight;
                scaledWidth = originalWidth * viewportWidthHeight / originalHeight;
            }
        }

        /// <summary>
        /// Method to fill in the different pixel arrays from the original and scaled images.
        /// </summary>
        private void PopulatePixelsOriginalAndScaled()
        {
            int bitsPerPixel = originalImage.Format.BitsPerPixel;

            if (bitsPerPixel == 24 || bitsPerPixel == 32)
            {
                byte red, green, blue;

                pixels8Red.Clear();
                pixels8Green.Clear();
                pixels8Blue.Clear();

                pixels8RedModified.Clear();
                pixels8GreenModified.Clear();
                pixels8BlueModified.Clear();

                pixels8RedScaled.Clear();
                pixels8GreenScaled.Clear();
                pixels8BlueScaled.Clear();

                pixels8RedScaledModified.Clear();
                pixels8GreenScaledModified.Clear();
                pixels8BlueScaledModified.Clear();                

                // Populate the Red, Green and Blue lists.
                if (bitsPerPixel == 24) // 24 bits per pixel
                {
                    for (int i = 0; i < scaledPixels.Count(); i += 3)
                    {
                        // In a 24-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red order.
                        blue = (byte)(scaledPixels[i]);
                        green = (byte)(scaledPixels[i + 1]);
                        red = (byte)(scaledPixels[i + 2]);

                        pixels8RedScaled.Add(red);
                        pixels8GreenScaled.Add(green);
                        pixels8BlueScaled.Add(blue);

                        pixels8RedScaledModified.Add(red);
                        pixels8GreenScaledModified.Add(green);
                        pixels8BlueScaledModified.Add(blue);
                    }

                    for (int i = 0; i < originalPixels.Count(); i += 3)
                    {
                        // In a 24-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red order.
                        blue = (byte)(originalPixels[i]);
                        green = (byte)(originalPixels[i + 1]);
                        red = (byte)(originalPixels[i + 2]);

                        pixels8Red.Add(red);
                        pixels8Green.Add(green);
                        pixels8Blue.Add(blue);

                        pixels8RedModified.Add(red);
                        pixels8GreenModified.Add(green);
                        pixels8BlueModified.Add(blue);
                    }
                }
                if (bitsPerPixel == 32) // 32 bits per pixel
                {
                    for (int i = 0; i < scaledPixels.Count(); i += 4)
                    {
                        // In a 32-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red Alpha order.
                        blue = (byte)(scaledPixels[i]);
                        green = (byte)(scaledPixels[i + 1]);
                        red = (byte)(scaledPixels[i + 2]);

                        pixels8RedScaled.Add(red);
                        pixels8GreenScaled.Add(green);
                        pixels8BlueScaled.Add(blue);

                        pixels8RedScaledModified.Add(red);
                        pixels8GreenScaledModified.Add(green);
                        pixels8BlueScaledModified.Add(blue);
                    }

                    for (int i = 0; i < originalPixels.Count(); i += 4)
                    {
                        // In a 32-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red Alpha order.
                        blue = (byte)(originalPixels[i]);
                        green = (byte)(originalPixels[i + 1]);
                        red = (byte)(originalPixels[i + 2]);

                        pixels8Red.Add(red);
                        pixels8Green.Add(green);
                        pixels8Blue.Add(blue);

                        pixels8RedModified.Add(red);
                        pixels8GreenModified.Add(green);
                        pixels8BlueModified.Add(blue);
                    }
                }
            }
        }

        private void bnOpen_Click(object sender, RoutedEventArgs e)
        {
            // Read in the image
            // Scale the image to 600 x 600, maintaining aspect ratio
            // Populate the lists pixels8RedScaled, pixels8GreenScaled, pixels8BlueScaled
            OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter =
                "All Image Files(*.bmp;*.png;*.tif;*.jpg)|*.bmp;*.png;*.tif;*.jpg|24-Bit Bitmap(*.bmp)|*.bmp|PNG(*.png)|*.png|TIFF(*.tif)|*.tif|JPEG(*.jpg)|*.jpg";
            Nullable<bool> result = ofd.ShowDialog();

            try
            {
                if (result == true)
                {
                    fileName = ofd.FileName;
                    Mouse.OverrideCursor = Cursors.Wait;
                    if (ReadImage(fileName, ofd.SafeFileName) == true)
                    {
                        bnSaveImage.IsEnabled = true;
                        ComputeScaledWidthAndHeight();
                        ScaleImage();
                        PopulatePixelsOriginalAndScaled();
                        ApplyVignette();
                    }
                    else
                    {
                        MessageBox.Show("Sorry, I'm unable to open this image!");
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Sorry, this does not seem to be an image. Please open an image!");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Method to apply the vignette
        /// </summary>
        private void ApplyVignette()
        {
            vignette = new VignetteEffect(this);
            vignette.Angle = sliderAngle.Value;
            vignette.Coverage = sliderPercent.Value;
            vignette.BandPixels = Convert.ToInt32(sliderBand.Value);
            vignette.NumberSteps = Convert.ToInt32(sliderSteps.Value);
            vignette.Xcentre = Convert.ToInt32(sliderOriginX.Value);
            vignette.Ycentre = Convert.ToInt32(sliderOriginY.Value);
            vignette.BorderColour = colour;
            vignette.Shape = shape;
            vignette.TransferImagePixels(ref pixels8RedScaled, ref pixels8GreenScaled, ref pixels8BlueScaled,
                    scaledWidth, scaledHeight,
                    ref pixels8RedScaledModified, ref pixels8GreenScaledModified, ref pixels8BlueScaledModified,
                    ModeOfOperation.DisplayMode);
            vignette.ApplyEffect();
        }

        /// <summary>
        /// Method to update the image. The Vignette class computes the modified colours and transfers
        /// these colours to the main window.
        /// </summary>
        public void UpdateImage(ref List<byte> pixels8RedScaledModified,
            ref List<byte> pixels8GreenScaledModified, 
            ref List<byte> pixels8BlueScaledModified)
        {
            int bitsPerPixel = 24;
            int stride = (scaledWidth * bitsPerPixel + 7) / 8;
            byte[] pixelsToWrite = new byte[stride * scaledHeight];
            int i1;

            for (int i = 0; i < pixelsToWrite.Count(); i += 3)
            {
                i1 = i / 3;
                pixelsToWrite[i] = pixels8RedScaledModified[i1];
                pixelsToWrite[i + 1] = pixels8GreenScaledModified[i1];
                pixelsToWrite[i + 2] = pixels8BlueScaledModified[i1];
            }

            newImage = BitmapSource.Create(scaledWidth, scaledHeight, 96, 96, PixelFormats.Rgb24,
                null, pixelsToWrite, stride);
            img.Source = newImage;
        }

        private void comboTechnique_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            sliderAngle.IsEnabled = true;
            if (comboTechnique.SelectedIndex == 0)
            {
                shape = VignetteShape.Circle;
                sliderAngle.IsEnabled = false;
            }
            else if (comboTechnique.SelectedIndex == 1)
            {
                shape = VignetteShape.Ellipse;
            }
            else if (comboTechnique.SelectedIndex == 2)
            {
                shape = VignetteShape.Diamond;
            }
            else if (comboTechnique.SelectedIndex == 3)
            {
                shape = VignetteShape.Square;
            }
            else //if(comboTechnique.SelectedIndex == 4)
            {
                shape = VignetteShape.Rectangle;
            }

            if (vignette != null)
            {
                vignette.Shape = shape;
                vignette.ApplyEffect();
            }
        }

        private void bnColour_Click(object sender, RoutedEventArgs e)
        {
            // ColorPickerDialog comes from ColorPicker.dll, which comes from the Microsoft stable.
            // I know that CodeProject has some colour picker dialogs available, but I choose the 
            // plain vanilla one from Microsoft, since our focus in this code is on providing a 
            // vignette effect, and not a fancy colour dialog application.
            ColorPickerDialog cPicker = new ColorPickerDialog();
            cPicker.WindowStartupLocation = WindowStartupLocation.Manual;
            cPicker.Top = this.Top + 200;
            cPicker.Left = this.Width;

            // I deliberately set the height to be this, so as not to show the Opacity and other 
            // colour text boxes in the dialog. I currently don't have the facility to change the 
            // opacity of the image, and therefore I don't want users to change the opacity, and hence
            // report a bug saying that "opacity is not working". So, I don't show the opacity button at all
            // and further, I don't allow this colour picker dialog to be resized.
            cPicker.Height = 169; 
            cPicker.ResizeMode = ResizeMode.NoResize;
            cPicker.StartingColor = colour;
            cPicker.Owner = this;

            bool? dialogResult = cPicker.ShowDialog();
            if (dialogResult != null && (bool)dialogResult == true)
            {
                colour = cPicker.SelectedColor;
                bnColour.Background = new SolidColorBrush(colour);

                if (vignette != null)
                {
                    vignette.BorderColour = colour;
                    vignette.ApplyEffect();
                }
            }
        }

        private void sliderAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if( vignette != null )
            {
                vignette.Angle = sliderAngle.Value;
                vignette.ApplyEffect();
            }
        }

        private void sliderPercent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (vignette != null)
            {
                vignette.Coverage = sliderPercent.Value;
                vignette.ApplyEffect();
            }
        }

        private void sliderBand_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (vignette != null)
            {
                vignette.BandPixels = Convert.ToInt32(sliderBand.Value);
                vignette.ApplyEffect();
            }
        }

        private void sliderOriginX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (vignette != null)
            {
                vignette.Xcentre = Convert.ToInt32(sliderOriginX.Value);
                vignette.ApplyEffect();
            }
        }

        private void sliderOriginY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (vignette != null)
            {
                vignette.Ycentre = Convert.ToInt32(sliderOriginY.Value);
                vignette.ApplyEffect();
            }
        }

        private void sliderSteps_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (vignette != null)
            {
                vignette.NumberSteps = Convert.ToInt32(sliderSteps.Value);
                vignette.ApplyEffect();
            }
        }

        private void bnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PNG Images (.png)|*.png|JPG Images (.jpg)|*.jpg|BMP Images (.bmp)|*.bmp";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            try
            {
                // Process save file dialog box results
                if (result == true)
                {
                    // Save image
                    VignetteEffect vig = new VignetteEffect(this);
                    vig.Angle = sliderAngle.Value;
                    vig.Coverage = sliderPercent.Value;
                    // While all other parameters are percentages, and are independent of image 
                    // dimensions, two parameters - Width of the band, and Number of Steps need 
                    // to be scaled depending upon the image dimensions.
                    // Here, the variable scaleFactor comes in handy to perform such scaling.
                    // Though scaleFactor can never be zero, we enclose the entire saving code 
                    // within a try-catch block, just in case things go out of control.
                    vig.BandPixels = Convert.ToInt32(sliderBand.Value / scaleFactor);
                    vig.NumberSteps = Convert.ToInt32(sliderSteps.Value / scaleFactor);
                    vig.Xcentre = Convert.ToInt32(sliderOriginX.Value);
                    vig.Ycentre = Convert.ToInt32(sliderOriginY.Value);
                    vig.BorderColour = colour;
                    vig.Shape = shape;
                    string fileToSave = dlg.FileName;
                    // I don't want the original file to be overwritten, since the vignetting operation
                    // is a lossy one (where some pixels of the original image may be lost).
                    // Therefore, if the user inadvertently selects the original filename for saving,
                    // I create the new file name with an underscore _ appended to the filename.
                    if (fileToSave == fileName)
                    {
                        fileToSave = GetNewFileName(fileToSave);
                    }
                    vig.FileNameToSave = fileToSave;

                    Mouse.OverrideCursor = Cursors.Wait;
                    vig.TransferImagePixels(ref pixels8Red, ref pixels8Green, ref pixels8Blue,
                            originalWidth, originalHeight,
                            ref pixels8RedModified, ref pixels8GreenModified, ref pixels8BlueModified,
                            ModeOfOperation.SaveMode);
                    vig.ApplyEffect();                    
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Function to compute the new filename with _ appended to the original filename.
        /// </summary>
        /// <param name="fileForSaving">Old file name</param>
        /// <returns>New file name</returns>
        private string GetNewFileName(string fileForSaving)
        {            
            string fileOnly = Path.GetFileNameWithoutExtension(fileForSaving);
            string extension = Path.GetExtension(fileForSaving);
            string newFileName = fileOnly + "_";
            newFileName += extension;
            return newFileName;
        }
    }
}
