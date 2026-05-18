using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void NewGame()  
    { 
        SceneManager.LoadScene("LaunchingLocalNewGame"); 
    }
    
    public void Multi() 
    { 
        SceneManager.LoadScene("LoginScene"); 
    }
    
    public void Options()  
    { 
        SceneManager.LoadScene("Options");   
    }
    
    public void Quit()     
    { 
        Application.Quit();                  
    }
}