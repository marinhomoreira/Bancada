using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media;

using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;

namespace ODTablet.MapModel
{
    public class LensFactory
    {

        static private double[] CanadaExtent = { -16133214.9413533, 5045906.11392677, -5418285.97972558, 10721470.048289 };

        static private double[] CalgaryExtent = {-12698770.20, 6629884.68,-12696155.45, 6628808.53};

        static private string
              WorldStreetMap = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer" // Streets!
            , WorldShadedRelief = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Shaded_Relief/MapServer" // Just shades
            , WorldSatelliteImagery = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer" // Images from satellites
            , WorldBoundariesAndPlacesLabels = "http://server.arcgisonline.com/arcgis/rest/services/Reference/World_Boundaries_and_Places/MapServer" // Just labels
            , CanadaElectoralDistricts = "http://136.159.14.25:6080/arcgis/rest/services/Politik/Boundaries/MapServer"
            , CanadaPopulationDensity = "http://maps.esri.ca/arcgis/rest/services/StatsServices/PopulationDensity/MapServer/"
            ;

        Dictionary<LensType, string> UrlDic = new Dictionary<LensType, string>() {
                {LensType.Basemap, WorldShadedRelief},
                {LensType.Satellite, WorldSatelliteImagery},
                {LensType.Streets, WorldStreetMap},
                {LensType.Population, CanadaPopulationDensity},
                {LensType.ElectoralDistricts, CanadaElectoralDistricts},
                {LensType.Cities, WorldBoundariesAndPlacesLabels},
            };

            Envelope CanadaEnvelope = new Envelope()
            {
                XMin = CanadaExtent[0],
                YMin = CanadaExtent[1],
                XMax = CanadaExtent[2],
                YMax = CanadaExtent[3],
                //SpatialReference = new SpatialReference();//102100
            };


            Envelope CalgaryEnvelope = new Envelope()
            {
                XMin = CalgaryExtent[0],
                YMin = CalgaryExtent[1],
                XMax = CalgaryExtent[2],
                YMax = CalgaryExtent[3],
            };

            Dictionary<LensType, Envelope> ModeExtentDic;


            Dictionary<LensType, Color> VFColorDic = new Dictionary<LensType, Color>()
            {
                {LensType.Basemap, Colors.Black},
                {LensType.Satellite, Colors.Green},
                {LensType.Streets, Colors.Red},
                {LensType.Population, Colors.Black},
                {LensType.ElectoralDistricts, Colors.Ivory},
                {LensType.Cities, Colors.Blue},
            };

            Dictionary<LensType, Layer> ModeLayerDic = new Dictionary<LensType, Layer>()
        {
            {LensType.Basemap, null},
            {LensType.Satellite, null},
            {LensType.Streets, null},
            {LensType.Population, null},
            {LensType.ElectoralDistricts, null},
            {LensType.Cities, null},
        };

        ArcGISTiledMapServiceLayer BasemapLayer;
        ArcGISTiledMapServiceLayer SatelliteLayer;
        ArcGISTiledMapServiceLayer StreetMapLayer;
        ArcGISDynamicMapServiceLayer PopulationLayer;
        ArcGISDynamicMapServiceLayer ElectoralDistrictsLayer;
        ArcGISDynamicMapServiceLayer CitiesLayer;


        public LensFactory()
        {
            ModeExtentDic = new Dictionary<LensType, Envelope>() {
                {LensType.Basemap, CanadaEnvelope},       
                {LensType.Satellite, CanadaEnvelope},
                {LensType.Streets, CanadaEnvelope},
                {LensType.Population, CanadaEnvelope},
                {LensType.ElectoralDistricts, CanadaEnvelope},
                {LensType.Cities, CanadaEnvelope}
            };

            BasemapLayer = new ArcGISTiledMapServiceLayer { Url =  UrlDic[LensType.Basemap]};

            SatelliteLayer = new ArcGISTiledMapServiceLayer() { Url = UrlDic[LensType.Satellite] };
            
            StreetMapLayer = new ArcGISTiledMapServiceLayer { Url = UrlDic[LensType.Streets] };
                        
            PopulationLayer = new ArcGISDynamicMapServiceLayer() { Url = UrlDic[LensType.Population] };
            PopulationLayer.DisableClientCaching = false;
            
            ElectoralDistrictsLayer = new ArcGISDynamicMapServiceLayer { Url = UrlDic[LensType.ElectoralDistricts] };
            ElectoralDistrictsLayer.DisableClientCaching = false;
            
            CitiesLayer = new ArcGISDynamicMapServiceLayer() { Url = UrlDic[LensType.Cities] };
            CitiesLayer.DisableClientCaching = false;

            ModeLayerDic[LensType.Satellite] = new ArcGISTiledMapServiceLayer() { Url = UrlDic[LensType.Satellite] };
            ModeLayerDic[LensType.Basemap] = new ArcGISTiledMapServiceLayer { Url = UrlDic[LensType.Basemap] }; ;
            ModeLayerDic[LensType.Streets] = new ArcGISTiledMapServiceLayer { Url = UrlDic[LensType.Streets] };
            ModeLayerDic[LensType.Population] = new ArcGISDynamicMapServiceLayer() { Url = UrlDic[LensType.Population] };
            ModeLayerDic[LensType.ElectoralDistricts] = new ArcGISDynamicMapServiceLayer { Url = UrlDic[LensType.ElectoralDistricts] };
            ModeLayerDic[LensType.Cities] = new ArcGISDynamicMapServiceLayer() { Url = UrlDic[LensType.Cities] };
        }


        public Lens CreateLens(LensType CurrentMode)
        {
            return new Lens(
                ModeLayerDic[CurrentMode]
                , ModeExtentDic[CurrentMode]
                , VFColorDic[CurrentMode]
                );
        }

        public Lens CreateLens(LensType CurrentMode, Envelope extent)
        {
            return new Lens(
                ModeLayerDic[CurrentMode]
                , extent
                , VFColorDic[CurrentMode]
                );
        }


    }
}
