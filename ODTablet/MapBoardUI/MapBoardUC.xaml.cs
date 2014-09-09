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
using ESRI.ArcGIS.Client.Geometry;

namespace ODTablet.MapBoardUI
{

    public delegate void MapModifiedEventHandler(object sender, MapEventArgs e);

    public class MapEventArgs : EventArgs
    {
        public readonly LensType ModifiedLens;
        public readonly Envelope Extent;
        public MapEventArgs(LensType lens, Envelope extent)
        {
            ModifiedLens = lens;
            Extent = extent;
        }
    }

    
    /// <summary>
    /// Interaction logic for MapBoardUC.xaml
    /// </summary>
    public partial class MapBoardUC : UserControl
    {
        public event MapModifiedEventHandler ExtentUpdated;
        
        private LensType _currentLens = LensType.None;
        
        public LensType CurrentLens
        {
            get { return _currentLens; }
        }

        private MapBoard _localBoard;


        # region Interface
        public MapBoardUC(LensType lens, MapBoard board)
        {
            InitializeComponent();
            _currentLens = lens;
            _localBoard = board;
            LoadUI();
        }

        public void ResetBoard(MapBoard board)
        {
            LensType lens = _currentLens;
            ClearUI();
            _currentLens = lens;
            _localBoard = board;
            LoadUI();
        }
        
        
        public void ClearUI()
        {
            Console.WriteLine("Resetting CurrentLens...");
            _currentLens = LensType.None;
            if (LensMap != null) LensMap.Layers.Clear();
            if (BasemapMap != null) BasemapMap.Layers.Clear();
            MBRoot.Children.Clear();
        }

        # endregion

        # region Initialization
        private void LoadUI()
        {
            if (_currentLens != LensType.None && _currentLens != LensType.All)
            {
                ConfigureBaseMap();
                DisplayBaseMap();
                if (_currentLens != LensType.Basemap)
                {
                    ConfigureLensMap();
                    DisplayLensMap();
                }
                DisplayViewFinders();
            }
        }

