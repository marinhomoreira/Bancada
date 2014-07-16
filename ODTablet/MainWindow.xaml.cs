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


// ArcGIS
using ESRI.ArcGIS.Client;

using SOD_CS_Library;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using ESRI.ArcGIS.Client.Symbols;

namespace ODTablet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SOD SoD;

        private string 
              WorldStreetMapUrl = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer" // Streets!
            , WorldShadedRelief = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Shaded_Relief/MapServer" // Just shades
            , WorldImagery = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer" // Images from satellites
            , WorldBoundaries = "http://server.arcgisonline.com/arcgis/rest/services/Reference/World_Boundaries_and_Places/MapServer" // Just labels
            , CanadaBaseMap = "http://geoappext.nrcan.gc.ca/arcgis/rest/services/BaseMaps/CBMT3978/MapServer" // Canada Base Map - Transportation
            , CanadaBoundariesBaseMap = "http://geoappext.nrcan.gc.ca/arcgis/rest/services/BaseMaps/CBME_CBCE_HS_RO_3978/MapServer" // Canada Base Map - 
            , CanadaSoilMap = "http://edumaps.esri.ca/arcgis/rest/services/MapServices/Soils/MapServer" // soils
            , Languages = "http://edumaps.esricanada.com/ArcGIS/rest/services/Demographics/Languages/MapServer" // has three layers
            , PolitikBoundaries = "http://136.159.14.25:6080/arcgis/rest/services/Politik/Boundaries/MapServer/0"
            ;

        private string
            CanadaExtent = "-16133214.9413533,5045906.11392677,-5418285.97972558,10721470.048289"
            , CalgaryExtent = "-12698770.20, 6629884.68,-12696155.45, 6628808.53";

        private string basemap_url,
            zoom_map_url, zoom_map_initial_extent,
            heatmap_url, heatmap_initial_extent,
            outline_url, outline_initial_extent;

        public MainWindow()
        {
            InitializeComponent();
            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();

            basemap_url = WorldImagery;

            // setting modes' urls
            zoom_map_url = WorldImagery;
            zoom_map_initial_extent = CanadaExtent;

            heatmap_url = WorldShadedRelief; //CanadaSoilMap;
            heatmap_initial_extent = CanadaExtent;

            outline_url = PolitikBoundaries;
            outline_initial_extent = CanadaExtent;

            SetBaseMapLayer(basemap_url);
            
            InitializeZoomMap(); //it only works if it's initialized, otherwise, need two clicks to show the map! wtf?
            
        }

        # region SoD
        private void ConfigureSoD()
        {
            // Configure and instantiate SOD object
            string address = "beastwin.marinhomoreira.com";
            int port = 3000;
            SoD = new SOD(address, port);
        }

        private void ConfigureDevice()
        {
            // Configure device with its dimensions (mm), location in physical space (X, Y, Z in meters, from sensor), orientation (degrees), Field Of View (FOV. degrees) and name
            double widthInMM = 750
                , heightInMM = 500
                , locationX = -2
                , locationY = 3
                , locationZ = 1;
            string deviceType = "WallDisplay";
            bool stationary = true;
            SoD.ownDevice.SetDeviceInformation(widthInMM, heightInMM, locationX, locationY, locationZ, deviceType, stationary);
            SoD.ownDevice.orientation = 300;
            SoD.ownDevice.FOV = 180;

            // Name and ID of device - displayed in Locator
            // TODO: Future: possible to look for devices using name, instead of ID.
            SoD.ownDevice.ID = "11";
            SoD.ownDevice.name = "ODTablet";
        }

        private void RegisterSoDEvents()
        {
            // register for 'connect' event with io server
            SoD.socket.On("connect", (data) =>
            {
                Console.WriteLine("\r\nConnected...");
                Console.WriteLine("Registering with server...\r\n");
                SoD.RegisterDevice();  //register the device with server everytime it connects or re-connects
            });

            SoD.socket.On("string", (data) =>
            {
                Dictionary<string, dynamic> parsedMessage = SoD.ParseMessageIntoDictionary(data);
                Console.WriteLine("Received string: " + parsedMessage["data"]);
            });

            // make the socket.io connection
            SoD.SocketConnect();
        }

        # endregion


        # region Utils
        private void MyMap_ExtentChanging(object sender, ExtentEventArgs e)
        {
            SendExtentToAllDevices(e.NewExtent.ToString());
        }
        
        private void SendExtentToAllDevices(string extentString)
        {
            SoD.SendStringToDevices(extentString, "all");
        }
        
        private void ClearMap()
        {
            Console.WriteLine("Cleaning map...");
            MyMap.Layers.LayersInitialized -= AddHeatMapLayers_LayersInitialized;
            MyMap.Layers.Clear();
            ModeStack.Children.Clear();
            SetBaseMapLayer(basemap_url);
        }

        private void ClearMap_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
        }
        # endregion


        # region Extent

        private double[] ExtentStringToArray(string extent)
        {
            return Array.ConvertAll(extent.Split(','), Double.Parse);
        }

        private void UpdateExtent(string extentString)
        {
            double[] extentPoints = ExtentStringToArray(extentString);

            ESRI.ArcGIS.Client.Geometry.Envelope myEnvelope = new ESRI.ArcGIS.Client.Geometry.Envelope();
            myEnvelope.XMin = extentPoints[0];
            myEnvelope.YMin = extentPoints[1];
            myEnvelope.XMax = extentPoints[2];
            myEnvelope.YMax = extentPoints[3];
            MyMap.Extent = myEnvelope;
        }

        # endregion


        # region Zoom

        private double currentFactor = 0;
        
        private void InitializeZoomMap()
        {
            Console.WriteLine("Initializing Zoom Map...");
            //SetBaseMapLayer(zoom_map_url);

            currentFactor = 1;
            
            UpdateExtent(zoom_map_initial_extent);

            SendExtentToAllDevices(MyMap.Extent.ToString());
            AddZoomButtons();
            ZoomIt(8);
        }

        private void AddZoomButtons()
        {
            ModeStack.Children.Add(CreateZoomButton("8x", Zoom8_Click));
            ModeStack.Children.Add(CreateZoomButton("16x", Zoom16_Click));
            ModeStack.Children.Add(CreateZoomButton("32x", Zoom32_Click));
            ModeStack.Children.Add(CreateZoomButton("64x", Zoom64_Click));
        }

        private Button CreateZoomButton(string content, RoutedEventHandler reh)
        {
            Button b = new Button();
            b.Content = content;
            b.Width = 35;
            b.Click += reh;
            return b;
        }

        private void ZoomIt(double factor)
        {
            if (currentFactor > 0)
            {
                MyMap.Zoom(factor / currentFactor);
                currentFactor = factor;
            }
            else
            {
                ResetZoomMap();
            }
        }

        private void ResetZoomMap()
        {
            ClearMap();
            InitializeZoomMap();
        }

        # region Zoom Buttons
        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            ResetZoomMap();
        }

        private void Zoom8_Click(object sender, RoutedEventArgs e)
        {
            ZoomIt(8);

        }

        private void Zoom16_Click(object sender, RoutedEventArgs e)
        {
            ZoomIt(16);
        }

        private void Zoom32_Click(object sender, RoutedEventArgs e)
        {
            ZoomIt(32);
        }

        private void Zoom64_Click(object sender, RoutedEventArgs e)
        {
            ZoomIt(64);
        }
        # endregion

        # endregion


        # region Heat map
        private void InitializeHeatMap()
        {
            Console.WriteLine("Initializing Heat Map...");
            
            //SetBaseMapLayer(heatmap_url);
            
            //HeatMapLayer heatMapLayer = new HeatMapLayer();
            //heatMapLayer.ID = "RandomHeatMapLayer";
            //MyMap.Layers.Add(heatMapLayer);


            ArcGISDynamicMapServiceLayer popl = new ArcGISDynamicMapServiceLayer { Url = "http://maps.esri.ca/arcgis/rest/services/StatsServices/PopulationDensity/MapServer/", ID="PopulationLayer" };
            MyMap.Layers.Add(popl);

            ESRI.ArcGIS.Client.Toolkit.Legend legend = new ESRI.ArcGIS.Client.Toolkit.Legend();
            legend.Map = MyMap;
            legend.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            legend.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            legend.LayerIDs = new string[] {"PopulationLayer"};
            legend.LayerItemsMode = ESRI.ArcGIS.Client.Toolkit.Legend.Mode.Flat;
            legend.ShowOnlyVisibleLayers = true;

            LayoutRoot.Children.Add(legend);

            UpdateExtent(heatmap_initial_extent);

            SendExtentToAllDevices(MyMap.Extent.ToString());
            AddHeatMapButtons();

            //MyMap.Layers.LayersInitialized += AddHeatMapLayers_LayersInitialized;
        }

        private void AddHeatMapButtons()
        {
            // TODO: Heat map controls?
        }

        private void ResetHeatMap()
        {
            ClearMap();
            InitializeHeatMap();
        }


        void AddHeatMapLayers_LayersInitialized(object sender, EventArgs args)
        {
            //Add 1000 random points to the heat map layer
            //Replace this with "real" data points that you want to display
            //in the heat map.



            //TODO : ADD HEATMAP HERE!
            //HeatMapLayer layer = MyMap.Layers["RandomHeatMapLayer"] as HeatMapLayer;

            //double[] extentPoints = ExtentStringToArray(heatmap_initial_extent);

            //double XMin = extentPoints[0]; // XMin
            //double YMin = extentPoints[1]; // YMin
            //double XMax = extentPoints[2]; // XMax
            //double YMax = extentPoints[3]; // YMax

            //double dX = (XMax - XMin)/4;
            //double dY = (YMax - YMin)/4;


            //Random rand = new Random();
            //for (int i = 0; i < 1000; i++)
            //{
            //    //double xW = rand.NextDouble() * MyMap.Extent.Width - MyMap.Extent.Width / 2;
            //    //double yH = rand.NextDouble() * MyMap.Extent.Height - MyMap.Extent.Height / 2;
            //    double x = XMin + rand.NextDouble() * dX;
            //    double y = YMin + rand.NextDouble() * dY;

            //    //Console.WriteLine("x: " + x + " XMin: " + XMin + " XMax: " + XMin + " YMin: " + YMin + " YMax: " + YMax + " y: " + y);

            //    layer.HeatMapPoints.Add(new ESRI.ArcGIS.Client.Geometry.MapPoint(x-1000, y-1000));
            //}
        }

        private void Heatmap_Click(object sender, RoutedEventArgs e)
        {
            ResetHeatMap();
        }

        # endregion


        # region Outline
        private void InitializeOutlineMap()
        {
            Console.WriteLine("Initializing Outline Map...");
            
            
            //SetBaseMapLayer(basemap_url);

            

            //FeatureLayer OutlineLayer = new FeatureLayer { Url = outline_url };
            //ArcGISTiledMapServiceLayer OutlineLayer = new ArcGISTiledMapServiceLayer { Url = "http://136.159.14.25:6080/arcgis/rest/services/Politik/Boundaries/MapServer" };
            AddOutlineLayer();

            UpdateExtent(outline_initial_extent);

            SendExtentToAllDevices(MyMap.Extent.ToString());
            AddOutlineButtons();
        }

        private void AddOutlineButtons()
        {
            ModeStack.Children.Add(CreateOutlineButton("Remove Black Outline", RemoveOutline_Click));
        }

        private void AddOutlineLayer()
        {
            ArcGISDynamicMapServiceLayer OutlineLayer = new ArcGISDynamicMapServiceLayer { Url = "http://136.159.14.25:6080/arcgis/rest/services/Politik/Boundaries/MapServer" };
            OutlineLayer.ID = "Outline";
            MyMap.Layers.Add(OutlineLayer);
        }

        private void RemoveOutlineLayer()
        {
            MyMap.Layers.Remove(MyMap.Layers["Outline"]);
        }

        private Button CreateOutlineButton(string content, RoutedEventHandler reh)
        {
            Button b = new Button();
            b.Content = content;
            //b.Width = 35;
            b.Click += reh;
            return b;
        }


        private void ResetOutlineMap()
        {
            ClearMap();
            InitializeOutlineMap();
        }

        private void Outline_Click(object sender, RoutedEventArgs e)
        {
            ResetOutlineMap();
        }

        private void RemoveOutline_Click(object sender, RoutedEventArgs e)
        {
            RemoveOutlineLayer();
        }

        # endregion




        private bool LabelsOn = false; // TODO: Remove flag

        private void ToggleLabels()
        {
            if (!LabelsOn)
            {
                ArcGISTiledMapServiceLayer LabelsLayer = new ArcGISTiledMapServiceLayer { Url = WorldBoundaries };
                LabelsLayer.ID = "Labels";
                MyMap.Layers.Add(LabelsLayer);
                LabelsOn = true;
            }
            else
            {
                MyMap.Layers.Remove(MyMap.Layers["Labels"]);
                LabelsOn = false;
            }
        }

        private void AddLabels_Click(object sender, RoutedEventArgs e)
        {
            ToggleLabels();
        }
        

        private void SetBaseMapLayer(string url)
        {
            basemap_url = url;
            int index = MyMap.Layers.IndexOf(MyMap.Layers["BaseMap"]);
            Console.WriteLine("index: " + index);

            ArcGISTiledMapServiceLayer BaseMapLayer = new ArcGISTiledMapServiceLayer { Url = basemap_url };
            BaseMapLayer.ID = "BaseMap";

            if(index >= 0)
            {
                MyMap.Layers.RemoveAt(index);
                MyMap.Layers.Insert(index, BaseMapLayer);
            } else
            {
                MyMap.Layers.Add(BaseMapLayer);
            }

            //MyMap.Layers.Remove(MyMap.Layers["BaseMap"]);
            
            //MyMap.Layers.Remove(MyMap.Layers["Labels"]);
            //ArcGISDynamicMapServiceLayer OutlineLayer = new ArcGISDynamicMapServiceLayer { Url = url };
            
            
            //if(LabelsOn)
            //{
            //    LabelsOn = false;
            //    ToggleLabels();
            //}
            
        }

        

        private void ImageryMap_Click(object sender, RoutedEventArgs e)
        {
            SetBaseMapLayer(WorldImagery);
        }

        private void ShadedMap_Click(object sender, RoutedEventArgs e)
        {
            SetBaseMapLayer(WorldShadedRelief);
        }

        private void StreetMap_Click(object sender, RoutedEventArgs e)
        {
            SetBaseMapLayer(WorldStreetMapUrl);
        }



    }
}
