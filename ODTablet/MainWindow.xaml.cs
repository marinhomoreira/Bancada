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
using ODTablet.MapBoardUI;
using ESRI.ArcGIS.Client.Geometry;

namespace ODTablet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private MapBoardMode CurrentAppMode = MapBoardMode.None;
        private MapBoard Board;

        public MainWindow()
        {
            InitializeComponent();
            //DisplayStartMenu();

            Board = new MapBoard();
            Board.LensAdded += Board_LensAdded;
            Board.LensExtentUpdated += Board_LensExtentUpdated;
            Board.LensRemoved += Board_LensRemoved;
            Board.LensStackPositionChanged += Board_LensStackPositionChanged;

            //CurrentAppMode = MapBoardMode.MultipleLenses;
            //LoadMultipleLensMode();
            CurrentAppMode = MapBoardMode.Overview;
            LoadOverviewMode();
            //Board.StartLens(LensType.Satellite);
            
            // SOD stuff
            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();

        }

        # region MainBoardUserControl
        MapBoardUC MainBoardUC;
        void MBUC_ExtentUpdated(object sender, MapEventArgs e)
        {
            Board.GetLens(e.ModifiedLens).Extent = e.Extent;
            //Console.WriteLine("Broadcasting {0}, {1}", e.ModifiedLens, e.Extent);
            BroadcastExtent(e.ModifiedLens, e.Extent);
        }
        # endregion


        # region Mode Selection
        private void LoadMode(MapBoardMode currentAppMode)
        {
            CurrentAppMode = currentAppMode;
            Console.WriteLine("Loading " + CurrentAppMode + " mode.");
            switch (CurrentAppMode)
            {
                case MapBoardMode.SingleLens:
                    LoadSingleLensMode();
                    break;
                case MapBoardMode.MultipleLenses:
                    LoadMultipleLensMode();
                    break;
                case MapBoardMode.Overview:
                    LoadOverviewMode();
                    break;
                default:
                    break;
            }
        }
        private void ClearCurrentMode()
        {
            Console.WriteLine("Resetting CurrentAppMode...");
            CurrentAppMode = MapBoardMode.None;
        }

        # region Application Mode Menu
        private StackPanel AppModeMenu;
        private void DisplayStartMenu()
        {
            DetailWindow.Title = "OWW DEE";
            ClearCurrentMode();
            ClearUI();
            DisplayAppModeMenu();
        }
        private void DisplayAppModeMenu()
        {
            ConfigureAppModeMenu();
            if (!LayoutRoot.Children.Contains(AppModeMenu))
            {
                LayoutRoot.Children.Add(AppModeMenu);
            }
        }
        private void ConfigureAppModeMenu()
        {
            AppModeMenu = new StackPanel()
            {
                Name = "AppModeSelectionMenu",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            AppModeMenu.Children.Add(CreateAppModeButton(MapBoardMode.Overview, AppModeButton_Click));
            AppModeMenu.Children.Add(CreateAppModeButton(MapBoardMode.MultipleLenses, AppModeButton_Click));
            AppModeMenu.Children.Add(CreateAppModeButton(MapBoardMode.SingleLens, AppModeButton_Click));
            //AppModeMenu.Children.Add(CreateRegularButton("Reset all devices", SendRemoveAllLensCommand_Click)); // TODO
            //AppModeMenu.Children.Add(CreateRegularButton("Synch", GetAllModes_Click)); // TODO
        }

        # region Buttons
        private void AppModeButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMode(MapBoard.StringToMapBoardMode(((Button)sender).Name));
        }

        private Button CreateAppModeButton(MapBoardMode appMode, RoutedEventHandler reh)
        {
            Button b = new Button();
            b.Content = (MapBoardMode)appMode;
            b.Width = 100;
            b.Height = 50;
            b.Click += reh;
            b.Name = appMode.ToString();
            b.BorderBrush = Brushes.Black;
            return b;
        }

        # endregion

        # endregion
        # endregion


        # region SingleLensMode
        private void LoadSingleLensMode()
        {
            DetailWindow.Title = "Detail";
            ClearUI();
            DisplayLensSelectionMenu();
        }

        private void BuildSingleLensUI(LensType lens)
        {
            ClearUI();
            StartLensUC(lens);
        }

        # endregion

        # region MultipleLensMode
        private void LoadMultipleLensMode()
        {
            DetailWindow.Title = "Detail";
            ClearUI();
            DisplayLensSelectionMenu();
        }

        private void BuildMultipleLensUI(LensType lens)
        {
            ClearUI();
            StartLensUC(lens);
            DisplayLensSelectionMenu();
        }

        # endregion

        # region OverviewMode
        
        private StackPanel InsectStack;
        
        private void LoadOverviewMode()
        {
            DetailWindow.Title = "Overview";
            ClearUI();
            //StartLensUC(LensType.Basemap);
            MainBoardUC = new MapBoardUC(LensType.Basemap, Board);
            LayoutRoot.Children.Add(MainBoardUC);

            InsectStack = new StackPanel()
            {
                Name = "InsectStack",
            };
            InsectStack.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            InsectStack.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            InsectStack.Orientation = Orientation.Horizontal;
            Canvas.SetZIndex(InsectStack, 99);
            LayoutRoot.Children.Add(InsectStack);
        }



        # endregion

        private void StartLensUC(LensType lens)
        {
            Board.StartLens(lens);
            MainBoardUC = new MapBoardUC(lens, Board);
            MainBoardUC.MapExtentUpdated += MBUC_ExtentUpdated;
            LayoutRoot.Children.Add(MainBoardUC);
        }


        # region Lens Operations
        private void LoadLens(LensType lens)
        {
            switch (CurrentAppMode)
            {
                case MapBoardMode.SingleLens:
                    BuildSingleLensUI(lens);
                    break;
                case MapBoardMode.MultipleLenses:
                    BuildMultipleLensUI(lens);
                    break;
                default:
                    break;
            }
        }





        # region Lens Selection Menu
        private StackPanel LensSelectionMenu;
        private void DisplayLensSelectionMenu()
        {
            if (LensSelectionMenu == null)
            {
                ConfigureLensSelectionMenu();
            }
            // TODO
            if (!LayoutRoot.Children.Contains(LensSelectionMenu) && (CurrentAppMode == MapBoardMode.MultipleLenses || (CurrentAppMode == MapBoardMode.SingleLens)))// && CurrentLens == LensType.None)))
            {
                LensSelectionMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                LensSelectionMenu.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                Canvas.SetZIndex(LensSelectionMenu, 99);
                LayoutRoot.Children.Add(LensSelectionMenu);
            }
        }
        private void ConfigureLensSelectionMenu()
        {
            LensSelectionMenu = new StackPanel()
            {
                Name = "LensSelectionMenu",
            };
            LensSelectionMenu.Children.Add(CreateLensButton(LensType.Satellite.ToString(), LensSelectionButton_Click));
            LensSelectionMenu.Children.Add(CreateLensButton(LensType.Streets.ToString(), LensSelectionButton_Click));
            LensSelectionMenu.Children.Add(CreateLensButton(LensType.Population.ToString(), LensSelectionButton_Click));
            LensSelectionMenu.Children.Add(CreateLensButton(LensType.ElectoralDistricts.ToString(), LensSelectionButton_Click));
            LensSelectionMenu.Children.Add(CreateLensButton(LensType.Cities.ToString(), LensSelectionButton_Click));
        }
        private void LensSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLens(MapBoard.StringToLensType(((Button)sender).Name));
        }

        private Button CreateLensButton(string content, RoutedEventHandler reh)
        {
            Button b = new Button();
            b.Content = content;
            b.Width = 100;
            b.Height = 50;
            b.Click += reh;
            if (Board != null && Board.LensCanBeActivated(content))
            {
                b.Background = new SolidColorBrush(MapBoard.GetColorOf(content));
                bool conditionToWhite = content.Equals(LensType.ElectoralDistricts.ToString()) || content.Equals(LensType.Cities.ToString());
                b.Foreground = conditionToWhite ? Brushes.White : Brushes.Black;
            }
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

        # endregion







        # region Activate/Deactivate Lens



        # endregion

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
            SoD.ownDevice.ID = "1";
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

        # region Remote I/O
        private void BroadcastExtent(LensType lensT, Envelope extent)
        {
            try
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("UpdateMode", lensT.ToString());
                Lens lens = Board.GetLens(lensT);
                dict.Add("Extent", lens.Extent.ToString());
                SoD.SendDictionaryToDevices(dict, "all");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Problem while broadcasting: " + exception.Message);
            }
        }

        private void SendRemoveLensModeMessage(LensType lensT)
        {
            Console.WriteLine("Sending remove lensmodemsg");
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("RemoveMode", lensT.ToString());
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

        private void ProcessDictionary(Dictionary<string, dynamic> parsedMessage)
        {
            String extentString = (String)parsedMessage["data"]["data"]["Extent"];
            String updateMode = (String)parsedMessage["data"]["data"]["UpdateMode"];
            String removeMode = (String)parsedMessage["data"]["data"]["RemoveMode"];
            String tableConfiguration = (String)parsedMessage["data"]["data"]["TableActiveModes"];

            if (updateMode != null)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    LensType lens = MapBoard.StringToLensType(updateMode);
                    Board.UpdateLens(lens, extentString);
                }));
            }

            if (removeMode != null)
            {
                LensType lens = MapBoard.StringToLensType(removeMode);
                if (lens.Equals(LensType.All))
                {
                    //ResetApp(); //TODO
                    return;
                }
                Board.RemoveLens(lens);
            }

            //SYNCH
            //if (tableConfiguration != null && tableConfiguration.Equals("All") && CurrentAppMode == MapBoardMode.None)
            //{
            //    Console.WriteLine("Dictionary with table config received!");
            //    Board.ClearBoardAndStackLensesAccordingToOverview(parsedMessage["data"]["data"].ToObject<Dictionary<string, string>>());
            //}
        }

        # endregion

        # region Events
        // TODO: Modularize Insects!
        void Board_LensStackPositionChanged(object sender, LensEventArgs e)
        {
            if (MainBoardUC != null)
            {
                MainBoardUC.UpdateZOf(e.ModifiedLens, (MapBoard)sender);
                if (CurrentAppMode == MapBoardMode.Overview)
                {
                    for (int i = 0; i < InsectStack.Children.Count; i++)
                    {
                        if (InsectStack.Children[i] is MapBoardUC)
                        {
                            ((MapBoardUC)InsectStack.Children[i]).UpdateZOf(e.ModifiedLens, (MapBoard)sender);
                        }
                    }
                }
            }
        }

        void Board_LensRemoved(object sender, LensEventArgs e)
        {
            if (MainBoardUC != null)
            {
                MainBoardUC.Remove(e.ModifiedLens, (MapBoard)sender);
                if (CurrentAppMode == MapBoardMode.Overview)
                {
                    for (int i = 0; i < InsectStack.Children.Count; i++)
                    {
                        if (InsectStack.Children[i] is MapBoardUC)
                        {
                            if (((MapBoardUC)(InsectStack.Children[i])).Equals(e.ModifiedLens.ToString()))
                            {
                                InsectStack.Children.RemoveAt(i);
                            }
                            else
                            {
                                ((MapBoardUC)InsectStack.Children[i]).Remove(e.ModifiedLens, (MapBoard)sender);
                            }
                            
                        }
                    }
                }
            }
        }

        void Board_LensExtentUpdated(object sender, LensEventArgs e)
        {
            if (MainBoardUC != null)
            {
                MainBoardUC.UpdateExtentOf(e.ModifiedLens, (MapBoard)sender);
                if (CurrentAppMode == MapBoardMode.Overview)
                {
                    for (int i = 0; i < InsectStack.Children.Count; i++)
                    {
                        if (InsectStack.Children[i] is MapBoardUC)
                        {
                            ((MapBoardUC)InsectStack.Children[i]).UpdateExtentOf(e.ModifiedLens, (MapBoard)sender);
                        }
                    }
                }
            }
        }

        void Board_LensAdded(object sender, LensEventArgs e)
        {
            // Modes... where? and so on...
            if (MainBoardUC != null)
            {
                MainBoardUC.UpdateExtentOf(e.ModifiedLens, (MapBoard)sender);
            }

            if (CurrentAppMode == MapBoardMode.Overview)
            {
                MapBoardUC mbuc = new MapBoardUC(e.ModifiedLens, (MapBoard)sender);
                mbuc.Name = e.ModifiedLens.ToString();
                mbuc.Width = 1920 / 5;
                mbuc.Height = 1080 / 5;
                //mbuc.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                //mbuc.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                mbuc.PassiveMode = true;
                mbuc.BorderBrush = new SolidColorBrush(MapBoard.GetColorOf(e.ModifiedLens));
                mbuc.BorderThickness = new Thickness(5.0);
                Canvas.SetZIndex(mbuc, 99);
                InsectStack.Children.Add(mbuc);
            }
        }


        #endregion



















        private void ClearUI()
        {
            // TODO: Remove layers from user controls (<UC>.ClearUI())
            for (int i = 0; i < LayoutRoot.Children.Count; i++)
            {
                if (LayoutRoot.Children[i] is MapBoardUC)
                {
                    ((MapBoardUC)LayoutRoot.Children[i]).ClearUI();
                    ((MapBoardUC)LayoutRoot.Children[i]).MapExtentUpdated -= MBUC_ExtentUpdated;
                }
            }
            LayoutRoot.Children.Clear();
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
                    //RefreshMaps();
                    MainBoardUC.ResetBoard(Board);
                    //MainBoardUC.RefreshCurrentLens(Board);
                    break;
                case Key.C:
                    //if (CurrentAppMode != MapBoardMode.None && CurrentAppMode != MapBoardMode.Overview && CurrentLens != LensType.All && CurrentLens != LensType.None)
                    //{
                        DisplayLensSelectionMenu();
                    //}
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