        # region Basemap
        private Map BasemapMap;
        private void ConfigureBaseMap()
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
                BasemapMap.Loaded += BasemapMap_Loaded;
            }
        }

        void BasemapMap_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayViewFinders();
        }

        private void DisplayBaseMap()
        {
            Console.WriteLine("Displaying basemap...");
            Lens basem = _localBoard.GetLens(LensType.Basemap);
            BasemapMap.Layers.Add(MapBoard.GenerateMapLayerCollection(LensType.Basemap)[0]);
            BasemapMap.Extent = basem.Extent;

            Grid.SetZIndex(BasemapMap, _localBoard.ZUIIndexOf(LensType.Basemap));
            MBRoot.Children.Add(BasemapMap);
        }
        # endregion

        # region LensMap
        private Map LensMap;
        
        private void ConfigureLensMap()
        {
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
                LensMap.Loaded += LensMap_Loaded;
            }
        }

        private void LensMap_ExtentChanging(object sender, ExtentEventArgs e)
        {
            OnMapExtentChanged(new MapEventArgs(_currentLens, e.NewExtent));
            try
            {
                BasemapMap.Extent = e.NewExtent;
                //UpdateAllLensAccordingCurrentModeExtent();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        void LensMap_Loaded(object sender, RoutedEventArgs e)
        {
            //UpdateAllLensAccordingCurrentModeExtent();
        }


        private void DisplayLensMap()
        {
            Console.WriteLine("Displaying " + _currentLens + " lens...");

            Lens LensToBeDisplayed = _localBoard.GetLens(_currentLens);
            LensMap.Layers.Add(LensToBeDisplayed.MapLayer); // TODO: NEW LAYER? MapBoard.GenerateMapLayerCollection(_currentLens)[0]
            LensMap.Extent = LensToBeDisplayed.Extent;

            Grid.SetZIndex(LensMap, _localBoard.ZUIIndexOf(_currentLens));
            MBRoot.Children.Add(LensMap);

            try
            {
                BasemapMap.Extent = LensToBeDisplayed.Extent != null ? LensToBeDisplayed.Extent : MapBoard.InitialExtentFrom(_currentLens);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Unable to use _currentLens extent to change basemap extent: " + exception.Message);
            }

            // Legend
            ESRI.ArcGIS.Client.Toolkit.Legend legend; // TODO: Remove it from here when solving legend problem.
            if (_currentLens.Equals(LensType.Population))
            {
                legend = new ESRI.ArcGIS.Client.Toolkit.Legend();
                legend.Map = LensMap;
                legend.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                legend.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                legend.LayerIDs = new string[] { _currentLens.ToString() };
                legend.LayerItemsMode = ESRI.ArcGIS.Client.Toolkit.Legend.Mode.Flat;
                legend.ShowOnlyVisibleLayers = false;
                Grid.SetZIndex(legend, 99);
                MBRoot.Children.Add(legend);
            }
        }


        # endregion

        # endregion

        # region ViewFinders
        private void DisplayViewFinders()
        {
            foreach (KeyValuePair<LensType, Lens> viewfinders in _localBoard.ViewFindersOf(_currentLens))
            {
                var viewfinderType = viewfinders.Key;

                if (viewfinderType != CurrentLens)
                {
                    //AddViewFinderToScreen(_localBoard, viewfinders.Key);
                    if (!ViewFinderExistsOnUI(viewfinderType))
                    {
                        Console.WriteLine("Adding " + viewfinderType + " viewfinder...");
                        Lens viewfinder = _localBoard.GetLens(viewfinderType);
                        MapViewFinder mvf = new MapViewFinder(viewfinder.Color, viewfinder.Extent)
                        {
                            Map = this.BasemapMap,
                            Name = viewfinderType.ToString(),
                            Layers = MapBoard.GenerateMapLayerCollection(viewfinderType),
                        };
                        int ZUIIndex = _localBoard.ZUIIndexOf(viewfinderType);
                        Grid.SetZIndex(mvf, ZUIIndex);
                        MBRoot.Children.Add(mvf);
                        mvf.UpdateExtent(viewfinder.Extent);
                        //mvf.Loaded += mvf_Loaded;
                    }
                }
            }
        }

        private void AddViewFinderToScreen(MapBoard _localBoard, LensType viewfinderType)
        {
            if (!ViewFinderExistsOnUI(viewfinderType))
            {
                Console.WriteLine("Adding " + viewfinderType + " viewfinder...");
                Lens viewfinder = _localBoard.GetLens(viewfinderType);
                MapViewFinder mvf = new MapViewFinder(viewfinder.Color, viewfinder.Extent)
                {
                    Map = this.BasemapMap,
                    Name = viewfinderType.ToString(),
                    Layers = MapBoard.GenerateMapLayerCollection(viewfinderType),
                };
                int ZUIIndex = _localBoard.ZUIIndexOf(viewfinderType);
                Grid.SetZIndex(mvf, ZUIIndex);
                MBRoot.Children.Add(mvf);
                mvf.UpdateExtent(viewfinder.Extent);
                mvf.Loaded += mvf_Loaded;
            }
        }

        private void mvf_Loaded(object sender, RoutedEventArgs e)
        {
            //UpdateAllLensAccordingCurrentModeExtent();
        }



        private void RemoveAllViewFinders()
        {
            for (int i = 0; i < MBRoot.Children.Count; i++)
            {
                if (MBRoot.Children[i] is MapViewFinder)
                {
                    Console.WriteLine("Removing " + ((MapViewFinder)MBRoot.Children[i]).Name + " viewfinder");
                    MBRoot.Children.Remove(MBRoot.Children[i]);
                }
            }
        }

        private void UpdateViewFinderExtent(MapBoard Board, LensType lens)
        {
            if (ViewFinderExistsOnUI(lens))
            {
                int i = GetViewFinderUIIndex(lens);
                ((MapViewFinder)MBRoot.Children[i]).UpdateExtent(Board.ViewFindersOf(CurrentLens)[lens].Extent);
            }
            else
            {
                // TODO: Add VF
            }
        }

        

        private void RefreshViewFinders()
        {
            RemoveAllViewFinders();
            DisplayViewFinders();
        }

        # endregion





        

        protected virtual void OnMapExtentChanged(MapEventArgs e)
        {
            if(ExtentUpdated != null)
            {
                ExtentUpdated(this, e);
            }
            //UpdateAllLensAccordingCurrentModeExtent();
        }

        private bool ViewFinderExistsOnUI(LensType viewfinderType)
        {
            for (int i = 0; i < MBRoot.Children.Count; i++)
            {
                if (MBRoot.Children[i] is MapViewFinder)
                {
                    LensType type = MapBoard.StringToLensType(((MapViewFinder)MBRoot.Children[i]).Name);
                    if (type == viewfinderType) return true;
                }
            }
            return false;
        }

        private int GetViewFinderUIIndex(LensType lens)
        {
            for (int i = 0; i < MBRoot.Children.Count; i++)
            {
                if (MBRoot.Children[i] is MapViewFinder)
                {
                    LensType type = MapBoard.StringToLensType(((MapViewFinder)MBRoot.Children[i]).Name);
                    if (type == lens) return i;
                }
            }
            return -1;
        }

    }
}
