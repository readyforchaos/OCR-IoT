using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Media.Ocr;



namespace OCRImgReadText
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Windows.Media.Capture.MediaCapture captureManager; 

        // Bitmap holder of currently loaded image.
        private WriteableBitmap bitmap;
        // OCR engine instance used to extract text from images.
        private OcrEngine ocrEngine;

        private StorageFile file2;
        public MainPage()
        {
            this.InitializeComponent();
            ocrEngine = new OcrEngine(OcrLanguage.English);
            TextOverlay.Children.Clear();
        }

        
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("TestImages\\mel2Final.jpg");
            await LoadImage(file);
        }
        private async Task LoadImage(StorageFile file)
        {
            ImageProperties imgProp = await file.Properties.GetImagePropertiesAsync();
            file2 = file;


            using (var sourceStream = await file.OpenAsync(FileAccessMode.Read))
            {
                bitmap = new WriteableBitmap((int)imgProp.Width, (int)imgProp.Height);
                //var aa = await CompressImageAsync(sourceStream, 1);
                bitmap.SetSource(sourceStream); 

                //var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, sourceStream);
                //encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                //      BitmapAlphaMode.Ignore,
                //      1000,
                //      1000,
                //      500,
                //      500,
                //      bitmap.PixelBuffer.ToArray());
                PreviewImage.Source = bitmap;

              //System.Windows.imaging.extensions



            }
        }
        private async void ExtractText_Click(object sender, RoutedEventArgs e)
        {
            //// Prevent another OCR request, since only image can be processed at the time at same OCR engine instance.
            //ExtractTextButton.IsEnabled = false;

            // Check whether is loaded image supported for processing.
            // Supported image dimensions are between 40 and 2600 pixels.
            if (bitmap.PixelHeight < 40 ||
                bitmap.PixelHeight > 2600 ||
                bitmap.PixelWidth < 40 ||
                bitmap.PixelWidth > 2600)
            {


                ImageText.Text = "Image size is not supported." +
                                    Environment.NewLine +
                                    "Loaded image size is " + bitmap.PixelWidth + "x" + bitmap.PixelHeight + "." +
                                    Environment.NewLine +
                                    "Supported image dimensions are between 40 and 2600 pixels.";
                //ImageText.Style = (Style)Application.Current.Resources["RedTextStyle"];

                return;

                //using (var sourceStream = await file2.OpenAsync(FileAccessMode.Read))
                //{
                //    bitmap = new WriteableBitmap(1000, 1000);
                //    destFileStream = sourceStream;
                //    var aa = await CompressImageAsync(sourceStream, 1);

                //    bitmap.SetSource(aa);

                //    //var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, sourceStream);
                //    //encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                //    //      BitmapAlphaMode.Ignore,
                //    //      1000,
                //    //      1000,
                //    //      500,
                //    //      500,
                //    //      bitmap.PixelBuffer.ToArray());
                //    PreviewImage.Source = bitmap;

                //    //System.Windows.imaging.extensions



                //}

            }

            // This main API call to extract text from image.
            var ocrResult = await ocrEngine.RecognizeAsync((uint)bitmap.PixelHeight, (uint)bitmap.PixelWidth, bitmap.PixelBuffer.ToArray());

            // OCR result does not contain any lines, no text was recognized. 
            if (ocrResult.Lines != null)
            {
                // Used for text overlay.
                // Prepare scale transform for words since image is not displayed in original format.
                var scaleTrasform = new ScaleTransform
                {
                    CenterX = 0,
                    CenterY = 0,
                    ScaleX = PreviewImage.ActualWidth / bitmap.PixelWidth,
                    ScaleY = PreviewImage.ActualHeight / bitmap.PixelHeight,
                };

                if (ocrResult.TextAngle != null)
                {
                 
                    PreviewImage.RenderTransform = new RotateTransform
                    {
                        Angle = (double)ocrResult.TextAngle,
                        CenterX = PreviewImage.ActualWidth / 2,
                        CenterY = PreviewImage.ActualHeight / 2
                    };
                }

                string extractedText = "";

                // Iterate over recognized lines of text.
                foreach (var line in ocrResult.Lines)
                {
                    // Iterate over words in line.
                    foreach (var word in line.Words)
                    {
                        var originalRect = new Rect(word.Left, word.Top, word.Width, word.Height);
                        var overlayRect = scaleTrasform.TransformBounds(originalRect);

                        var wordTextBlock = new TextBlock()
                        {
                            Height = overlayRect.Height,
                            Width = overlayRect.Width,
                            FontSize = overlayRect.Height * 0.8,
                            Text = word.Text,
                          
                        };

                        // Define position, background, etc.
                        var border = new Border()
                        {
                            Margin = new Thickness(overlayRect.Left, overlayRect.Top, 0, 0),
                            Height = overlayRect.Height,
                            Width = overlayRect.Width,
                            Background=new SolidColorBrush(Colors.Orange),Opacity=0.5,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,
                            Child = wordTextBlock,
                          
                        };
                        OverlayTextButton.IsEnabled = true;
                        // Put the filled textblock in the results grid.
                        TextOverlay.Children.Add(border);
                        extractedText += word.Text + " ";
                    }
                    extractedText += Environment.NewLine;
                }

                

                ImageText.Text = extractedText;


                txtResult.Text = "";
                if (ImageText.Text.Contains("LETTMELK"))
                {
                    txtResult.Text = "Contains lactose, a sugar found in milk.";
                }
                else if (ImageText.Text.Contains("Siktet mel"))
                {
                    txtResult.Text = "Contains wheat, may contain traces of gluten";
                }
                else if (ImageText.Text.Contains("vegetabilsk"))
                {
                    txtResult.Text = "Contains oil, might be palmoil which is dangerous if grasped in large quantities";
                }

               
            }
            else
            {
                ImageText.Text = "No text.";
               
            }
        }


        //ANDREAS KAN DU SE HER :( Vil at bildet skal byttes til riktig når riktig element er valgt
        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            

            var result = ((ComboBoxItem)ComboBoxMenu.SelectedItem).Content.ToString();

            

            if (result == "Melk")
            {
                    var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("TestImages\\melkFinal.jpg");
            await LoadImage(file);
            }
            else if (result == "Mel")
            {
                    var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("TestImages\\mel2Final.jpg");
            await LoadImage(file);
            }
            else if (result == "Løk")
            {
                    var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("TestImages\\lokFinal.jpg");
            await LoadImage(file);
                }



            
        }

        private void OverlayText_Click(object sender, RoutedEventArgs e)
        {

            if(TextOverlay.Visibility==Visibility.Visible)
            {
                TextOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                TextOverlay.Visibility = Visibility.Visible;
            }
        }

        private async void launchCamera_Tapped(object sender, TappedRoutedEventArgs e)
        {

            //captureManager = new MediaCapture();
            ////captureManager.
            //await captureManager.InitializeAsync();
            //cam.Source = captureManager;
            //await captureManager.StartPreviewAsync(); 
            
        }

        void mMediaManager__Failed(Windows.Media.Capture.MediaCapture sender, Windows.Media.Capture.MediaCaptureFailedEventArgs errorEventArgs)
        {
            //throw new NotImplementedException();
        }

        void mMediaManager__RecordLimitationExceeded(Windows.Media.Capture.MediaCapture sender)
        {
            //throw new NotImplementedException();
        }

        private async void takeIMg(object sender, TappedRoutedEventArgs e)
        {
            //ImageEncodingProperties imgFormat = ImageEncodingProperties.CreateJpeg();


            //// create storage file in local app storage 
            //StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
            //    "TestPhoto.jpg",
            //    CreationCollisionOption.GenerateUniqueName);

            //// take photo 
            //captureManager.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, imgFormat);
            //await captureManager.CapturePhotoToStorageFileAsync(imgFormat, file);

            //LoadImage(file);
            //// Get photo as a BitmapImage 
            //BitmapImage bmpImage = new BitmapImage(new Uri(file.Path));

            //// imagePreivew is a <Image> object defined in XAML 
            //imagePreivew.Source = bmpImage; 
        }

        //private async Task<IRandomAccessStream> CompressImageAsync(IRandomAccessStream sourceStream, double newQuality)
        //{
        //    // create bitmap decoder from source stream
        //    BitmapDecoder bmpDecoder = await BitmapDecoder.CreateAsync(sourceStream);

        //    // bitmap transform if you need any
        //    BitmapTransform bmpTransform = new BitmapTransform() { ScaledHeight = 1000, ScaledWidth = 1000, InterpolationMode = BitmapInterpolationMode.Cubic };

        //    PixelDataProvider pixelData = await bmpDecoder.GetPixelDataAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, bmpTransform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);
        //    InMemoryRandomAccessStream destStream = new InMemoryRandomAccessStream(); // destination stream

        //    // define new quality for the image
        //    var propertySet = new BitmapPropertySet();
        //    var quality = new BitmapTypedValue(newQuality, PropertyType.Single);
        //    propertySet.Add("ImageQuality", quality);
        //    // create encoder with desired quality
        //    BitmapEncoder bmpEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, destFileStream, propertySet);
        //    bmpEncoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, 1000, 1000, 300, 300, pixelData.DetachPixelData());
        //    bmpEncoder.
        //    //await bmpEncoder.FlushAsync();
        //    return destStream;
        //}

        //public Windows.Storage.Streams.IRandomAccessStream storageStream { get; set; }

        //public IRandomAccessStream destFileStream { get; set; }
    }
}
