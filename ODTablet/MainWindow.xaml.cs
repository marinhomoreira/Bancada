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

namespace ODTablet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SOD SoD;

        private string WorldStreetMapUrl = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer";

        

        private string CanadaExtent = "-16133214.9413533,5045906.11392677,-5418285.97972558,10721470.048289";
        private string CalgaryExtent = "-12698770.20, 6629884.68,-12696155.45, 6628808.53";

        private string zoom_map_initial_extent, zoom_map_url,
            heatmap_url, heatmap_initial_extent,
            outline_url, outline_initial_extent;

        public MainWindow()
        {
            InitializeComponent();
            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();

            // setting modes' urls
            zoom_map_url = WorldStreetMapUrl;
            zoom_map_initial_extent = CanadaExtent;

            heatmap_url = WorldStreetMapUrl;
            heatmap_initial_extent = CanadaExtent;

            outline_url = WorldStreetMapUrl;
            outline_initial_extent = CanadaExtent;

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
            SoD.ownDevice.ID = "2";
            SoD.ownDevice.name = "MAIN_WALLDISPLAY";
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
        }

        private void ClearMap_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
        }



        # region Extent

        private double[] ExtentStringToArray(string extent)
        {
            return Array.ConvertAll(extent.Split(','), Double.Parse);
        }

        # endregion


        # region Zoom

        private double currentFactor = 0;
        
        private void InitializeZoomMap()
        {
            Console.WriteLine("Initializing Zoom Map...");
            ArcGISTiledMapServiceLayer zoomLayer = new ArcGISTiledMapServiceLayer { Url = zoom_map_url };
            MyMap.Layers.Add(zoomLayer);

            currentFactor = 1;
            
            double[] extentPoints = ExtentStringToArray(zoom_map_initial_extent);

            ESRI.ArcGIS.Client.Geometry.Envelope myEnvelope = new ESRI.ArcGIS.Client.Geometry.Envelope();
            myEnvelope.XMin = extentPoints[0];
            myEnvelope.YMin = extentPoints[1];
            myEnvelope.XMax = extentPoints[2];
            myEnvelope.YMax = extentPoints[3];
            MyMap.Extent = myEnvelope;

            SendExtentToAllDevices(MyMap.Extent.ToString());
            AddZoomButtons();
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



        void AddHeatMapLayers_LayersInitialized(object sender, EventArgs args)
        {
            //Add 1000 random points to the heat map layer
            //Replace this with "real" data points that you want to display
            //in the heat map.
            HeatMapLayer layer = MyMap.Layers["RandomHeatMapLayer"] as HeatMapLayer;

            Random rand = new Random();
            for (int i = 0; i < 1000; i++)
            {
                double x = rand.NextDouble() * MyMap.Extent.Width - MyMap.Extent.Width / 2;
                double y = rand.NextDouble() * MyMap.Extent.Height - MyMap.Extent.Height / 2;
                layer.HeatMapPoints.Add(new ESRI.ArcGIS.Client.Geometry.MapPoint(x, y));
            }
        }

        private void Heatmap_Click(object sender, RoutedEventArgs e)
        {
            //MyMap.Layers.LayersInitialized += AddHeatMapLayers_LayersInitialized;


        }

        # endregion


        # region Outline


        private void Outline_Click(object sender, RoutedEventArgs e)
        {
            MyMap.Layers.LayersInitialized -= AddHeatMapLayers_LayersInitialized;
            MyMap.Layers.Clear();
            ArcGISTiledMapServiceLayer t2 = new ArcGISTiledMapServiceLayer { Url = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer" };
            MyMap.Layers.Add(t2);
        }

        # endregion

        

    }
}
