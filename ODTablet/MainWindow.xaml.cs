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
                //this.ProcessDictionary(SoD.ParseMessageIntoDictionary(dict));
            });

            // make the socket.io connection
            SoD.SocketConnect();
        }

        # endregion

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
            //ModesListMenu.Children.Add(CreateStackPanelButton("Remove all lens", SendRemoveAllLensCommand_Click));
            //ModesListMenu.Children.Add(CreateStackPanelButton("All modes msg", GetAllModes_Click));

            //BackOffMenu.Children.Add(CreateStackPanelButton("Back", BackButton_Click));
            BackOffMenu.Children.Add(CreateStackPanelButton("Turn off lens", TurnOffLens_Click));

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


        # endregion
        # endregion


        private void InitializeModeUI()
        {
            Console.WriteLine("Initializing " + CurrentMode + " mode...");
            DetailWindow.Title = CurrentMode;

            LayoutRoot.Children.Clear();

            ActivateMode(CurrentMode);

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
            LayoutRoot.Children.Add(BackOffMenu);

            BroadcastCurrentExtent();
        }

        private void ActivateMode(string mode)
        {
            if (!ActiveLens.ContainsKey(mode))
            {
                ActiveLens.Add(mode, new LensFactory().CreateLens(mode));
                ActiveLens[mode].UIIndex = ActiveLens.Count();
            }
        }

        private void LensMap_ExtentChanging(object sender, ExtentEventArgs e)
        {
            BasemapMap.Extent = e.NewExtent;
            ActiveLens[CurrentMode].Extent = e.NewExtent;
            //UpdateAllLensAccordingCurrentModeExtent(ActiveLens[CurrentMode].Extent);
            BroadcastCurrentExtent();
        }



    }
}
