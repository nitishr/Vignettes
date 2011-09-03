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

        private bool ReadImage(string fn, string fileNameOnly)
        {
            // Open the image
            _originalImage = new BitmapImage(new Uri(fn, UriKind.RelativeOrAbsolute));
            _originalWidth = _originalImage.PixelWidth;
            _originalHeight = _originalImage.PixelHeight;

            if ((_originalImage.Format == PixelFormats.Bgra32) ||
                (_originalImage.Format == PixelFormats.Bgr32))
            {
                _originalPixels = Pixels(_originalImage, _originalHeight);
                Title = "Vignette Effect: " + fileNameOnly;
                return true;
            }
            MessageBox.Show("Sorry, I don't support this image format.");

            return false;
        }

        private static byte[] Pixels(BitmapSource image, int height)
        {
            var stride = (image.PixelWidth*image.Format.BitsPerPixel + 7)/8;
            var pixels = new byte[stride*height];
            image.CopyPixels(Int32Rect.Empty, pixels, stride, 0);
            return pixels;
        }

        void ScaleImage()
        {
            var scale = new ScaleTransform(Convert.ToDouble(_scaledWidth/(1.0*_originalWidth)),
                                           Convert.ToDouble(_scaledHeight/(1.0*_originalHeight)));
            _scaleFactor = Math.Min(scale.ScaleX, scale.ScaleY);
            _scaledImage = new TransformedBitmap(_originalImage, scale);
            img.Source = _scaledImage;
            _scaledPixels = Pixels(_scaledImage, _scaledHeight);
        }

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

        private void PopulatePixelsOriginalAndScaled()
        {
            int bitsPerPixel = _originalImage.Format.BitsPerPixel;

            if (bitsPerPixel != 24 && bitsPerPixel != 32) return;

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
                    AddPixels(_scaledPixels[i + 2], _scaledPixels[i + 1], _scaledPixels[i], _pixels8RedScaled, _pixels8GreenScaled, _pixels8BlueScaled, _pixels8RedScaledModified, _pixels8GreenScaledModified, _pixels8BlueScaledModified);
                }

                for (int i = 0; i < _originalPixels.Count(); i += 3)
                {
                    // In a 24-bit per pixel image, the bytes are stored in the order 
                    // BGR - Blue Green Red order.
                    AddPixels(_originalPixels[i + 2], _originalPixels[i + 1], _originalPixels[i], _pixels8Red, _pixels8Green, _pixels8Blue, _pixels8RedModified, _pixels8GreenModified, _pixels8BlueModified);
                }
            }
            if (bitsPerPixel != 32) return;
            for (int i = 0; i < _scaledPixels.Count(); i += 4)
            {
                // In a 32-bit per pixel image, the bytes are stored in the order 
                // BGR - Blue Green Red Alpha order.
                AddPixels(_scaledPixels[i + 2], _scaledPixels[i + 1], _scaledPixels[i], _pixels8RedScaled, _pixels8GreenScaled, _pixels8BlueScaled, _pixels8RedScaledModified, _pixels8GreenScaledModified, _pixels8BlueScaledModified);
            }

            for (int i = 0; i < _originalPixels.Count(); i += 4)
            {
                // In a 32-bit per pixel image, the bytes are stored in the order 
                // BGR - Blue Green Red Alpha order.
                AddPixels(_originalPixels[i + 2], _originalPixels[i + 1], _originalPixels[i], _pixels8Red, _pixels8Green, _pixels8Blue, _pixels8RedModified, _pixels8GreenModified, _pixels8BlueModified);
            }
        }

        private static void AddPixels(byte red, byte green, byte blue, List<byte> pixels8Red, List<byte> pixels8Green, List<byte> pixels8Blue, List<byte> pixels8RedModified, List<byte> pixels8GreenModified, List<byte> pixels8BlueModified)
        {
            pixels8Red.Add(red);
            pixels8Green.Add(green);
            pixels8Blue.Add(blue);
            pixels8RedModified.Add(red);
            pixels8GreenModified.Add(green);
            pixels8BlueModified.Add(blue);
        }

        private void BnOpenClick(object sender, RoutedEventArgs e)
        {
            // Read in the image
            // Scale the image to 600 x 600, maintaining aspect ratio
            // Populate the lists pixels8RedScaled, pixels8GreenScaled, pixels8BlueScaled
            var ofd = new OpenFileDialog
                          {
                              Filter =
                                  "All Image Files(*.bmp;*.png;*.tif;*.jpg)|*.bmp;*.png;*.tif;*.jpg|24-Bit Bitmap(*.bmp)|*.bmp|PNG(*.png)|*.png|TIFF(*.tif)|*.tif|JPEG(*.jpg)|*.jpg"
                          };
            bool? result = ofd.ShowDialog();

            try
            {
                if (result == true)
                {
                    _fileName = ofd.FileName;
                    Mouse.OverrideCursor = Cursors.Wait;
                    if (ReadImage(_fileName, ofd.SafeFileName))
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

        private void ApplyVignette()
        {
            _vignette = new VignetteEffect(this)
                            {
                                OrientationInDegrees = sliderAngle.Value,
                                CoveragePercent = sliderPercent.Value,
                                BandWidthInPixels = Convert.ToInt32(sliderBand.Value),
                                NumberOfGradationSteps = Convert.ToInt32(sliderSteps.Value),
                                CenterXOffsetPercent = Convert.ToInt32(sliderOriginX.Value),
                                CenterYOffsetPercent = Convert.ToInt32(sliderOriginY.Value),
                                BorderColor = _borderColor,
                                Shape = _shape
                            };
            _vignette.TransferImagePixels(ref _pixels8RedScaled, ref _pixels8GreenScaled, ref _pixels8BlueScaled,
                    _scaledWidth, _scaledHeight,
                    ref _pixels8RedScaledModified, ref _pixels8GreenScaledModified, ref _pixels8BlueScaledModified,
                    ModeOfOperation.DisplayMode);
            _vignette.ApplyEffect();
        }

        public void UpdateImage(ref List<byte> pixels8RedScaledModified,
            ref List<byte> pixels8GreenScaledModified, 
            ref List<byte> pixels8BlueScaledModified)
        {
            int stride = (_scaledWidth * VignetteEffect.BitsPerPixel + 7) / 8;
            var pixelsToWrite = new byte[stride * _scaledHeight];

            for (int i = 0; i < pixelsToWrite.Count(); i += 3)
            {
                int i1 = i / 3;
                pixelsToWrite[i] = pixels8RedScaledModified[i1];
                pixelsToWrite[i + 1] = pixels8GreenScaledModified[i1];
                pixelsToWrite[i + 2] = pixels8BlueScaledModified[i1];
            }

            _newImage = BitmapSource.Create(_scaledWidth, _scaledHeight, 96, 96, PixelFormats.Rgb24,
                null, pixelsToWrite, stride);
            img.Source = _newImage;
        }

        private void ComboTechniqueSelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            sliderAngle.IsEnabled = true;
            switch (comboTechnique.SelectedIndex)
            {
                case 0:
                    _shape = VignetteShape.Circle;
                    sliderAngle.IsEnabled = false;
                    break;
                case 1:
                    _shape = VignetteShape.Ellipse;
                    break;
                case 2:
                    _shape = VignetteShape.Diamond;
                    break;
                case 3:
                    _shape = VignetteShape.Square;
                    break;
                default:
                    _shape = VignetteShape.Rectangle;
                    break;
            }

            if (_vignette == null) return;
            _vignette.Shape = _shape;
            _vignette.ApplyEffect();
        }

        private void BnColourClick(object sender, RoutedEventArgs e)
        {
            // ColorPickerDialog comes from ColorPicker.dll, which comes from the Microsoft stable.
            // I know that CodeProject has some colour picker dialogs available, but I choose the 
            // plain vanilla one from Microsoft, since our focus in this code is on providing a 
            // vignette effect, and not a fancy colour dialog application.
            var cPicker = new ColorPickerDialog
                              {
                                  WindowStartupLocation = WindowStartupLocation.Manual,
                                  Top = Top + 200,
                                  Left = Width,
                                  Height = 169,
                                  ResizeMode = ResizeMode.NoResize,
                                  StartingColor = _borderColor,
                                  Owner = this
                              };

            // I deliberately set the height to be this, so as not to show the Opacity and other 
            // colour text boxes in the dialog. I currently don't have the facility to change the 
            // opacity of the image, and therefore I don't want users to change the opacity, and hence
            // report a bug saying that "opacity is not working". So, I don't show the opacity button at all
            // and further, I don't allow this colour picker dialog to be resized.


            bool? dialogResult = cPicker.ShowDialog();
            if (dialogResult == null || !((bool) dialogResult)) return;
            _borderColor = cPicker.SelectedColor;
            bnColour.Background = new SolidColorBrush(_borderColor);

            if (_vignette == null) return;
            _vignette.BorderColor = _borderColor;
            _vignette.ApplyEffect();
        }

        private void SliderAngleValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.OrientationInDegrees = sliderAngle.Value;
            _vignette.ApplyEffect();
        }

        private void SliderPercentValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.CoveragePercent = sliderPercent.Value;
            _vignette.ApplyEffect();
        }

        private void SliderBandValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.BandWidthInPixels = Convert.ToInt32(sliderBand.Value);
            _vignette.ApplyEffect();
        }

        private void SliderOriginXValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.CenterXOffsetPercent = Convert.ToInt32(sliderOriginX.Value);
            _vignette.ApplyEffect();
        }

        private void SliderOriginYValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.CenterYOffsetPercent = Convert.ToInt32(sliderOriginY.Value);
            _vignette.ApplyEffect();
        }

        private void SliderStepsValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.NumberOfGradationSteps = Convert.ToInt32(sliderSteps.Value);
            _vignette.ApplyEffect();
        }

        private void BnSaveImageClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
                          {Filter = "PNG Images (.png)|*.png|JPG Images (.jpg)|*.jpg|BMP Images (.bmp)|*.bmp"};

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            try
            {
                // Process save file dialog box results
                if (result == true)
                {
                    // Save image
                    // While all other parameters are percentages, and are independent of image 
                    // dimensions, two parameters - Width of the band, and Number of Steps need 
                    // to be scaled depending upon the image dimensions.
                    // Here, the variable scaleFactor comes in handy to perform such scaling.
                    // Though scaleFactor can never be zero, we enclose the entire saving code 
                    // within a try-catch block, just in case things go out of control.
                    var vig = new VignetteEffect(this)
                                  {
                                      OrientationInDegrees = sliderAngle.Value,
                                      CoveragePercent = sliderPercent.Value,
                                      BandWidthInPixels = Convert.ToInt32(sliderBand.Value/_scaleFactor),
                                      NumberOfGradationSteps = Convert.ToInt32(sliderSteps.Value/_scaleFactor),
                                      CenterXOffsetPercent = Convert.ToInt32(sliderOriginX.Value),
                                      CenterYOffsetPercent = Convert.ToInt32(sliderOriginY.Value),
                                      BorderColor = _borderColor,
                                      Shape = _shape
                                  };
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
