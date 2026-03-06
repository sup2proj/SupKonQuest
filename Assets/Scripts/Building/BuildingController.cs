
using UnityEngine;

namespace Building
{
    public class BuildingController: MonoBehaviour
    {
        private int _coordX{get;set;}
        private int _coordY{get;set;}
        private int _ownerID { get; set; }
        private int _incomeValue { get; set; }

        public void Init(int x, int y, int ownerID, int income)
        {
            _coordX = x;
            _coordY = y;
            _ownerID = ownerID;
            _incomeValue = income;
        }
        private void OnMouseDown()
        {
            Debug.Log($"Bâtiment {_coordX}-{_coordY} | Revenu : {_incomeValue} | Propriétaire : {_ownerID}");
        }
        
    }
}