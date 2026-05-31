using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class StructureAttribution
{
    public static MapJsonData LastModifiedJsonData { get; set; } = null;
    
    private MapJsonData jsonData;
    
    private int totalPlayers;

    public List<Assignment> campAssignments = new List<Assignment>();
    
    /// <summary>
    /// Charge les données de la carte depuis le dossier spécifié et prépare les assignations de joueurs.
    /// </summary>
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

    /// <summary>
    /// Crée et ajoute une assignation de joueur (nom, numéro, type) en utilisant les startPoints JSON.
    /// </summary>
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
    
    /// <summary>
    /// Assigne aléatoirement les joueurs humains aux camps disponibles en remplaçant des AIs.
    /// </summary>
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
    
    /// <summary>
    /// Retourne la position de départ de la caméra (x,y) pour le joueur nommé, ou (0,0) si non trouvé.
    /// </summary>
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
    
    /// <summary>
    /// Attribue aléatoirement des propriétaires aux points de départ en veillant à répartir
    /// les joueurs et ajouter des neutres si nécessaire. Met à jour LastModifiedJsonData.
    /// </summary>
    public void AssignRandomOwners(int numberOfPlayers)
    {
        if (jsonData == null || numberOfPlayers <= 0)
            return;

        if (jsonData.startPoints != null && jsonData.startPoints.Count > 0)
        {
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
            
            for (int i = playersPool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = playersPool[i];
                playersPool[i] = playersPool[j];
                playersPool[j] = tmp;
            }
            
            for (int i = 0; i < jsonData.startPoints.Count; i++)
            {
                jsonData.startPoints[i].owner = playersPool[i];
            }
            
            Debug.Log($"[StructureAttribution] StartPoints attribués: {string.Join(", ", playersPool)}");
        }

        LastModifiedJsonData = jsonData;
    }
}

