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
                        , BackOffMenu
                        , OverviewMenu;
        private Map
                BasemapMap
                , LensMap;
        private MapBoardMode CurrentAppMode = MapBoardMode.None;
        private LensType CurrentLens;
        private MapBoard Board;
        # endregion

        public MainWindow()
        {
            InitializeComponent();

            InitializeUIElements();
            DisplayStartMenu();

            Board = new MapBoard();
            Board.LensCollectionChanged += Board_LensCollectionChanged;
            Board.ViewFindersChanged += Board_ViewFindersChanged;
            CurrentLens = LensType.None;
            CurrentAppMode = MapBoardMode.None;
            // SOD stuff
            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();
        }

        void Board_ViewFindersChanged(object sender, EventArgs e)
        {
            if (CurrentAppMode != MapBoardMode.None)
            {
                UpdateAllLensAccordingCurrentModeExtent();
            }
        }

        void Board_LensCollectionChanged(object sender, EventArgs e)
        {
            Console.WriteLine("FIRE IN THA EVENT");
            if (CurrentAppMode != MapBoardMode.None)
            {
                RefreshUI();
            }
        }

        void RefreshUI()
        {
            // TODO : map refresh? not sure... it's working right now, i guess....
            RefreshViewFinders();
        }


        # region Map UI

        ESRI.ArcGIS.Client.Toolkit.Legend legend; // TODO: Remove it from here when solving legend problem.

        private void InitializeModeUI()
        {
            Console.WriteLine("Initializing " + CurrentLens + " mode...");
            DetailWindow.Title = CurrentLens.ToString(); // Change Window Title according to the mode.

            LayoutRoot.Children.Clear();
            if (CurrentAppMode == MapBoardMode.Overview)
            {
                // TODO : CUT CUT CUT REUSE REUSE REUSE YEAH BABE!
                Lens CurrentLensMode = Board.GetLens(CurrentLens);
                LensMap.Layers.Add(CurrentLensMode.MapLayer);
                LensMap.Extent = CurrentLensMode.Extent;
                Grid.SetZIndex(LensMap, Board.ZUIIndexOf(CurrentLens));

                Lens basem = Board.GetLens(LensType.Basemap);
                BasemapMap.Layers.Add(MapBoard.GenerateMapLayerCollection(CurrentLens)[0]);
                BasemapMap.Extent = CurrentLensMode.Extent;
                Grid.SetZIndex(BasemapMap, Board.ZUIIndexOf(LensType.Basemap));

                LayoutRoot.Children.Add(BasemapMap);
                LayoutRoot.Children.Add(LensMap);
                LayoutRoot.Children.Add(OverviewMenu);
                DetailWindow.Title = "Overview";
            }
            else
            {
                Lens CurrentLensMode = Board.GetLens(CurrentLens);
                LensMap.Layers.Add(CurrentLensMode.MapLayer);
                LensMap.Extent = CurrentLensMode.Extent;
                Grid.SetZIndex(LensMap, Board.ZUIIndexOf(CurrentLens));

                Lens basem = Board.GetLens(LensType.Basemap);
                BasemapMap.Layers.Add(basem.MapLayer);
                BasemapMap.Extent = CurrentLensMode.Extent;
                Grid.SetZIndex(BasemapMap, Board.ZUIIndexOf(LensType.Basemap));

                LayoutRoot.Children.Add(BasemapMap);
                LayoutRoot.Children.Add(LensMap);
                LayoutRoot.Children.Add(BackOffMenu);
            }

            

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

            InitializeViewFinders();

            BroadcastCurrentExtent();

        }

        private void InitializeViewFinders()
        {
            foreach (KeyValuePair<LensType, Lens> modeKP in Board.ViewFindersOf(CurrentLens))
            {
                AddViewFinderToScreen(modeKP.Key);
            }
        }

        private void RefreshViewFinders()
        {
            RemoveAllViewFinders();
            InitializeViewFinders();
        }

        private void RemoveAllViewFinders()
        {
            for (int i = 0; i < LayoutRoot.Children.Count; i++ )
            {
                if (LayoutRoot.Children[i] is MapViewFinder)
                {
                    LayoutRoot.Children.Remove(LayoutRoot.Children[i]);
                }
            }
        }

        private void UpdateAllLensAccordingCurrentModeExtent() // TODO: Update a specific lens maybe?
        {
            for (int i = 0; i < LayoutRoot.Children.Count; i++)
            {
                if (LayoutRoot.Children[i] is MapViewFinder)
                {
                    LensType type = MapBoard.StringToLensType(((MapViewFinder)LayoutRoot.Children[i]).Name);
                    if (Board.ViewFindersOf(CurrentLens).ContainsKey(type))
                    {
                        // PERFORMANCE: IT'S FASTER DOING THIS THAN WITH THE EVENT.
                        ((MapViewFinder)LayoutRoot.Children[i]).UpdateExtent(Board.ViewFindersOf(CurrentLens)[type].Extent.ToString());
                        if (Grid.GetZIndex(LayoutRoot.Children[i]) != Board.ZUIIndexOf(type))
                        {
                            Grid.SetZIndex(LayoutRoot.Children[i], Board.ZUIIndexOf(type));
                        }
                    }

                    else
                    {
                        LayoutRoot.Children.Remove(LayoutRoot.Children[i]);
                    }
                }
            }
        }

        private void AddViewFinderToScreen(LensType viewfinderType)
        {
            Lens viewfinder = Board.GetLens(viewfinderType);
            MapViewFinder mvf = new MapViewFinder(viewfinder.Color, viewfinder.Extent.ToString())
            {
                Map = LensMap,
                Name = viewfinderType.ToString(),
                Layers = MapBoard.GenerateMapLayerCollection(viewfinderType),
            };
            Grid.SetZIndex(mvf, Board.ZUIIndexOf(viewfinderType));
            LayoutRoot.Children.Add(mvf);
            mvf.UpdateExtent(viewfinder.Extent.ToString());
            mvf.Loaded += mvf_Loaded;
        }

        void mvf_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAllLensAccordingCurrentModeExtent();
        }

        private void RemoveViewFinderFromScreen(LensType viewfinderType)
        {
            // TODO: DO THIS IN A BETTER WAY!
            for (int i = 0; i < LayoutRoot.Children.Count; i++)
            {
                if (LayoutRoot.Children[i] is MapViewFinder && viewfinderType.ToString().Equals(((MapViewFinder)LayoutRoot.Children[i]).Name))
                {
                    LayoutRoot.Children.Remove(LayoutRoot.Children[i]);
                }
            }
        }

        private void LensMap_ExtentChanging(object sender, ExtentEventArgs e)
        {
            try
            {
                BasemapMap.Extent = e.NewExtent;
                Board.ActiveLenses[CurrentLens].Extent = e.NewExtent;
                UpdateAllLensAccordingCurrentModeExtent();
                if (CurrentAppMode != MapBoardMode.Overview)
                {
                    BroadcastCurrentExtent();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                LensType cl = CurrentLens;
                ClearMapCanvas();
                CurrentLens = cl;
                InitializeModeUI();
            }
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

            OverviewMenu = new StackPanel()
            {
                Name = "Overview",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Orientation = Orientation.Horizontal,
            };

            // Adding buttons for each menu
            AppModeMenu.Children.Add(CreateStackPanelButton(MapBoardMode.Overview, AppModeButton_Click));
            AppModeMenu.Children.Add(CreateStackPanelButton(MapBoardMode.MultipleLenses, AppModeButton_Click));
            AppModeMenu.Children.Add(CreateStackPanelButton(MapBoardMode.SingleLens, AppModeButton_Click));
            AppModeMenu.Children.Add(CreateStackPanelButton("Reset all devices", SendRemoveAllLensCommand_Click));

            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Population.ToString(), ModeButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.ElectoralDistricts.ToString(), ModeButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Satellite.ToString(), ModeButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Streets.ToString(), ModeButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Cities.ToString(), ModeButton_Click));
            //LensSelectionMenu.Children.Add(CreateStackPanelButton("Shutdown lenses", SendRemoveAllLensCommand_Click));
            //LensSelectionMenu.Children.Add(CreateStackPanelButton("All modes msg", GetAllModes_Click));

            BackOffMenu.Children.Add(CreateStackPanelButton("Change lens", BackButton_Click));
            BackOffMenu.Children.Add(CreateStackPanelButton("Turn off this lens", TurnOffLens_Click));

            OverviewMenu.Children.Add(CreateStackPanelButton("Remove all lenses", RemoveAllLens_Click));
            OverviewMenu.Children.Add(CreateStackPanelButton("Reset Map", InitialState_Click));

            // Canvas position
            Canvas.SetZIndex(BackOffMenu, 99);
            Canvas.SetZIndex(OverviewMenu, 99);
        }

        private void InitialState_Click(object sender, RoutedEventArgs e)
        {
            this.BasemapMap.Extent = MapBoard.InitialExtentFrom(LensType.Basemap); //Initial extent
            this.LensMap.Extent = this.BasemapMap.Extent;
        }

        private void RemoveAllLens_Click(object sender, RoutedEventArgs e)
        {
            RemoveAllViewFinders();
        }


        private void InitializeSingleLensMode()
        {
            CurrentAppMode = MapBoardMode.SingleLens;
            DisplayLensSelectionMenu();
        }

        private void InitializeMultipleLensesMode()
        {
            CurrentAppMode = MapBoardMode.MultipleLenses;
            DisplayLensSelectionMenu();
        }

        private void InitializeOverviewMode()
        {
            // TODO : CUT CUT CUT REUSE REUSE REUSE YEAH BABE!
            CurrentAppMode = MapBoardMode.Overview;
            LayoutRoot.Children.Clear();
            LayoutRoot.Children.Add(OverviewMenu);
            DetailWindow.Title = "Overview";
            CurrentLens = LensType.Basemap;
            InitializeModeUI();
        }

        private void DisplayStartMenu()
        {
            DetailWindow.Title = "ODODODODODOD";
            ClearMapCanvas();
            CurrentAppMode = MapBoardMode.None;
            LayoutRoot.Children.Clear();
            LayoutRoot.Children.Add(AppModeMenu);
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
            CurrentLens = MapBoard.StringToLensType(((Button)sender).Name);
            InitializeModeUI();
            //BroadcastCurrentExtent(); // TODO
        }

        private void TurnOffLens_Click(object sender, RoutedEventArgs e)
        {
            SendRemoveLensModeMessage();
            Board.RemoveLens(CurrentLens);
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
            Board.RemoveLens(LensType.All);
            SendResetAppMessage();
        }

        private void AppModeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentAppMode = MapBoard.StringToMapBoardMode(((Button)sender).Name);
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
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("UpdateMode", CurrentLens.ToString());
            dict.Add("Extent", this.LensMap.Extent.ToString());
            SoD.SendDictionaryToDevices(dict, "all");
        }

        private void SendRemoveLensModeMessage()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("RemoveMode", CurrentLens.ToString());
            SoD.SendDictionaryToDevices(dict, "all");
        }

        private void SendResetAppMessage()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("RemoveMode", LensType.All.ToString());
            SoD.SendDictionaryToDevices(dict, "all");
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
                Dictionary<string, dynamic> parsedMessage = SoD.ParseMessageIntoDictionary(data);
                String receivedString = (String)parsedMessage["data"]["data"];
                if (receivedString.Equals("GetAllModes"))
                {
                    BroadcastAllActiveModes();
                }
            });

            SoD.socket.On("dictionary", (dict) =>
            {
                this.ProcessDictionary(SoD.ParseMessageIntoDictionary(dict));
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
                this.Dispatcher.Invoke((Action)(() =>
                {
                    LensType lens = MapBoard.StringToLensType(updateMode);
                    Board.UpdateLens(lens, extentString);
                }));
            }

            if (removeMode != null && !removeMode.Equals(CurrentLens.ToString()))
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    LensType lens = MapBoard.StringToLensType(removeMode);
                    if (lens.Equals(LensType.All))
                    {
                        ResetApp();
                        return;
                    }
                    Board.RemoveLens(lens);
                }));
            }
        }

        private void ResetApp()
        {
            Board.RemoveLens(LensType.All);
            DisplayStartMenu();
        }

        private void BroadcastAllActiveModes()
        {
            Dictionary<string, string> dic = Board.ActiveLensesToDictionary();
            dic.Add("TableActiveModes", "All");
            SoD.SendDictionaryToDevices(dic, "all");
        }


        private void KeyEvent(object sender, KeyEventArgs e) //Keyup Event 
        {
            if (e.Key == Key.F2)
            {
                DisplayStartMenu();
            }
        }

    }
}
