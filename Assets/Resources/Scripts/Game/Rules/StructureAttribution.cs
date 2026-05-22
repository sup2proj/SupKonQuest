using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class StructureAttribution
{
    // Variable statique pour permettre à MapGenerator d'accéder aux données modifiées
    public static MapJsonData LastModifiedJsonData { get; set; } = null;
    
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
    
    public void AssignRandomOwners(int numberOfPlayers)
    {
        if (jsonData == null || numberOfPlayers <= 0)
            return;

        // === PHASE 1 : Attribuer les startPoints (chaque joueur une fois) ===
        if (jsonData.startPoints != null && jsonData.startPoints.Count > 0)
        {
            // Créer une liste de joueurs à attribuer: [1, 2, 3, ..., numberOfPlayers]
            var playersPool = new List<int>();
            for (int p = 1; p <= numberOfPlayers; p++)
            {
                playersPool.Add(p);
            }
            
            // Ajouter des joueurs neutres (-1) pour les startPoints restants
            for (int i = numberOfPlayers; i < jsonData.startPoints.Count; i++)
            {
                playersPool.Add(-1);
            }
            
            // Mélanger la liste
            for (int i = playersPool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = playersPool[i];
                playersPool[i] = playersPool[j];
                playersPool[j] = tmp;
            }
            
            // Attribuer les owners aux startPoints
            for (int i = 0; i < jsonData.startPoints.Count; i++)
            {
                jsonData.startPoints[i].owner = playersPool[i];
            }
            
            Debug.Log($"[StructureAttribution] StartPoints attribués: {string.Join(", ", playersPool)}");
        }

        LastModifiedJsonData = jsonData;
    }
}

