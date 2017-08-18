using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class AppStartupController : MonoBehaviour
{

    public const string APP_ID = "ESP";
    //public const string APP_LANGUAGE = "ec-europe-app-prueba";
    //public const string SERVER_DOMAIN = "http://ec-europe.com:8081";
    public const string SERVER_DOMAIN = "http://192.168.1.38:8080";
    //public const string SERVER_DOMAIN = "http://localhost:8080";
    public const string SERVER_URI_GETAPP = "/get-app?identApp=" + APP_ID;
    public const string SERVER_URI_GETAPPXML = "/get-app/xml?vuforiaAppDatabaseId=";
    public const string SERVER_URI_GETAPPDAT = "/get-app/dat?vuforiaAppDatabaseId=";
    public const string SERVER_URI_GETPRODUCTXML = "/get-product/xml?vuforiaProductDatabaseId=";
    public const string SERVER_URI_GETPRODUCTDAT = "/get-product/dat?vuforiaProductDatabaseId=";

    private DataSet dataSet = null;

    private Canvas mainCanvas, dataSetCanvas, confirmCanvas, richConfirmCanvas, warningCanvas, imageCanvas, infoCanvas, loadingCanvas;
    private Button activateProductPopupButton = null;

    private float imagePopupWidth;
    private float imagePopupHeight;

    public ObjectTracker tracker;
    public GameObject prefabDataSetController = null;

    [NonSerialized]
    public AppResponseJson AppResponseDataset;

    void Awake()
    {
        // Create version files directory if needed
        if (!Directory.Exists(Application.persistentDataPath + Globals.VERSION_FILES_URI))
            Directory.CreateDirectory(Application.persistentDataPath + Globals.VERSION_FILES_URI);

        // Cache files directory is deleted and recreated every time the app starts
        if (Directory.Exists(Application.persistentDataPath + Globals.CACHE_FILES_URI))
            Directory.Delete(Application.persistentDataPath + Globals.CACHE_FILES_URI, true);

        Directory.CreateDirectory(Application.persistentDataPath + Globals.CACHE_FILES_URI);
        // END Cache files directory is deleted and recreated every time the app starts
    }

    // Use this for initialization
    void Start()
    {
#if UNITY_EDITOR
        //  BE CAREFUL THIS IS ONLY FOR TEST WITH EDITOR RUN. IF NOT, COMMENT THIS LINE
       // PlayerPrefs.DeleteAll();
#endif

        // Obtain Canvas references
        mainCanvas = GameObject.Find("MainCanvas").GetComponent<Canvas>();
        mainCanvas.enabled = true;
        dataSetCanvas = GameObject.Find("DataSetCanvas").GetComponent<Canvas>();
        dataSetCanvas.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
        dataSetCanvas.enabled = false;
        confirmCanvas = GameObject.Find("ConfirmCanvas").GetComponent<Canvas>();
        confirmCanvas.enabled = false;
        richConfirmCanvas = GameObject.Find("RichConfirmCanvas").GetComponent<Canvas>();
        richConfirmCanvas.enabled = false;
        warningCanvas = GameObject.Find("WarningCanvas").GetComponent<Canvas>();
        warningCanvas.enabled = false;
        imageCanvas = GameObject.Find("ImageCanvas").GetComponent<Canvas>();
        imageCanvas.enabled = false;
        infoCanvas = GameObject.Find("InfoCanvas").GetComponent<Canvas>();
        infoCanvas.enabled = false;
        loadingCanvas = GameObject.Find("LoadingCanvas").GetComponent<Canvas>();
        loadingCanvas.enabled = false;

        LocaleTranslationJson localeJson = Localization.GetTranslations();
        Debug.Log("-----------------localeJson takePhoto: " + localeJson.takePhoto);

        // Obtain the imagePopupButton reference and disable it at startup
        Button[] buttons = mainCanvas.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (button.name == "ImagePopupButton" || button.name == "VideoPopupButton")
            {
                button.GetComponent<UnityEngine.UI.Image>().enabled = false;
                button.GetComponent<Button>().enabled = false;
            }
        }
        UnityEngine.UI.Image[] images = mainCanvas.GetComponentsInChildren<UnityEngine.UI.Image>();
        foreach (UnityEngine.UI.Image image in images)
        {
            if (image.name == "VideoThumbnail")
            {
                image.enabled = false;
            }
        }

        // Show message establishing connection to server
        ShowToast.Show(Localization.GetTranslations().message.establishingConnection);

        //string url = "https://s3.eu-central-1.amazonaws.com/ec-europe/json/json-resp-vuforia-app.json";
        //string url = "https://s3.eu-central-1.amazonaws.com/ec-europe/json/json-resp-vuforia-app-2videos.json";
        //string url = "http://localhost/json-resp-vuforia-app-dev.json";
        WWW www = new WWW(SERVER_DOMAIN + SERVER_URI_GETAPP);
        StartCoroutine(GetAppDataJson(www));

    }

    // Update is called once per frame
    void Update()
    {
        // Update spinner
        ShowSpinner.Update();

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public IEnumerator GetAppDataJson(WWW www)
    {
        yield return www;

        // check for errors
        if (www.error != null)
        {
            Debug.Log("WWW Error: " + www.error);
            ServerResponseShowError(Localization.GetTranslations().error.connectionFailed);

            yield break;
        }
        string appResponseJson = www.text;
        Debug.Log("GetAppDataJson response: " + appResponseJson);

        // Init products
        StartCoroutine(initProducts(appResponseJson));
    }

    private IEnumerator initProducts(string appResponseJson)
    {
        Debug.Log("initProducts");

        // Check device version against downloaded json
        int deviceAppVersion = PlayerPrefs.GetInt(Globals.APP_VERSION_KEY);
        AppResponseJson json = new AppResponseJson();
        try
        {
            json = JsonHelper.getJson<AppResponseJson>(appResponseJson);
        }
        catch (Exception e)
        {
            Debug.Log("Invalid server response------------------------------" + e.Message);
            ServerResponseShowError(Localization.GetTranslations().error.invalidResponse);

            yield break;
        }
        int downloadedAppVersion = json.vuforiaAppDatabaseVersion;

        if (deviceAppVersion == downloadedAppVersion)
        {
            Debug.Log("Versions match " + deviceAppVersion);

            // vuforiaAppDatabaseVersion is the same, but products version might have changed
            PlayerPrefs.SetString(Globals.APP_JSONDATA_KEY, appResponseJson);
            LoadAllProductsDataSet(json);

        }
        else
        {
            ShowSpinner.Show();
            Debug.Log("Versions do not match " + deviceAppVersion + " - " + downloadedAppVersion);

            // Delete version files directory and recreate it
            if (Directory.Exists(Application.persistentDataPath + Globals.VERSION_FILES_URI))
                Directory.Delete(Application.persistentDataPath + Globals.VERSION_FILES_URI, true);

            System.Threading.Thread.Sleep(500);
            Directory.CreateDirectory(Application.persistentDataPath + Globals.VERSION_FILES_URI);
            System.Threading.Thread.Sleep(500);
            // END Delete version files directory and recreate it

            // Retrieve XML and DAT files
            string xmlURL = SERVER_DOMAIN + SERVER_URI_GETAPPXML + json.vuforiaAppDatabaseId;
            string datURL = SERVER_DOMAIN + SERVER_URI_GETAPPDAT + json.vuforiaAppDatabaseId;
            var xmlDevicePath = Application.persistentDataPath + Globals.VERSION_FILES_URI + "/app_" + json.vuforiaAppDatabaseId + ".xml";
            // XML and DAT names MUST match, force it using vuforiaAppDatabaseXmlId
            var datDevicePath = Application.persistentDataPath + Globals.VERSION_FILES_URI + "/app_" + json.vuforiaAppDatabaseId + ".dat";

            using (WWW wwwXml = new WWW(xmlURL))
            {
                yield return wwwXml;
                if (wwwXml.error != null)
                {
                    Debug.Log("wwwXml download had an error:" + wwwXml.error);
                    ServerResponseShowError(Localization.GetTranslations().error.connectionFailed);
                    yield break;
                    //throw new Exception("WWW download had an error:" + wwwXml.error);
                }
                Debug.Log("Save vuforia file from: " + xmlURL);
                //Debug.Log("NonCachingLoadExample - bytes downloaded: " + www.bytesDownloaded);   

                // Save data into user's folder
                System.IO.File.WriteAllBytes(xmlDevicePath, wwwXml.bytes);
                Debug.Log("Save vuforia file - Asset was saved at: " + xmlDevicePath);

                using (WWW wwwDat = new WWW(datURL))
                {
                    yield return wwwDat;
                    if (wwwDat.error != null)
                    {
                        ServerResponseShowError(Localization.GetTranslations().error.connectionFailed);
                        yield break;
                        //throw new Exception("WWW download had an error:" + wwwDat.error);
                    }
                    Debug.Log("Save vuforia file from: " + datURL);
                    //Debug.Log("NonCachingLoadExample - bytes downloaded: " + www.bytesDownloaded);   

                    // Save data into user's folder
                    System.IO.File.WriteAllBytes(datDevicePath, wwwDat.bytes);
                    Debug.Log("Save vuforia file - Asset was saved: " + datDevicePath);
                    //showInfoPanel("Saved: " + xmlDevicePath);

                    // Update device versions
                    PlayerPrefs.SetString(Globals.APP_XMLDEVICEPATH_KEY, xmlDevicePath);
                    PlayerPrefs.SetString(Globals.APP_DATDEVICEPATH_KEY, datDevicePath);
                    PlayerPrefs.SetInt(Globals.APP_VERSION_KEY, downloadedAppVersion);
                    PlayerPrefs.SetString(Globals.APP_JSONDATA_KEY, appResponseJson);
                    Debug.Log("JSON: " + appResponseJson);

                    // Load dataset from saved files
                    LoadAllProductsDataSet(json);
                } // memory is freed from the web stream (www.Dispose() gets called implicitly)

                //showInfoPanel("Saved: " + xmlDevicePath);
            } // 

            ShowSpinner.Hide();
        }


    }

    public void LoadAllProductsDataSet(AppResponseJson json)
    {
        Debug.Log("LoadAllProductsDataSet");
        string xmldevicePath = PlayerPrefs.GetString(Globals.APP_XMLDEVICEPATH_KEY);

        tracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        dataSet = tracker.CreateDataSet();
        //showInfoPanel("DataSetLoader before dataSet.Load");
        if (dataSet.Load(xmldevicePath, VuforiaUnity.StorageType.STORAGE_ABSOLUTE))
        {
            Debug.Log("Dataset loaded! path: " + xmldevicePath);

            tracker.Stop();
            if (!tracker.ActivateDataSet(dataSet))
            {
                Debug.Log("<color=yellow>Failed to Activate DataSet: " + dataSet + "</color>");
            }
            if (!tracker.Start())
            {
                Debug.Log("<color=yellow>Tracker Failed to Start.</color>");
            }

            Debug.Log("CustomImageTargetBehaviour - GetActiveDataSets: " + tracker.GetActiveDataSets());

            int counter = 0;
            IEnumerable<TrackableBehaviour> tbs = TrackerManager.Instance.GetStateManager().GetTrackableBehaviours();
            foreach (TrackableBehaviour tb in tbs)
            {
                Debug.Log("Dataset TrackableBehaviour foreach");
                if (tb != null && tb.name == "New Game Object")
                {
                    bool foundProductionJson = false;
                    foreach (AppResponseJson.ProductLineJson productJson in json.products)
                    {
                        if (tb.TrackableName == productJson.vuforiaProductTrackableName)
                        {
                            foundProductionJson = true;
                            break;
                        }
                    }

                    if (!foundProductionJson)
                    {
                        // disable product
                        PlayerPrefs.SetString(Globals.getProductIsEnabledKey(tb.TrackableName), "false");
                        // change version to zero
                        PlayerPrefs.SetString(Globals.getProductVersionKey(tb.TrackableName), "0");
                    }

                    if (foundProductionJson)
                    {
                        // change generic name to include trackable name
                        tb.gameObject.name = ++counter + ":ProductImageTarget-" + tb.TrackableName;

                        // add additional script components for trackable
                        tb.gameObject.AddComponent<DefaultTrackableEventHandler>();
                        tb.gameObject.AddComponent<TurnOffBehaviour>();

                        if ("true".Equals(PlayerPrefs.GetString(Globals.getProductIsEnabledKey(tb.TrackableName))))
                        {
                            // if product is enabled then load it
                            prefabDataSetController.GetComponent<DataSetLoader>().vuforiaResourceName = tb.TrackableName;

                            GameObject dataSetController = Instantiate(prefabDataSetController) as GameObject;
                            dataSetController.name = tb.TrackableName;
                        }

                        string productHeader = "";
                        string productText = "";
                        foreach (AppResponseJson.ProductLineJson productJson in json.products)
                        {
                            if (tb.TrackableName == productJson.vuforiaProductTrackableName)
                            {
                                productHeader = productJson.activateProductHeader;
                                productText = productJson.activateProductText;
                                // vuforiaProductDatabaseid
                                break;
                            }
                        }

                        // Attach trackable to product
                        tb.RegisterTrackableEventHandler(new OpenModalEnableProductTrackable(this, richConfirmCanvas, dataSetCanvas, tb.TrackableName, productHeader, productText));
                    }
                }
            }
        }
    }

    public void ActivateProduct(string trackableName)
    {
        // Check if already exists
        GameObject[] loadedDataSetControllers = GameObject.FindGameObjectsWithTag("DataSetController");
        bool found = false;
        foreach (GameObject loadedDataSetController in loadedDataSetControllers)
        {
            if (loadedDataSetController.GetComponent<DataSetLoader>().vuforiaResourceName == trackableName)
            {
                found = true;
                break;
            }
        }

        // Load dataSet if not found
        if (!found)
        {
            prefabDataSetController.GetComponent<DataSetLoader>().vuforiaResourceName = trackableName;

            //GameObject dataSetController = Instantiate(prefabDataSetController, new Vector3(1.0f, 1.0f, 1.0f), Quaternion.identity) as GameObject;
            GameObject dataSetController = Instantiate(prefabDataSetController) as GameObject;
            dataSetController.name = trackableName;
        }
    }



    public static void OpenWarningModal(string mainMessage, string acceptButtonText, UnityEngine.Events.UnityAction acceptListener, string cancelButtonText, UnityEngine.Events.UnityAction cancelListener)
    {
        Canvas warningCanvas = GameObject.Find("WarningCanvas").GetComponent<Canvas>();
        warningCanvas.enabled = true;

        GameObject.Find("WarningMessagePanel").GetComponentInChildren<Text>().text = mainMessage;//Localization.GetTranslations().disableProductMessage;
        GameObject.Find("WarningCancelButton").GetComponentInChildren<Text>().text = cancelButtonText; //Localization.GetTranslations().cancel;
        GameObject.Find("WarningAcceptButton").GetComponentInChildren<Text>().text = acceptButtonText; //Localization.GetTranslations().ok;

        // Add button listeners
        Button[] buttons = warningCanvas.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (button.name == "WarningCancelButton")
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(cancelListener);
            }
            else if (button.name == "WarningAcceptButton")
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(acceptListener);
            }
        }
    }

    void ServerResponseShowError(string message)
    {
        OpenWarningModal(message, Localization.GetTranslations().retry, () =>
        {
            warningCanvas.enabled = false;

            WWW www2 = new WWW(SERVER_DOMAIN + SERVER_URI_GETAPP);
            StartCoroutine(GetAppDataJson(www2));
        }, Localization.GetTranslations().workOffline, () =>
        {
            warningCanvas.enabled = false;

            StartCoroutine(initProducts(PlayerPrefs.GetString(Globals.APP_JSONDATA_KEY)));
        });
    }

    private IEnumerator createDirectory()
    {
        yield return new WaitForSeconds(2);
    }
}

