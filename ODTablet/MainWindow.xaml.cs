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
            
            ConfigureBoard();
            DisplayStartMenu();
            //CurrentAppMode = MapBoardMode.MultipleLenses;
            //LoadMultipleLensMode();

            //CurrentAppMode = MapBoardMode.Overview;
            //LoadOverviewMode();

            //CurrentAppMode = MapBoardMode.SingleLens;
            //LoadSingleLensMode();

            //Board.StartLens(LensType.Satellite);
            
            // SOD stuff
            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();
        }

        private void ConfigureBoard()
        {
            Board = new MapBoard();
            Board.LensAdded += Board_LensAdded;
            Board.LensExtentUpdated += Board_LensExtentUpdated;
            Board.LensRemoved += Board_LensRemoved;
            Board.LensStackPositionChanged += Board_LensStackPositionChanged;
        }

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

        private void Reset()
        {
            LensType l = LensType.None;
            if (MainBoardUC != null)
            {
                l = MainBoardUC.CurrentLens;
            }
            ConfigureBoard();
            LoadMode(CurrentAppMode);

            if (CurrentAppMode == MapBoardMode.SingleLens && l != LensType.None)
            {
                BuildSingleLensUI(l);
                DeactivateCurrentLens();
            }
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
            Canvas.SetZIndex(AppModeMenu, 100);
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
            Board = new MapBoard();
            ActivateLens(lens);
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
            if(!LayoutRoot.Children.Contains(LensMap))
            {
                ClearUI();
            }
            ActivateLens(lens);
        }
        # endregion


        # region Operations with Lenses - SL and ML

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

        # region Activate/Deactivate

        private void ActivateLens(LensType lens)
        {
            if (LensCanBeActivated(lens))
            {
                CurrentLocalLens = lens;
                aLensToKillFor = LensType.None; // if you don't do this, it will keep the last one and everytime you change a lens it will destroy the last one.
                DisplayBaseMap();
                AddLayerToMap(lens);
                LensMap.IsHitTestVisible = true;
                DisplayLensSelectionMenu();
                RemoveRemoteLensInUseMsg();
                RemoveDeactivatedMsg();
                DisplayActivationMenu();
                DisplayCurrentLensLabel();
                BroadcastStartLens(lens);
                BroadcastExtent(lens, Board.GetLens(lens).Extent);
                return;
            }
            else if (!LensCanBeActivated(lens) && RemoteLens == lens)
            {
                DisplayRemoteLensInUseMsg();
                DisplayBaseMap();
                DisplayLensSelectionMenu();
            }
        }

        LensType aLensToKillFor = LensType.None;

        private void DeactivateCurrentLens()
        {
            if (CurrentAppMode == MapBoardMode.SingleLens || CurrentAppMode == MapBoardMode.MultipleLenses)
            {
                aLensToKillFor = CurrentLocalLens;

                DisplayDeactivatedMsg();
                LensMap.IsHitTestVisible = false;
                RemoveLayerFromMap(CurrentLocalLens);

                DisplayActivationMenu();
                FreeLens(CurrentLocalLens);
                //SendRemoveLensModeMessage(CurrentLocalLens); // TODO: Remove from overview
            }
        }

        private bool LensCanBeActivated(LensType lens)
        {
            return ((CurrentAppMode == MapBoardMode.SingleLens || CurrentAppMode == MapBoardMode.MultipleLenses) && RemoteLens != lens);
        }

        private void FreeLens(LensType lens)
        {
            BroadcastFreedLens(lens);
        }

        #region ActivationMenu
        private StackPanel ActivationMenu;
        Button ActivateLensButton, DeactivateLensButton;

        private void ConfigureActivationMenu()
        {
            if (ActivationMenu == null)
            {
                ActivationMenu = new StackPanel()
                {
                    Name = "ActivationMenu"
                };

                Canvas.SetZIndex(ActivationMenu, 99);
                ActivationMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                ActivationMenu.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                ActivationMenu.Orientation = Orientation.Horizontal;
            }

            if (DeactivateLensButton == null) DeactivateLensButton = CreateRegularButton("Remove", Deactivate_Click);
            if (ActivateLensButton == null) ActivateLensButton = CreateRegularButton("Activate", Activate_Click);
        }

        private void DisplayActivationMenu()
        {
            if ((CurrentAppMode == MapBoardMode.MultipleLenses || CurrentAppMode == MapBoardMode.SingleLens) && LensMap != null)
            {
                if (LayoutRoot.Children.Contains(ActivationMenu))
                {
                    LayoutRoot.Children.Remove(ActivationMenu);
                }

                ConfigureActivationMenu();

                if ((!ActivationMenu.Children.Contains(DeactivateLensButton) || ActivationMenu.Children.Contains(ActivateLensButton)) && !MsgsPanel.Children.Contains(DeactivatedLensMsg))
                {
                    ActivationMenu.Children.Remove(ActivateLensButton);
                    ActivationMenu.Children.Add(DeactivateLensButton);
                }
                else if ((ActivationMenu.Children.Contains(DeactivateLensButton) || !ActivationMenu.Children.Contains(ActivateLensButton)) && MsgsPanel.Children.Contains(DeactivatedLensMsg))
                {
                    ActivationMenu.Children.Remove(DeactivateLensButton);
                    ActivationMenu.Children.Add(ActivateLensButton);
                }
                LayoutRoot.Children.Add(ActivationMenu);
            }
        }

        private void Deactivate_Click(object sender, RoutedEventArgs e)
        {
            // This button is only available if in Multi/Singlelens mode and the behavior is the same.
            DeactivateCurrentLens();
        }

        private void Activate_Click(object sender, RoutedEventArgs e)
        {
            // This button is only available if in Multi/Singlelens mode and the behavior is the same.
            ActivateLens(CurrentLocalLens);
        }
        #endregion


        private StackPanel MsgsPanel = new StackPanel() { HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Center};

        #region Deactivated msg
        private Label DeactivatedLensMsg;
        private void ConfigureDeactivatedMsg()
        {
            if (DeactivatedLensMsg == null)
            {
                DeactivatedLensMsg = new Label();
                DeactivatedLensMsg.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                DeactivatedLensMsg.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                DeactivatedLensMsg.FontWeight = FontWeights.Bold;
                DeactivatedLensMsg.FontSize = 60;
                DeactivatedLensMsg.BorderBrush = Brushes.Black;
                Grid.SetZIndex(DeactivatedLensMsg, 100);
            }
            DeactivatedLensMsg.Content = CurrentLocalLens + " removed.\nPress Activate to place it on top.";
        }

        private void DisplayDeactivatedMsg()
        {
            ConfigureDeactivatedMsg();
            if (!LayoutRoot.Children.Contains(MsgsPanel))
            {
                Canvas.SetZIndex(MsgsPanel, 99);
                LayoutRoot.Children.Add(MsgsPanel);
            }
            if (!MsgsPanel.Children.Contains(DeactivatedLensMsg))
            {
                MsgsPanel.Children.Clear();
                MsgsPanel.Children.Add(DeactivatedLensMsg);
            }
        }

        private void RemoveDeactivatedMsg()
        {
            if (MsgsPanel.Children.Contains(DeactivatedLensMsg))
            {
                MsgsPanel.Children.Remove(DeactivatedLensMsg);
            }
        }
        #endregion

        #region RemoteLensInUse msg
        private Label RemoteLensInUseMsg;
        private void ConfigureRemoteLensInUseMsg()
        {
            if (RemoteLensInUseMsg == null)
            {
                RemoteLensInUseMsg = new Label();
                RemoteLensInUseMsg.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                RemoteLensInUseMsg.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                RemoteLensInUseMsg.FontWeight = FontWeights.Bold;
                RemoteLensInUseMsg.FontSize = 60;
                RemoteLensInUseMsg.BorderBrush = Brushes.Black;
                Grid.SetZIndex(RemoteLensInUseMsg, 100);
            }
            RemoteLensInUseMsg.Content = RemoteLens + " being used remotely. Deactivate there first.";
        }

        private void DisplayRemoteLensInUseMsg()
        {
            ConfigureRemoteLensInUseMsg();
            if (!LayoutRoot.Children.Contains(MsgsPanel))
            {
                Canvas.SetZIndex(MsgsPanel, 99);
                LayoutRoot.Children.Add(MsgsPanel);
            }
            if (!MsgsPanel.Children.Contains(RemoteLensInUseMsg))
            {
                MsgsPanel.Children.Clear();
                MsgsPanel.Children.Add(RemoteLensInUseMsg);
            }
        }
        private void RemoveRemoteLensInUseMsg()
        {
            if (MsgsPanel.Children.Contains(RemoteLensInUseMsg))
            {
                MsgsPanel.Children.Remove(RemoteLensInUseMsg);
            }
        }

        #endregion

        # endregion
        
        # region LensMap

        private Map LensMap;
        ESRI.ArcGIS.Client.Toolkit.Legend legend;

        LensType CurrentLocalLens = LensType.None;
        LensType RemoteLens = LensType.None;

        private void ResetLensMap()
        {
            if (LensMap == null)
            {
                LensMap = new Map()
                {
                    Name = "LensMap",
                    WrapAround = false,
                    IsLogoVisible = false,
                };
                LensMap.ExtentChanging += LensMap_ExtentChanging;
                LensMap.Loaded += LensMap_Loaded;
            }
            if (LensMap.Layers.Count() <= 1)
            {
                // Add basemap
                LensMap.Layers.Insert(0, MapBoard.GenerateMapLayerCollection(LensType.Basemap)[0]);
                LensMap.Layers[0].ID = LensType.Basemap.ToString();
            }
            LensMap.IsHitTestVisible = false;
            CleanMap();
        }

        private void CleanMap()
        {
            // Removes all layers but Basemap
            if (LensMap.Layers.Count() > 0)
            {
                for (int i = 0; i < LensMap.Layers.Count(); ++i)
                {
                    if(!LensMap.Layers[i].ID.Equals(LensType.Basemap.ToString()))
                    {
                        LensMap.Layers.RemoveAt(i);
                    }
                }
            }
        }

        private void LensMap_ExtentChanging(object sender, ExtentEventArgs e)
        {
            Board.GetLens(CurrentLocalLens).Extent = e.NewExtent;
            BroadcastExtent(CurrentLocalLens, e.NewExtent);
        }

        void LensMap_Loaded(object sender, RoutedEventArgs e)
        {
            //TODO send event saying lens was loaded
            SendBringToFrontEvent();
        }

        private void DisplayBaseMap()
        {
            ResetLensMap();
            Grid.SetZIndex(LensMap, 1);
            if(!LayoutRoot.Children.Contains(LensMap))
            {
                LayoutRoot.Children.Add(LensMap);
            }
        }

        private void AddLayerToMap(LensType lens)
        {
            // Add layer related to current lens
            Layer lc = MapBoard.GenerateMapLayerCollection(lens)[0];
            lc.ID = lens.ToString();

            // Extent
            Lens LensToBeDisplayed = Board.GetLens(lens);
            LensToBeDisplayed.Extent.SpatialReference = new SpatialReference() { WKID = 3857 }; // TODO: remove this!
            LensMap.Extent = LensToBeDisplayed.Extent;
            LensMap.Layers.Add(lc);

            // Legend            
            if (lens.Equals(LensType.Population))
            {
                legend = new ESRI.ArcGIS.Client.Toolkit.Legend();
                legend.Map = LensMap;
                legend.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                legend.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                legend.LayerIDs = new string[] { lens.ToString() };
                legend.LayerItemsMode = ESRI.ArcGIS.Client.Toolkit.Legend.Mode.Flat;
                legend.ShowOnlyVisibleLayers = true;
                Grid.SetZIndex(legend, 99);
                if (!LayoutRoot.Children.Contains(legend))
                {
                    LayoutRoot.Children.Add(legend);
                }
            }
        }

        private void RemoveLayerFromMap(LensType lens)
        {
            for (int i = 0; i < LensMap.Layers.Count(); i++)
            {
                if (LensMap.Layers[i].ID != null && LensMap.Layers[i].ID.Equals(lens.ToString()))
                {
                    LensMap.Layers.RemoveAt(i);
                }
            }
        }

        # endregion


        #region CurrentLensLabel
        Label CurrentLensLabel = new Label();
        private void DisplayCurrentLensLabel()
        {
            if (CurrentLocalLens != LensType.None)
            {
                CurrentLensLabel.Content = "Current lens: " + CurrentLocalLens;
                CurrentLensLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                CurrentLensLabel.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                CurrentLensLabel.FontSize = 24;
                Grid.SetZIndex(CurrentLensLabel, 99);
                if (!LayoutRoot.Children.Contains(CurrentLensLabel))
                {
                    LayoutRoot.Children.Add(CurrentLensLabel);
                }
            }
        }
        #endregion

        private void SendBringToFrontEvent()
        {
            // TODO : IMPLEMENT!

        }

        # region Lens Selection Menu
        private StackPanel LensSelectionMenu;
        private void DisplayLensSelectionMenu()
        {
            ConfigureLensSelectionMenu();
            if (!LayoutRoot.Children.Contains(LensSelectionMenu) && (CurrentAppMode == MapBoardMode.MultipleLenses || (CurrentAppMode == MapBoardMode.SingleLens && CurrentLocalLens == LensType.None)))
            {
                LensSelectionMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                LensSelectionMenu.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                Canvas.SetZIndex(LensSelectionMenu, 99);
                LayoutRoot.Children.Add(LensSelectionMenu);
            }
        }

        private void ConfigureLensSelectionMenu()
        {
            if (LensSelectionMenu == null)
            {
                LensSelectionMenu = new StackPanel()
                {
                    Name = "LensSelectionMenu",
                };
            }
            LensSelectionMenu.Children.Clear();
            AddLensSelectionButtons(LensType.Cities);
            //AddLensSelectionButtons(LensType.ElectoralDistricts);
            AddLensSelectionButtons(LensType.Population);
            AddLensSelectionButtons(LensType.Streets);
            AddLensSelectionButtons(LensType.Satellite);
        }

        private void AddLensSelectionButtons(LensType lens)
        {
            if (LensCanBeActivated(lens))
            {
                LensSelectionMenu.Children.Add(CreateLensButton(lens.ToString(), LensSelectionButton_Click));
            }
        }

        private void LensSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (aLensToKillFor != LensType.None)
            {
                Board.RemoveLens(aLensToKillFor);
                FreeLens(aLensToKillFor);
            }
            
            LensType lens = MapBoard.StringToLensType(((Button)sender).Name);
            FreeLens(CurrentLocalLens);
            LoadLens(lens);
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

        


        # endregion


        # region OverviewMode

        private StackPanel InsectStack;
        
        private void LoadOverviewMode()
        {
            DetailWindow.Title = "Overview";
            ClearUI();
            BuildOverviewUI();
        }

        private void BuildOverviewUI()
        {
            MainBoardUC = new MapBoardUC(LensType.Basemap, Board);
            LayoutRoot.Children.Add(MainBoardUC);

            RefreshInsectStack();
            
            DisplayOverviewMenu();
        }


        private StackPanel OverviewMenu;
        Button StartTaskBtn, TaskCompletedBtn;

        private void ConfigureOverviewMenu()
        {
            OverviewMenu = new StackPanel()
            {
                Name = "Overview",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Orientation = Orientation.Horizontal,
            };

            TaskCompletedBtn = CreateRegularButton("Complete Task", CompleteTask_Click);
            StartTaskBtn = CreateRegularButton("Start task", StartTask_Click);

            //OverviewMenu.Children.Add(CreateRegularButton("Reset", Reset_Click));
            //OverviewMenu.Children.Add(CreateRegularButton("Refresh", RefreshOverview_Click));
            OverviewMenu.Children.Add(StartTaskBtn);
        }

        private void StartTask_Click(object sender, RoutedEventArgs e)
        {
            OverviewMenu.Children.Remove(StartTaskBtn);
            if (!OverviewMenu.Children.Contains(TaskCompletedBtn))
                OverviewMenu.Children.Add(TaskCompletedBtn);
            SendStartTaskMsg();
        }

        private void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            SendCompleteTaskMsg();
            SendResetAppMessage();
            OverviewMenu.Children.Remove(TaskCompletedBtn);
            if (!OverviewMenu.Children.Contains(StartTaskBtn))
                OverviewMenu.Children.Add(StartTaskBtn);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            SendResetAppMessage();
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

        private void RefreshOverview_Click(object sender, RoutedEventArgs e)
        {
            LoadOverviewMode();
        }

        private Button CreateRegularButton(string content, RoutedEventHandler reh)
        {
            Button b = new Button();
            b.Content = content;
            b.Width = 100;
            b.Height = 50;
            b.Click += reh;
            b.BorderBrush = Brushes.Black;
            b.Background = Brushes.White;
            return b;
        }

        
        # endregion


        # region MainBoardUserControl

        MapBoardUC MainBoardUC;
        void MBUC_ExtentUpdated(object sender, MapEventArgs e)
        {
            Board.GetLens(e.ModifiedLens).Extent = e.Extent;
            if(MainBoardUC != null && MainBoardUC.IsActive)
                BroadcastExtent(e.ModifiedLens, e.Extent);
        }

        private void InitializeMainBoardWith(LensType lens)
        {
            Board.StartLens(lens);
            MainBoardUC = new MapBoardUC(lens, Board);
            if(CurrentAppMode == MapBoardMode.MultipleLenses || CurrentAppMode == MapBoardMode.SingleLens)
            {
                MainBoardUC.DisplayViewfinders = false;
            }
            MainBoardUC.MapExtentUpdated += MBUC_ExtentUpdated;
            LayoutRoot.Children.Add(MainBoardUC);
        }
        # endregion


        #region InsectStack
        
        Canvas ShadowsCanvas; // Canvas to draw insects' shadows

        private void AddInsect(LensType lens, MapBoard board)
        {
            MapBoardUC mbuc = new MapBoardUC(lens, board);
            mbuc.Name = lens.ToString();
            
            //double ratio = 3;
            double width = this.Width / 3;
            double ratio = width / 1920;
            mbuc.Width = 1920 * ratio;
            mbuc.Height = 1080 * ratio;
            mbuc.PassiveMode = true;
            mbuc.BorderBrush = new SolidColorBrush(MapBoard.GetColorOf(lens));
            mbuc.BorderThickness = new Thickness(3.0);
            mbuc.Margin = new Thickness(2.0);
            mbuc.Loaded += insect_Loaded;
            Canvas.SetZIndex(mbuc, 99);
            InsectStack.Children.Add(mbuc);
        }

        void insect_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExtentAtInsectStack(((MapBoardUC)sender).CurrentLens, Board);
            RefreshShadowsCanvas();
        }

        private void UpdateZAtInsectStack(LensType lens, MapBoard board)
        {
            for (int i = 0; i < InsectStack.Children.Count; i++)
            {
                if (InsectStack.Children[i] is MapBoardUC)
                {
                    ((MapBoardUC)InsectStack.Children[i]).UpdateZOf(lens, board);
                }
            }
            RefreshShadowsCanvas();
        }

        private void UpdateExtentAtInsectStack(LensType lens, MapBoard board)
        {
            for (int i = 0; i < InsectStack.Children.Count; i++)
            {
                if (InsectStack.Children[i] is MapBoardUC)
                {
                    ((MapBoardUC)InsectStack.Children[i]).UpdateExtentOf(lens, board);
                }
            }
            RefreshShadowsCanvas();
        }

        private void RemoveAllInsects()
        {
            if (InsectStack != null)
            {
                for (int i = 0; i < InsectStack.Children.Count; i++)
                {
                    if (InsectStack.Children[i] is MapBoardUC)
                    {
                        ((MapBoardUC)InsectStack.Children[i]).ClearUI();
                    }
                }
                InsectStack.Children.Clear();
            }
            ClearShadowsCanvas();
        }

        private void RemoveLensFromInsectStack(LensType lens, MapBoard board)
        {
            if(lens == LensType.All)
            {
                RemoveAllInsects();
                return;
            }

            for (int i = 0; i < InsectStack.Children.Count; i++)
            {
                if (InsectStack.Children[i] is MapBoardUC)
                {
                    if (((MapBoardUC)(InsectStack.Children[i])).CurrentLens.Equals(lens))
                    {
                        ((MapBoardUC)InsectStack.Children[i]).ClearUI();
                        InsectStack.Children.RemoveAt(i);
                    }
                    else
                    {
                        ((MapBoardUC)InsectStack.Children[i]).Remove(lens, board);
                    }
                }
            }
            RefreshInsectStack();
        }

        private void RefreshInsectStack()
        {
            ConfigureInsectStack();

            RemoveAllInsects();

            foreach (LensType lens in Board.LensStack)
            {
                if (lens != LensType.Basemap)
                {
                    AddInsect(lens, Board);
                }
            }

            if (!LayoutRoot.Children.Contains(ShadowsCanvas))
            {
                LayoutRoot.Children.Add(ShadowsCanvas);
            }

            if (!LayoutRoot.Children.Contains(InsectStack))
            {
                LayoutRoot.Children.Add(InsectStack);
            }
            RefreshShadowsCanvas();
        }

        private void RefreshInsects()
        {
            for (int i = 0; i < InsectStack.Children.Count; i++)
            {
                if (InsectStack.Children[i] is MapBoardUC)
                {
                    ((MapBoardUC)InsectStack.Children[i]).ResetBoard(Board);
                }
            }
        }


        #region Insect Shadow
        private void RefreshShadowsCanvas()
        {
            ClearShadowsCanvas();
            DrawAllShadows();
        }

        private void ClearShadowsCanvas()
        {
            if (ShadowsCanvas != null)
            {
                ShadowsCanvas.Children.Clear();
            }
        }

        private void DrawAllShadows()
        {
            foreach (LensType lens in Board.LensStack)
            {
                if (lens != LensType.Basemap)
                    DrawShadowOf(lens);
            }
        }

        private void DrawShadowOf(LensType lens)
        {
            if(InsectExistsOnUI(lens))
            {
                Dictionary<string, Point> lensCoord = MainBoardUC.GetScreenCoordinatesOf(lens);
                if (lensCoord.ContainsKey("topLeft"))
                {

                    int i = GetInsectIndex(lens);
                    MapBoardUC mbuc = (MapBoardUC)InsectStack.Children[i];
                    Point p = mbuc.TranslatePoint(new Point(0, 0), this);
                    Point screenCord = this.PointToScreen(p);

                    Point I_topLeft = new Point(screenCord.X, screenCord.Y);
                    Point I_bottomLeft = new Point(screenCord.X, screenCord.Y + mbuc.ActualHeight);
                    Point I_topRight = new Point(screenCord.X + mbuc.ActualWidth, screenCord.Y);
                    Point I_bottomRight = new Point(screenCord.X + mbuc.ActualWidth, screenCord.Y + mbuc.ActualHeight);

                    Point L_topLeft = lensCoord["topLeft"];
                    Point L_bottomLeft = lensCoord["bottomLeft"];
                    Point L_topRight = lensCoord["topRight"];
                    Point L_bottomRight = lensCoord["bottomRight"];

                    Point[] leftSide = { L_topLeft, I_topLeft, I_bottomLeft, L_bottomLeft };
                    Point[] rightSide = { I_topRight, I_bottomRight, L_bottomRight, L_topRight };
                    Point[] rear = { L_topLeft, I_topLeft, I_topRight, L_topRight };
                    Point[] front = { L_bottomRight, I_bottomRight, I_bottomLeft, L_bottomLeft };

                    Brush color = new SolidColorBrush(MapBoard.GetColorOf(lens));

                    int ZIndex = Board.ZUIIndexOf(lens);

                    DrawSide(rear, color, ZIndex);
                    DrawSide(leftSide, color, ZIndex);
                    DrawSide(rightSide, color, ZIndex);
                    DrawSide(front, color, ZIndex);
                }
            }
        }

        private void DrawSide(Point[] points, Brush color, int ZIndex)
        {
            System.Windows.Shapes.Polygon side = new System.Windows.Shapes.Polygon();
            side.Fill = color;
            side.Opacity = 0.1;
            side.Points = new System.Windows.Media.PointCollection() {
                   points[0], points[1], points[2], points[3]
                };
            ShadowsCanvas.Children.Add(side);
            Grid.SetZIndex(side, ZIndex);
        }

        #endregion


        




        #region Auxiliar functions
        private void ConfigureInsectStack()
        {
            if (InsectStack == null)
            {
                InsectStack = new StackPanel()
                {
                    Name = "InsectStack",
                };
                InsectStack.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                InsectStack.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                InsectStack.Orientation = Orientation.Horizontal;
                Canvas.SetZIndex(InsectStack, 99);
            }
            if (ShadowsCanvas == null)
            {
                ShadowsCanvas = new Canvas()
                {
                    Name = "InsectCanvas"
                };
                Canvas.SetZIndex(InsectStack, 98);
            }
        }

        private bool InsectExistsOnUI(LensType viewfinderType)
        {
            if (InsectStack != null)
            {
                for (int i = 0; i < InsectStack.Children.Count; i++)
                {
                    if (InsectStack.Children[i] is MapBoardUC)
                    {
                        LensType type = MapBoard.StringToLensType(((MapBoardUC)InsectStack.Children[i]).Name);
                        if (type == viewfinderType) return true;
                    }
                }
            }
            return false;
        }

        private int GetInsectIndex(LensType lens)
        {
            for (int i = 0; i < InsectStack.Children.Count; i++)
            {
                if (InsectStack.Children[i] is MapBoardUC)
                {
                    LensType type = MapBoard.StringToLensType(((MapBoardUC)InsectStack.Children[i]).Name);
                    if (type == lens) return i;
                }
            }
            return -1;
        }

        #endregion

        #endregion


        # region Board Event Handlers
        void Board_LensStackPositionChanged(object sender, LensEventArgs e)
        {
            if (CurrentAppMode == MapBoardMode.Overview && MainBoardUC != null)
            {
                MainBoardUC.UpdateZOf(e.ModifiedLens, (MapBoard)sender);
                UpdateZAtInsectStack(e.ModifiedLens, (MapBoard)sender);
            }
        }

        void Board_LensRemoved(object sender, LensEventArgs e)
        {
            if (CurrentAppMode == MapBoardMode.Overview && MainBoardUC != null)
            {
                MainBoardUC.Remove(e.ModifiedLens, (MapBoard)sender);
                RemoveLensFromInsectStack(e.ModifiedLens, (MapBoard)sender);
                RefreshInsects();
            }
        }

        void Board_LensExtentUpdated(object sender, LensEventArgs e)
        {
            if (CurrentAppMode == MapBoardMode.Overview && MainBoardUC != null)
            {
                MainBoardUC.UpdateExtentOf(e.ModifiedLens, (MapBoard)sender);
                UpdateExtentAtInsectStack(e.ModifiedLens, (MapBoard)sender);
            }
        }

        void Board_LensAdded(object sender, LensEventArgs e)
        {
            if (CurrentAppMode == MapBoardMode.Overview && MainBoardUC != null)
            {
                MainBoardUC.UpdateExtentOf(e.ModifiedLens, (MapBoard)sender);
                AddInsect(e.ModifiedLens, (MapBoard)sender);
            }
        }

        #endregion


        private void ClearUI()
        {
            RemoveAllInsects();

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
            SoD.ownDevice.ID = "69";
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

            SoD.socket.On("LensStarted", (dict) => {
                this.RemoteLensStarted(SoD.ParseMessageIntoDictionary(dict));
            });
            
            SoD.socket.On("FreedLens", (dict) =>
            {
                this.RemoteLensWasFreed(SoD.ParseMessageIntoDictionary(dict));
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

        private void BroadcastStartLens(LensType lensT)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("lens", lensT.ToString());
            SoD.SendEventToDevices("LensStarted", dict, "all");
        }

        private void BroadcastFreedLens(LensType lensT)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("lens", lensT.ToString());
            SoD.SendEventToDevices("FreedLens", dict, "all");
        }

        private void SendRemoveLensModeMessage(LensType lensT)
        {
            Console.WriteLine("Sending remove lensmodemsg");
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("RemoveMode", lensT.ToString());
            SoD.SendDictionaryToDevices(dict, "all");
        }

        private void SendCompleteTaskMsg()
        {
            SoD.SendStringToDevices("TaskCompleted", "all");
        }

        private void SendStartTaskMsg()
        {
            SoD.SendStringToDevices("TaskStarted", "all");
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
                if (CurrentAppMode != MapBoardMode.SingleLens)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        LensType lens = MapBoard.StringToLensType(updateMode);
                        Board.UpdateLens(lens, extentString);
                        if(lens != RemoteLens)
                        {
                            RemoteLens = lens;
                            DisplayLensSelectionMenu();
                        }
                    }));
                }
            }

            if (removeMode != null)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    LensType lens = MapBoard.StringToLensType(removeMode);
                    if (lens.Equals(LensType.All))
                    {
                        Reset();
                        return;
                    }
                    Board.RemoveLens(lens);
                }));
            }

            //SYNCH
            if (tableConfiguration != null && tableConfiguration.Equals("All") && CurrentAppMode == MapBoardMode.None)
            {
                Console.WriteLine("Dictionary with table config received!");
                Board.ClearBoardAndStackLensesAccordingToOverview(parsedMessage["data"]["data"].ToObject<Dictionary<string, string>>());
            }
        }

        private void RemoteLensStarted(Dictionary<string, dynamic> parsedMessage)
        {
            RemoteLens = MapBoard.StringToLensType((String)parsedMessage["data"]["data"]["lens"]);
            this.Dispatcher.Invoke((Action)(() =>
                {
                    if (CurrentAppMode == MapBoardMode.Overview)
                    {
                        Board.BringToFront(RemoteLens);
                    }
                    else if ((CurrentAppMode == MapBoardMode.SingleLens && CurrentLocalLens == LensType.None)|| CurrentAppMode == MapBoardMode.MultipleLenses)
                    {
                        DisplayLensSelectionMenu();
                        if(RemoteLens == CurrentLocalLens)
                        {
                            DisplayRemoteLensInUseMsg();
                        }
                        else
                        {
                            RemoveRemoteLensInUseMsg();
                        }
                    }
                }));
        }

        private void RemoteLensWasFreed(Dictionary<string, dynamic> parsedMessage)
        {
            LensType fLens = MapBoard.StringToLensType((String)parsedMessage["data"]["data"]["lens"]);
            this.Dispatcher.Invoke((Action)(() =>
            {
                if ((CurrentAppMode == MapBoardMode.SingleLens && CurrentLocalLens == LensType.None) || CurrentAppMode == MapBoardMode.MultipleLenses)
                {
                    if (fLens == CurrentLocalLens)
                    {
                        RemoveRemoteLensInUseMsg();
                        RemoteLens = LensType.None;
                        ActivateLens(CurrentLocalLens);
                    }
                    else
                    {
                        RemoteLens = LensType.None;
                        DisplayLensSelectionMenu();
                    }
                }
            }));
        }

        # endregion


        private void KeyEvent(object sender, KeyEventArgs e) //Keyup Event 
        {
            switch (e.Key)
            {
                case Key.P:
                    LoadMode(MapBoardMode.Overview);
                    break;
                case Key.O:
                    LoadMode(MapBoardMode.SingleLens);
                    break;
                case Key.I:
                    LoadMode(MapBoardMode.MultipleLenses);
                    break;
                case Key.M:
                    DisplayStartMenu();
                    break;
                case Key.R:
                    MainBoardUC.ResetBoard(Board);
                    if(CurrentAppMode == MapBoardMode.Overview) RefreshInsectStack();
                    break;
                //case Key.A:
                //    ActivateMBUC();
                //    break;
                //case Key.S:
                //    DeactivateMBUC();
                //    break;
                case Key.C:
                    //if (CurrentAppMode != MapBoardMode.None && CurrentAppMode != MapBoardMode.Overview && CurrentLens != LensType.All && CurrentLens != LensType.None)
                    //{
                        DisplayLensSelectionMenu();
                    //}
                    break;
                case Key.Q:
                    Reset();
                    if (CurrentAppMode == MapBoardMode.Overview)
                    {
                        SendResetAppMessage();
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
