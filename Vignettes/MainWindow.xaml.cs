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
    public partial class MainWindow
    {
        private const int ViewportWidthHeight = 600;

        List<byte> _pixels8Red = new List<byte>();
        List<byte> _pixels8Green = new List<byte>();
        List<byte> _pixels8Blue = new List<byte>();

        List<byte> _pixels8RedModified = new List<byte>();
        List<byte> _pixels8GreenModified = new List<byte>();
        List<byte> _pixels8BlueModified = new List<byte>();

        List<byte> _pixels8RedScaled = new List<byte>();
        List<byte> _pixels8GreenScaled = new List<byte>();
        List<byte> _pixels8BlueScaled = new List<byte>();

        List<byte> _pixels8RedScaledModified = new List<byte>();
        List<byte> _pixels8GreenScaledModified = new List<byte>();
        List<byte> _pixels8BlueScaledModified = new List<byte>();

        BitmapSource _originalImage;
        BitmapSource _newImage;
        TransformedBitmap _scaledImage;

        // Tried to use a List<byte> for this, but found that BitmapSource.CopyPixels does not take 
        // this data type as one of its arguments.
        byte[] _originalPixels;
        byte[] _scaledPixels;

        int _originalWidth;
        int _originalHeight;
        int _scaledWidth;
        int _scaledHeight;
        string _fileName;

        VignetteEffect _vignette;
        VignetteShape _shape;

        // Magic numbers to represent the starting colour - predominantly blue
        Color _borderColor = Color.FromRgb(20, 20, 240);

        double _scaleFactor = 1.0;

        public MainWindow()
        {
            InitializeComponent();
            bnColour.Background = new SolidColorBrush(_borderColor);
            comboTechnique.SelectedIndex = 1; // Select the ellipse shape
            _vignette = null;
        }

        /// <summary>
        /// Method to read in an image.
        /// </summary>
        private bool ReadImage(string fn, string fileNameOnly)
        {
            bool retVal = false;
            // Open the image
            Uri imageUri = new Uri(fn, UriKind.RelativeOrAbsolute);
            _originalImage = new BitmapImage(imageUri);
            int stride = (_originalImage.PixelWidth * _originalImage.Format.BitsPerPixel + 7) / 8;
            _originalWidth = _originalImage.PixelWidth;
            _originalHeight = _originalImage.PixelHeight;

            if ((_originalImage.Format == PixelFormats.Bgra32) ||
                (_originalImage.Format == PixelFormats.Bgr32))
            {
                _originalPixels = new byte[stride * _originalHeight];
                // Read in pixel values from the image
                _originalImage.CopyPixels(Int32Rect.Empty, _originalPixels, stride, 0);
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
            double fac1 = Convert.ToDouble(_scaledWidth / (1.0 * _originalWidth));
            double fac2 = Convert.ToDouble(_scaledHeight / (1.0 * _originalHeight));
            _scaleFactor = Math.Min(fac1, fac2);
            var scale = new ScaleTransform(fac1, fac2);
            _scaledImage = new TransformedBitmap(_originalImage, scale);

            img.Source = _scaledImage;

            int stride = (_scaledImage.PixelWidth * _scaledImage.Format.BitsPerPixel + 7) / 8;
            _scaledPixels = new byte[stride * _scaledHeight];

            // Update the array scaledPixels from the scaled image
            _scaledImage.CopyPixels(Int32Rect.Empty, _scaledPixels, stride, 0);
        }

        /// <summary>
        /// Computes the scaled width and height of the image, so as to 
        /// maintain the aspect ratio of original image.
        /// </summary>
        private void ComputeScaledWidthAndHeight()
        {
            if (_originalWidth > _originalHeight)
            {
                _scaledWidth = ViewportWidthHeight;
                _scaledHeight = _originalHeight * ViewportWidthHeight / _originalWidth;
            }
            else
            {
                _scaledHeight = ViewportWidthHeight;
                _scaledWidth = _originalWidth * ViewportWidthHeight / _originalHeight;
            }
        }

        /// <summary>
        /// Method to fill in the different pixel arrays from the original and scaled images.
        /// </summary>
        private void PopulatePixelsOriginalAndScaled()
        {
            int bitsPerPixel = _originalImage.Format.BitsPerPixel;

            if (bitsPerPixel == 24 || bitsPerPixel == 32)
            {
                byte red, green, blue;

                _pixels8Red.Clear();
                _pixels8Green.Clear();
                _pixels8Blue.Clear();

                _pixels8RedModified.Clear();
                _pixels8GreenModified.Clear();
                _pixels8BlueModified.Clear();

                _pixels8RedScaled.Clear();
                _pixels8GreenScaled.Clear();
                _pixels8BlueScaled.Clear();

                _pixels8RedScaledModified.Clear();
                _pixels8GreenScaledModified.Clear();
                _pixels8BlueScaledModified.Clear();                

                // Populate the Red, Green and Blue lists.
                if (bitsPerPixel == 24) // 24 bits per pixel
                {
                    for (int i = 0; i < _scaledPixels.Count(); i += 3)
                    {
                        // In a 24-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red order.
                        blue = (byte)(_scaledPixels[i]);
                        green = (byte)(_scaledPixels[i + 1]);
                        red = (byte)(_scaledPixels[i + 2]);

                        _pixels8RedScaled.Add(red);
                        _pixels8GreenScaled.Add(green);
                        _pixels8BlueScaled.Add(blue);

                        _pixels8RedScaledModified.Add(red);
                        _pixels8GreenScaledModified.Add(green);
                        _pixels8BlueScaledModified.Add(blue);
                    }

                    for (int i = 0; i < _originalPixels.Count(); i += 3)
                    {
                        // In a 24-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red order.
                        blue = (byte)(_originalPixels[i]);
                        green = (byte)(_originalPixels[i + 1]);
                        red = (byte)(_originalPixels[i + 2]);

                        _pixels8Red.Add(red);
                        _pixels8Green.Add(green);
                        _pixels8Blue.Add(blue);

                        _pixels8RedModified.Add(red);
                        _pixels8GreenModified.Add(green);
                        _pixels8BlueModified.Add(blue);
                    }
                }
                if (bitsPerPixel == 32) // 32 bits per pixel
                {
                    for (int i = 0; i < _scaledPixels.Count(); i += 4)
                    {
                        // In a 32-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red Alpha order.
                        blue = (byte)(_scaledPixels[i]);
                        green = (byte)(_scaledPixels[i + 1]);
                        red = (byte)(_scaledPixels[i + 2]);

                        _pixels8RedScaled.Add(red);
                        _pixels8GreenScaled.Add(green);
                        _pixels8BlueScaled.Add(blue);

                        _pixels8RedScaledModified.Add(red);
                        _pixels8GreenScaledModified.Add(green);
                        _pixels8BlueScaledModified.Add(blue);
                    }

                    for (int i = 0; i < _originalPixels.Count(); i += 4)
                    {
                        // In a 32-bit per pixel image, the bytes are stored in the order 
                        // BGR - Blue Green Red Alpha order.
                        blue = (byte)(_originalPixels[i]);
                        green = (byte)(_originalPixels[i + 1]);
                        red = (byte)(_originalPixels[i + 2]);

                        _pixels8Red.Add(red);
                        _pixels8Green.Add(green);
                        _pixels8Blue.Add(blue);

                        _pixels8RedModified.Add(red);
                        _pixels8GreenModified.Add(green);
                        _pixels8BlueModified.Add(blue);
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
                    _fileName = ofd.FileName;
                    Mouse.OverrideCursor = Cursors.Wait;
                    if (ReadImage(_fileName, ofd.SafeFileName) == true)
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
            _vignette = new VignetteEffect(this);
            _vignette.Angle = sliderAngle.Value;
            _vignette.Coverage = sliderPercent.Value;
            _vignette.BandPixels = Convert.ToInt32(sliderBand.Value);
            _vignette.NumberSteps = Convert.ToInt32(sliderSteps.Value);
            _vignette.Xcentre = Convert.ToInt32(sliderOriginX.Value);
            _vignette.Ycentre = Convert.ToInt32(sliderOriginY.Value);
            _vignette.BorderColour = _borderColor;
            _vignette.Shape = _shape;
            _vignette.TransferImagePixels(ref _pixels8RedScaled, ref _pixels8GreenScaled, ref _pixels8BlueScaled,
                    _scaledWidth, _scaledHeight,
                    ref _pixels8RedScaledModified, ref _pixels8GreenScaledModified, ref _pixels8BlueScaledModified,
                    ModeOfOperation.DisplayMode);
            _vignette.ApplyEffect();
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
            int stride = (_scaledWidth * bitsPerPixel + 7) / 8;
            byte[] pixelsToWrite = new byte[stride * _scaledHeight];
            int i1;

            for (int i = 0; i < pixelsToWrite.Count(); i += 3)
            {
                i1 = i / 3;
                pixelsToWrite[i] = pixels8RedScaledModified[i1];
                pixelsToWrite[i + 1] = pixels8GreenScaledModified[i1];
                pixelsToWrite[i + 2] = pixels8BlueScaledModified[i1];
            }

            _newImage = BitmapSource.Create(_scaledWidth, _scaledHeight, 96, 96, PixelFormats.Rgb24,
                null, pixelsToWrite, stride);
            img.Source = _newImage;
        }

        private void comboTechnique_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            sliderAngle.IsEnabled = true;
            if (comboTechnique.SelectedIndex == 0)
            {
                _shape = VignetteShape.Circle;
                sliderAngle.IsEnabled = false;
            }
            else if (comboTechnique.SelectedIndex == 1)
            {
                _shape = VignetteShape.Ellipse;
            }
            else if (comboTechnique.SelectedIndex == 2)
            {
                _shape = VignetteShape.Diamond;
            }
            else if (comboTechnique.SelectedIndex == 3)
            {
                _shape = VignetteShape.Square;
            }
            else //if(comboTechnique.SelectedIndex == 4)
            {
                _shape = VignetteShape.Rectangle;
            }

            if (_vignette != null)
            {
                _vignette.Shape = _shape;
                _vignette.ApplyEffect();
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
            cPicker.StartingColor = _borderColor;
            cPicker.Owner = this;

            bool? dialogResult = cPicker.ShowDialog();
            if (dialogResult != null && (bool)dialogResult == true)
            {
                _borderColor = cPicker.SelectedColor;
                bnColour.Background = new SolidColorBrush(_borderColor);

                if (_vignette != null)
                {
                    _vignette.BorderColour = _borderColor;
                    _vignette.ApplyEffect();
                }
            }
        }

        private void sliderAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if( _vignette != null )
            {
                _vignette.Angle = sliderAngle.Value;
                _vignette.ApplyEffect();
            }
        }

        private void sliderPercent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette != null)
            {
                _vignette.Coverage = sliderPercent.Value;
                _vignette.ApplyEffect();
            }
        }

        private void sliderBand_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette != null)
            {
                _vignette.BandPixels = Convert.ToInt32(sliderBand.Value);
                _vignette.ApplyEffect();
            }
        }

        private void sliderOriginX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette != null)
            {
                _vignette.Xcentre = Convert.ToInt32(sliderOriginX.Value);
                _vignette.ApplyEffect();
            }
        }

        private void sliderOriginY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette != null)
            {
                _vignette.Ycentre = Convert.ToInt32(sliderOriginY.Value);
                _vignette.ApplyEffect();
            }
        }

        private void sliderSteps_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette != null)
            {
                _vignette.NumberSteps = Convert.ToInt32(sliderSteps.Value);
                _vignette.ApplyEffect();
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
                    vig.BandPixels = Convert.ToInt32(sliderBand.Value / _scaleFactor);
                    vig.NumberSteps = Convert.ToInt32(sliderSteps.Value / _scaleFactor);
                    vig.Xcentre = Convert.ToInt32(sliderOriginX.Value);
                    vig.Ycentre = Convert.ToInt32(sliderOriginY.Value);
                    vig.BorderColour = _borderColor;
                    vig.Shape = _shape;
                    string fileToSave = dlg.FileName;
                    // I don't want the original file to be overwritten, since the vignetting operation
                    // is a lossy one (where some pixels of the original image may be lost).
                    // Therefore, if the user inadvertently selects the original filename for saving,
                    // I create the new file name with an underscore _ appended to the filename.
                    if (fileToSave == _fileName)
                    {
                        fileToSave = GetNewFileName(fileToSave);
                    }
                    vig.FileNameToSave = fileToSave;

                    Mouse.OverrideCursor = Cursors.Wait;
                    vig.TransferImagePixels(ref _pixels8Red, ref _pixels8Green, ref _pixels8Blue,
                            _originalWidth, _originalHeight,
                            ref _pixels8RedModified, ref _pixels8GreenModified, ref _pixels8BlueModified,
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
