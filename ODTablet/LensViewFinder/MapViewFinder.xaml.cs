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

        public MapViewFinder()
            : this(Colors.Black)
        {
        }

        public MapViewFinder(Color BorderColor)
        {
            InitializeComponent();
            this.MagShadow.Stroke = new SolidColorBrush(BorderColor);
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
            //UpdateMagnifier();   

        }

        private void UpdateMagnifier()
        {
            if (this.Visibility == Visibility.Collapsed) { return; }
            
            if (this.Map == null) { return; }

            DependencyObject rola = VisualTreeHelper.GetParent(this.Map);
            DependencyObject roleta = VisualTreeHelper.GetParent(this);

            if(VisualTreeHelper.GetParent(this.Map) == VisualTreeHelper.GetParent(this))
            {
                Point point = this.TransformToVisual(this.Map).Transform(
                    new Point(
                        this.RenderSize.Width * 0.5d + this.Translate.X,
                        this.RenderSize.Height * 0.5d + this.Translate.Y
                    )
                );
                MapPoint center = this.Map.ScreenToMap(point);
                double resolution = this.Map.Resolution;
                double zoomResolution = resolution / 1; //this.ZoomFactor;
                double width = 0.5d * this.VFMap.ActualWidth * zoomResolution;
                Envelope envelope = new Envelope()
                {
                    XMin = center.X - width,
                    YMin = center.Y - width,
                    XMax = center.X + width,
                    YMax = center.Y + width,
                    SpatialReference = this.VFMap.SpatialReference
                };
                this.VFMap.Extent = envelope;
                return;
            }
            Console.WriteLine("EH ADOTADO!");
        }


        public void UpdateExtent(double[] extent)
        {
            if (this.Visibility == Visibility.Collapsed) { return; }
            if (this.Map == null) { return; }

            // ViewFinder Resizing
            MapPoint topLeft = new MapPoint(extent[0], extent[3]);
            //MapPoint topRight = new MapPoint(extent[2], extent[3]);
            MapPoint bottomLeft = new MapPoint(extent[0], extent[1]);
            MapPoint bottomRight = new MapPoint(extent[2], extent[1]);

            double VFHeight = this.Map.MapToScreen(topLeft).Y - this.Map.MapToScreen(bottomLeft).Y;
            double VFWidth = this.Map.MapToScreen(bottomRight).X - this.Map.MapToScreen(bottomLeft).X;

            this.Width = Math.Abs(VFWidth);
            this.Height = Math.Abs(VFHeight);

            // ViewFinder Translation
            Envelope envelope = new Envelope()
            {
                XMin = extent[0],
                YMin = extent[1],
                XMax = extent[2],
                YMax = extent[3],
                SpatialReference = this.VFMap.SpatialReference
            };
            try
            {
                TranslateVF(envelope);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("PAU EXCEPTION INVALID OPERATION UPDATE(EXTENT)!");
            }
            this.VFMap.Extent = envelope;

        }

        public void TranslateVF(Envelope envelope)
        {
            MapPoint envelopeCenter = envelope.GetCenter();
            Point destiny = this.Map.MapToScreen(envelopeCenter);

            DependencyObject rola = VisualTreeHelper.GetParent(this.Map);
            DependencyObject roleta = VisualTreeHelper.GetParent(this);
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
                Console.WriteLine("EH ADOTADO EXTENT!");
            }
        }

        internal void UpdateExtent(string p)
        {
            this.UpdateExtent(ExtentStringToArray(p));
        }

        private double[] ExtentStringToArray(string extent)
        {
            return Array.ConvertAll(extent.Split(','), Double.Parse);
        }
    }
}