public static class Globals
{
    public const String VERSION_FILES_URI = "/versionfiles";
    public const String CACHE_FILES_URI = "/cachefiles";

    public const String APP_VERSION_KEY = "app-version-key";
    public const String APP_JSONDATA_KEY = "app-jsondata-key";
    public const String APP_XMLDEVICEPATH_KEY = "app-xmldevicepath-key";
    public const String APP_DATDEVICEPATH_KEY = "app-datdevicepath-key";

    private const String PRODUCT_ISENABLED_KEY_START = "product-isenabled-";
    public static String getProductIsEnabledKey(string name)
    {
        return PRODUCT_ISENABLED_KEY_START + name;
    }
    public const String PRODUCT_VERSION_KEY = "product-version-key-";
    public static String getProductVersionKey(string name)
    {
        return PRODUCT_VERSION_KEY + name;
    }
}


public class OpenModalEnableProductTrackable : ITrackableEventHandler
{
    private AppStartupController parent;
    private Canvas richConfirmCanvas;
    private Canvas dataSetCanvas;
    private string trackableName;
    private string productHeader;
    private string productText;

    public OpenModalEnableProductTrackable(AppStartupController parent, Canvas richConfirmCanvas, Canvas dataSetCanvas, string trackableName, string productHeader, string productText)
    {
        this.parent = parent;
        this.richConfirmCanvas = richConfirmCanvas;
        this.dataSetCanvas = dataSetCanvas;
        this.trackableName = trackableName;
        if (productHeader == null || productHeader == "")
            this.productHeader = Localization.GetTranslations().enableProductMessage;
        else
            this.productHeader = productHeader;
        if (productText == null || productText == "")
            this.productText = trackableName;
        else
            this.productText = productText;
    }

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        var detected = (newStatus == TrackableBehaviour.Status.DETECTED || newStatus == TrackableBehaviour.Status.TRACKED);
        var isTrackableEnabled = "true".Equals(PlayerPrefs.GetString(Globals.getProductIsEnabledKey(trackableName)));
        var isDataSetCanvasEnabled = dataSetCanvas.enabled;

