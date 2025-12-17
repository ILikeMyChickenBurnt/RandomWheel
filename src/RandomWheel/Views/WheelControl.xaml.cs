using RandomWheel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RandomWheel.Views
{
    public partial class WheelControl : UserControl
    {
        private List<ListItem> _unmarkedItems = new();
        private IEnumerable<ListItem>? _lastRenderedItems;

        // Predefined color palette for better aesthetics
        private static readonly Color[] WheelColors = new[]
        {
            Color.FromRgb(255, 107, 107),  // Red
            Color.FromRgb(255, 193, 7),    // Amber
            Color.FromRgb(76, 175, 80),    // Green
            Color.FromRgb(33, 150, 243),   // Blue
            Color.FromRgb(156, 39, 176),   // Purple
            Color.FromRgb(255, 152, 0),    // Orange
            Color.FromRgb(0, 188, 212),    // Cyan
            Color.FromRgb(233, 30, 99),    // Pink
            Color.FromRgb(63, 81, 181),    // Indigo
            Color.FromRgb(139, 195, 74),   // Light Green
            Color.FromRgb(255, 87, 34),    // Deep Orange
            Color.FromRgb(103, 58, 183),   // Deep Purple
        };

        // Branding logo offset and scale settings
        private double _logoOffsetX = 0;
        private double _logoOffsetY = 0;
        private double _logoScale = 1.0;

        public WheelControl()
        {
            InitializeComponent();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Re-render when size changes
            if (_lastRenderedItems != null)
            {
                RenderWheel();
            }
            UpdatePointerPosition();
            UpdateRotationCenter();
            UpdateBrandingLogoSize();
        }

        private void UpdatePointerPosition()
        {
            double size = Math.Min(ActualWidth, ActualHeight);
            if (size <= 0) return;

            double cx = ActualWidth / 2;
            double cy = ActualHeight / 2;
            double radius = (size / 2) - 15; // Same margin as wheel
            
            // Position pointer so it's half above and half overlaying the wheel
            double wheelTop = cy - radius;
            
            // Create pointer triangle - narrower and taller for a sharper point
            double pointerWidth = Math.Max(12, size * 0.04);  // Narrower width
            double pointerHeight = Math.Max(24, size * 0.10); // Taller height
            double pointerTop = wheelTop - (pointerHeight / 2); // Half above the wheel edge
            
            WinnerPointer.Points = new PointCollection
            {
                new Point(cx, wheelTop + (pointerHeight / 2)),   // Tip pointing down into wheel
                new Point(cx - pointerWidth / 2, pointerTop),    // Top left
                new Point(cx + pointerWidth / 2, pointerTop)     // Top right
            };
        }

        private void UpdateRotationCenter()
        {
            double cx = ActualWidth / 2;
            double cy = ActualHeight / 2;
            WheelRotation.CenterX = cx;
            WheelRotation.CenterY = cy;
        }

        public void Render(IEnumerable<ListItem> items)
        {
            _lastRenderedItems = items;
            _unmarkedItems = items.Where(i => !i.IsMarked).ToList();
            RenderWheel();
        }

        private void RenderWheel()
        {
            WheelCanvas.Children.Clear();

            if (_unmarkedItems.Count == 0)
            {
                return;
            }

            // Calculate dynamic dimensions based on control size
            double size = Math.Min(ActualWidth, ActualHeight);
            if (size <= 0) size = 400; // Default fallback
            
            // Always use the center of the control
            double cx = ActualWidth / 2;
            double cy = ActualHeight / 2;
            double radius = (size / 2) - 15; // Leave margin for pointer

            // Draw background circle first (white fill)
            var backgroundCircle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = Brushes.White
            };
            Canvas.SetLeft(backgroundCircle, cx - radius);
            Canvas.SetTop(backgroundCircle, cy - radius);
            WheelCanvas.Children.Add(backgroundCircle);

            double anglePerSegment = 360.0 / _unmarkedItems.Count;

            // Pre-calculate colors to ensure no adjacent segments have the same color
            var segmentColors = GetNonAdjacentColors(_unmarkedItems.Count);

            for (int i = 0; i < _unmarkedItems.Count; i++)
            {
                double startAngle = i * anglePerSegment;
                double endAngle = (i + 1) * anglePerSegment;

                // Create segment path
                var segment = CreateSegmentPath(cx, cy, radius, startAngle, endAngle);
                
                // Assign color from pre-calculated list - no stroke for seamless segments
                segment.Fill = new SolidColorBrush(segmentColors[i]);
                segment.Stroke = null;
                segment.StrokeThickness = 0;

                WheelCanvas.Children.Add(segment);

                // Add text label
                var textBlock = CreateSegmentLabel(_unmarkedItems[i].Name, startAngle, endAngle, cx, cy, radius);
                WheelCanvas.Children.Add(textBlock);
            }

            UpdateRotationCenter();
        }

        /// <summary>
        /// Gets a list of colors ensuring no two adjacent colors are the same.
        /// Also ensures first and last colors are different (since wheel is circular).
        /// </summary>
        private List<Color> GetNonAdjacentColors(int count)
        {
            if (count == 0) return new List<Color>();
            if (count == 1) return new List<Color> { WheelColors[0] };

            var result = new List<Color>(count);
            int colorCount = WheelColors.Length;
            int lastColorIndex = -1;
            int firstColorIndex = 0;

            for (int i = 0; i < count; i++)
            {
                int colorIndex;
                
                if (i == 0)
                {
                    // First segment - just pick first color
                    colorIndex = 0;
                    firstColorIndex = colorIndex;
                }
                else if (i == count - 1 && count > 2)
                {
                    // Last segment - must differ from both previous and first (circular)
                    colorIndex = 0;
                    while (colorIndex == lastColorIndex || colorIndex == firstColorIndex)
                    {
                        colorIndex = (colorIndex + 1) % colorCount;
                        // Safety check to avoid infinite loop if not enough colors
                        if (colorIndex == 0 && colorCount < 3) break;
                    }
                }
                else
                {
                    // Middle segments - just differ from previous
                    colorIndex = (lastColorIndex + 1) % colorCount;
                }

                result.Add(WheelColors[colorIndex]);
                lastColorIndex = colorIndex;
            }

            return result;
        }

        private Path CreateSegmentPath(double cx, double cy, double radius, double startDeg, double endDeg)
        {
            // Convert degrees to radians
            double startRad = startDeg * Math.PI / 180.0;
            double endRad = endDeg * Math.PI / 180.0;

            // Calculate points on the circle
            Point startPoint = new Point(
                cx + radius * Math.Cos(startRad),
                cy + radius * Math.Sin(startRad)
            );

            Point endPoint = new Point(
                cx + radius * Math.Cos(endRad),
                cy + radius * Math.Sin(endRad)
            );

            // Determine if we need a large arc (>180 degrees)
            bool isLargeArc = (endDeg - startDeg) > 180;

            // Create path figure
            var pathFigure = new PathFigure
            {
                StartPoint = new Point(cx, cy),
                IsClosed = true
            };

            // Line from center to start of arc
            pathFigure.Segments.Add(new LineSegment(startPoint, true));

            // Arc segment
            pathFigure.Segments.Add(new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                RotationAngle = 0,
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise,
                IsStroked = true,
                IsSmoothJoin = true
            });

            // Line back to center
            pathFigure.Segments.Add(new LineSegment(new Point(cx, cy), true));

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            return new Path { Data = pathGeometry };
        }

        private TextBlock CreateSegmentLabel(string text, double startDeg, double endDeg, double cx, double cy, double radius)
        {
            double midDeg = (startDeg + endDeg) / 2.0;
            double midRad = midDeg * Math.PI / 180.0;
            double segmentAngle = endDeg - startDeg;

            // Calculate size-based scaling factor
            double sizeFactor = radius / 180.0; // 180 is the reference radius

            // For very small segments, we need different handling
            bool isVerySmallSegment = segmentAngle < 10;
            bool isSmallSegment = segmentAngle < 30;
            bool isMediumSegment = segmentAngle < 90;

            // Position text further from center toward the edge for more space
            double textDistance;
            if (segmentAngle >= 120)
            {
                textDistance = radius * 0.62;
            }
            else if (segmentAngle >= 30)
            {
                textDistance = radius * 0.70;
            }
            else if (segmentAngle >= 10)
            {
                textDistance = radius * 0.75;
            }
            else
            {
                // Very small segments - position near edge
                textDistance = radius * 0.80;
            }

            // Calculate text position
            double textX = cx + textDistance * Math.Cos(midRad);
            double textY = cy + textDistance * Math.Sin(midRad);

            // Dynamically adjust font size - can be slightly larger with more space
            double fontSize;
            if (segmentAngle >= 120)
            {
                fontSize = 16 * sizeFactor;
            }
            else if (segmentAngle >= 30)
            {
                fontSize = 12 * sizeFactor;
            }
            else if (segmentAngle >= 10)
            {
                fontSize = 9 * sizeFactor;
            }
            else
            {
                // Very small - use minimum readable size
                fontSize = 7 * sizeFactor;
            }
            fontSize = Math.Max(6, Math.Min(fontSize, 18));

            // For very small segments, truncate text aggressively
            string displayText = text;
            int maxChars;
            if (segmentAngle < 10)
            {
                maxChars = (int)Math.Max(2, radius / 30);
                if (text.Length > maxChars)
                    displayText = text.Substring(0, maxChars - 1) + "..";
            }
            else if (segmentAngle < 20)
            {
                maxChars = (int)Math.Max(5, radius / 18);
                if (text.Length > maxChars)
                    displayText = text.Substring(0, maxChars - 2) + "..";
            }
            else if (segmentAngle < 45)
            {
                // Medium-small segments - also truncate if needed
                maxChars = (int)Math.Max(8, radius / 12);
                if (text.Length > maxChars)
                    displayText = text.Substring(0, maxChars - 2) + "..";
            }

            var textBlock = new TextBlock
            {
                Text = displayText,
                Foreground = Brushes.White,
                FontSize = fontSize,
                FontWeight = isVerySmallSegment ? FontWeights.Normal : FontWeights.SemiBold,
                TextAlignment = TextAlignment.Left,
                RenderTransformOrigin = new Point(0, 0.5) // Rotate from left-center
            };

            // Measure text
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = textBlock.DesiredSize.Width;
            double textHeight = textBlock.DesiredSize.Height;

            // Calculate rotation - text should read from outside to inside (toward center)
            // Adjust by 180 degrees so text points inward
            double textRotation = midDeg + 180;
            
            // Normalize to 0-360
            while (textRotation >= 360) textRotation -= 360;
            while (textRotation < 0) textRotation += 360;

            // If text would be upside down, flip it
            if (textRotation > 90 && textRotation < 270)
            {
                textRotation -= 180;
                // Position at outer edge when flipped
                textX = cx + (textDistance + textWidth * 0.5) * Math.Cos(midRad);
                textY = cy + (textDistance + textWidth * 0.5) * Math.Sin(midRad);
            }
            else
            {
                // Position starting from inner edge going outward
                textX = cx + (textDistance - textWidth * 0.5) * Math.Cos(midRad);
                textY = cy + (textDistance - textWidth * 0.5) * Math.Sin(midRad);
            }

            // For large segments, keep text horizontal and centered
            if (segmentAngle >= 90)
            {
                textX = cx + textDistance * Math.Cos(midRad);
                textY = cy + textDistance * Math.Sin(midRad);
                Canvas.SetLeft(textBlock, textX - textWidth / 2);
                Canvas.SetTop(textBlock, textY - textHeight / 2);
                textBlock.TextAlignment = TextAlignment.Center;
                // No rotation for large segments
            }
            else
            {
                // Position and rotate for smaller segments
                Canvas.SetLeft(textBlock, textX - textWidth / 2);
                Canvas.SetTop(textBlock, textY - textHeight / 2);
                textBlock.RenderTransformOrigin = new Point(0.5, 0.5);
                textBlock.RenderTransform = new RotateTransform(textRotation);
            }

            return textBlock;
        }

        public void Spin(int winnerIndex, Action<int> onComplete)
        {
            if (_unmarkedItems.Count == 0 || winnerIndex < 0 || winnerIndex >= _unmarkedItems.Count)
            {
                onComplete?.Invoke(-1);
                return;
            }

            // Disable interaction during spin
            this.IsEnabled = false;

            double anglePerSegment = 360.0 / _unmarkedItems.Count;
            
            double segmentStartAngle = winnerIndex * anglePerSegment;
            double segmentEndAngle = segmentStartAngle + anglePerSegment;
            
            // Land near the edge of the segment for more suspense
            // Randomly choose to land near the START (almost landed on previous item) 
            // or near the END (almost went to next item)
            var random = new Random();
            bool landNearStart = random.Next(2) == 0;
            double edgeOffset = 0.05 + (random.NextDouble() * 0.08); // 5-13% from edge for closer calls
            
            double landingPosition;
            if (landNearStart)
            {
                // Land just past the start edge - looks like it almost stopped on previous item
                // The wheel rotates clockwise, so "start" of segment is where it enters from
                landingPosition = segmentStartAngle + (anglePerSegment * edgeOffset);
            }
            else
            {
                // Land just before the end edge - looks like it almost went to next item
                landingPosition = segmentEndAngle - (anglePerSegment * edgeOffset);
            }
            
            // To align segment with TOP pointer (pointer is at 270 degrees / 12 o'clock):
            // We need to rotate so landingPosition ends up at 270 degrees
            double targetAngle = 270.0 - landingPosition;
            if (targetAngle < 0) targetAngle += 360.0;
            
            // Number of full rotations before landing - more spins for dramatic effect
            double fullRotations = 18.0;
            double baseRotation = fullRotations * 360.0;
            double finalAngle = baseRotation + targetAngle;

            // Reset rotation to 0 to start fresh
            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, null);
            WheelRotation.Angle = 0;
            
            // Reset zoom
            WheelZoom.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            WheelZoom.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            WheelZoom.ScaleX = 1;
            WheelZoom.ScaleY = 1;

            // Spin duration balanced for visual stop matching animation end
            var spinDuration = TimeSpan.FromSeconds(12);
            
            // Create the spin animation with gradual but not excessive slowdown
            // Power of 4 keeps wheel visibly moving until closer to the end
            var spinAnimation = new DoubleAnimation
            {
                From = 0,
                To = finalAngle,
                Duration = new Duration(spinDuration),
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 4 }
            };

            // Create zoom animation - prominent zoom effect
            var zoomInAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(spinDuration)
            };
            // Stay at 1.0 for first 15%, then zoom to 1.85 over next 70%, hold for final 15%
            zoomInAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(0)));
            zoomInAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(0.15)));
            zoomInAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1.85, KeyTime.FromPercent(0.85), 
                new QuadraticEase { EasingMode = EasingMode.EaseOut }));
            zoomInAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(1.85, KeyTime.FromPercent(1.0)));

            // Handle completion
            spinAnimation.Completed += (s, e) =>
            {
                this.IsEnabled = true;
                onComplete?.Invoke(winnerIndex);
            };

            // Apply animations
            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, spinAnimation);
            WheelZoom.BeginAnimation(ScaleTransform.ScaleXProperty, zoomInAnimation);
            WheelZoom.BeginAnimation(ScaleTransform.ScaleYProperty, zoomInAnimation.Clone());
        }

        public void ResetRotation()
        {
            // Clear any running animation by setting it to null, then reset angle
            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, null);
            WheelRotation.Angle = 0;
            
            // Animate zoom back to normal
            var zoomOutAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            WheelZoom.BeginAnimation(ScaleTransform.ScaleXProperty, zoomOutAnimation);
            WheelZoom.BeginAnimation(ScaleTransform.ScaleYProperty, zoomOutAnimation.Clone());
        }

        /// <summary>
        /// Sets the branding logo displayed in the center of the wheel.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <param name="offsetX">Normalized X offset (-1 to 1).</param>
        /// <param name="offsetY">Normalized Y offset (-1 to 1).</param>
        /// <param name="scale">Scale factor for the image within the circle.</param>
        public void SetBrandingLogo(string imagePath, double offsetX = 0, double offsetY = 0, double scale = 1.0)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                _logoOffsetX = offsetX;
                _logoOffsetY = offsetY;
                _logoScale = scale;

                BrandingLogoImage.Source = bitmap;
                BrandingLogoBorder.Visibility = Visibility.Visible;
                UpdateBrandingLogoSize();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading branding logo: {ex.Message}");
                ClearBrandingLogo();
            }
        }

        /// <summary>
        /// Clears the branding logo from the wheel center.
        /// </summary>
        public void ClearBrandingLogo()
        {
            BrandingLogoImage.Source = null;
            BrandingLogoBorder.Visibility = Visibility.Collapsed;
            _logoOffsetX = 0;
            _logoOffsetY = 0;
            _logoScale = 1.0;
        }

        /// <summary>
        /// Updates the branding logo size based on the wheel size.
        /// </summary>
        private void UpdateBrandingLogoSize()
        {
            if (BrandingLogoBorder.Visibility != Visibility.Visible) return;
            if (BrandingLogoImage.Source == null) return;

            double size = Math.Min(ActualWidth, ActualHeight);
            if (size <= 0) return;

            double radius = (size / 2) - 15;
            // Logo is now 45% of wheel radius (25% bigger than the original 36%)
            double logoSize = radius * 0.45;
            logoSize = Math.Max(50, Math.Min(logoSize, 150)); // Clamp between 50-150 pixels

            // Update the circular container size
            BrandingLogoClip.Width = logoSize;
            BrandingLogoClip.Height = logoSize;

            // Calculate image size based on source aspect ratio and user scale
            var bitmap = BrandingLogoImage.Source as BitmapImage;
            if (bitmap != null)
            {
                double aspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
                double imageSize = logoSize * _logoScale;
                
                if (aspectRatio >= 1)
                {
                    BrandingLogoImage.Width = imageSize;
                    BrandingLogoImage.Height = imageSize / aspectRatio;
                }
                else
                {
                    BrandingLogoImage.Height = imageSize;
                    BrandingLogoImage.Width = imageSize * aspectRatio;
                }

                // Apply offset (normalized offset * half the logo size for positioning within circle)
                double maxOffset = logoSize / 2;
                BrandingLogoImage.Margin = new Thickness(
                    _logoOffsetX * maxOffset,
                    _logoOffsetY * maxOffset,
                    -_logoOffsetX * maxOffset,
                    -_logoOffsetY * maxOffset);
            }
            
            // Update the clip geometry to match the new size
            BrandingLogoClipGeometry.Center = new Point(logoSize / 2, logoSize / 2);
            BrandingLogoClipGeometry.RadiusX = logoSize / 2;
            BrandingLogoClipGeometry.RadiusY = logoSize / 2;
        }
    }
}
