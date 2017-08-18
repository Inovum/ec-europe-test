using UnityEngine;
using System.Collections;
using Vuforia;
using UnityEngine.UI;
using System;
using System.IO;

public class ProductButtonController : MonoBehaviour {

    public GameObject prefabObjectRowSelector;
    public GameObject selector;
    public GameObject loadedDataSetController;
    public string vuforiaResourceName;

    //public GameObject prefabObjectRowSelector = null;
    private Canvas productDetailsCanvas;
    private Canvas confirmCanvas;
    private Canvas datasetCanvas;

    private GameObject productDetailsDataGrid;

    void Awake()
    {
        // Obtain the info canvas reference
        datasetCanvas = GameObject.Find("DataSetCanvas").GetComponent<Canvas>();

        var productMain = GameObject.Find("ProductDetailCanvas");
        productDetailsCanvas = productMain.GetComponent<Canvas>();

        var productBackButton = GameObject.Find("ProductBackButton");
        var productRemoveButton = GameObject.Find("ProductRemoveButton");

        productBackButton.GetComponentInChildren<Button>().onClick.AddListener(CloseModal);
        productRemoveButton.GetComponentInChildren<Button>().onClick.AddListener(OpenConfirmRemoveProductModal);

        productDetailsDataGrid = GameObject.Find("ProductDetailsDataGrid");
    }

    //private void ButtonPressed()
    //{
    //    isShow = !isShow; // Change the state to show the modal or not.
    //    if (isShow)
    //        OpenModal();
    //    else
    //        CloseModal();
    //}

    // Update is called once per frame
    void Update () {
	}

    //public void OpenModal()
    //{
    //    ShowModalCanvas(true);

    //    foreach (GameObject dataset in GameObject.FindGameObjectsWithTag("DataSetController"))
    //    {
    //        foreach (Transform childObject in dataset.transform)
    //        {
    //            string name = childObject.name;
    //            prefabObjectRowSelector.name = "ProductDetailRow";

    //            GameObject row = Instantiate(prefabObjectRowSelector);
    //            row.GetComponentInChildren<Text>().text = childObject.name;

    //            row.transform.SetParent(productDetailsDataGrid.transform);
    //        }
    //    }
    //}

    public void CloseModal()
    {
        ShowModalCanvas(false);
    }

    private void ShowModalCanvas(bool show)
    {
        productDetailsCanvas.enabled = show;
        datasetCanvas.enabled = !show;
        if (!show)
        {
            // Modify the scale of  the rows in order to avoid the re-scaling of the parent!
            var dataSetRows = datasetCanvas.GetComponentsInChildren<SelectProductEventHandler>();
            foreach (var dataSetRow in dataSetRows)
                dataSetRow.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }

    }

    void OpenConfirmRemoveProductModal()
    {
        Debug.Log("---------***-------OpenConfirmModal OpenModal");
        confirmCanvas = GameObject.Find("ConfirmCanvas").GetComponent<Canvas>();
        confirmCanvas.enabled = true;

        GameObject.Find("MessagePanel").GetComponentInChildren<Text>().text = Localization.GetTranslations().disableProductMessage;
        GameObject.Find("ProductInfoPanel").GetComponentInChildren<Text>().text = vuforiaResourceName;
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
                    CloseModal();

                    //text.text = loadedDataSetController.GetComponent<DataSetLoader>().enabled ? "DataSet is now enabled" : "DataSet is now disabled";
                });
            }
        }
    }


}
