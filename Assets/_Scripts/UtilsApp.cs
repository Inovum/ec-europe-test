using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class UtilsApp : MonoBehaviour {

    public InputField iu;
    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void showAddProductCanvas()
    {
     //   Canvas dataSetCanvas = GameObject.Find("DataSetCanvas").GetComponent<Canvas>();
      //  dataSetCanvas.enabled = false;
        Canvas addProductCanvas = GameObject.Find("AddProductCanvas").GetComponent<Canvas>();
        addProductCanvas.enabled = true;
        GameObject.Find("APMessage").GetComponentInChildren<Text>().text = Localization.GetTranslations().activateProduct;
        GameObject.Find("APAcceptButton").GetComponentInChildren<Text>().text = Localization.GetTranslations().ok;
        GameObject.Find("APCancelButton").GetComponentInChildren<Text>().text = Localization.GetTranslations().cancel;
    }

    public void activate()
    {
        string product = iu.text;
        Canvas addProductCanvas = GameObject.Find("AddProductCanvas").GetComponent<Canvas>();
        addProductCanvas.enabled = false;
     //   Canvas dataSetCanvas = GameObject.Find("DataSetCanvas").GetComponent<Canvas>();
     //   dataSetCanvas.enabled = true;
        AppStartupController appsc = GameObject.Find("MainController").GetComponent<AppStartupController>();
        appsc.ActivateProduct(product);
        OpenLoadedDatasetsModal opload = GameObject.Find("LoadedDatasetsButton").GetComponent<OpenLoadedDatasetsModal>();
        opload.OpenModal();
    }

    public void cancel()
    {
        clearInput();
        Canvas addProductCanvas = GameObject.Find("AddProductCanvas").GetComponent<Canvas>();
        addProductCanvas.enabled = false;
        Canvas dataSetCanvas = GameObject.Find("DataSetCanvas").GetComponent<Canvas>();
        dataSetCanvas.enabled = true;

    }

    public void clearInput()
    {
        iu.text = "";

    }
}
