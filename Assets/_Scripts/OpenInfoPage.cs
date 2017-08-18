using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class OpenInfoPage : MonoBehaviour {

    private Canvas mainCanvas, infoCanvas;

    private bool mShowGUIPanel = false;
    

    // Use this for initialization
    void Awake() {
        Debug.Log("---------***-------OpenInfoPage Awake");
        
        // Obtain the info canvas reference
        mainCanvas = GameObject.Find("MainCanvas").GetComponent<Canvas>();
        infoCanvas = GameObject.Find("InfoCanvas").GetComponent<Canvas>();

        gameObject.GetComponent<Button>().onClick.AddListener(OpenModal);

        foreach (Button button in infoCanvas.GetComponentsInChildren<Button>())
        {
            if (button.name == "InfoCloseButton")
            {
                //button.GetComponentInChildren<Text>().text = Localization.GetTranslations().close;
                button.onClick.AddListener(CloseModal);
                break;
            }
        }
    }

   
	
	// Update is called once per frame
	void Update () {
	
	}

    public void OpenModal()
    {
        Debug.Log("---------***-------OpenInfoPage OpenModal");
        mShowGUIPanel = true;
        mainCanvas.enabled = !mShowGUIPanel;
        infoCanvas.enabled = mShowGUIPanel;
        ObjectTracker tracker = GameObject.Find("MainController").GetComponent<AppStartupController>().tracker;
        if (tracker != null)
        {
            tracker.Stop();
        }
    }

    public void CloseModal()
    {
        Debug.Log("---------***-------OpenInfoPage CloseModal");
        mShowGUIPanel = false;
        mainCanvas.enabled = !mShowGUIPanel;
        infoCanvas.enabled = mShowGUIPanel;
        ObjectTracker tracker = GameObject.Find("MainController").GetComponent<AppStartupController>().tracker;
        if (tracker != null)
        {
            tracker.Start();
        }
    }
}
