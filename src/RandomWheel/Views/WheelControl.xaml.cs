using RandomWheel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        }

        private void UpdatePointerPosition()
        {
            double size = Math.Min(ActualWidth, ActualHeight);
            if (size <= 0) return;

            double cx = ActualWidth / 2;
            
            // Create pointer triangle at top center
            double pointerWidth = Math.Max(16, size * 0.06);
            double pointerHeight = Math.Max(20, size * 0.06);
            
            WinnerPointer.Points = new PointCollection
            {
                new Point(cx, pointerHeight + 5),           // Tip pointing down
                new Point(cx - pointerWidth / 2, 5),        // Top left
                new Point(cx + pointerWidth / 2, 5)         // Top right
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

            for (int i = 0; i < _unmarkedItems.Count; i++)
            {
                double startAngle = i * anglePerSegment;
                double endAngle = (i + 1) * anglePerSegment;

                // Create segment path
                var segment = CreateSegmentPath(cx, cy, radius, startAngle, endAngle);
                
                // Assign color from palette
                Color segmentColor = WheelColors[i % WheelColors.Length];
                segment.Fill = new SolidColorBrush(segmentColor);
                segment.Stroke = Brushes.White;
                segment.StrokeThickness = 2;

                WheelCanvas.Children.Add(segment);

                // Add text label
                var textBlock = CreateSegmentLabel(_unmarkedItems[i].Name, startAngle, endAngle, cx, cy, radius);
                WheelCanvas.Children.Add(textBlock);
            }

            UpdateRotationCenter();
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

            // Position text based on segment size - for small segments, position near edge
            double textDistance;
            if (segmentAngle >= 120)
            {
                textDistance = radius * 0.5;
            }
            else if (segmentAngle >= 30)
            {
                textDistance = radius * 0.6;
            }
            else if (segmentAngle >= 10)
            {
                textDistance = radius * 0.65;
            }
            else
            {
                // Very small segments - position at 75% from center toward edge
                textDistance = radius * 0.75;
            }

            // Calculate text position
            double textX = cx + textDistance * Math.Cos(midRad);
            double textY = cy + textDistance * Math.Sin(midRad);

            // Dynamically adjust font size
            double fontSize;
            if (segmentAngle >= 120)
            {
                fontSize = 18 * sizeFactor;
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
            fontSize = Math.Max(6, Math.Min(fontSize, 20));

            // For very small segments, truncate text aggressively
            string displayText = text;
            int maxChars;
            if (segmentAngle < 10)
            {
                maxChars = (int)Math.Max(3, radius / 25);
                if (text.Length > maxChars)
                    displayText = text.Substring(0, maxChars - 1) + "..";
            }
            else if (segmentAngle < 20)
            {
                maxChars = (int)Math.Max(6, radius / 15);
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
            if (_unmarkedItems.Count == 0)
            {
                onComplete?.Invoke(-1);
                return;
            }

            // Disable interaction during spin
            this.IsEnabled = false;

            double anglePerSegment = 360.0 / _unmarkedItems.Count;
            
            // Calculate target angle for the winner
            // Segments are drawn starting from the right (0 degrees = 3 o'clock position)
            // The pointer is at the TOP (12 o'clock = 270 degrees in standard math, or -90 degrees)
            // We need to rotate the wheel so the winning segment lands under the top pointer
            
            double segmentStartAngle = winnerIndex * anglePerSegment;
            double segmentMidAngle = segmentStartAngle + (anglePerSegment / 2.0);
            
            // To align segment with TOP pointer:
            // The top is at -90 degrees (or 270 degrees) from the starting position
            // We need to rotate the segment's mid-angle to reach the top
            // Rotation needed = -90 - segmentMidAngle (to bring it to top)
            // But we want positive rotation, so: 360 - 90 - segmentMidAngle = 270 - segmentMidAngle
            double targetAngle = 270.0 - segmentMidAngle;
            if (targetAngle < 0) targetAngle += 360.0;
            
            // Number of full rotations before landing (more spins = more dramatic)
            double fullRotations = 8.0;
            double baseRotation = fullRotations * 360.0;
            double finalAngle = baseRotation + targetAngle;

            // Reset rotation to 0 to start fresh
            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, null);
            WheelRotation.Angle = 0;

            // Create the animation - use BeginAnimation directly on the transform
            var animation = new DoubleAnimation
            {
                From = 0,
                To = finalAngle,
                Duration = new Duration(TimeSpan.FromSeconds(6)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Handle completion
            animation.Completed += (s, e) =>
            {
                this.IsEnabled = true;
                onComplete?.Invoke(winnerIndex);
            };

            // Apply animation directly to the RotateTransform's Angle property
            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, animation);
        }

        public void ResetRotation()
        {
            // Clear any running animation by setting it to null, then reset angle
            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, null);
            WheelRotation.Angle = 0;
        }
    }
}
