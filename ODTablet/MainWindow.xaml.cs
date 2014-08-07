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
using ESRI.ArcGIS.Client.Geometry;

using ODTablet.MapModel;
using ODTablet.LensViewFinder;

using SOD_CS_Library;

namespace ODTablet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        # region Variables
        private StackPanel
                        AppModeMenu
                        , LensSelectionMenu
                        , BackOffMenu;
        private Map
                BasemapMap
                , LensMap;
        private MapBoardMode CurrentAppMode;
        private LensType CurrentLens;
        private MapBoard Board;
        # endregion

        public MainWindow()
        {
            InitializeComponent();

            InitializeUIElements();
            DisplayStartMenu();

            Board = new MapBoard();
            Board.Changed += Board_Changed;
            CurrentLens = LensType.None;

            // SOD stuff
            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();
        }

        void Board_Changed(object sender, EventArgs e)
        {
            Console.WriteLine("UGA UGA EEEEEEEEEEEEEEEEEEEEEEEEE PORRA!");
            // TODO: update UI from here! :D
        }








        # region Map UI

        ESRI.ArcGIS.Client.Toolkit.Legend legend; // TODO: Remove it from here when solving legend problem.

        private void InitializeModeUI()
        {
            Console.WriteLine("Initializing " + CurrentLens + " mode...");
            DetailWindow.Title = CurrentLens.ToString(); // Change Window Title according to the mode.

            LayoutRoot.Children.Clear();

            Lens CurrentLensMode = Board.GetLens(CurrentLens);
            Lens basem = Board.GetLens(LensType.Basemap);
            BasemapMap.Layers.Add(basem.MapLayer);
            BasemapMap.Extent = CurrentLensMode.Extent;
            Canvas.SetZIndex(LensMap, 0);

            LensMap.Layers.Add(CurrentLensMode.MapLayer);
            LensMap.Extent = CurrentLensMode.Extent;
            Canvas.SetZIndex(LensMap, CurrentLensMode.UIIndex);

            LayoutRoot.Children.Add(BasemapMap);
            LayoutRoot.Children.Add(LensMap);
            LayoutRoot.Children.Add(BackOffMenu);

            if (CurrentLens.Equals(LensType.Population))
            {
                // TODO: Y U NO WORK?!
                legend = new ESRI.ArcGIS.Client.Toolkit.Legend();
                legend.Map = LensMap;
                legend.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                legend.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                legend.LayerIDs = new string[] { CurrentLens.ToString() };
                legend.LayerItemsMode = ESRI.ArcGIS.Client.Toolkit.Legend.Mode.Flat;
                legend.ShowOnlyVisibleLayers = true;
                Canvas.SetZIndex(legend, 99);
                LayoutRoot.Children.Add(legend);
            }

            AddActiveLensesToScreen();

            //BroadcastCurrentExtent();

            UpdateAllLensAccordingCurrentModeExtent();
        }

        private void AddActiveLensesToScreen()
        {
            foreach (KeyValuePair<LensType, Lens> modeKP in Board.ActiveLenses)
            {
                LensType activeModeName = modeKP.Key;
                Lens activeMode = modeKP.Value;

                if (!CurrentLens.Equals(activeModeName) && !activeModeName.Equals(LensType.Basemap) && !activeModeName.Equals(LensType.None))
                {
                    MapViewFinder mvf = new MapViewFinder(activeMode.Color, Board.GetLens(activeModeName).Extent.ToString())
                    {
                        Map = LensMap,
                        Name = activeModeName.ToString(),
                        Layers = new LensFactory().CreateLens(activeModeName).MapLayerCollection, // TODO: HOW TO REMOVE THIS?
                    };
                    Canvas.SetZIndex(mvf, activeMode.UIIndex);
                    LayoutRoot.Children.Add(mvf);
                    mvf.UpdateExtent(Board.GetLens(activeModeName).Extent.ToString());
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
                    LensType type = (LensType)Enum.Parse(typeof(LensType), ((MapViewFinder)element).Name, true);

                    // PERFORMANCE: IT'S FASTER DOING THIS THAN WITH THE EVENT.
                    ((MapViewFinder)element).UpdateExtent(Board.ActiveLenses[type].Extent.ToString());
                    if (Canvas.GetZIndex(element) != Board.ActiveLenses[type].UIIndex)
                    {
                        Canvas.SetZIndex(element, Board.ActiveLenses[type].UIIndex);
                    }
                }
            }
        }

        private void LensMap_ExtentChanging(object sender, ExtentEventArgs e)
        {
            BasemapMap.Extent = e.NewExtent;
            Board.ActiveLenses[CurrentLens].Extent = e.NewExtent;
            UpdateAllLensAccordingCurrentModeExtent();
            // BroadcastCurrentExtent(); // TODO
        }

        # endregion




        


        









        # region General UI Elements
        private void InitializeUIElements()
        {
            InitializeMaps();
            InitializeMenus();
        }

        private void InitializeMaps()
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
            LensMap.ExtentChanging += LensMap_ExtentChanging;

        }

        private void InitializeMenus()
        {
            // Start menu
            AppModeMenu = new StackPanel()
            {
                Name = "AppModeSelectionMenu",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            // Lens Selection menu
            LensSelectionMenu = new StackPanel()
            {
                Name = "LensSelectionMenu",
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
            AppModeMenu.Children.Add(CreateStackPanelButton(MapBoardMode.Overview, AppModeButton_Click));
            AppModeMenu.Children.Add(CreateStackPanelButton(MapBoardMode.MultipleLenses, AppModeButton_Click));
            AppModeMenu.Children.Add(CreateStackPanelButton(MapBoardMode.SingleLens, AppModeButton_Click));

            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Population.ToString(), ModeButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.ElectoralDistricts.ToString(), ModeButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Satellite.ToString(), ModeButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Streets.ToString(), ModeButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Cities.ToString(), ModeButton_Click));
            //LensSelectionMenu.Children.Add(CreateStackPanelButton("Shutdown lenses", SendRemoveAllLensCommand_Click));
            //LensSelectionMenu.Children.Add(CreateStackPanelButton("All modes msg", GetAllModes_Click));

            BackOffMenu.Children.Add(CreateStackPanelButton("Change lens", BackButton_Click));
            BackOffMenu.Children.Add(CreateStackPanelButton("Turn off this lens", TurnOffLens_Click));

            // Canvas position
            Canvas.SetZIndex(BackOffMenu, 99);
        }


        private void InitializeSingleLensMode()
        {
            DisplayLensSelectionMenu();
        }

        private void InitializeMultipleLensesMode()
        {
            DisplayLensSelectionMenu();
        }

        private void InitializeOverviewMode()
        {
            throw new NotImplementedException();
        }

        private void DisplayStartMenu()
        {
            LayoutRoot.Children.Clear();
            LayoutRoot.Children.Add(AppModeMenu);
            DetailWindow.Title = "Detail";
        }

        private void DisplayLensSelectionMenu()
        {
            LayoutRoot.Children.Clear();
            LayoutRoot.Children.Add(LensSelectionMenu);
            DetailWindow.Title = "Detail";
        }

        private void ClearMapCanvas()
        {
            Console.WriteLine("Cleaning map...");
            CurrentLens = LensType.None;
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

        private UIElement CreateStackPanelButton(MapBoardMode appMode, RoutedEventHandler reh)
        {
            Button b = new Button();
            b.Content = (MapBoardMode)appMode;
            b.Width = 100;
            b.Height = 50;
            b.Click += reh;
            b.Name = appMode.ToString();
            b.BorderBrush = Brushes.Red;
            return b;
        }

        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            //SendMsgToGetActiveModesFromTable(); // TODO
            CurrentLens = (LensType)Enum.Parse(typeof(LensType), ((Button)sender).Name, true);
            InitializeModeUI();
            //BroadcastCurrentExtent(); // TODO
        }

        private void TurnOffLens_Click(object sender, RoutedEventArgs e)
        {
            //SendRemoveLensModeMessage(); // TODO
            //ActiveLens.Remove(CurrentMode); // TODO
            ClearMapCanvas();
            DisplayLensSelectionMenu();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ClearMapCanvas();
            DisplayLensSelectionMenu();
        }

        private void GetAllModes_Click(object sender, RoutedEventArgs e)
        {
            //SendMsgToGetActiveModesFromTable(); // TODO
        }

        private void SendRemoveAllLensCommand_Click(object sender, RoutedEventArgs e)
        {
            //ActiveLens.Clear();
            ClearMapCanvas();
            //CurrentMode = "All"; // TODO: Hardcoding CurrentMode. Too bad.
            //SendRemoveLensModeMessage();
            DisplayLensSelectionMenu();
        }

        private void AppModeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentAppMode = (MapBoardMode)Enum.Parse(typeof(MapBoardMode), ((Button)sender).Name, true);
            Console.WriteLine("Loading " + CurrentAppMode + " mode.");
            switch (CurrentAppMode)
            {
                case MapBoardMode.SingleLens:
                    InitializeSingleLensMode();
                    break;
                case MapBoardMode.MultipleLenses:
                    InitializeMultipleLensesMode();
                    break;
                case MapBoardMode.Overview:
                    InitializeOverviewMode();
                    break;
                default:
                    break;
            }
        }

        # endregion
        # endregion






        # region BroadcastCurrentExtent(), SendRemoveLensModeMessage(), SendMsgToGetActiveModesFromTable()
        private void BroadcastCurrentExtent()
        {
            // TODO
            throw new NotImplementedException();
            //Dictionary<string, string> dict = new Dictionary<string, string>();
            //dict.Add("UpdateMode", CurrentMode);
            //dict.Add("Extent", ActiveLens[CurrentMode].Extent.ToString());

            //SoD.SendDictionaryToDevices(dict, "all");
        }

        private void SendRemoveLensModeMessage()
        {
            // TODO
            //Dictionary<string, string> dict = new Dictionary<string, string>();
            //dict.Add("RemoveMode", CurrentMode);
            //SoD.SendDictionaryToDevices(dict, "all");
            throw new NotImplementedException();
        }

        private void SendMsgToGetActiveModesFromTable()
        {
            // TODO
            throw new NotImplementedException();
            //SoD.SendStringToDevices("GetAllModes", "all");
        }


        # endregion



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
                //this.ProcessDictionary(SoD.ParseMessageIntoDictionary(dict)); // TODO
            });

            // make the socket.io connection
            SoD.SocketConnect();
        }

        # endregion

        private void ProcessDictionary(Dictionary<string, dynamic> parsedMessage)
        {
            String extentString = (String)parsedMessage["data"]["data"]["Extent"];
            String updateMode = (String)parsedMessage["data"]["data"]["UpdateMode"];
            String removeMode = (String)parsedMessage["data"]["data"]["RemoveMode"];
            String tableConfiguration = (String)parsedMessage["data"]["data"]["TableActiveModes"];

            if (tableConfiguration != null && tableConfiguration.Equals("All"))
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    Board.UpdateLens(parsedMessage["data"]["data"].ToObject<Dictionary<string, string>>());
                }));
            }

            if (updateMode != null && !updateMode.Equals(CurrentLens.ToString()))
            {
                //ActivateMode(updateMode);
                //ActiveLens[updateMode].Extent = StringToEnvelope(extentString);
                // TODO
            }

            if (removeMode != null && !removeMode.Equals(CurrentLens.ToString()))
            {
                //ActiveLens.Remove(removeMode);
                // TODO
            }

            //TODO: Update UI based on ActiveLens
        }




        private void KeyEvent(object sender, KeyEventArgs e) //Keyup Event 
        {
            if (e.Key == Key.F2)
            {
                MessageBox.Show("Function F2");
                // TODO
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
