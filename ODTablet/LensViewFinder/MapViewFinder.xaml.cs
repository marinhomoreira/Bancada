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
        private Envelope _extent;

        public MapViewFinder(Color BorderColor, Envelope extent)
        {
            InitializeComponent();
            
            this.MagShadow.Stroke = new SolidColorBrush(BorderColor);
            _extent = extent;
            UpdateWindow();
        }

        public void Refresh()
        {
            UpdateWindow();
        }

        #region UpdateExtent
        public void UpdateExtent(Envelope extent)
        {
            _extent = extent;
            UpdateWindow();
        }

        private void UpdateExtent(string p) // TODO: public for update from SoD? 
        {
            if (p != null)
            {
                double[] extent = ExtentStringToArray(p);
                Envelope ext = new Envelope()
                {
                    XMin = extent[0],
                    YMin = extent[1],
                    XMax = extent[2],
                    YMax = extent[3],
                    SpatialReference = new SpatialReference() { WKID = 3857 }
                };
                this.UpdateExtent(ext);
            }
        }
        #endregion



        #region Properties
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
        public Envelope Extent
        {
            get { return _extent; }
            set { _extent = value; UpdateWindow(); }
        }
        #endregion



        # region Events
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
                //mapOld.ExtentChanging -= glass.Map_ExtentChanged;
                mapOld.ExtentChanged -= glass.Map_ExtentChanged;
            }
            Map mapNew = e.NewValue as Map;
            if (mapNew != null)
            {
                //mapNew.ExtentChanging += glass.Map_ExtentChanged;
                mapNew.ExtentChanged += glass.Map_ExtentChanged;
            }
        }
        private void Map_ExtentChanged(object sender, ExtentEventArgs e)
        {
            UpdateWindow();
        }
        #endregion

        

        #region Update Window
        private void UpdateWindow()
        {
            if (this.Visibility == Visibility.Collapsed) { return; }
            if (this.Map == null) { return; }

            Envelope lensExtent = _extent;
            
            if (this.Map.Extent != null && this.Map.Extent.Intersects(lensExtent))
            {
                Envelope MapLensIntersectionExtent = lensExtent.Intersection(this.Map.Extent);
                MapLensIntersectionExtent.SpatialReference = new SpatialReference() { WKID = 3857 };
                try
                {
                    ResizeWindow(MapLensIntersectionExtent);
                    TranslateVF(MapLensIntersectionExtent);
                    this.VFMap.Extent = MapLensIntersectionExtent;
                    this.Opacity = 1;
                }
                catch (InvalidOperationException e)
                {
                    //Console.WriteLine("Invalid operation exception! FAIL: " + e.Message);
                }
                catch (Exception e)
                {
                    //Console.WriteLine("FAIL: " + e.Message);
                }
            }
            else
            {
                this.Opacity = 0;
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

        private void TranslateVF(Envelope lensExtent)
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
        }

        private double[] ExtentStringToArray(string extent)
        {
            return Array.ConvertAll(extent.Split(','), Double.Parse);
        }
        #endregion
    }
}
