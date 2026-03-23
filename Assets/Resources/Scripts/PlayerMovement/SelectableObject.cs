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
        Debug.Log("Selected : " + gameObject.name);
        SelectionMarker.SetActive(true);
        IsSelected = true;
    }
    
    public void DeselectMe()
    {
        SelectionMarker.SetActive(false);
        IsSelected = false;
    }

    public void SetColorRed()
    {
        MyMeshRenderer.material = RedMat;
    }

    public void SetColorGreen()
    {
        MyMeshRenderer.material = GreenMat;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //SelectionMarker.SetActive(false);

        Debug.Log("GameObject: " + gameObject.name + ", SelectionMarker: " + SelectionMarker);
        if (SelectionMarker != null)
        {
            SelectionMarker.SetActive(false);
        }
        else
        {
            Debug.LogError("SelectionMarker NON assigné sur sdddddddddddddddddddd " + gameObject.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
