using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace RandomWheel.Views
{
    public partial class BrandingLogoPreviewWindow : Window
    {
        private readonly string _imagePath;
        private bool _isDragging;
        private Point _lastMousePosition;
        private double _offsetX;
        private double _offsetY;
        private double _scale = 1.0;

        /// <summary>
        /// Gets whether the user applied the changes.
        /// </summary>
        public bool Applied { get; private set; }

        /// <summary>
        /// Gets the final X offset (normalized -1 to 1).
        /// </summary>
        public double OffsetX => _offsetX / 150.0; // Normalize to -1 to 1 range based on preview radius

        /// <summary>
        /// Gets the final Y offset (normalized -1 to 1).
        /// </summary>
        public double OffsetY => _offsetY / 150.0; // Normalize to -1 to 1 range based on preview radius

        /// <summary>
        /// Gets the final scale factor.
        /// </summary>
        public double Scale => _scale;

        public BrandingLogoPreviewWindow(string imagePath, double initialOffsetX = 0, double initialOffsetY = 0, double initialScale = 1.0)
        {
            InitializeComponent();
            _imagePath = imagePath;
            
            // Convert normalized offsets back to pixel offsets for preview
            _offsetX = initialOffsetX * 150.0;
            _offsetY = initialOffsetY * 150.0;
            _scale = initialScale;

            LoadImage();
        }

        private void LoadImage()
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                PreviewImage.Source = bitmap;

                // Size the image to fit the preview area initially
                double previewSize = 300;
                double aspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
                
                if (aspectRatio >= 1)
                {
                    // Wider than tall - fit to width
                    PreviewImage.Width = previewSize * _scale;
                    PreviewImage.Height = (previewSize / aspectRatio) * _scale;
                }
                else
                {
                    // Taller than wide - fit to height
                    PreviewImage.Height = previewSize * _scale;
                    PreviewImage.Width = (previewSize * aspectRatio) * _scale;
                }

                // Apply initial transforms
                ImageScale.ScaleX = _scale;
                ImageScale.ScaleY = _scale;
                ImageTranslate.X = _offsetX;
                ImageTranslate.Y = _offsetY;
                ZoomSlider.Value = _scale;
                UpdateZoomLabel();

                // Center the image in the canvas
                CenterImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void CenterImage()
        {
            double canvasSize = 300;
            double left = (canvasSize - PreviewImage.Width) / 2 + _offsetX;
            double top = (canvasSize - PreviewImage.Height) / 2 + _offsetY;
            
            System.Windows.Controls.Canvas.SetLeft(PreviewImage, left);
            System.Windows.Controls.Canvas.SetTop(PreviewImage, top);
        }

        private void UpdateZoomLabel()
        {
            ZoomLabel.Text = $"{(int)(_scale * 100)}%";
        }

        private void ImageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(ImageCanvas);
            ImageCanvas.CaptureMouse();
        }

        private void ImageCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ImageCanvas.ReleaseMouseCapture();
        }

        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var currentPosition = e.GetPosition(ImageCanvas);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;

            _offsetX += deltaX;
            _offsetY += deltaY;

            // Clamp offsets to reasonable bounds
            double maxOffset = 200;
            _offsetX = Math.Max(-maxOffset, Math.Min(maxOffset, _offsetX));
            _offsetY = Math.Max(-maxOffset, Math.Min(maxOffset, _offsetY));

            CenterImage();
            _lastMousePosition = currentPosition;
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PreviewImage?.Source == null) return;

            _scale = ZoomSlider.Value;
            
            var bitmap = PreviewImage.Source as BitmapImage;
            if (bitmap != null)
            {
                double previewSize = 300;
                double aspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
                
                if (aspectRatio >= 1)
                {
                    PreviewImage.Width = previewSize * _scale;
                    PreviewImage.Height = (previewSize / aspectRatio) * _scale;
                }
                else
                {
                    PreviewImage.Height = previewSize * _scale;
                    PreviewImage.Width = (previewSize * aspectRatio) * _scale;
                }

                CenterImage();
            }

            UpdateZoomLabel();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _offsetX = 0;
            _offsetY = 0;
            _scale = 1.0;
            ZoomSlider.Value = 1.0;
            
            var bitmap = PreviewImage.Source as BitmapImage;
            if (bitmap != null)
            {
                double previewSize = 300;
                double aspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
                
                if (aspectRatio >= 1)
                {
                    PreviewImage.Width = previewSize;
                    PreviewImage.Height = previewSize / aspectRatio;
                }
                else
                {
                    PreviewImage.Height = previewSize;
                    PreviewImage.Width = previewSize * aspectRatio;
                }
            }

            CenterImage();
            UpdateZoomLabel();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Applied = false;
            DialogResult = false;
            Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Applied = true;
            DialogResult = true;
            Close();
        }
    }
}
