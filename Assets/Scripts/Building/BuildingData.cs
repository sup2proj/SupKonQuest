using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PointData {
    public int x;
    public int y;
    public int owner;
}

[System.Serializable]
public class MapJsonData {
    public List<PointData> startPoints;
    public List<PointData> castles;
    public List<PointData> harbours;
    public List<PointData> special;
}

public class BuildingData
{
    public static MapJsonData LoadDataFromPath(string path) {
        TextAsset targetFile = Resources.Load<TextAsset>(path);
        if (targetFile != null) {
            return JsonUtility.FromJson<MapJsonData>(targetFile.text);
        }
        return null;
    }
}