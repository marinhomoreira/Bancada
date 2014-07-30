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

        private const String
            SatelliteMode = "Satellite"
            , StreetMode = "Street"
            , PopulationMode = "Population"
            , ElectoralDistrictsMode = "ElectoralDistricts"
            , CitiesMode = "City"
            //, ZoomMode = "Zoom" // TODO: To be implemented
            , BaseMap = "BaseMap";

        static private string
              WorldStreetMap = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer" // Streets!
            , WorldShadedRelief = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Shaded_Relief/MapServer" // Just shades
            , WorldSatelliteImagery = "http://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer" // Images from satellites
            , WorldBoundariesAndPlacesLabels = "http://server.arcgisonline.com/arcgis/rest/services/Reference/World_Boundaries_and_Places/MapServer" // Just labels
            , CanadaElectoralDistricts = "http://136.159.14.25:6080/arcgis/rest/services/Politik/Boundaries/MapServer"
            , CanadaPopulationDensity = "http://maps.esri.ca/arcgis/rest/services/StatsServices/PopulationDensity/MapServer/"
            ;

         Dictionary<string, string> UrlDic = new Dictionary<string, string>() {
                {BaseMap, WorldShadedRelief},
                {SatelliteMode, WorldSatelliteImagery},
                {StreetMode, WorldStreetMap},
                {PopulationMode, CanadaPopulationDensity},
                {ElectoralDistrictsMode, CanadaElectoralDistricts},
                {CitiesMode, WorldBoundariesAndPlacesLabels},
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

         Dictionary<string, Envelope> ModeExtentDic;


         Dictionary<string, Color> VFColorDic = new Dictionary<string, Color>()
            {
                {BaseMap, Colors.Black},
                {SatelliteMode, Colors.Green},
                {StreetMode, Colors.Red},
                {PopulationMode, Colors.Black},
                {ElectoralDistrictsMode, Colors.Ivory},
                {CitiesMode, Colors.Blue},
                //{ZoomMode, null}, // TODO: To be implemented
            };

        Dictionary<string, Layer> ModeLayerDic = new Dictionary<string, Layer>()
        {
            {BaseMap, null},
            {SatelliteMode, null},
            {StreetMode, null},
            {PopulationMode, null},
            {ElectoralDistrictsMode, null},
            {CitiesMode, null},
            //{ZoomMode, null}, // TODO: To be implemented
        };

        ArcGISTiledMapServiceLayer BaseMapLayer;
        ArcGISTiledMapServiceLayer SatelliteLayer;
        ArcGISTiledMapServiceLayer StreetMapLayer;
        ArcGISDynamicMapServiceLayer PopulationLayer;
        ArcGISDynamicMapServiceLayer ElectoralDistrictsLayer;
        ArcGISDynamicMapServiceLayer CitiesLayer;


        public LensFactory()
        {
            ModeExtentDic = new Dictionary<string, Envelope>() {
                {BaseMap, CanadaEnvelope},       
                {SatelliteMode, CanadaEnvelope},
                {StreetMode, CanadaEnvelope},
                {PopulationMode, CanadaEnvelope},
                {ElectoralDistrictsMode, CanadaEnvelope},
                {CitiesMode, CanadaEnvelope}
            };

            BaseMapLayer = new ArcGISTiledMapServiceLayer { Url =  UrlDic[BaseMap]};

            SatelliteLayer = new ArcGISTiledMapServiceLayer() { Url = UrlDic[SatelliteMode] };
            
            StreetMapLayer = new ArcGISTiledMapServiceLayer { Url = UrlDic[StreetMode] };
                        
            PopulationLayer = new ArcGISDynamicMapServiceLayer() { Url = UrlDic[PopulationMode] };
            PopulationLayer.DisableClientCaching = false;
            
            ElectoralDistrictsLayer = new ArcGISDynamicMapServiceLayer { Url = UrlDic[ElectoralDistrictsMode] };
            ElectoralDistrictsLayer.DisableClientCaching = false;
            
            CitiesLayer = new ArcGISDynamicMapServiceLayer() { Url = UrlDic[CitiesMode] };
            CitiesLayer.DisableClientCaching = false;

            /*ModeLayerDic[SatelliteMode] = SatelliteLayer;
            ModeLayerDic[BaseMap] = BaseMapLayer;
            ModeLayerDic[StreetMode] = StreetMapLayer;
            ModeLayerDic[PopulationMode] = PopulationLayer;
            ModeLayerDic[ElectoralDistrictsMode] = ElectoralDistrictsLayer;
            ModeLayerDic[CitiesMode] = CitiesLayer;
             * */
            ModeLayerDic[SatelliteMode] = new ArcGISTiledMapServiceLayer() { Url = UrlDic[SatelliteMode] };
            ModeLayerDic[BaseMap] = new ArcGISTiledMapServiceLayer { Url = UrlDic[BaseMap] }; ;
            ModeLayerDic[StreetMode] = new ArcGISTiledMapServiceLayer { Url = UrlDic[StreetMode] };
            ModeLayerDic[PopulationMode] = new ArcGISDynamicMapServiceLayer() { Url = UrlDic[PopulationMode] };
            ModeLayerDic[ElectoralDistrictsMode] = new ArcGISDynamicMapServiceLayer { Url = UrlDic[ElectoralDistrictsMode] };
            ModeLayerDic[CitiesMode] = new ArcGISDynamicMapServiceLayer() { Url = UrlDic[CitiesMode] };
        }


        public LensMode CreateLens(String CurrentMode)
        {
            return new LensMode(
                ModeLayerDic[CurrentMode]
                , ModeExtentDic[CurrentMode]
                , VFColorDic[CurrentMode]
                , null
                , null);
        }


    }
}
