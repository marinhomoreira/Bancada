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
        public event MapModifiedEventHandler MapExtentUpdated;
        
        private LensType _currentLens = LensType.None;
        
        public LensType CurrentLens
        {
            get { return _currentLens; }
        }

        private bool _passiveMode = false;

        MapModifiedEventHandler temp;

        private bool _isActive = true;

        public bool IsActive
        {
            get { return _isActive; }
        }

        public bool PassiveMode
        {
            get { return _passiveMode; }
            set {
                _passiveMode = value;
                if(value)
                {
                    temp = MapExtentUpdated;
                    MapExtentUpdated = null;
                    if (LensMap != null)
                    {
                        LensMap.IsHitTestVisible = false;
                    }
                }
                else
                {
                    MapExtentUpdated = temp; // TODO: NEED TO TEST!
                    if (LensMap != null)
                    {
                        LensMap.IsHitTestVisible = true;
                    }
                }
            }
        }

        private MapBoard _localBoard;

        private bool _displayViewfinders = true;

        public bool DisplayViewfinders
        {
            get { return _displayViewfinders; }
            set { _displayViewfinders = value; ResetBoard(_localBoard); }
        }



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

        public void AddViewFinder(LensType lens, MapBoard board)
        {
            _localBoard = board;
            AddViewFinderToScreen(lens);
        }

        public void UpdateExtentOf(LensType lens, MapBoard board)
        {
            _localBoard = board;
            
            if (_currentLens == lens)
            {
                LensMap.Extent = _localBoard.GetLens(lens).Extent;
            }
            else if (lens == LensType.All)
            {
                LensMap.Extent = _localBoard.GetLens(lens).Extent;
                UpdateViewFinderExtent(lens);
            }
            else if (lens != LensType.None)
            {
                UpdateViewFinderExtent(lens);
            }
        }

        public void UpdateZOf(LensType lens, MapBoard board)
        {
            _localBoard = board;
            
            // TODO: Update stack box, if present.
            if (_currentLens == lens)
            {
                Grid.SetZIndex(LensMap, board.ZUIIndexOf(lens));
            }
            else if (lens == LensType.All)
            {
                Grid.SetZIndex(LensMap, board.ZUIIndexOf(lens));
                UpdateViewFinderZ(lens);
            }
            else if (lens != LensType.None)
            {
                UpdateViewFinderZ(lens);
            }
            RefreshAllViewFinders();
        }

        public void Remove(LensType lens, MapBoard board)
        {
            _localBoard = board;

            if (_currentLens == lens)
            {
                Deactivate();
            }
            else if (lens == LensType.All)
            {
                Deactivate();
                RemoveViewFinder(lens);
            }
            else if (lens != LensType.None && _currentLens != lens)
            {
                RemoveViewFinder(lens);
            }
        }

        public void ClearUI()
        {
            //Console.WriteLine("Resetting CurrentLens...");
            _currentLens = LensType.None;
            if (LensMap != null) LensMap.Layers.Clear();
            if (BasemapMap != null) BasemapMap.Layers.Clear();
            MBRoot.Children.Clear();
        }

        public void Activate()
        {
            if (this.LensMap != null)
            {
                this.LensMap.IsHitTestVisible = true;
                LensMap.Opacity = 1;
                if (tempExtent != null)
                {
                    LensMap.Extent = tempExtent;
                    _localBoard.GetLens(_currentLens).Extent = tempExtent;
                    tempExtent = null;
                }
                ResetBoard(_localBoard); // TODO: TEST!
                _isActive = true;
            }
        }

        Envelope tempExtent;

        public void Deactivate()
        {
            if(this.LensMap != null)
            {
                this.LensMap.IsHitTestVisible = false;
                LensMap.Opacity = 0.3;
                tempExtent = LensMap.Extent;
                _isActive = false;
            }
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

                if(_displayViewfinders)
                {
                    DisplayViewFinders();
                    RefreshAllViewFinders();
                }
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

        private void BasemapMap_Loaded(object sender, RoutedEventArgs e)
        {
            ResetBoard(_localBoard); // I HAVE NO IDEA WHY, BUT IT ONLY WORKS THIS WAY.
        }

        private void DisplayBaseMap()
        {
            //Console.WriteLine("Displaying basemap...");
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
                RefreshAllViewFinders();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        void LensMap_Loaded(object sender, RoutedEventArgs e)
        {
            //ResetBoard(_localBoard); // I HAVE NO IDEA WHY, BUT IT ONLY WORKS THIS WAY.
            OnMapExtentChanged(new MapEventArgs(_currentLens, LensMap.Extent));
        }

        private void DisplayLensMap()
        {
            Lens LensToBeDisplayed = _localBoard.GetLens(_currentLens);
            
            Layer layer = MapBoard.GenerateMapLayerCollection(_currentLens)[0];
            layer.ID = _currentLens.ToString();
            LensMap.Layers.Add(layer);
            
            LensToBeDisplayed.Extent.SpatialReference = new SpatialReference() { WKID = 3857 }; // TODO: remove this!
            
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
            ESRI.ArcGIS.Client.Toolkit.Legend legend;
            if (_currentLens.Equals(LensType.Population))
            {
                legend = new ESRI.ArcGIS.Client.Toolkit.Legend();
                legend.Map = LensMap;
                legend.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                legend.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                legend.LayerIDs = new string[] { _currentLens.ToString() };
                legend.LayerItemsMode = ESRI.ArcGIS.Client.Toolkit.Legend.Mode.Flat;
                legend.ShowOnlyVisibleLayers = true;
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

                if (viewfinderType != _currentLens)
                {
                    AddViewFinderToScreen(viewfinderType);
                }
            }
        }

        private void AddViewFinderToScreen(LensType lens)
        {
            if (!ViewFinderExistsOnUI(lens) && _displayViewfinders)
            {
                //Console.WriteLine("Adding " + lens + " viewfinder...");
                Lens viewfinder = _localBoard.GetLens(lens);
                MapViewFinder mvf = new MapViewFinder(viewfinder.Color, viewfinder.Extent)
                {
                    Map = this.BasemapMap,
                    Name = lens.ToString(),
                    Layers = MapBoard.GenerateMapLayerCollection(lens),
                };
                int ZUIIndex = _localBoard.ZUIIndexOf(lens);
                Grid.SetZIndex(mvf, ZUIIndex);
                MBRoot.Children.Add(mvf);
                mvf.UpdateExtent(viewfinder.Extent);
                mvf.Loaded += mvf_Loaded;
            }
        }

        private void mvf_Loaded(object sender, RoutedEventArgs e)
        {
            ((MapViewFinder)sender).Refresh();
            RefreshAllViewFinders();
        }

        private void RefreshAllViewFinders()
        {
            for (int i = 0; i < MBRoot.Children.Count; i++)
            {
                if (MBRoot.Children[i] is MapViewFinder)
                {
                    LensType type = MapBoard.StringToLensType(((MapViewFinder)MBRoot.Children[i]).Name);
                    
                    if (_localBoard.ViewFindersOf(_currentLens).ContainsKey(type))
                    {
                        // Check Z-index
                        if (Grid.GetZIndex(MBRoot.Children[i]) != _localBoard.ZUIIndexOf(type))
                        {
                            Grid.SetZIndex(MBRoot.Children[i], _localBoard.ZUIIndexOf(type));
                        }
                        
                        // Refresh MVF
                        ((MapViewFinder)(MBRoot.Children[i])).Refresh();
                    }
                    else
                    {
                        MBRoot.Children.Remove(MBRoot.Children[i]);
                    }
                }
            }
        }

        private void UpdateViewFinderExtent(LensType lens)
        {
            if (_displayViewfinders)
            {
                if (ViewFinderExistsOnUI(lens))
                {
                    ((MapViewFinder)MBRoot.Children[GetViewFinderUIIndex(lens)])
                        .UpdateExtent(_localBoard.GetLens(lens).Extent);
                }
                else if (lens == LensType.All)
                {
                    RefreshAllViewFinders();
                }
                else
                {
                    AddViewFinderToScreen(lens);
                }
            }
        }

        private void UpdateViewFinderZ(LensType lens)
        {
            if (_displayViewfinders)
            {
                if (ViewFinderExistsOnUI(lens))
                {
                    Grid.SetZIndex(MBRoot.Children[GetViewFinderUIIndex(lens)], _localBoard.ZUIIndexOf(lens));
                }
                else if (lens == LensType.All)
                {
                    RefreshAllViewFinders();
                }
                else
                {
                    AddViewFinderToScreen(lens);
                }
            }
        }

        private void RemoveViewFinder(LensType lens)
        {
            if (ViewFinderExistsOnUI(lens))
            {
                MBRoot.Children.RemoveAt(GetViewFinderUIIndex(lens));
                RefreshAllViewFinders();
            }
            else if (lens == LensType.All)
            {
                RefreshAllViewFinders();
            }
        }

        # endregion

        public Dictionary<string, Point> GetScreenCoordinatesOf(LensType lens)
        {
            Dictionary<string, Point> dict = new Dictionary<string, Point>();

            if(ViewFinderExistsOnUI(lens))
            {
                try
                {
                    int i = GetViewFinderUIIndex(lens);
                    MapViewFinder mvf = (MapViewFinder)MBRoot.Children[i];
                    Point canvasP = this.TranslatePoint(new Point(0, 0), mvf);

                    Point p = (mvf.TransformToVisual(this)).Transform(
                        new Point(
                            mvf.Translate.X,
                            mvf.Translate.Y
                        )
                    );

                    Point screenCord;
                    screenCord = this.PointToScreen(p);

                    Point topLeft = new Point(screenCord.X, screenCord.Y);
                    Point bottomLeft = new Point(screenCord.X, screenCord.Y + mvf.ActualHeight);
                    Point topRight = new Point(screenCord.X + mvf.ActualWidth, screenCord.Y);
                    Point bottomRight = new Point(screenCord.X + mvf.ActualWidth, screenCord.Y + mvf.ActualHeight);

                    dict.Add("topLeft", topLeft);
                    dict.Add("bottomLeft", bottomLeft);
                    dict.Add("topRight", topRight);
                    dict.Add("bottomRight", bottomRight);
                }
                catch (Exception e)
                {

                }
            }

            return dict;
        }

        protected virtual void OnMapExtentChanged(MapEventArgs e)
        {
            if(MapExtentUpdated != null)
            {
                MapExtentUpdated(this, e);
            }
        }

        private bool ViewFinderExistsOnUI(LensType viewfinderType)
        {
            if (_displayViewfinders)
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
