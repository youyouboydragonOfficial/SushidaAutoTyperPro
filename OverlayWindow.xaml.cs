using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SushidaAutoTyper
{
    public partial class OverlayWindow : Window
    {
        private Point _startPoint;
        private bool _isSelecting = false;

        public Int32Rect SelectedRegion { get; private set; }
        public bool IsRegionSelected { get; private set; } = false;

        public OverlayWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isSelecting = true;
                _startPoint = e.GetPosition(OverlayCanvas);

                Canvas.SetLeft(SelectionBox, _startPoint.X);
                Canvas.SetTop(SelectionBox, _startPoint.Y);
                SelectionBox.Width = 0;
                SelectionBox.Height = 0;
                SelectionBox.Visibility = Visibility.Visible;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                Point currentPoint = e.GetPosition(OverlayCanvas);

                double x = Math.Min(_startPoint.X, currentPoint.X);
                double y = Math.Min(_startPoint.Y, currentPoint.Y);
                double w = Math.Abs(currentPoint.X - _startPoint.X);
                double h = Math.Abs(currentPoint.Y - _startPoint.Y);

                Canvas.SetLeft(SelectionBox, x);
                Canvas.SetTop(SelectionBox, y);
                SelectionBox.Width = w;
                SelectionBox.Height = h;

                SizeInfoText.Text = $"Position: ({ (int)x }, { (int)y }) | Size: { (int)w } x { (int)h }";
                Canvas.SetLeft(SizeInfoText, x);
                Canvas.SetTop(SizeInfoText, Math.Max(0, y - 25));
                SizeInfoText.Visibility = Visibility.Visible;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                Point endPoint = e.GetPosition(OverlayCanvas);

                int x = (int)Math.Min(_startPoint.X, endPoint.X);
                int y = (int)Math.Min(_startPoint.Y, endPoint.Y);
                int w = (int)Math.Abs(endPoint.X - _startPoint.X);
                int h = (int)Math.Abs(endPoint.Y - _startPoint.Y);

                if (w > 10 && h > 10)
                {
                    // Scale for DPI if necessary
                    Point screenPoint = PointToScreen(new Point(x, y));
                    SelectedRegion = new Int32Rect((int)screenPoint.X, (int)screenPoint.Y, w, h);
                    IsRegionSelected = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    SelectionBox.Visibility = Visibility.Collapsed;
                    SizeInfoText.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                IsRegionSelected = false;
                DialogResult = false;
                Close();
            }
        }
    }
}
