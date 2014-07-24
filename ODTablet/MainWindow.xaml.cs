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

        static private string
              WorldStreetMap = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer" // Streets!
            , WorldShadedRelief = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Shaded_Relief/MapServer" // Just shades
            , WorldSatelliteImagery = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer" // Images from satellites
            , WorldBoundariesAndPlacesLabels = "http://server.arcgisonline.com/arcgis/rest/services/Reference/World_Boundaries_and_Places/MapServer" // Just labels
            , CanadaElectoralDistricts = "http://136.159.14.25:6080/arcgis/rest/services/Politik/Boundaries/MapServer"
            , CanadaPopulationDensity = "http://maps.esri.ca/arcgis/rest/services/StatsServices/PopulationDensity/MapServer/"
            ;

        static private string
            CanadaExtent = "-16133214.9413533,5045906.11392677,-5418285.97972558,10721470.048289";
            //, CalgaryExtent = "-12698770.20, 6629884.68,-12696155.45, 6628808.53";

        // Modes
        private string CurrentMode;
        private Dictionary<String, String> ModeExtentDic, UrlDic;
        private const String
            SatelliteMode = "Satellite"
            , StreetMode = "Street"
            , PopulationMode = "Population"
            , ElectoralDistrictsMode = "ElectoralDistricts"
            , CitiesMode = "City"
            //, ZoomMode = "Zoom" // TODO: To be implemented
            , BaseMap = "BaseMap";

        // UI
        Map MyMap;
        StackPanel ModesStackPanel, ModeSettingsStackPanel, BackOffStackPanel;


        public MainWindow()
        {
            InitializeComponent();
            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();

            SetUpDictionaries();

            InitializeUIElements();
            ModeDeactivated();
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
                //Console.WriteLine("Received string: " + parsedMessage["data"]);
            });

            // make the socket.io connection
            SoD.SocketConnect();
        }

        # endregion

        private void SetUpDictionaries()
        {
            UrlDic = new Dictionary<string, string>() {
                {SatelliteMode, WorldSatelliteImagery},
                {StreetMode, WorldStreetMap},
                {PopulationMode, CanadaPopulationDensity},
                {ElectoralDistrictsMode, CanadaElectoralDistricts},
                {CitiesMode, WorldBoundariesAndPlacesLabels},
                //{ZoomMode, WorldShadedRelief}, // TODO: To be implemented
                {BaseMap, WorldShadedRelief}
            };

            ModeExtentDic = new Dictionary<string, string>() {
                {SatelliteMode, CanadaExtent},
                {StreetMode, CanadaExtent},
                {PopulationMode, CanadaExtent},
                {ElectoralDistrictsMode, CanadaExtent},
                {CitiesMode, CanadaExtent},
                //{ZoomMode, CanadaExtent}, // TODO: To be implemented
                {BaseMap, CanadaExtent}
            };
        }

        # region General UI Elements
        private void InitializeUIElements()
        {
            ModesStackPanel = new StackPanel()
            {
                Name = "ModesStackPanel",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            ModesStackPanel.Children.Add(CreateStackPanelButton(PopulationMode, PopulationMode_Click));
            ModesStackPanel.Children.Add(CreateStackPanelButton(ElectoralDistrictsMode, ElectoralDistricts_Click));
            ModesStackPanel.Children.Add(CreateStackPanelButton(SatelliteMode, SatelliteMode_Click));
            ModesStackPanel.Children.Add(CreateStackPanelButton(StreetMode, StreetsMode_Click));
            ModesStackPanel.Children.Add(CreateStackPanelButton(CitiesMode, CitiesMode_Click));
            ModesStackPanel.Children.Add(CreateStackPanelButton("Remove all lens", SendRemoveAllLensCommand_Click));

            // Only used in Zoom Mode!
            // TODO: Remove if not being used
            ModeSettingsStackPanel = new StackPanel()
            {
                Name = "ModeSettingsStackPanel",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                Orientation = Orientation.Horizontal,
            };
            
            BackOffStackPanel = new StackPanel()
            {
                Name = "BackOffStackPanel",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Orientation = Orientation.Horizontal,
            };

            BackOffStackPanel.Children.Add(CreateStackPanelButton("Back", BackButton_Click));
            BackOffStackPanel.Children.Add(CreateStackPanelButton("Turn off lens", TurnOffLens_Click));


            MyMap = new Map()
            {
                Name = "MyMap",
                WrapAround = false,
                IsLogoVisible = false,
            };
            MyMap.ExtentChanging += MyMap_ExtentChanging;
        }

        private void ModeActivated()
        {
            LayoutRoot.Children.Remove(ModesStackPanel);
            LayoutRoot.Children.Add(MyMap);
            LayoutRoot.Children.Add(BackOffStackPanel);
            LayoutRoot.Children.Add(ModeSettingsStackPanel);
        }

        private void ModeDeactivated()
        {
            LayoutRoot.Children.Remove(BackOffStackPanel);
            LayoutRoot.Children.Remove(ModeSettingsStackPanel);
            LayoutRoot.Children.Remove(MyMap);
            LayoutRoot.Children.Add(ModesStackPanel);
        }

        private Button CreateStackPanelButton(string content, RoutedEventHandler reh)
        {
            Button b = new Button();
            b.Content = content;
            b.Width = 100;
            b.Height = 50;
            b.Click += reh;
            return b;
        }

        private void TurnOffLens_Click(object sender, RoutedEventArgs e)
        {
            SendRemoveLensModeMessage();
        }

        private void SendRemoveAllLensCommand_Click(object sender, RoutedEventArgs e)
        {
            CurrentMode = "All"; // TODO: Hardcoding mode. Too bad.
            SendRemoveLensModeMessage();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
            ModeDeactivated();
        }

        #endregion

        # region BroadcastCurrentExtent(), SendRemoveLensModeMessage()
        private void BroadcastCurrentExtent()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("UpdateMode", CurrentMode);
            dict.Add("Extent", ModeExtentDic[CurrentMode]);
            SoD.SendDictionaryToDevices(dict, "all");
        }

        private void SendRemoveLensModeMessage()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("RemoveMode", CurrentMode);
            SoD.SendDictionaryToDevices(dict, "all");
        }

        # endregion
        
        private void ClearMap()
        {
            Console.WriteLine("Cleaning map...");
            CurrentMode = null;
            MyMap.Layers.Clear();
            ModeSettingsStackPanel.Children.Clear();
            SetBaseMapLayer(UrlDic[BaseMap]);
        }

        private void SetBaseMapLayer(string url)
        {
            int basemapLayerIndex = MyMap.Layers.IndexOf(MyMap.Layers[BaseMap]);

            ArcGISTiledMapServiceLayer BaseMapLayer = new ArcGISTiledMapServiceLayer { Url = url };
            BaseMapLayer.ID = BaseMap;

            if (basemapLayerIndex != -1)
            {
                MyMap.Layers.RemoveAt(basemapLayerIndex);
                MyMap.Layers.Insert(basemapLayerIndex, BaseMapLayer);
            }
            else
            {
                MyMap.Layers.Add(BaseMapLayer);
            }
        }

        private void UpdateExtent(string extentString)
        {
            double[] extentPoints = Array.ConvertAll(extentString.Split(','), Double.Parse);

            ESRI.ArcGIS.Client.Geometry.Envelope myEnvelope = new ESRI.ArcGIS.Client.Geometry.Envelope();
            myEnvelope.XMin = extentPoints[0];
            myEnvelope.YMin = extentPoints[1];
            myEnvelope.XMax = extentPoints[2];
            myEnvelope.YMax = extentPoints[3];
            MyMap.Extent = myEnvelope;
            ModeExtentDic[CurrentMode] = extentString;
            BroadcastCurrentExtent();
        }

        private void MyMap_ExtentChanging(object sender, ExtentEventArgs e)
        {
            ModeExtentDic[CurrentMode] = e.NewExtent.ToString();
            BroadcastCurrentExtent();
        }

        private void ConfigMap()
        {
            ModeActivated();
            Console.WriteLine("Initializing " + CurrentMode + " mode...");
            switch (CurrentMode)
            {
                case SatelliteMode:
                    SetBaseMapLayer(UrlDic[CurrentMode]);
                    break;
                case StreetMode:
                    SetBaseMapLayer(WorldStreetMap);
                    break;
                case PopulationMode:
                    SetBaseMapLayer(UrlDic[BaseMap]);
                    ArcGISDynamicMapServiceLayer popl = new ArcGISDynamicMapServiceLayer { Url = UrlDic[CurrentMode], ID = CurrentMode };
                    MyMap.Layers.Add(popl);
                    ESRI.ArcGIS.Client.Toolkit.Legend legend = new ESRI.ArcGIS.Client.Toolkit.Legend();
                    legend.Map = MyMap;
                    legend.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    legend.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    legend.LayerIDs = new string[] { CurrentMode };
                    legend.LayerItemsMode = ESRI.ArcGIS.Client.Toolkit.Legend.Mode.Flat;
                    legend.ShowOnlyVisibleLayers = true;
                    LayoutRoot.Children.Add(legend);
                    break;
                case ElectoralDistrictsMode:
                    SetBaseMapLayer(UrlDic[BaseMap]);
                    ArcGISDynamicMapServiceLayer OutlineLayer = new ArcGISDynamicMapServiceLayer { Url = UrlDic[CurrentMode] };
                    OutlineLayer.DisableClientCaching = false;
                    OutlineLayer.ID = CurrentMode;
                    MyMap.Layers.Add(OutlineLayer);
                    break;
                case CitiesMode:
                    SetBaseMapLayer(WorldShadedRelief);
                    ArcGISTiledMapServiceLayer LabelsLayer = new ArcGISTiledMapServiceLayer { Url = WorldBoundariesAndPlacesLabels };
                    LabelsLayer.ID = CurrentMode;
                    MyMap.Layers.Add(LabelsLayer);
                    break;
                //case ZoomMode:
                // TODO : Does it make sense?
                //    break;
                default:
                    break;
            }
            UpdateExtent(ModeExtentDic[CurrentMode]);
        }

        private void PopulationMode_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
            CurrentMode = PopulationMode;
            ConfigMap();
        }

        private void ElectoralDistricts_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
            CurrentMode = ElectoralDistrictsMode;
            ConfigMap();
        }

        private void SatelliteMode_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
            CurrentMode = SatelliteMode;
            ConfigMap();
        }

        private void StreetsMode_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
            CurrentMode = StreetMode;
            ConfigMap();
        }

        private void CitiesMode_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
            CurrentMode = CitiesMode;
            ConfigMap();
        }

        




        /*

        # region Zoom

        private double currentFactor = 0;
        
        private void InitializeZoomMap()
        {
            Console.WriteLine("Initializing Zoom Map...");
            SetBaseMapLayer(WorldSatelliteImagery);

            currentFactor = 1;

            UpdateExtent(ModeExtentDic["Zoom"]);

            ModeSettingsStackPanel.Children.Add(CreateZoomButton("8x", Zoom8_Click));
            ModeSettingsStackPanel.Children.Add(CreateZoomButton("16x", Zoom16_Click));
            ModeSettingsStackPanel.Children.Add(CreateZoomButton("32x", Zoom32_Click));
            ModeSettingsStackPanel.Children.Add(CreateZoomButton("64x", Zoom64_Click));
            ZoomIt(8);
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

        */

    }
}
