using JetBrains.Annotations;
using UnityEngine;

public class SelectableObject : MonoBehaviour
{
    public GameObject SelectionMarker;
    public MeshRenderer MyMeshRenderer;
    public Material RedMat,GreenMat; 
    public bool IsSelected { get; private set; }

    public void SelectMe()
    {
        Debug.Log("Selected selectme: " + gameObject.name);
        SelectionMarker.SetActive(true);
        IsSelected = true;
    }
    
    public void DeselectMe()
    {
        Debug.Log("Selected deselectme: " + gameObject.name);
        SelectionMarker.SetActive(false);
        IsSelected = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //SelectionMarker.SetActive(false);

        //Debug.Log("GameObject: " + gameObject.name + ", SelectionMarker: " + SelectionMarker);
        if (SelectionMarker != null)
        {
            SelectionMarker.SetActive(false);
        }
        else
        {
            Debug.LogError("SelectionMarker NON assigné" + gameObject.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
