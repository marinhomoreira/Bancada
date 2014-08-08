using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODTablet.MapModel
{

    public enum MapBoardMode { Overview, SingleLens, MultipleLenses};
    public enum LensType { None, All, Satellite, Streets, Population, ElectoralDistricts, Cities, Basemap };

    public delegate void ChangedEventHandler(object sender, EventArgs e);

    class MapBoard
    {
        private Dictionary<LensType, Lens> _lensCollection;

        public event ChangedEventHandler LensCollectionChanged;
        public event ChangedEventHandler ViewFindersChanged;
        
        public Dictionary<LensType, Lens> ActiveLenses
        {
            get { return _lensCollection; }
        }

        public MapBoard()
        {
            _lensCollection = new Dictionary<LensType, Lens>();
        }
        
        int zIndexCounter = 1;
        bool isDirty = false;

        # region UpdateLens
        public void UpdateLens(String lensName, String extent)
        {
            LensType lens = StringToLensType(lensName);
            Envelope e = StringToEnvelope(extent);
            UpdateLens(lens, e);
        }

        public void UpdateLens(LensType lens, String extent)
        {
            Envelope e = StringToEnvelope(extent);
            UpdateLens(lens, e);
        }

        public void UpdateLens(LensType lens, Envelope extent)
        {
            if (!LensIsActive(lens) && LensCanBeActivated(lens))
            {
                if(extent != null) // TODO: ExtentIsValid()?
                {
                    _lensCollection.Add(lens, new LensFactory().CreateLens(lens, extent));
                }
                else
                {
                    _lensCollection.Add(lens, new LensFactory().CreateLens(lens));
                }
                lensStack.Add(lens);
                isDirty = true;
            }
            else
            {
                if (_lensCollection[lens].Extent.ToString().Equals(extent.ToString())) // TODO: Change this for something better!
                {
                    _lensCollection[lens].Extent = extent;
                    isDirty = true;
                }
            }
            UpdateZIndex(lens, -1);
            SendEventIfDirty();
        }

        public void UpdateLens(LensType lens, Envelope extent, int? ZIndex)
        {
            UpdateLens(lens, extent);
            if(LensIsActive(lens))
            {
                UpdateZIndex(lens, -1);
            }
            SendEventIfDirty();
        }

        public void UpdateLens(Dictionary<string, string> remoteDictionary)
        {
            // TODO: diff and so on....
            // If RemoteTableConfiguration is empty
            foreach (KeyValuePair<string, string> entry in remoteDictionary)
            {
                if (LensCanBeActivated(entry.Key))
                {
                    UpdateLens(entry.Key, entry.Value);
                }
            }
        }

        private List<LensType> lensStack = new List<LensType>();

        public void UpdateZIndex(LensType lens, int? zIndex)
        {
            if (lens == LensType.Basemap && _lensCollection[LensType.Basemap].UIIndex != 0)
            {
                _lensCollection[lens].UIIndex = 0;
                return;
            }
            if (zIndex != null && (int)zIndex == -1)
            {
                _lensCollection[lens].UIIndex = lensStack.IndexOf(lens)+1;
                return;
            }

            if (zIndex == null) // TODO: One condition is missing. What is it? u_u'
            {
                _lensCollection[lens].UIIndex = zIndexCounter++; // TODO: I think zIndexCounter doesn't work as expected
                return;
            }

            if (_lensCollection[lens].UIIndex != (int)zIndex)
            {
                _lensCollection[lens].UIIndex = (int)zIndex;
            }
        }
        # endregion
        
        public void SendEventIfDirty()
        {
            if(isDirty)
            {
                OnLensCollectionChanged(EventArgs.Empty);
                isDirty = false;
            }
        }
        
        public Dictionary<string, string> ActiveLensesToDictionary()
        {
            Dictionary<string, string> lensesDic = new Dictionary<string,string>();
            foreach(KeyValuePair<LensType, Lens> lens in _lensCollection)
            {
                lensesDic.Add(lens.Key.ToString(), lens.Value.Extent.ToString());
            }
            return lensesDic;
        }

        public Lens GetLens(LensType lens)
        {
            if (!LensIsActive(lens))
            {
                UpdateLens(lens, null, null);
            }
            return _lensCollection[lens];
        }

        public Dictionary<LensType, Lens> ViewFindersOf(LensType lens)
        {
            Dictionary<LensType, Lens> vfal = new Dictionary<LensType,Lens>();
            foreach(KeyValuePair<LensType, Lens> viewfinder in _lensCollection)
            {
                if (viewfinder.Key != lens 
                    && viewfinder.Key != LensType.Basemap 
                    && viewfinder.Key != LensType.None 
                    && viewfinder.Key != LensType.All)
                {
                    vfal.Add(viewfinder.Key, viewfinder.Value);
                }
            }
            return vfal;
        }

        public static LayerCollection GenerateMapLayerCollection(LensType lens) // Method used to create new layers and avoid the "Layer is being used by another map" problem.
        {
            return new LensFactory().CreateLens(lens).MapLayerCollection;
        }

        public bool RemoveLens(LensType lens)
        {
            if (LensIsActive(lens))
            {
                _lensCollection.Remove(lens);
                OnLensCollectionChanged(EventArgs.Empty);
                return true;
            } else if(lens == LensType.All)
            {
                _lensCollection.Clear();
                OnLensCollectionChanged(EventArgs.Empty);
                return true;
            }
            return false;
        }

        public bool RemoveLens(string lens)
        {
            try
            {
                return RemoveLens(StringToLensType(lens));
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid lens: " + lens + " " + e.Message);
                return false;
            }
        }


        private bool LensIsActive(LensType lens)
        {
            return (_lensCollection.ContainsKey(lens) && _lensCollection[lens] != null);
        }

        private bool LensCanBeActivated(LensType lens)
        {
            return lens != LensType.None && lens != LensType.All;
        }

        private bool LensCanBeActivated(String lens)
        {
            try
            {
                return LensCanBeActivated(StringToLensType(lens));
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid lens: " + lens + " " + e.Message);
                return false;
            }
        }

        protected virtual void OnLensCollectionChanged(EventArgs e)
        {
            if (LensCollectionChanged != null)
            {
                LensCollectionChanged(this, e);
            }
        }

        protected virtual void OnViewFindersChanged(EventArgs e)
        {
            if (ViewFindersChanged != null)
            {
                ViewFindersChanged(this, e);
            }
        }

        private Envelope StringToEnvelope(String extent)
        {
            double[] extentPoints = Array.ConvertAll(extent.Split(','), Double.Parse);
            ESRI.ArcGIS.Client.Geometry.Envelope myEnvelope = new ESRI.ArcGIS.Client.Geometry.Envelope();
            myEnvelope.XMin = extentPoints[0];
            myEnvelope.YMin = extentPoints[1];
            myEnvelope.XMax = extentPoints[2];
            myEnvelope.YMax = extentPoints[3];
            //myEnvelope.SpatialReference = this.BasemapMap.SpatialReference; //TODO: how to define this?
            return myEnvelope;
        }
        
        public static LensType StringToLensType(string lens)
        {
            return (LensType)Enum.Parse(typeof(LensType), lens, true);
        }

        public static MapBoardMode StringToMapBoardMode(string mapboardMode)
        {
            return (MapBoardMode)Enum.Parse(typeof(MapBoardMode), mapboardMode, true);
        }

    }
}
