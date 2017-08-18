using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class OpenLoadedDatasetsModal : MonoBehaviour {

    public GameObject prefabDataSetSelector = null;

    private bool mShowGUIPanel = false;
    private float popupWidth;
    private float popupHeight;
    private Rect popupRect;

    private Canvas mainCanvas, dataSetCanvas, confirmCanvas;
    private Image grid;

    // Use this for initialization
    void Awake()
    {
        Debug.Log("---------***-------OpenLoadedDatasetsModal Awake");
        popupWidth = 300.0f;
        popupHeight = 250.0f;

        // Obtain the main canvas reference
        mainCanvas = GameObject.Find("MainCanvas").GetComponent<Canvas>();
        dataSetCanvas = GameObject.Find("DataSetCanvas").GetComponent<Canvas>();
        grid = GameObject.Find("LoadedDataSetGrid").GetComponent<Image>();
        
        gameObject.GetComponent<Button>().onClick.AddListener(OpenModal);

        foreach (Button button in dataSetCanvas.GetComponentsInChildren<Button>())
        {
            if (button.name == "DataSetCloseButton")
            {
                //button.GetComponentInChildren<Text>().text = Localization.GetTranslations().close;
                button.onClick.AddListener(CloseModal);
                break;
            }
        }

        foreach (Text text in dataSetCanvas.GetComponentsInChildren<Text>())
        {
            if (text.name == "EnabledProductsText")
            {
                text.text = Localization.GetTranslations().enabledProducts;
                break;
            }
        }

        /*
        prefabDataSetSelector.name = "DynamicDataSetSelector";

        //prefabDataSetController.GetComponent<DataSetLoader>().filename = dataSetXmlFile;
        //GameObject selector = Instantiate(prefabDataSetSelector, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity) as GameObject;
        GameObject selector = Instantiate(prefabDataSetSelector) as GameObject;
        selector.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        //selector.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        selector.transform.SetParent(grid.transform);
        selector.GetComponentInChildren<Text>().text = "hello";*/
    }

    // Use this for initialization
    void Start () {

    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void OpenModal()
    {
        Debug.Log("---------***-------OpenLoadedDatasetsModal OpenModal");
        popupWidth = Screen.width * 8 / 10;
        popupHeight = Screen.height * 8 / 10;
        popupRect = new Rect((Screen.width / 10), (Screen.height / 10), popupWidth, popupHeight);

        mShowGUIPanel = true;
        mainCanvas.enabled = !mShowGUIPanel;
        dataSetCanvas.enabled = mShowGUIPanel;

        //grid.transform.DetachChildren();
        Debug.Log("---------***-------OpenLoadedDatasetsModal OpenModal grid: " + grid.name);

        // Get the DynamicDataSetSelector childs that will be removed later
        List<Transform> destroyChilds = new List<Transform>();
        foreach (Transform child in grid.transform.GetComponentsInChildren<Transform>())
        {
            if (child.name.StartsWith("DynamicDataSetSelectorNew"))
                // Add to destroyChilds array to be destroyed later
                destroyChilds.Add(child);
        }

        // Remove existing DynamicDataSetSelectors and their childrens
        foreach (Transform child in destroyChilds)
        {
            Destroy(child.gameObject);
        }

        // Add loaded DataSetControllers
        foreach (GameObject loadedDataSetController in GameObject.FindGameObjectsWithTag("DataSetController"))
        {
            prefabDataSetSelector.name = "DynamicDataSetSelectorNew";
            Debug.Log("DynamicDataSetSelector Scale: " + prefabDataSetSelector.transform.localScale);

            //prefabDataSetController.GetComponent<DataSetLoader>().filename = dataSetXmlFile;
            //GameObject selector = Instantiate(prefabDataSetSelector, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity) as GameObject;
            prefabDataSetSelector.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);

            GameObject selector = Instantiate(prefabDataSetSelector) as GameObject;

            //selector.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
            string info = loadedDataSetController.GetComponent<DataSetLoader>().enabled ? "" : " is disabled";

            selector.GetComponentInChildren<Text>().text = loadedDataSetController.name + info;
            selector.transform.SetParent(grid.transform);

            // Scale it to 1.0f to prevent parent canvas scale
            selector.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);

            string vuforiaResourceName = loadedDataSetController.GetComponent<DataSetLoader>().vuforiaResourceName;

            SelectProductEventHandler clickHandler = selector.GetComponentInChildren<SelectProductEventHandler>();
            //if (clickHandler != null)
            //{
            clickHandler.vuforiaResourceName = vuforiaResourceName;
            clickHandler.loadedDataSetController = loadedDataSetController;
            clickHandler.selector = selector;
            //}
        }
    }

    void CloseModal()
    {
        Debug.Log("---------***-------OpenLoadedDatasetsModal CloseModal");
        mShowGUIPanel = false;
        mainCanvas.enabled = !mShowGUIPanel;
        dataSetCanvas.enabled = mShowGUIPanel;
    }

    public void AddProduct()
    {
        Debug.Log("---------***-------OpenLoadedDatasetsModal Addproduct");
        mShowGUIPanel = false;
        mainCanvas.enabled = !mShowGUIPanel;
        dataSetCanvas.enabled = mShowGUIPanel;
    }

    void ToggleLoadedDataSet(string vuforiaResourceName, GameObject selector)
    {


        foreach (GameObject loadedDataSetController in GameObject.FindGameObjectsWithTag("DataSetController"))
        {
            Debug.Log("---------***-------OpenLoadedDatasetsModal ToggleLoadedDataSet");
            if (vuforiaResourceName == loadedDataSetController.GetComponent<DataSetLoader>().vuforiaResourceName)
            {
                /*
                loadedDataSetController.GetComponent<DataSetLoader>().Disable();
                Destroy(loadedDataSetController);
                
                text.text = loadedDataSetController.GetComponent<DataSetLoader>().enabled ? "DataSet is now enabled" : "DataSet is now disabled";
                */

                OpenConfirmModal(loadedDataSetController, selector);
                
                //gameObject.GetComponent<Button>().onClick.AddListener(OpenConfirmModal);

                break;
            }
        }
    }

    void OpenConfirmModal(GameObject loadedDataSetController, GameObject selector)
    {
        Debug.Log("---------***-------OpenConfirmModal OpenModal");
        confirmCanvas = GameObject.Find("ConfirmCanvas").GetComponent<Canvas>();
        confirmCanvas.enabled = true;
        
        GameObject.Find("MessagePanel").GetComponentInChildren<Text>().text = Localization.GetTranslations().disableProductMessage;
        GameObject.Find("ProductInfoPanel").GetComponentInChildren<Text>().text = loadedDataSetController.GetComponent<DataSetLoader>().vuforiaResourceName;
        GameObject.Find("CancelButton").GetComponentInChildren<Text>().text = Localization.GetTranslations().cancel;
        GameObject.Find("AcceptButton").GetComponentInChildren<Text>().text = Localization.GetTranslations().ok;

        // Obtain the imagePopupButton reference and disable it at startup
        Button[] buttons = confirmCanvas.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (button.name == "CancelButton")
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    Debug.Log("---------***-------OpenConfirmModal OpenModal CancelButton");
                    confirmCanvas.enabled = false;
                });
            }
            else if (button.name == "AcceptButton")
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    Debug.Log("---------***-------OpenConfirmModal OpenModal AcceptButton");
                    confirmCanvas.enabled = false;
                    // Delete all resources linked to this product
                    int nresources = loadedDataSetController.GetComponent<DataSetLoader>().localResourcesPath.Count;
                    string cadena = "";
                    foreach (string recurso in loadedDataSetController.GetComponent<DataSetLoader>().localResourcesPath)
                    {                        
                        File.Delete(recurso);
                    }
                    //ShowToast.Show("RESOURCES: " + cadena);

                    // TODO: Disable product
                    loadedDataSetController.GetComponent<DataSetLoader>().Disable();
                    Destroy(loadedDataSetController);
                    Destroy(selector);
                    
                    
                    //text.text = loadedDataSetController.GetComponent<DataSetLoader>().enabled ? "DataSet is now enabled" : "DataSet is now disabled";
                });
            }
        }
    }
}
