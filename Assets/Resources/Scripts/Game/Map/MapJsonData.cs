using System.Collections.Generic;
    
    [System.Serializable]
    public class MapJsonData
    {
        public List<PointData> startPoints;
        public List<PointData> castles;
        public List<PointData> harbours;
        public List<PointData> special;
    }