using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;


namespace ODTablet.LensViewFinder
{
    /// <summary>
    /// Interaction logic for MapViewFinder.xaml
    /// </summary>
    public partial class MapViewFinder : UserControl
    {
        private Point _begin;
        private Point _current;
        private Cursor _cursor = null;
        private bool _isdrag = false;

        private String lastValidExtent;

        public MapViewFinder(Color BorderColor, String extent)
        {
            InitializeComponent();
            this.MagShadow.Stroke = new SolidColorBrush(BorderColor);
            lastValidExtent = extent;
            UpdateExtent(lastValidExtent);
        }

        public static readonly DependencyProperty MapProperty =
            DependencyProperty.Register(
                "Map",
                typeof(Map),
                typeof(MapViewFinder),
                new PropertyMetadata(MapViewFinder.OnMapPropertyChanged));
        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.RegisterAttached(
                "Layers",
                typeof(LayerCollection),
                typeof(MapViewFinder),
                new PropertyMetadata(MapViewFinder.OnLayersPropertyChanged));
        public Map Map
        {
            get { return (Map)this.GetValue(MapProperty); }
            set { this.SetValue(MapProperty, value); }
        }
        public LayerCollection Layers
        {
            get { return (LayerCollection)GetValue(LayersProperty); }
            set { SetValue(LayersProperty, value); }
        }

        private static void OnLayersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MapViewFinder MapViewFinder = d as MapViewFinder;
            if (MapViewFinder.VFMap != null)
            {
                MapViewFinder.VFMap.Layers = e.NewValue as LayerCollection;
            }
        }
        private static void OnMapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MapViewFinder glass = d as MapViewFinder;
            Map mapOld = e.OldValue as Map;
            if (mapOld != null)
            {
                mapOld.ExtentChanged -= glass.Map_ExtentChanged;
            }
            Map mapNew = e.NewValue as Map;
            if (mapNew != null)
            {
                mapNew.ExtentChanged += glass.Map_ExtentChanged;
            }
        }

        // TODO: REMOVE?
        private void Map_ExtentChanged(object sender, ExtentEventArgs e)
        {
            UpdateExtentAccordingToLastValid();
        }

        private void UpdateExtentAccordingToLastValid()
        {
            if (this.VFMap.Extent == null)
            {
                UpdateExtent(lastValidExtent);
            }
            else
            {
                UpdateExtent(this.VFMap.Extent.ToString());
            }
        }



        public void UpdateExtent(double[] extent)
        {
            if (this.Visibility == Visibility.Collapsed) { return; }
            if (this.Map == null) { return; }

            Envelope lensExtent = new Envelope()
            {
                XMin = extent[0],
                YMin = extent[1],
                XMax = extent[2],
                YMax = extent[3],
                SpatialReference = this.VFMap.SpatialReference
            };

            if(this.Map.Extent != null && this.Map.Extent.Intersects(lensExtent))
            {
                Envelope MapLensIntersectionExtent = lensExtent.Intersection(this.Map.Extent);
                try
                {
                    ResizeWindow(MapLensIntersectionExtent);
                    TranslateVF(MapLensIntersectionExtent);
                    this.VFMap.Extent = MapLensIntersectionExtent;
                    lastValidExtent = this.VFMap.Extent.ToString();
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine("Invalid operation exception! FAIL.");
                }
            }
            else
            {
                //Console.WriteLine(this.Name + " was friendzoned.");
            }
        }

        private void ResizeWindow(Envelope lensExtent)
        {
            // ViewFinder Resizing
            MapPoint topLeft = new MapPoint(lensExtent.XMin, lensExtent.YMax);
            MapPoint bottomLeft = new MapPoint(lensExtent.XMin, lensExtent.YMin);
            MapPoint bottomRight = new MapPoint(lensExtent.XMax, lensExtent.YMin);

            double VFHeight = this.Map.MapToScreen(topLeft).Y - this.Map.MapToScreen(bottomLeft).Y;
            double VFWidth = this.Map.MapToScreen(bottomRight).X - this.Map.MapToScreen(bottomLeft).X;

            this.Width = Math.Abs(VFWidth);
            this.Height = Math.Abs(VFHeight);
        }

        public void TranslateVF(Envelope lensExtent)
        {
            MapPoint lensCenter = lensExtent.GetCenter();
            Point destiny = this.Map.MapToScreen(lensCenter);

            if (VisualTreeHelper.GetParent(this.Map) == VisualTreeHelper.GetParent(this))
            {
                Point point = (this.TransformToVisual(this.Map)).Transform(
                    new Point(
                        this.RenderSize.Width * 0.5d + this.Translate.X,
                        this.RenderSize.Height * 0.5d + this.Translate.Y
                    )
                );
                double x = destiny.X - point.X;
                double y = destiny.Y - point.Y;
                if (this.FlowDirection == FlowDirection.RightToLeft)
                {
                    x *= -1d;
                }
                this.Translate.X += x;
                this.Translate.Y += y;
            }
            else
            {
                //Console.WriteLine("EH ADOTADO EXTENT!");
            }
        }

        internal void UpdateExtent(string p)
        {
            if(p != null)
            {
                this.UpdateExtent(ExtentStringToArray(p));
            }
        }

        internal void UpdateExtent()
        {
            if(this.lastValidExtent != null)
            {
                UpdateExtent(lastValidExtent);
            }
        }

        private double[] ExtentStringToArray(string extent)
        {
            return Array.ConvertAll(extent.Split(','), Double.Parse);
        }
    }
}
