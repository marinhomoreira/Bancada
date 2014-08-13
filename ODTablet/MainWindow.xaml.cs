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
        private StackPanel AppModeMenu, LensSelectionMenu, BackOffMenu, OverviewMenu;
        

        private Map
                BasemapMap
                , LensMap;
        
        private MapBoardMode CurrentAppMode = MapBoardMode.None;
        private LensType CurrentLens = LensType.None;
        bool CurrentLensIsActive = false;

        private MapBoard Board;
        
        # endregion

        public MainWindow()
        {
            InitializeComponent();

            DisplayStartMenu();

            Board = new MapBoard();
            Board.LensCollectionChanged += Board_LensCollectionChanged;
            Board.ViewFindersChanged += Board_ViewFindersChanged;
            
            // SOD stuff
            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();
        }


        

        # region Menus' configuration

        private void ConfigureAppModeMenu()
        {
            AppModeMenu = new StackPanel()
            {
                Name = "AppModeSelectionMenu",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            
            AppModeMenu.Children.Add(CreateStackPanelButton(MapBoardMode.Overview, AppModeButton_Click));
            AppModeMenu.Children.Add(CreateStackPanelButton(MapBoardMode.MultipleLenses, AppModeButton_Click));
            AppModeMenu.Children.Add(CreateStackPanelButton(MapBoardMode.SingleLens, AppModeButton_Click));
            AppModeMenu.Children.Add(CreateStackPanelButton("Reset all devices", SendRemoveAllLensCommand_Click));
            AppModeMenu.Children.Add(CreateStackPanelButton("Synch", GetAllModes_Click));
        }

        private void ConfigureLensSelectionMenu()
        {
            LensSelectionMenu = new StackPanel()
            {
                Name = "LensSelectionMenu",
            };

            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Population.ToString(), LensSelectionButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.ElectoralDistricts.ToString(), LensSelectionButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Satellite.ToString(), LensSelectionButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Streets.ToString(), LensSelectionButton_Click));
            LensSelectionMenu.Children.Add(CreateStackPanelButton(LensType.Cities.ToString(), LensSelectionButton_Click));
        }

        private void ConfigureBackOffMenu()
        {
            // Menu with Back and Turn off buttons
            BackOffMenu = new StackPanel()
            {
                Name = "BackOffMenu"
            };
            
            Button ChangeLensButton, ActivateLensButton, DeactivateLensButton;

            if (CurrentAppMode == MapBoardMode.MultipleLenses)
            {
                //ChangeLensButton = CreateStackPanelButton("Change lens", BackButton_Click);
                //BackOffMenu.Children.Add(ChangeLensButton);
            }

            if (CurrentAppMode == MapBoardMode.MultipleLenses || CurrentAppMode == MapBoardMode.SingleLens)
            {
                DeactivateLensButton = CreateStackPanelButton("Deactivate", TurnOffLens_Click);
                ActivateLensButton = CreateStackPanelButton("Activate", TurnOnLens_Click);
                if (CurrentLensIsActive && (!BackOffMenu.Children.Contains(DeactivateLensButton) || BackOffMenu.Children.Contains(ActivateLensButton)))
                {
                    BackOffMenu.Children.Remove(ActivateLensButton);
                    BackOffMenu.Children.Add(DeactivateLensButton);
                }
                else if (!CurrentLensIsActive && (BackOffMenu.Children.Contains(DeactivateLensButton) || !BackOffMenu.Children.Contains(ActivateLensButton)))
                {
                    BackOffMenu.Children.Remove(DeactivateLensButton);
                    BackOffMenu.Children.Add(ActivateLensButton);
                }
            }
        }

        private void ConfigureOverviewMenu()
        {
            OverviewMenu = new StackPanel()
            {
                Name = "Overview",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Orientation = Orientation.Horizontal,
            };
            
            OverviewMenu.Children.Add(CreateStackPanelButton("Remove all lenses", RemoveAllLens_Click));
            OverviewMenu.Children.Add(CreateStackPanelButton("Reset Map", InitialState_Click));
        }


        # region Create buttons
        private Button CreateStackPanelButton(string content, RoutedEventHandler reh)
        {
            // TODO: Color!
            Button b = new Button();
            b.Content = content;
            b.Width = 100;
            b.Height = 50;
            b.Click += reh;
            try
            {
                b.Name = content;
            }
            catch (Exception exception)
            {
                b.BorderBrush = Brushes.Red;
                Console.WriteLine("Fail: " + exception.Message);
            }
            return b;
        }

        private UIElement CreateStackPanelButton(MapBoardMode appMode, RoutedEventHandler reh)
        {
            // TODO: Color!
            Button b = new Button();
            b.Content = (MapBoardMode)appMode;
            b.Width = 100;
            b.Height = 50;
            b.Click += reh;
            b.Name = appMode.ToString();
            b.BorderBrush = Brushes.Red;
            return b;
        }


        # endregion

        # endregion

        # region Display menus
        private void DisplayStartMenu()
        {
            DetailWindow.Title = "OWW DEE";
            ClearCurrentMode();
            ClearCurrentLens();
            ClearUI();
            DisplayAppModeMenu();
        }

        private void DisplayLensSelectionMenu()
        {
            if(LensSelectionMenu == null)
            {
                ConfigureLensSelectionMenu();
            }

            if (!LayoutRoot.Children.Contains(LensSelectionMenu) && (CurrentAppMode == MapBoardMode.MultipleLenses || (CurrentAppMode == MapBoardMode.SingleLens && CurrentLens == LensType.None)))
            {
                LensSelectionMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                LensSelectionMenu.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                Canvas.SetZIndex(LensSelectionMenu, 99);
                LayoutRoot.Children.Add(LensSelectionMenu);
            }
        }

        private void DisplayAppModeMenu()
        {
            ConfigureAppModeMenu();
            if (!LayoutRoot.Children.Contains(AppModeMenu))
            {
                LayoutRoot.Children.Add(AppModeMenu);
            }
        }

        private void DisplayBackOffMenu()
        {
            if (LayoutRoot.Children.Contains(BackOffMenu))
            {
                LayoutRoot.Children.Remove(BackOffMenu);
            }
            ConfigureBackOffMenu();
            Canvas.SetZIndex(BackOffMenu, 99);
            BackOffMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            BackOffMenu.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            BackOffMenu.Orientation = Orientation.Horizontal;
            LayoutRoot.Children.Add(BackOffMenu);

        }

        private void DisplayOverviewMenu()
        {
            ConfigureOverviewMenu();
            Canvas.SetZIndex(OverviewMenu, 99);
            if (!LayoutRoot.Children.Contains(OverviewMenu))
            {
                LayoutRoot.Children.Add(OverviewMenu);
            }
        }

        

        # endregion

        # region Operation with UI Elements
        private void ClearCurrentMode()
        {
            Console.WriteLine("Resetting CurrentAppMode...");
            CurrentAppMode = MapBoardMode.None;
        }

        private void ClearCurrentLens()
        {
            Console.WriteLine("Resetting CurrentLens...");
            CurrentLens = LensType.None;
        }
        
        private void ClearUI()
        {
            Console.WriteLine("Resetting local screen...");
            if(LensMap != null) LensMap.Layers.Clear();
            if (BasemapMap != null) BasemapMap.Layers.Clear();
            LayoutRoot.Children.Clear();
        }

        # endregion


        # region Buttons
        private void AppModeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentAppMode = MapBoard.StringToMapBoardMode(((Button)sender).Name);
            Console.WriteLine("Loading " + CurrentAppMode + " mode.");
            switch (CurrentAppMode)
            {
                case MapBoardMode.SingleLens:
                case MapBoardMode.MultipleLenses:
                    LoadLensMode();
                    break;
                case MapBoardMode.Overview:
                    LoadOverviewMode();
                    break;
                default:
                    break;
            }
        }

        private void LensSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentLens = MapBoard.StringToLensType(((Button)sender).Name);
            CurrentLensIsActive = true;
            LoadBoardUI();
            //BroadcastCurrentExtent(); // TODO
        }

        private void InitialState_Click(object sender, RoutedEventArgs e)
        {
            MoveMapToInitialExtent();
        }






        # endregion

        # region Maps' operations

        private void ConfigureMaps()
        {
            // Basemap map
            if (BasemapMap == null)
            {
                BasemapMap = new Map()
                {
                    Name = "BasemapMap",
                    WrapAround = false,
                    IsLogoVisible = false,
                    IsHitTestVisible = false,
                };
            }
            // Actual lens map layer
            if (LensMap == null)
            {
                LensMap = new Map()
                {
                    Name = "LensMap",
                    WrapAround = false,
                    IsLogoVisible = false,
                    IsHitTestVisible = true
                };
                LensMap.ExtentChanging += LensMap_ExtentChanging;
            }
        }

        private void LensMap_ExtentChanging(object sender, ExtentEventArgs e)
        {
            try
            {
                BasemapMap.Extent = e.NewExtent;
                Board.GetLens(CurrentLens).Extent = e.NewExtent;
                UpdateAllLensAccordingCurrentModeExtent();
                if (CurrentAppMode != MapBoardMode.Overview)
                {
                    BroadcastCurrentExtent();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        private void LoadBoardUI()
        {
            LoadBoardUI(CurrentLens);
        }

        private void LoadBoardUI(LensType lens)
        {
            ClearUI();
            DisplayBackOffMenu();
            DisplayLensSelectionMenu();
            ConfigureMaps();
            DisplayBaseMap();
            DisplayLensMap(lens);
            DisplayViewFindersOf(lens);
        }

        private void DisplayBaseMap()
        {
            Console.WriteLine("Displaying basemap...");
            Lens basem = Board.GetLens(LensType.Basemap);
            BasemapMap.Layers.Add(MapBoard.GenerateMapLayerCollection(LensType.Basemap)[0]);
            BasemapMap.Extent = basem.Extent;
            
            Grid.SetZIndex(BasemapMap, Board.ZUIIndexOf(LensType.Basemap));
            LayoutRoot.Children.Add(BasemapMap);
        }

        private void DisplayLensMap(LensType lens)
        {
            Console.WriteLine("Displaying "+ lens +" lens...");

            DetailWindow.Title = lens.ToString();
            
            Lens LensToBeDisplayed = Board.GetLens(lens);
            LensMap.Layers.Add(LensToBeDisplayed.MapLayer);
            LensMap.Extent = LensToBeDisplayed.Extent;
            
            Grid.SetZIndex(LensMap, Board.ZUIIndexOf(lens));
            LayoutRoot.Children.Add(LensMap);

            try
            {
                BasemapMap.Extent = LensToBeDisplayed.Extent != null ? LensToBeDisplayed.Extent : MapBoard.InitialExtentFrom(lens);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Unable to use lens extent to change basemap extent: " + exception.Message);
            }

            // Legend
            ESRI.ArcGIS.Client.Toolkit.Legend legend; // TODO: Remove it from here when solving legend problem.
            if (lens.Equals(LensType.Population))
            {
                legend = new ESRI.ArcGIS.Client.Toolkit.Legend();
                legend.Map = LensMap;
                legend.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                legend.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                legend.LayerIDs = new string[] { lens.ToString() };
                legend.LayerItemsMode = ESRI.ArcGIS.Client.Toolkit.Legend.Mode.Flat;
                legend.ShowOnlyVisibleLayers = true;
                Grid.SetZIndex(legend, 99);
                LayoutRoot.Children.Add(legend);
            }

            
        }

        private void DisplayLensMap()
        {
            DisplayLensMap(CurrentLens);
        }

        private void MoveMapToInitialExtent()
        {
            this.BasemapMap.Extent = MapBoard.InitialExtentFrom(LensType.Basemap); //Initial extent
            this.LensMap.Extent = this.BasemapMap.Extent;
        }

        # endregion




        # region ViewFinders
        private void DisplayViewFindersOf(LensType lens)
        {
            foreach (KeyValuePair<LensType, Lens> viewfinders in Board.ViewFindersOf(lens))
            {
                AddViewFinderToScreen(viewfinders.Key);
            }
        }

        private void DisplayViewFinders()
        {
            DisplayViewFindersOf(CurrentLens);
        }

        private void AddViewFinderToScreen(LensType viewfinderType)
        {
            Console.WriteLine("Adding "+viewfinderType+" viewfinder...");
            Lens viewfinder = Board.GetLens(viewfinderType);
            MapViewFinder mvf = new MapViewFinder(viewfinder.Color, viewfinder.Extent.ToString())
            {
                Map = this.BasemapMap,
                Name = viewfinderType.ToString(),
                Layers = MapBoard.GenerateMapLayerCollection(viewfinderType),
            };
            int ZUIIndex = Board.ZUIIndexOf(viewfinderType);
            Grid.SetZIndex(mvf, ZUIIndex);
            LayoutRoot.Children.Add(mvf);
            mvf.UpdateExtent(viewfinder.Extent.ToString());
            mvf.Loaded += mvf_Loaded;
        }

        void mvf_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAllLensAccordingCurrentModeExtent();
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

        # endregion






        # region Modes
        private void LoadOverviewMode()
        {
            DetailWindow.Title = "Overview";
            ClearUI();
            CurrentLens = LensType.Basemap;
            DisplayOverviewMenu();
            ConfigureMaps();
            DisplayBaseMap();
            DisplayViewFinders();
        }

        private void LoadLensMode()
        {
            DetailWindow.Title = "Detail";
            ClearUI();
            DisplayLensSelectionMenu();
        }

        private void ResetApp()
        {
            Board.RemoveLens(LensType.All);
            DisplayStartMenu();
        }

        public void ActivateLens()
        {
            CurrentLensIsActive = true;
            // Make UI available
            this.LensMap.IsHitTestVisible = true;
            Board.GetLens(CurrentLens).Extent = this.BasemapMap.Extent;
            DisplayBackOffMenu();
            RefreshMaps();
        }


        public void DeactivateLens()
        {
            SendRemoveLensModeMessage();
            CurrentLensIsActive = false;
            ////Make UI unavailable
            this.LensMap.IsHitTestVisible = false;
            Board.RemoveLens(CurrentLens);
            DisplayBackOffMenu();
            RefreshMaps();
        }

        # endregion


        



        



        


        private void TurnOffLens_Click(object sender, RoutedEventArgs e)
        {
            // This button is only available if in Multi/Singlelens mode and the behavior is the same.
            DeactivateLens();
        }

        private void TurnOnLens_Click(object sender, RoutedEventArgs e)
        {
            // This button is only available if in Multi/Singlelens mode and the behavior is the same.
            ActivateLens();
        }

        private void GetAllModes_Click(object sender, RoutedEventArgs e)
        {
            SendMsgToGetActiveModesFromTable();
        }

        private void SendRemoveAllLensCommand_Click(object sender, RoutedEventArgs e)
        {
            Board.RemoveLens(LensType.All);
            SendResetAppMessage();
        }

        private void RemoveAllLens_Click(object sender, RoutedEventArgs e)
        {
            Board.RemoveLens(LensType.All);
            RemoveAllViewFinders();
        }


        
        void Board_ViewFindersChanged(object sender, EventArgs exception)
        {
            if (CurrentAppMode != MapBoardMode.None)
            {
                UpdateAllLensAccordingCurrentModeExtent();
            }
        }

        void Board_LensCollectionChanged(object sender, EventArgs exception)
        {
            Console.WriteLine("Lens collection changed.");
            if (CurrentAppMode != MapBoardMode.None)
            {
                Console.WriteLine("Refreshing UI.");
                RefreshMaps();
            }
        }
        
        void RefreshMaps()
        {
            if(!CurrentLensIsActive)
            {
                LensMap.Opacity = 0.5;
            }
            else
            {
                LensMap.Opacity = 1;
            }

            if (Board.ZUIIndexOf(CurrentLens) != -1)
            {
                if (Grid.GetZIndex(this.LensMap) != Board.ZUIIndexOf(CurrentLens))
                {
                    Grid.SetZIndex(this.LensMap, Board.ZUIIndexOf(CurrentLens));
                }
            }
            else
            {
                //LayoutRoot.Children.Remove(this.LensMap); // TODO: is it really needed?
            }

            RefreshViewFinders();
        }

        private void RefreshViewFinders()
        {
            RemoveAllViewFinders();
            DisplayViewFinders();
        }

        private void RemoveAllViewFinders()
        {
            for (int i = 0; i < LayoutRoot.Children.Count; i++)
            {
                if (LayoutRoot.Children[i] is MapViewFinder)
                {
                    LayoutRoot.Children.Remove(LayoutRoot.Children[i]);
                }
            }
        }






        # region Broadcast messages
        private void BroadcastCurrentExtent()
        {
            try
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("UpdateMode", CurrentLens.ToString());
                Lens lens = Board.GetLens(CurrentLens);
                dict.Add("Extent", lens.Extent.ToString());
                SoD.SendDictionaryToDevices(dict, "all");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Problem when broadcasting: " + exception.Message);
            }

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
            SoD.SendStringToDevices("GetAllModes", "all");
        }

        private void BroadcastAllActiveModes()
        {
            Dictionary<string, string> dic = Board.ActiveLensesToDictionary();
            dic.Add("TableActiveModes", "All");
            SoD.SendDictionaryToDevices(dic, "all");
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

            if (tableConfiguration != null && tableConfiguration.Equals("All") && CurrentAppMode == MapBoardMode.None)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    Console.WriteLine("Dictionary with table config received!");
                    Board.ClearBoardAndDisplayLensesAccordingToOverview(parsedMessage["data"]["data"].ToObject<Dictionary<string, string>>());
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
                    RefreshMaps();
                }));
            }
        }

        
        private void KeyEvent(object sender, KeyEventArgs e) //Keyup Event 
        {
            switch (e.Key)
            {
                case Key.F2:
                case Key.M:
                    DisplayStartMenu();
                    break;
                case Key.F5:
                case Key.R:
                    RefreshMaps();
                    break;
                case Key.C:
                    if (CurrentAppMode != MapBoardMode.None && CurrentAppMode != MapBoardMode.Overview && CurrentLens != LensType.All && CurrentLens != LensType.None)
                    {
                        DisplayLensSelectionMenu();
                    }
                    break;
                case Key.Escape:
                    Application.Current.Shutdown();
                    break;
                default:
                    break;
            }


        }
        
        
    }
}
