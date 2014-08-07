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
        // bool removeLens ()
        private Dictionary<LensType, Lens> _lensCollection;

        public Dictionary<LensType, Lens> ActiveLenses
        {
            get { return _lensCollection; }
        }

        public MapBoard()
        {
            _lensCollection = new Dictionary<LensType, Lens>();
        }

        public bool UpdateLens(String lensName, String Extent)
        {
            OnChanged(EventArgs.Empty);
            return false;
        }

        public bool UpdateLens(Dictionary<string, string> remoteDictionary)
        {
            // To do the diff and so on....
            OnChanged(EventArgs.Empty);
            return false;
        }

        public Dictionary<string, string> AllLensesDictionary()
        {
            return null;
        }

        public Lens GetLens(LensType lens)
        {
            if (!_lensCollection.ContainsKey(lens) || _lensCollection[lens] == null)
            {
                _lensCollection[lens] = new LensFactory().CreateLens(lens);
                OnChanged(EventArgs.Empty);
            }
            return _lensCollection[lens];
        }

        public bool RemoveLens(LensType lens)
        {
            OnChanged(EventArgs.Empty);
            return false;
        }

        public event ChangedEventHandler Changed;

        protected virtual void OnChanged(EventArgs e)
        {
            if (Changed != null)
            {
                Changed(this, e);
            }
        }



        //private void ActivateMode(string mode)
        //{
        //    this.Dispatcher.Invoke((Action)(() =>
        //    {
        //        if (!ActiveLens.ContainsKey(mode))
        //        {
        //            ActiveLens.Add(mode, new LensFactory().CreateLens(mode));
        //            // There's only two lenses, if you remove the element that is below other and first (aka, has lowest Z and it's in index 0), 
        //            // when another element is added, it will be added to element 0 and the count will be 1. the resultant index will be 1.
        //            ActiveLens[mode].UIIndex = ActiveLens.Count() + 1; // TODO: How to define this? Get highest uiindex from all elements in the dictionary?
        //        }
        //    }));
        //}

        //private void UpdateLocalConfiguration(Dictionary<string, string> RemoteTableConfiguration)
        //{
        //    List<string> remoteActiveModes = new List<string>();
        //    if (RemoteTableConfiguration.Count != 0)
        //    {
        //        foreach (KeyValuePair<string, string> remoteMode in RemoteTableConfiguration)
        //        {
        //            string remoteModeName = remoteMode.Key;
        //            string remoteModeExtent = remoteMode.Value;
        //            Console.WriteLine("Received Mode " + remoteModeName + " with extent " + remoteModeExtent);
        //            if (!remoteModeName.Equals("TableActiveModes"))
        //            {
        //                remoteActiveModes.Add(remoteModeName);

        //                ActivateMode(remoteModeName);

        //                // Compare extents.
        //                // If remote is different, update local extent
        //                Envelope newEnv = StringToEnvelope(remoteModeExtent);
        //                if (ActiveLens[remoteModeName].Extent != newEnv)
        //                {
        //                    ActiveLens[remoteModeName].Extent = newEnv;
        //                }
        //                // Compare Z positions
        //                // If remote is different, update local Z position
        //                if (ActiveLens[remoteModeName].UIIndex != remoteActiveModes.IndexOf(remoteModeName))
        //                {
        //                    // Update Z position
        //                    ActiveLens[remoteModeName].UIIndex = remoteActiveModes.IndexOf(remoteModeName);
        //                }
        //            }
        //        }

        //    }

        //}

    }
}