        if (detected && !isTrackableEnabled && !isDataSetCanvasEnabled)
        {
            // Set texts and translations
            // Using Unity Rich Text: https://docs.unity3d.com/Manual/StyledText.html
            Text richMessagePanel = GameObject.Find("RichMessagePanel").GetComponentInChildren<Text>();
            richMessagePanel.supportRichText = true;
            richMessagePanel.text = productHeader;

            Text richProductInfoPanel = GameObject.Find("RichProductInfoPanel").GetComponentInChildren<Text>();
            richProductInfoPanel.supportRichText = true;
            richProductInfoPanel.text = productText;

            Text richCancelButton = GameObject.Find("RichCancelButton").GetComponentInChildren<Text>();
            richCancelButton.text = Localization.GetTranslations().cancel;
            richCancelButton.text = Localization.GetTranslations().ok;

            // Open canvas
            richConfirmCanvas.enabled = true;

            // Add button listeners
            Button[] buttons = richConfirmCanvas.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                if (button.name == "RichCancelButton")
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        // Close canvas
                        richConfirmCanvas.enabled = false;
                    });
                }
                else if (button.name == "RichAcceptButton")
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        // Close canvas
                        richConfirmCanvas.enabled = false;

                        // Activate product
                        parent.ActivateProduct(trackableName);
                    });
                }
            }
        }
        else
        {
            richConfirmCanvas.enabled = false;
        }
    }

    public void forceTrackableProduct(string fproduct)
    {
        // Set texts and translations
        // Using Unity Rich Text: https://docs.unity3d.com/Manual/StyledText.html
        GameObject.Find("RichMessagePanel").GetComponentInChildren<Text>().supportRichText = true;
        GameObject.Find("RichMessagePanel").GetComponentInChildren<Text>().text = productHeader;
        GameObject.Find("RichProductInfoPanel").GetComponentInChildren<Text>().supportRichText = true;
        GameObject.Find("RichProductInfoPanel").GetComponentInChildren<Text>().text = productText;
        GameObject.Find("RichCancelButton").GetComponentInChildren<Text>().text = Localization.GetTranslations().cancel;
        GameObject.Find("RichAcceptButton").GetComponentInChildren<Text>().text = Localization.GetTranslations().ok;

        // Open canvas
        richConfirmCanvas.enabled = true;

        // Add button listeners
        Button[] buttons = richConfirmCanvas.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (button.name == "RichCancelButton")
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    // Close canvas
                    richConfirmCanvas.enabled = false;
                });
            }
            else if (button.name == "RichAcceptButton")
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    // Close canvas
                    richConfirmCanvas.enabled = false;

                    // Activate product
                    trackableName = fproduct;
                    parent.ActivateProduct(trackableName);
                });
            }
        }

    }
}

[Serializable]
public struct AppResponseJson
{
    public string vuforiaAppDatabaseName;
    public int vuforiaAppDatabaseVersion;
    public int vuforiaAppDatabaseId;

    public ProductLineJson[] products;

    [Serializable]
    public struct ProductLineJson
    {
        public int idProduct;
        public string vuforiaProductTrackableName;
        public int vuforiaProductDatabaseVersion;
        public int vuforiaProductDatabaseId;
        public string activateProductHeader;
        public string activateProductText;
        public string title;

        public ImageTargetLineJson[] imageTargets;
    }

    [Serializable]
    public struct ImageTargetLineJson
    {
        public int idImageTarget;
        public string trackableName;
        public string androidFileType;
        public string iphoneFileType;
        public string backgroundColor;
        public string bundleAndroidUrl;
        public string bundleiPhoneUrl;
        public string videoThumbnailUrl;
        public string shareImageUrl;
        public string thumbnailUserUrl;
        public bool download;
        public float scale;
        public string title;
        public int product;
    }
}
