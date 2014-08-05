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


        # region Initialization - Variables

        private const String
                        SatelliteMode = "Satellite"
                        , StreetMode = "Street"
                        , PopulationMode = "Population"
                        , ElectoralDistrictsMode = "ElectoralDistricts"
                        , CitiesMode = "City"
                        , BaseMap = "BaseMap"
                        , BaseMode = "Base";

        private StackPanel
                        ModesListMenu
                        , BackOffMenu;

        // UI
        private Map
                BasemapMap
                , LensMap;


        // Modes
        private string CurrentMode;

        private Dictionary<string, LensMode> ActiveLens;

        # endregion

        public MainWindow()
        {
            InitializeComponent();

            InitializeUIElements();
            DisplayStartMenu();

            ActiveLens = new Dictionary<string, LensMode>();

            // SOD stuff
            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();
        }

        # region SoD
        
        SOD SoD;
        
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
                //Dictionary<string, dynamic> parsedMessage = SoD.ParseMessageIntoDictionary(data);
            });

            SoD.socket.On("dictionary", (dict) =>
            {
                this.ProcessDictionary(SoD.ParseMessageIntoDictionary(dict));
            });

            // make the socket.io connection
            SoD.SocketConnect();
        }

        # endregion

        # region BroadcastCurrentExtent(), SendRemoveLensModeMessage(), SendMsgToGetActiveModesFromTable()
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

            
            // Start menu
            ModesListMenu = new StackPanel()
            {
                Name = "ModesListMenu",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            // Menu with Back and Turn off buttons
            BackOffMenu = new StackPanel()
            {
                Name = "BackOffMenu",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Orientation = Orientation.Horizontal,
            };

            // Adding buttons for each menu
            ModesListMenu.Children.Add(CreateStackPanelButton(PopulationMode, ModeButton_Click));
            ModesListMenu.Children.Add(CreateStackPanelButton(ElectoralDistrictsMode, ModeButton_Click));
            ModesListMenu.Children.Add(CreateStackPanelButton(SatelliteMode, ModeButton_Click));
            ModesListMenu.Children.Add(CreateStackPanelButton(StreetMode, ModeButton_Click));
            ModesListMenu.Children.Add(CreateStackPanelButton(CitiesMode, ModeButton_Click));
            ModesListMenu.Children.Add(CreateStackPanelButton("Remove all lens", SendRemoveAllLensCommand_Click));
            ModesListMenu.Children.Add(CreateStackPanelButton("All modes msg", GetAllModes_Click));

            BackOffMenu.Children.Add(CreateStackPanelButton("Change lens", BackButton_Click));
            BackOffMenu.Children.Add(CreateStackPanelButton("Turn off this lens", TurnOffLens_Click));

            // Canvas position
            Canvas.SetZIndex(BackOffMenu, 99);

            LensMap.ExtentChanging += LensMap_ExtentChanging;

        }

        private void DisplayStartMenu()
        {
            LayoutRoot.Children.Clear();
            LayoutRoot.Children.Add(ModesListMenu);
            DetailWindow.Title = "Detail";
        }

        private void ClearMapCanvas()
        {
            Console.WriteLine("Cleaning map...");
            CurrentMode = null; // TODO : Is this really here?
            LensMap.Layers.Clear();
            BasemapMap.Layers.Clear();
            LayoutRoot.Children.Clear();
        }

        # region Buttons
        private Button CreateStackPanelButton(string content, RoutedEventHandler reh)
        {
            Button b = new Button();
            b.Content = content;
            b.Width = 100;
            b.Height = 50;
            b.Click += reh;
            try
            {
                b.Name = content;
            }
            catch (Exception e)
            {
                b.BorderBrush = Brushes.Red;
                Console.WriteLine("Fail: " + e.Message);
            }
            return b;
        }

        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            //SendMsgToGetActiveModesFromTable();
            CurrentMode = ((Button)sender).Name;
            InitializeModeUI();
            BroadcastCurrentExtent();
        }

        private void TurnOffLens_Click(object sender, RoutedEventArgs e)
        {
            SendRemoveLensModeMessage();
            ActiveLens.Remove(CurrentMode);
            ClearMapCanvas();
            DisplayStartMenu();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ClearMapCanvas();
            DisplayStartMenu();
        }

        private void SendRemoveAllLensCommand_Click(object sender, RoutedEventArgs e)
        {
            ActiveLens.Clear();
            ClearMapCanvas();
            CurrentMode = "All"; // TODO: Hardcoding CurrentMode. Too bad.
            SendRemoveLensModeMessage();
            DisplayStartMenu();
        }

        # endregion
        # endregion

        # region Map UI
        ESRI.ArcGIS.Client.Toolkit.Legend legend; // TODO: Remove it from here when solving legend problem.
        private void InitializeModeUI()
        {
            Console.WriteLine("Initializing " + CurrentMode + " mode...");
            DetailWindow.Title = CurrentMode; // Change Window Title according to the mode.

            LayoutRoot.Children.Clear();

            ActivateMode(CurrentMode);

            LensMode CurrentLensMode = ActiveLens[CurrentMode];

            LensMode basem = new LensFactory().CreateLens(BaseMap);
            BasemapMap.Layers.Add(basem.MapLayer);
            BasemapMap.Extent = CurrentLensMode.Extent;
            Canvas.SetZIndex(LensMap, 0);
            
            LensMap.Layers.Add(CurrentLensMode.MapLayer);
            LensMap.Extent = CurrentLensMode.Extent;
            Canvas.SetZIndex(LensMap, CurrentLensMode.UIIndex);

            LayoutRoot.Children.Add(BasemapMap);
            LayoutRoot.Children.Add(LensMap);
            LayoutRoot.Children.Add(BackOffMenu);

            if (CurrentMode.Equals(PopulationMode))
            {
                // TODO: Y U NO WORK?!
                legend = new ESRI.ArcGIS.Client.Toolkit.Legend();
                legend.Map = LensMap;
                legend.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                legend.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                legend.LayerIDs = new string[] { CurrentMode };
                legend.LayerItemsMode = ESRI.ArcGIS.Client.Toolkit.Legend.Mode.Flat;
                legend.ShowOnlyVisibleLayers = true;
                Canvas.SetZIndex(legend, 99);
                LayoutRoot.Children.Add(legend);
            }

            AddActiveLensesToScreen();
            
            BroadcastCurrentExtent();
            
            UpdateAllLensAccordingCurrentModeExtent();
        }

        private void AddActiveLensesToScreen()
        {
            foreach (KeyValuePair<string, LensMode> modeKP in ActiveLens)
            {
                string activeModeName = modeKP.Key;
                LensMode activeMode = modeKP.Value;

                if (!CurrentMode.Equals(activeModeName))
                {
                    MapViewFinder mvf = new MapViewFinder(activeMode.Color, ActiveLens[activeModeName].Extent.ToString())
                    {
                        Map = LensMap,
                        Name = activeModeName,
                        Layers = new LensFactory().CreateLens(activeModeName).MapLayerCollection,
                    };
                    Canvas.SetZIndex(mvf, activeMode.UIIndex);
                    LayoutRoot.Children.Add(mvf);
                    mvf.UpdateExtent(ActiveLens[activeModeName].Extent.ToString());
                }
            }
            UpdateAllLensAccordingCurrentModeExtent();
        }

        private void UpdateAllLensAccordingCurrentModeExtent()
        {
            foreach (UIElement element in LayoutRoot.Children)
            {
                if (element is MapViewFinder)
                {
                    // PERFORMANCE: IT'S FASTER DOING THIS THAN WITH THE EVENT.
                    ((MapViewFinder)element).UpdateExtent(ActiveLens[((MapViewFinder)element).Name].Extent.ToString());
                    if (Canvas.GetZIndex(element) != ActiveLens[((MapViewFinder)element).Name].UIIndex)
                    {
                        Canvas.SetZIndex(element, ActiveLens[((MapViewFinder)element).Name].UIIndex);
                    }
                }
            }
        }

        private void LensMap_ExtentChanging(object sender, ExtentEventArgs e)
        {
            BasemapMap.Extent = e.NewExtent;
            ActiveLens[CurrentMode].Extent = e.NewExtent;
            UpdateAllLensAccordingCurrentModeExtent();
            BroadcastCurrentExtent();
        }

        private void ActivateMode(string mode)
        {
            if (!ActiveLens.ContainsKey(mode))
            {
                ActiveLens.Add(mode, new LensFactory().CreateLens(mode));
                // There's only two lenses, if you remove the element that is below other and first (aka, has lowest Z and it's in index 0), 
                // when another element is added, it will be added to element 0 and the count will be 1. the resultant index will be 1.
                ActiveLens[mode].UIIndex = ActiveLens.Count() + 1; // TODO: How to define this? Get highest uiindex from all elements in the dictionary?
            }
        }

        # endregion

       




        private void GetAllModes_Click(object sender, RoutedEventArgs e)
        {
            SendMsgToGetActiveModesFromTable();
        }


        private void ProcessDictionary(Dictionary<string, dynamic> parsedMessage)
        {
            String extentString = (String)parsedMessage["data"]["data"]["Extent"];
            String updateMode = (String)parsedMessage["data"]["data"]["UpdateMode"];
            String removeMode = (String)parsedMessage["data"]["data"]["RemoveMode"];
            String tableConfiguration = (String)parsedMessage["data"]["data"]["TableActiveModes"];

            if (tableConfiguration != null && tableConfiguration.Equals("All"))
            {
                //TODO: DIFF DA PORRA TODA COM ACTIVELENS.
                this.Dispatcher.Invoke((Action)(() => {
                    UpdateLocalConfiguration(parsedMessage["data"]["data"].ToObject<Dictionary<string, string>>());
                }));
            }

            if (updateMode != null && !updateMode.Equals(CurrentMode))
            {
                ActivateMode(updateMode);
                ActiveLens[updateMode].Extent = StringToEnvelope(extentString);
            }

            if (removeMode != null && !removeMode.Equals(CurrentMode))
            {
                ActiveLens.Remove(removeMode);
            }
            
            //TODO: Update UI based on ActiveLens
        }

        private void UpdateLocalConfiguration(Dictionary<string, string> RemoteTableConfiguration)
        {
            List<string> remoteActiveModes = new List<string>();
            if(RemoteTableConfiguration.Count != 0)
            {
                foreach (KeyValuePair<string, string> remoteMode in RemoteTableConfiguration)
                {
                    string remoteModeName = remoteMode.Key;
                    string remoteModeExtent = remoteMode.Value;
                    Console.WriteLine("Received Mode "+remoteModeName + " with extent " + remoteModeExtent);
                    if (!remoteModeName.Equals("TableActiveModes"))
                    {
                        remoteActiveModes.Add(remoteModeName);

                        ActivateMode(remoteModeName);

                        // Compare extents.
                        // If remote is different, update local extent
                        Envelope newEnv = StringToEnvelope(remoteModeExtent);
                        if (ActiveLens[remoteModeName].Extent != newEnv)
                        {
                            ActiveLens[remoteModeName].Extent = newEnv;
                        }
                        // Compare Z positions
                        // If remote is different, update local Z position
                        if (ActiveLens[remoteModeName].UIIndex != remoteActiveModes.IndexOf(remoteModeName))
                        {
                            // Update Z position
                            ActiveLens[remoteModeName].UIIndex = remoteActiveModes.IndexOf(remoteModeName);
                        }
                    }
                }

            }
            
        }

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




    }
}
