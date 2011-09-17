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
        private const int ViewportLength = 600;

        private BitmapSource _image;
        private BitmapSource _scaledImage;

        private string _fileName;
        private VignetteEffect _vignette;

        private Color _borderColor = Color.FromRgb(20, 20, 240);
        private double _scaleFactor = 1.0;

        public MainWindow()
        {
            InitializeComponent();
            bnColour.Background = new SolidColorBrush(_borderColor);
            comboTechnique.SelectedIndex = 1; // Select the ellipse shape
        }

        private static bool ReadImage(BitmapSource image)
        {
            PixelFormat format = image.Format;
            return (format == PixelFormats.Bgra32 || format == PixelFormats.Bgr32) &&
                   (format.BitsPerPixel == 24 || format.BitsPerPixel == 32);
        }

        private BitmapSource ScaleImage(BitmapSource image)
        {
            _scaleFactor = ViewportLength / Math.Max(image.Width, image.Height);
            _scaledImage = new TransformedBitmap(image, new ScaleTransform(_scaleFactor, _scaleFactor));
            return _scaledImage;
        }

        private void BnOpenClick(object sender, RoutedEventArgs e)
        {
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
                    Mouse.OverrideCursor = Cursors.Wait;
                    _fileName = ofd.FileName;
                    _image = new BitmapImage(new Uri(_fileName, UriKind.RelativeOrAbsolute));
                    if (ReadImage(_image))
                    {
                        Title = "Vignette Effect: " + ofd.SafeFileName;
                        bnSaveImage.IsEnabled = true;
                        img.Source = ScaleImage(_image);
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
            _vignette = new VignetteEffect
                            {
                                OrientationInDegrees = sliderAngle.Value,
                                CoveragePercent = sliderPercent.Value,
                                BandWidthInPixels = Convert.ToInt32(sliderBand.Value),
                                NumberOfGradationSteps = Convert.ToInt32(sliderSteps.Value),
                                CenterXOffsetPercent = sliderOriginX.Value,
                                CenterYOffsetPercent = sliderOriginY.Value,
                                BorderColor = _borderColor,
                                Figure = Shape
                            };
            ApplyEffect();
        }

        private void ApplyEffect()
        {
            img.Source = _vignette.Transform(_scaledImage);
        }

        private void ComboTechniqueSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vignette == null) return;
            VignetteFigure figure = Shape;
            _vignette.Figure = figure;
            sliderAngle.IsEnabled = !(figure is Circle);
            ApplyEffect();
        }

        private VignetteFigure Shape
        {
            get
            {
                switch (comboTechnique.SelectedIndex)
                {
                    case 0:
                        return new Circle();
                    case 1:
                        return new Ellipse();
                    case 2:
                        return new Diamond();
                    case 3:
                        return new Square();
                    default:
                        return new Rectangle();
                }
            }
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
            ApplyEffect();
        }

        private void SliderAngleValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.OrientationInDegrees = sliderAngle.Value;
            ApplyEffect();
        }

        private void SliderPercentValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.CoveragePercent = sliderPercent.Value;
            ApplyEffect();
        }

        private void SliderBandValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.BandWidthInPixels = Convert.ToInt32(sliderBand.Value);
            ApplyEffect();
        }

        private void SliderOriginXValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.CenterXOffsetPercent = Convert.ToInt32(sliderOriginX.Value);
            ApplyEffect();
        }

        private void SliderOriginYValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.CenterYOffsetPercent = Convert.ToInt32(sliderOriginY.Value);
            ApplyEffect();
        }

        private void SliderStepsValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vignette == null) return;
            _vignette.NumberOfGradationSteps = Convert.ToInt32(sliderSteps.Value);
            ApplyEffect();
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
                    var vig = new VignetteEffect
                                  {
                                      OrientationInDegrees = sliderAngle.Value,
                                      CoveragePercent = sliderPercent.Value,
                                      BandWidthInPixels = Convert.ToInt32(sliderBand.Value/_scaleFactor),
                                      NumberOfGradationSteps = Convert.ToInt32(sliderSteps.Value/_scaleFactor),
                                      CenterXOffsetPercent = sliderOriginX.Value,
                                      CenterYOffsetPercent = sliderOriginY.Value,
                                      BorderColor = _borderColor,
                                      Figure = Shape,
                                  };

                    Mouse.OverrideCursor = Cursors.Wait;
                    SaveImage(vig.Transform(_image), FileToSave(dlg));
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

        private string FileToSave(FileDialog dlg)
        {
            // I don't want the original file to be overwritten, since the vignetting operation
            // is a lossy one (where some pixels of the original image may be lost).
            // Therefore, if the user inadvertently selects the original filename for saving,
            // I create the new file name with an underscore _ appended to the filename.
            return dlg.FileName == _fileName ? GetNewFileName(dlg.FileName) : dlg.FileName;
        }

        private static string GetNewFileName(string fileForSaving)
        {
            return Path.GetFileNameWithoutExtension(fileForSaving) + "_" + Path.GetExtension(fileForSaving);
        }

        private static void SaveImage(BitmapSource image, string fileName)
        {
            var encoder = BitmapEncoderFor(fileName);
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }

        private static BitmapEncoder BitmapEncoderFor(string fileName)
        {
            switch (Path.GetExtension(fileName))
            {
                case ".png":
                    return new PngBitmapEncoder();
                case ".jpg":
                    return new JpegBitmapEncoder();
                default:
                    return new BmpBitmapEncoder();
            }
        }
    }

    public static class BitmapSourceExt
    {
        public static List<Color> Pixels(this BitmapSource image)
        {
            byte[] pixelData = PixelData(image);
            int step = image.Format.BitsPerPixel / 8;
            var pixels = new List<Color>();
            for (int i = 0; i < pixelData.Count(); i += step)
            {
                pixels.Add(Color.FromRgb(pixelData[i + 2], pixelData[i + 1], pixelData[i]));
            }
            return pixels;
        }

        private static byte[] PixelData(BitmapSource image)
        {
            var stride = (image.PixelWidth * image.Format.BitsPerPixel + 7) / 8;
            var pixelData = new byte[stride * image.PixelHeight];
            image.CopyPixels(pixelData, stride, 0);
            return pixelData;
        }
    }
}
