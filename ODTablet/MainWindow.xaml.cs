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
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using ESRI.ArcGIS.Client.Symbols;

using ODTablet.MapModel;
using SOD_CS_Library;
using ODTablet.LensViewFinder;
using ESRI.ArcGIS.Client.Geometry;


namespace ODTablet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        SOD SoD;

        private const String
            SatelliteMode = "Satellite"
            , StreetMode = "Street"
            , PopulationMode = "Population"
            , ElectoralDistrictsMode = "ElectoralDistricts"
            , CitiesMode = "City"
            , BaseMap = "BaseMap"
            , BaseMode = "Base";
        // Modes
        private string CurrentMode;

        // UI
        Map BasemapMap;
        Map LensMap;
        StackPanel ModesStackPanel, BackOffStackPanel;

        private Dictionary<string, LensMode> ActiveLens;
        
        Dictionary<string, string> TableConfiguration;

        public MainWindow()
        {
            InitializeComponent();

            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();

            InitializeUIElements();
            ModeDeactivated();

            ActiveLens = new Dictionary<string, LensMode>();
            SendMsgToGetActiveModesFromTable();
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
            });

            SoD.socket.On("dictionary", (dict) =>
            {
                this.ProcessDictionary(SoD.ParseMessageIntoDictionary(dict));
            });

            // make the socket.io connection
            SoD.SocketConnect();
        }

        

        private void ProcessDictionary(Dictionary<string, dynamic> parsedMessage)
        {
            String extentString = (String)parsedMessage["data"]["data"]["Extent"];
            String updateMode = (String)parsedMessage["data"]["data"]["UpdateMode"];
            String removeMode = (String)parsedMessage["data"]["data"]["RemoveMode"];
            String tableConfiguration = (String)parsedMessage["data"]["data"]["TableActiveModes"];

            if (tableConfiguration != null && tableConfiguration.Equals("All"))
            {
                TableConfiguration = parsedMessage["data"]["data"].ToObject<Dictionary<string, string>>();
                UpdateLocalConfiguration(TableConfiguration);
                //UpdateLensExtentMode(updateMode, extentString);
            }
        }

        
        # endregion

        bool isDirty = false;
        private void UpdateLocalConfiguration(Dictionary<string, string> TableConfiguration)
        {
            List<string> remoteActiveModes = new List<string>();
            
            foreach (KeyValuePair<string, string> remoteMode in TableConfiguration)
            {
                string remoteModeName = remoteMode.Key;
                string remoteModeExtent = remoteMode.Value;

                Console.WriteLine(remoteModeName + ": " + remoteModeExtent);

                if(!remoteModeName.Equals("TableActiveModes"))
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        remoteActiveModes.Add(remoteModeName);
                        
                        VerifyAndCreateLens(remoteModeName);
                        
                        // Compare extents.
                        // If remote is different, update local extent
                        Envelope newEnv = StringToEnvelope(remoteModeExtent);
                        if(ActiveLens[remoteModeName].Extent != newEnv)
                        {
                            // Update Extent
                            ActiveLens[remoteModeName].Extent = newEnv;
                            isDirty = true;
                        }
                        
                        // Compare Z positions
                        // If remote is different, update local Z position
                        if(ActiveLens[remoteModeName].UIIndex != remoteActiveModes.IndexOf(remoteModeName))
                        {
                            // Update Z position
                            ActiveLens[remoteModeName].UIIndex = remoteActiveModes.IndexOf(remoteModeName);
                            isDirty = true;
                        }
                    }));
                }
            }
            VerifyIfDirty();
        }

        private void VerifyIfDirty()
        {
            if (isDirty)
            {
                UpdateCurrentModeMap();
                isDirty = false;
            }
        }

        private void UpdateCurrentModeMap()
        {
            if (CurrentMode == null)
            {
                return;
            }

            this.Dispatcher.Invoke((Action)(() =>
            {
                //ConfigMap();
            }));
        }

        private void VerifyAndCreateLens(string Mode)
        {
            // TODO change this method's name for something better
            if (!ActiveLens.ContainsKey(Mode))
            {
                ActiveLens.Add(Mode, new LensFactory().CreateLens(Mode));
                //isDirty = true;
            }
            //VerifyIfDirty(); // TODO: better way to do this?
        }


        private void ConfigMap()
        {
            //ModeActivated();
            Console.WriteLine("Initializing " + CurrentMode + " CurrentMode...");

            LayoutRoot.Children.Clear();

            if (!ActiveLens.ContainsKey(CurrentMode))
            {
                ActiveLens.Add(CurrentMode, new LensFactory().CreateLens(CurrentMode));
            }

            LensMode CurrentLensMode = ActiveLens[CurrentMode];

            LensMode basem = new LensFactory().CreateLens(BaseMap);
            BasemapMap.Layers.Add(basem.MapLayer);
            BasemapMap.Extent = CurrentLensMode.Extent;
            Canvas.SetZIndex(LensMap, 0);

            LensMap.Layers.Add(CurrentLensMode.MapLayer);
            LensMap.Extent = CurrentLensMode.Extent;
            Canvas.SetZIndex(LensMap, CurrentLensMode.UIIndex + 5);

            LayoutRoot.Children.Add(BasemapMap);
            LayoutRoot.Children.Add(LensMap);
            LayoutRoot.Children.Add(BackOffStackPanel);

            foreach (KeyValuePair<string, LensMode> modeKP in ActiveLens)
            {
                string activeModeName = modeKP.Key;
                LensMode activeMode = modeKP.Value;

                if (!CurrentMode.Equals(activeModeName))
                {
                    MapViewFinder mvf = new MapViewFinder(activeMode.Color)
                    {
                        Map = BasemapMap,
                        Name = activeModeName,
                        Layers = new LensFactory().CreateLens(activeModeName).MapLayerCollection,
                        
                    };
                    Canvas.SetZIndex(mvf, activeMode.UIIndex+5);
                    LayoutRoot.Children.Add(mvf);
                    mvf.UpdateExtent(ActiveLens[activeModeName].Extent.ToString());
                }
            }

            switch (CurrentMode)
            {
                case PopulationMode:
                    ESRI.ArcGIS.Client.Toolkit.Legend legend = new ESRI.ArcGIS.Client.Toolkit.Legend();
                    legend.Map = BasemapMap;
                    legend.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    legend.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    legend.LayerIDs = new string[] { CurrentMode };
                    legend.LayerItemsMode = ESRI.ArcGIS.Client.Toolkit.Legend.Mode.Flat;
                    legend.ShowOnlyVisibleLayers = true;
                    Canvas.SetZIndex(legend, 99);
                    LayoutRoot.Children.Add(legend);
                    break;
                default:
                    break;
            }
            
            UpdateExtent(CurrentLensMode.Extent.ToString());
        }





        # region BroadcastCurrentExtent(), SendRemoveLensModeMessage()
        private void BroadcastCurrentExtent()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("UpdateMode", CurrentMode);
            dict.Add("Extent", ActiveLens[CurrentMode].Extent.ToString());
            SoD.SendDictionaryToDevices(dict, "all");
        }

        private void SendRemoveLensModeMessage()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("RemoveMode", CurrentMode);
            SoD.SendDictionaryToDevices(dict, "all");
        }

        private void SendMsgToGetActiveModesFromTable()
        {
            SoD.SendStringToDevices("GetAllModes", "all");
        }


        # endregion



        # region General UI Elements
        private void InitializeUIElements()
        {
            // Initial menu to select the CurrentMode
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
            
            ModesStackPanel.Children.Add(CreateStackPanelButton("All modes msg", GetAllModes_Click));

            // Back and turn off buttons stack panel
            BackOffStackPanel = new StackPanel()
            {
                Name = "BackOffStackPanel",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Orientation = Orientation.Horizontal,
            };

            BackOffStackPanel.Children.Add(CreateStackPanelButton("Back", BackButton_Click));
            BackOffStackPanel.Children.Add(CreateStackPanelButton("Turn off lens", TurnOffLens_Click));

            Canvas.SetZIndex(BackOffStackPanel, 99);

            // Basemap map
            BasemapMap = new Map()
            {
                Name = "BasemapMap",
                WrapAround = false,
                IsLogoVisible = false,
                IsHitTestVisible = false,
            };

            // Actual lens map layer
            LensMap = new Map()
            {
                Name = "LensMap",
                WrapAround = false,
                IsLogoVisible = false,
                IsHitTestVisible = true
            };
            LensMap.ExtentChanging += LensMap_ExtentChanging;
        }

        private void GetAllModes_Click(object sender, RoutedEventArgs e)
        {
            SendMsgToGetActiveModesFromTable();
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

        private void ModeActivated()
        {
            //LayoutRoot.Children.Remove(ModesStackPanel);
            LayoutRoot.Children.Clear();
            LayoutRoot.Children.Add(BasemapMap);
            LayoutRoot.Children.Add(LensMap);
            LayoutRoot.Children.Add(BackOffStackPanel);
        }

        private void ModeDeactivated()
        {
            //LayoutRoot.Children.Remove(BackOffStackPanel);
            LayoutRoot.Children.Clear();
            LayoutRoot.Children.Add(ModesStackPanel);
        }

        private void TurnOffLens_Click(object sender, RoutedEventArgs e)
        {
            SendRemoveLensModeMessage();
        }

        private void SendRemoveAllLensCommand_Click(object sender, RoutedEventArgs e)
        {
            CurrentMode = "All"; // TODO: Hardcoding CurrentMode. Too bad.
            SendRemoveLensModeMessage();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
            ModeDeactivated();
        }

        #endregion


        private Envelope StringToEnvelope(String extentString)
        {
            double[] extentPoints = Array.ConvertAll(extentString.Split(','), Double.Parse);
            ESRI.ArcGIS.Client.Geometry.Envelope myEnvelope = new ESRI.ArcGIS.Client.Geometry.Envelope();
            myEnvelope.XMin = extentPoints[0];
            myEnvelope.YMin = extentPoints[1];
            myEnvelope.XMax = extentPoints[2];
            myEnvelope.YMax = extentPoints[3];
            myEnvelope.SpatialReference = this.BasemapMap.SpatialReference;
            return myEnvelope;
        }

        private void UpdateExtent(string extentString)
        {
            LensMap.Extent = StringToEnvelope(extentString);
            BroadcastCurrentExtent();
        }

        private void ClearMap()
        {
            Console.WriteLine("Cleaning map...");
            CurrentMode = null;
            LensMap.Layers.Clear();
            BasemapMap.Layers.Clear();
        }

        private void LensMap_ExtentChanging(object sender, ExtentEventArgs e)
        {
            //ModeExtentDic[CurrentMode] = e.NewExtent.ToString();
            BasemapMap.Extent = e.NewExtent;
            ActiveLens[CurrentMode].Extent = e.NewExtent;
            foreach (UIElement element in LayoutRoot.Children)
            {
                if (element is MapViewFinder)
                {
                    ((MapViewFinder)element).TranslateVF(e.NewExtent);
                }
            }
            BroadcastCurrentExtent();
        }

        # region Mode buttons
        private void SatelliteMode_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
            CurrentMode = SatelliteMode;
            ConfigMap();
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
        #endregion

        
        



    }
}
