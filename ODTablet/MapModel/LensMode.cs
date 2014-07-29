using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;


namespace ODTablet.MapModel
{
    public class LensMode
    {

        private Envelope _extent;

        public Layer MapLayer 
        { 
            get; set;
        }

        public LayerCollection MapLayerCollection
        {
            get { return new LayerCollection() { MapLayer }; }
        }

        public Envelope Extent
        {
            get { return _extent; }
            set { this._extent = value; }
        }
        public System.Windows.Media.Color Color { get; set; }
        public List<LensMode> BackgroundLens { get; set; }
        public List<LensMode> ForegroundLens { get; set; }

        public LensMode(Layer l, Envelope extent, System.Windows.Media.Color color, List<LensMode> background, List<LensMode> foreground)
        {
            this.MapLayer = l;
            this._extent = extent;
            this.Color = color;
            this.BackgroundLens = background;
            this.ForegroundLens = foreground;
        }

    }
}
