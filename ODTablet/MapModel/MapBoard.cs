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


    // TODO: Own file for this, please?
    public delegate void LensModifiedEventHandler(object sender, LensEventArgs e);

    public class LensEventArgs : EventArgs
    {
        public readonly LensType ModifiedLens;
        public LensEventArgs(LensType lens)
        {
            ModifiedLens = lens;
        }
    }

    class MapBoard
    {
        private Dictionary<LensType, Lens> _lensCollection;

        public event LensModifiedEventHandler LensRemoved;
        public event LensModifiedEventHandler LensAdded;
        public event LensModifiedEventHandler LensExtentUpdated;
        public event LensModifiedEventHandler LensCollectionChanged; // Not used.
        public event LensModifiedEventHandler LensStackPositionChanged;

        private List<LensType> lensStack;

        public MapBoard()
        {
            _lensCollection = new Dictionary<LensType, Lens>();
            lensStack = new List<LensType>();
            lensStack.Add(LensType.Basemap);
        }
        
        # region Update Lens
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

        public void UpdateLens(LensType lens, Envelope extent, string ZIndex)
        {
            UpdateLens(lens, extent);
            if (LensIsActive(lens))
            {
                UpdateZIndex(lens, ZIndex);
            }
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
                OnLensAdded(new LensEventArgs(lens));
            }
            else
            {
                if (!_lensCollection[lens].Extent.ToString().Equals(extent.ToString())) // TODO: Change this for something better!
                {
                    _lensCollection[lens].Extent = extent;
                    OnLensExtentUpdated(new LensEventArgs(lens));
                }
            }
            UpdateZIndex(lens, (string)null);
        }
                
        public void ClearBoardAndStackLensesAccordingToOverview(Dictionary<string, string> remoteDictionary)
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

        
        public void UpdateZIndex(LensType lens, int? zIndex)
        {
            // Basemap is always 0;
            if (lens == LensType.Basemap && !lensStack.Contains(LensType.Basemap) && lensStack.IndexOf(LensType.Basemap) != 0)
            {
                lensStack.Remove(lens);
                lensStack.Insert(0, lens);
                OnLensStackPositionChanged(new LensEventArgs(lens));
                return;
            }

            if (zIndex == null)
            {
                if (!lensStack.Contains(lens))
                {
                    // No lens present and zIndex is null, just add to the stack.
                    lensStack.Add(lens);
                    OnLensStackPositionChanged(new LensEventArgs(lens));
                }
            }
            else
            {
                if (!lensStack.Contains(lens))
                {
                    // zIndex is valid but the stack doesn't contain the given lens
                    lensStack.Insert((int)zIndex, lens);
                    OnLensStackPositionChanged(new LensEventArgs(lens));
                }
                else
                {
                    // zIndex is valid but the stack contains the given lens in a different position
                    if (lensStack.IndexOf(lens) != (int)zIndex)
                    {
                        lensStack.Remove(lens);
                        lensStack.Insert((int)zIndex, lens);
                        OnLensStackPositionChanged(new LensEventArgs(lens));
                    }
                }
            }
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
        }


        internal void BringToFront(LensType CurrentLens)
        {
            lensStack.Remove(CurrentLens);
            UpdateZIndex(CurrentLens, (int?)null);
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

        public Dictionary<LensType, Lens> AllActiveLenses()
        {
            Dictionary<LensType, Lens> vfal = new Dictionary<LensType, Lens>();
            foreach (KeyValuePair<LensType, Lens> viewfinder in _lensCollection)
            {
                if (viewfinder.Key != LensType.Basemap
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

        public static System.Windows.Media.Color GetColorOf(LensType lens)
        {
            return new LensFactory().CreateLens(lens).Color;
        }

        public static System.Windows.Media.Color GetColorOf(string lens)
        {
            try
            {
                return new LensFactory().CreateLens((StringToLensType(lens))).Color;
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid lens: " + lens + " " + e.Message);
                return System.Windows.Media.Colors.Red;
            }
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
                OnLensRemoved(new LensEventArgs(lens));
                return true;
            } else if(lens == LensType.All)
            {
                lensStack.Clear();
                _lensCollection.Clear();
                OnLensRemoved(new LensEventArgs(LensType.All));
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

        public bool LensCanBeActivated(String lens)
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
        
        
        

        protected virtual void OnLensExtentUpdated(LensEventArgs e)
        {
            if(LensExtentUpdated != null)
            {
                LensExtentUpdated(this, e);
            }
        }

        protected virtual void OnLensStackPositionChanged(LensEventArgs e)
        {
            if(LensStackPositionChanged != null)
            {
                LensStackPositionChanged(this, e);
            }
        }

        protected virtual void OnLensCollectionChanged(LensEventArgs e)
        {
            if (LensCollectionChanged != null)
            {
                LensCollectionChanged(this, e);
            }
        }

        protected virtual void OnLensRemoved(LensEventArgs e)
        {
            if (LensRemoved != null)
            {
                LensRemoved(this, e);
            }
        }

        protected virtual void OnLensAdded(LensEventArgs e)
        {
            if (LensAdded != null)
            {
                LensAdded(this, e);
            }
        }
    }
}
