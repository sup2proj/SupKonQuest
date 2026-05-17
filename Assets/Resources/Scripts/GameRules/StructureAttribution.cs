using System.Collections.Generic;
using Random = UnityEngine.Random;

public class StructureAttribution
{
    private MapJsonData jsonData;
    
    private int totalPlayers;

    public List<Assignment> campAssignments = new List<Assignment>();
    
    public void Setup(string folderName)
    {
        string path = "Maps/" + folderName + "/";
        jsonData = StructureInstance.LoadDataFromPath(path + "MapData");
        totalPlayers=jsonData.startPoints.Count;
        for (int i = 0; i < totalPlayers; i++)
        {
            this.AssignPlayer("AI"+(i+1),i+1,"AI");
        }
    }

    void AssignPlayer(string playerName, int playerNumber, string playerType)
    {
        Assignment newAssignment = new Assignment
        {
            playerName = playerName,
            playerType = playerType,
            playerNumber = playerNumber,
            startPointX = jsonData.startPoints[playerNumber-1].x,
            startPointY = jsonData.startPoints[playerNumber-1].y
        };
        campAssignments.Add(newAssignment);

    }
    public void SetCampAssignment(string[] playerList)
    {
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < totalPlayers; i++)
        {
            availableIndices.Add(i+1);
        }
        
        foreach (string playerName in playerList)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            int assignedNumber = availableIndices[randomIndex];
            availableIndices.RemoveAt(randomIndex);
            campAssignments[assignedNumber-1].playerName = playerName;
            campAssignments[assignedNumber-1].playerType = "Human";
            
        }
    }
    
    public (int, int) GetPlayerCameraStartPosition(string playerName)
    {
        foreach (Assignment assignment in campAssignments)
        {
            if (assignment.playerName == playerName)
            {
                return (assignment.startPointX, assignment.startPointY);
            }
        }
        return (0, 0); 
    }    
}
