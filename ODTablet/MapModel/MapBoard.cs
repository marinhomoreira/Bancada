using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODTablet.MapModel
{

    public enum MapBoardMode {None, Overview, SingleLens, MultipleLenses };
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
        
        bool isDirty = false;

        # region ClearBoardAndDisplayLensesAccordingToOverview
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


        // What really matters starts here
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
                OnLensCollectionChanged(EventArgs.Empty);
            }
            else
            {
                if (!_lensCollection[lens].Extent.ToString().Equals(extent.ToString())) // TODO: Change this for something better!
                {
                    _lensCollection[lens].Extent = extent;
                    isDirty = true;
                }
            }
            UpdateZIndex(lens, (string)null);
            SendViewFindersChangedEventIfDirty();
        }

        public void UpdateLens(LensType lens, Envelope extent, string ZIndex)
        {
            UpdateLens(lens, extent);
            if(LensIsActive(lens))
            {
                UpdateZIndex(lens, ZIndex);
            }
        }

        

        public void ClearBoardAndDisplayLensesAccordingToOverview(Dictionary<string, string> remoteDictionary)
        {
            if (remoteDictionary.Count > 1)
            {
                _lensCollection = new Dictionary<LensType, Lens>();
                foreach (KeyValuePair<string, string> entry in remoteDictionary)
                {
                    if (LensCanBeActivated(entry.Key))
                    {
                        string[] extentPointsPlusZUIIndex = entry.Value.Split(';');
                        UpdateLens(entry.Key, extentPointsPlusZUIIndex[0]);
                        UpdateZIndex(entry.Key, extentPointsPlusZUIIndex[1]);
                    }
                }
            }
            else
            {
                Console.WriteLine("Empty dictionary");
            }
            
        }

        # endregion




        # region ZUIIndex

        private List<LensType> lensStack = new List<LensType>();
        public void UpdateZIndex(LensType lens, int? zIndex)
        {
            // Basemap is always 0;
            if (lens == LensType.Basemap && !lensStack.Contains(LensType.Basemap) && lensStack.IndexOf(LensType.Basemap) != 0)
            {
                lensStack.Remove(lens);
                lensStack.Insert(0, lens);
                isDirty = true;
                return;
            }

            if (zIndex == null)
            {
                if (!lensStack.Contains(lens))
                {
                    // No lens present and zIndex is null, just add to the stack.
                    lensStack.Add(lens);
                    isDirty = true;
                }
            }
            else
            {
                if (!lensStack.Contains(lens))
                {
                    // zIndex is valid but the stack doesn't contain the given lens
                    lensStack.Insert((int)zIndex, lens);
                    isDirty = true;
                }
                else
                {
                    // zIndex is valid but the stack contains the given lens in a different position
                    if (lensStack.IndexOf(lens) != (int)zIndex)
                    {
                        lensStack.Remove(lens);
                        lensStack.Insert((int)zIndex, lens);
                        isDirty = true;
                    }
                }
            }
            SendViewFindersChangedEventIfDirty();
        }

        public void UpdateZIndex(string lensName, string zIndex)
        {
            LensType lens = StringToLensType(lensName);
            if (zIndex.Equals("") ||zIndex == null )
            {
                UpdateZIndex(lens, (int?)null);
                return;
            }
            try {
                int index = Convert.ToInt32(zIndex);
                UpdateZIndex(lens, index);
            }
            catch(Exception e){
                Console.WriteLine("Problem in UpdateZIndex(string, string) : " + e.Message);
            }
        }

        private void UpdateZIndex(LensType lens, string zIndex)
        {
            if (zIndex == null || zIndex.Equals(""))
            {
                UpdateZIndex(lens, (int?)null);
                return;
            }
            try
            {
                UpdateZIndex(lens, Convert.ToInt32(zIndex));
            }
            catch (Exception e)
            {
                Console.WriteLine("Problem in UpdateZIndex(LensType, string) : " + e.Message);
            }
        }

        public int ZUIIndexOf(LensType lens)
        {
            return lensStack.IndexOf(lens);
            //return _lensCollection[lens].UIIndex;
        }
        # endregion



        



        public Lens GetLens(LensType lens)
        {
            if (!LensIsActive(lens))
            {
                UpdateLens(lens, null, null);
            }
            return _lensCollection[lens];
        }

        public Dictionary<LensType, Lens> ViewFindersOnTopOf(LensType lens)
        {
            Dictionary<LensType, Lens> vfal = new Dictionary<LensType,Lens>();
            foreach(KeyValuePair<LensType, Lens> viewfinder in _lensCollection)
            {
                if (viewfinder.Key != lens 
                    && viewfinder.Key != LensType.Basemap 
                    && viewfinder.Key != LensType.None 
                    && viewfinder.Key != LensType.All
                    && ZUIIndexOf(viewfinder.Key) > ZUIIndexOf(lens))
                {
                    vfal.Add(viewfinder.Key, viewfinder.Value);
                }
            }
            return vfal;
        }

        public Dictionary<LensType, Lens> ViewFindersOf(LensType lens)
        {
            Dictionary<LensType, Lens> vfal = new Dictionary<LensType, Lens>();
            foreach (KeyValuePair<LensType, Lens> viewfinder in _lensCollection)
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

        public Dictionary<string, string> ActiveLensesToDictionary()
        {
            Dictionary<string, string> lensesDic = new Dictionary<string, string>();
            foreach (KeyValuePair<LensType, Lens> lens in _lensCollection)
            {
                lensesDic.Add(lens.Key.ToString(), lens.Value.Extent.ToString() +";"+ lensStack.IndexOf(lens.Key));
            }
            return lensesDic;
        }





        public bool RemoveLens(LensType lens)
        {
            if (LensIsActive(lens))
            {
                _lensCollection.Remove(lens);
                lensStack.Remove(lens);
                OnLensCollectionChanged(EventArgs.Empty);
                return true;
            } else if(lens == LensType.All)
            {
                lensStack.Clear();
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

        internal static Envelope InitialExtentFrom(LensType lens)
        {
            return new LensFactory().CreateLens(lens).Extent;
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


        public void SendViewFindersChangedEventIfDirty()
        {
            if (isDirty)
            {
                OnViewFindersChanged(EventArgs.Empty);
                isDirty = false;
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



        
    }
}
