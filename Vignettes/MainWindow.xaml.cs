using System;
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

        private Color _borderColor = Color.FromRgb(20, 20, 240);
        private double _scaleFactor = 1.0;

        public MainWindow()
        {
            InitializeComponent();
            bnColour.Background = new SolidColorBrush(_borderColor);
            comboTechnique.SelectedIndex = 1; // Select the ellipse shape
        }

        private void BnOpenClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                          {
                              Filter =
                                  "All Image Files(*.bmp;*.png;*.tif;*.jpg)|*.bmp;*.png;*.tif;*.jpg|24-Bit Bitmap(*.bmp)|*.bmp|PNG(*.png)|*.png|TIFF(*.tif)|*.tif|JPEG(*.jpg)|*.jpg"
                          };
            if(ofd.ShowDialog() != true) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _fileName = ofd.FileName;
                _image = new BitmapImage(new Uri(_fileName, UriKind.RelativeOrAbsolute));
                if (VignetteEffect.CanTransform(_image))
                {
                    Title = "Vignette Effect: " + ofd.SafeFileName;
                    bnSaveImage.IsEnabled = true;
                    FitImageToViewport();
                    ApplyEffect();
                }
                else
                {
                    MessageBox.Show("Sorry, I'm unable to open this image!");
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

        private void FitImageToViewport()
        {
            _scaleFactor = ViewportLength / Math.Max(_image.Width, _image.Height);
            _scaledImage = new TransformedBitmap(_image, new ScaleTransform(_scaleFactor, _scaleFactor));
            img.Source = _scaledImage;
        }

        private void ApplyEffect()
        {
            if (_scaledImage == null) return;
            img.Source = Transform(_scaledImage, 1);
        }

        private void ComboTechniqueSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sliderAngle.IsEnabled = !(Shape is Circle);
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

            if (cPicker.ShowDialog() != true) return;

            _borderColor = cPicker.SelectedColor;
            bnColour.Background = new SolidColorBrush(_borderColor);

            ApplyEffect();
        }

        private void SliderAngleValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyEffect();
        }

        private void SliderPercentValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyEffect();
        }

        private void SliderBandValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyEffect();
        }

        private void SliderOriginXValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyEffect();
        }

        private void SliderOriginYValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyEffect();
        }

        private void SliderStepsValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyEffect();
        }

        private void BnSaveImageClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
                          {Filter = "PNG Images (.png)|*.png|JPG Images (.jpg)|*.jpg|BMP Images (.bmp)|*.bmp"};
            if(dlg.ShowDialog() != true) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                // Though scaleFactor can never be zero, we enclose the entire saving code 
                // within a try-catch block, just in case things go out of control.
                SaveImage(Transform(_image, _scaleFactor), FileToSave(dlg.FileName));
            }
            catch (Exception)
            {
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // While all other parameters are percentages, and are independent of image 
        // dimensions, two parameters - Width of the band, and Number of Steps need 
        // to be scaled depending upon the image dimensions.
        // Here, the variable scaleFactor comes in handy to perform such scaling.
        private BitmapSource Transform(BitmapSource image, double scaleFactor)
        {
            return new VignetteEffect
                       {
                           OrientationInDegrees = sliderAngle.Value,
                           CoveragePercent = sliderPercent.Value,
                           BandWidthInPixels = Convert.ToInt32(sliderBand.Value/scaleFactor),
                           NumberOfGradationSteps = Convert.ToInt32(sliderSteps.Value/scaleFactor),
                           CenterXOffsetPercent = sliderOriginX.Value,
                           CenterYOffsetPercent = sliderOriginY.Value,
                           BorderColor = _borderColor,
                           Figure = Shape,
                       }.Transform(image);
        }

        private string FileToSave(string fileName)
        {
            // I don't want the original file to be overwritten, since the vignetting operation
            // is a lossy one (where some pixels of the original image may be lost).
            // Therefore, if the user inadvertently selects the original filename for saving,
            // I create the new file name with an underscore _ appended to the filename.
            return fileName == _fileName
                       ? Path.GetFileNameWithoutExtension(fileName) + "_" + Path.GetExtension(fileName)
                       : fileName;
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
}
