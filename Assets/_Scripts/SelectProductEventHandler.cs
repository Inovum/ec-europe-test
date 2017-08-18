using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class SelectProductEventHandler : MonoBehaviour
{
    private const int THUMBNAIL_BUTTON_INDEX = 2;

    private const int THUMBNAIL_IMAGE_INDEX = 0;
    private const int SHARE_IMAGE_INDEX = 1;

    [NonSerialized]
    public string vuforiaResourceName = "None <asigned in code>";
    [NonSerialized]
    public GameObject loadedDataSetController;
    [NonSerialized]
    public GameObject selector;

    public ProductDetailRow prefabObjectRowSelector;


    private Canvas productDetailsCanvas;
    private ProductButtonController productDetailController;
    private Transform productDetailsDataGrid;
    private Canvas mainCanvas;
    private Canvas datasetCanvas;

    private bool isShow = false;
    private Hashtable loadedImages = new Hashtable();

    /// <summary>
    /// Used to flag the  creation of object: if is set, the objects are already created..
    /// </summary>
    private bool createdObjects = false;

    private AppResponseJson appJsonDataset;

    /// <summary>
    /// Container to manage the click events to open the data grid
    /// </summary>
    private EventTrigger eventTrigger;

    // Use this for initialization
    void Start()
    {
        Debug.Log("prefabObjectRowSelector == OK " + (prefabObjectRowSelector != null).ToString());
        // Assign the values to instanciate the product controller with the values of the product
        productDetailController.loadedDataSetController = loadedDataSetController;
        productDetailController.selector = selector;
        productDetailController.vuforiaResourceName = vuforiaResourceName;
    }

    private void Awake()
    {
        eventTrigger = GetComponent<EventTrigger>();
        mainCanvas = GameObject.Find("MainCanvas").GetComponent<Canvas>();
        datasetCanvas = GameObject.Find("DataSetCanvas").GetComponent<Canvas>();

        // Get the controller for the current product and customize the properties
        productDetailController = GameObject.FindObjectOfType<ProductButtonController>();

        // The canvas on draw the data
        productDetailsCanvas = productDetailController.GetComponent<Canvas>();
        productDetailsDataGrid = GameObject.Find("ProductDetailsDataGrid").transform;

        // Attach a click to this row
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener(e => OpenModal());

        eventTrigger.triggers.Add(entry);

        // Now, Get the data downloaded from the database
        string data = PlayerPrefs.GetString(Globals.APP_JSONDATA_KEY);
        appJsonDataset = JsonHelper.getJson<AppResponseJson>(data);

    }

    private List<AppResponseJson.ImageTargetLineJson> searchPDFItemsProductByName(string name)
    {
        List<AppResponseJson.ImageTargetLineJson> result = new List<global::AppResponseJson.ImageTargetLineJson>();

        // Retrieve data from device
        string data = PlayerPrefs.GetString(Globals.APP_JSONDATA_KEY);
        AppResponseJson json = JsonHelper.getJson<AppResponseJson>(data);

        foreach (AppResponseJson.ProductLineJson product in json.products)
        {
            // The product is found
            if (product.vuforiaProductTrackableName == name)
            {
                foreach (var trackable in product.imageTargets)
                {
                    var trackableType = string.Empty;

#if UNITY_IOS
                    trackableType = trackable.iphoneFileType;
#elif UNITY_ANDROID
                    trackableType = trackable.androidFileType;
#endif
                    if (trackableType == DataSetLoader.PDF_TRACKABLE_RESOURCE_TYPE)
                        result.Add(trackable); // Add the PDF
                }
            }
        }

        return result;
    }

    private AppResponseJson.ImageTargetLineJson searchImageTargeByName(string name)
    {
        // Retrieve data from device
        string data = PlayerPrefs.GetString(Globals.APP_JSONDATA_KEY);
        AppResponseJson json = JsonHelper.getJson<AppResponseJson>(data);

        foreach (AppResponseJson.ProductLineJson product in json.products)
        {
            foreach (AppResponseJson.ImageTargetLineJson target in product.imageTargets)
            {
                if (target.trackableName == name)
                    return target;
            }
        }

        return new AppResponseJson.ImageTargetLineJson(); // Returns an empty value
    }


    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Change the state of flag to show/hide the canvas with the details
    /// </summary>
    private void ButtonPressed()
    {
        isShow = !isShow; // Change the state to show the modal or not.
        if (isShow)
            OpenModal();
        else
            CloseModal();
    }


    private void CreateProductDataRows()
    {
        Debug.Log("childCount=" + productDetailsDataGrid.transform.childCount.ToString());
        // First, remove the already created rows (if any)
        if (productDetailsDataGrid.transform.childCount > 0)
        {
            List<Transform> objectsToRemove = new List<Transform>();
            foreach (Transform oldObject in productDetailsDataGrid.transform)
                objectsToRemove.Add(oldObject);
            // Remove it efectively!
            foreach (Transform objectToRemove in objectsToRemove)
                Destroy(objectToRemove.gameObject);
        }

        // Look for the data and create the rows with the names of resources
        GameObject dataset = GameObject.Find(vuforiaResourceName);

        productDetailController.vuforiaResourceName = vuforiaResourceName;
        productDetailController.loadedDataSetController = loadedDataSetController;
        productDetailController.selector = selector;

        // First, the trackables
        foreach (Transform childObject in dataset.transform)
        {
            ProductDetailRow newRow = buildDataRow(childObject.name);
            newRow.targetAnimationObject = childObject;
            newRow.vuforiaResourceName = vuforiaResourceName;
        }

        // Second, the PDF files
        var pdfItems = searchPDFItemsProductByName(vuforiaResourceName);
        foreach (var pdfItem in pdfItems)
        {
            var bundleUrl = pdfItem.bundleAndroidUrl;

#if UNITY_IOS
            bundleUrl = pdfItem.bundleiPhoneUrl;
#endif

            // Create a custom string to show the PDF
            string codedString = "-1|" + pdfItem.trackableName + "|" + pdfItem.title + "|" + bundleUrl; // Use -1 for PDF icon
            buildDataRow(codedString);
        }

        // Set the flag to indicate that all row objects are already created
        createdObjects = true;
    }

    /// <summary>
    /// Generates a new row
    /// </summary>
    private ProductDetailRow buildDataRow(string name)
    {
        //prefabObjectRowSelector.name = "ProductDetailRow";

        ProductDetailRow newRow = Instantiate(prefabObjectRowSelector);
        newRow.onSelected += NewRow_onSelected;

        // The name of the object has the metadata
        string[] nameParts = name.Split('|');
        var imageID = int.Parse(nameParts[0]);
        var imageTargetName = nameParts[1];
        var rowText = nameParts[2].Length != 0 ? nameParts[2] : imageTargetName;

        newRow.ImageTargetData = searchImageTargeByName(imageTargetName);

        Debug.Log("Showing product rows. Parameters: " + String.Join(", ", nameParts));

        // now, get the string for the name
        setRowText(newRow, rowText);
        // Now, get the icon
        setRowIcon(newRow, imageID);
        // Get the buttons to assign the events

        var rowButtons = newRow.gameObject.GetComponentsInChildren<Button>();
        var previewButton = rowButtons[THUMBNAIL_IMAGE_INDEX];
        var shareButton = rowButtons[SHARE_IMAGE_INDEX];

        switch (imageID)
        {
            case -1:
                // set the handler to download the PDF: the parameter 4 is the URL for the PDF object
                shareButton.onClick.AddListener(new UnityEngine.Events.UnityAction(delegate { SharePDF(nameParts[3], false); }));
                previewButton.onClick.AddListener(new UnityEngine.Events.UnityAction(delegate { SharePDF(nameParts[3], true); }));

                break;
            default:
                previewButton.onClick.AddListener(new UnityEngine.Events.UnityAction(delegate { newRow.animateTarget(); }));
                shareButton.onClick.AddListener(new UnityEngine.Events.UnityAction(delegate { ShareResource(newRow); }));
                break;
        }
        newRow.transform.SetParent(productDetailsDataGrid.transform);

        return newRow;
    }

    private void NewRow_onSelected()
    {
    }

    private void setRowIcon(ProductDetailRow row, int imageID)
    {
        if (imageID >= 0)
        {
            if (loadedImages.ContainsKey(imageID)) // Check to know if the images has been already loaded
            {
                Debug.Log("Images in cache: Not loading from URI");
                var images = row.GetComponentsInChildren<Image>();
                Image thumbnailImage = images[THUMBNAIL_BUTTON_INDEX]; // Always get the secondOne
                thumbnailImage.sprite = (Sprite)loadedImages[imageID];
            }
            else
            {
                Debug.Log("Images not in cache: Trying to load from URI");
                // Load the image from the service
                var routineImage = loadThumbnailImage(row, imageID); // The first value is the ID of the image tracke
                StartCoroutine(routineImage);
            }
        }
        else // If the image ID is an "special" number (less than zero) it is an "special" icon 
        {
            Texture2D thumbnailTexture = null;

            switch (imageID)
            {
                case -1: // PDF Icon
                    thumbnailTexture = (Texture2D)Resources.Load("pdf_icon");
                    break;
            }
            if (thumbnailTexture == null)
                throw new Exception("The texture for the resource cannot be found in the resources directory");

            // Assign the predefined image
            Image thumbnailImage = createThumbnailSprite(row, thumbnailTexture);
        }
    }

    private void setRowText(ProductDetailRow row, string rowText)
    {
        Text rowTextObject = row.GetComponentInChildren<Text>();
        rowTextObject.text = rowText;
    }

    private IEnumerator loadThumbnailImage(ProductDetailRow row, int imageID)
    {
        string imageUri = string.Format(AppStartupController.SERVER_DOMAIN + "/thumbnail/image-target?idImageTarget={0}", imageID);
        Debug.Log("Trying to download the image: " + imageUri);
        WWW www = new WWW(imageUri); //TODO: This address should not be wired here... It needs a constant

        yield return www;
        try
        {
            //// Create a texture in DXT1 format
            Texture2D texture = new Texture2D(www.texture.width, www.texture.height);

            ////// assign the downloaded image to sprite
            www.LoadImageIntoTexture(texture);
            // Assign the texture to the thumbnail image in the row
            Image thumbnailImage = createThumbnailSprite(row, texture);

            // Add the current image to the cache table
            if (imageID >= 0)
                loadedImages.Add(imageID, thumbnailImage.sprite);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private Image createThumbnailSprite(ProductDetailRow row, Texture2D texture)
    {
        // Use the texture to create the sprite
        var images = row.GetComponentsInChildren<Image>();
        Image thumbnailImage = images[THUMBNAIL_BUTTON_INDEX]; // Always get the first

        Rect rec = new Rect(0.0f, 0.0f, texture.width, texture.height);
        thumbnailImage.sprite = Sprite.Create(texture, rec, new Vector2(0f, 0f));

        //row.transform.position = new Vector3(20, 0, 0);

        return thumbnailImage;
    }

    public void OpenModal()
    {
        ShowModalCanvas(true);
        CreateProductDataRows();
    }

    public void CloseModal()
    {
        ShowModalCanvas(false);
    }

    private void ShowModalCanvas(bool show)
    {
        datasetCanvas.enabled = !show; // Hide the main screen

        mainCanvas.enabled = !show; // Hide the screen with datasets listing
        productDetailsCanvas.enabled = show;
    }

    string subject = "eg. subject";
    string body = "eg. body text";

    public void ShareResource(ProductDetailRow row)
    {
        Debug.Log("ShareResource: share element from the list");

        string contentType = "image/*";
        string destinationFilename;

        try
        {
            // It is important to know which type of resource is
            var isAndroid = true;
            var trackableType = row.ImageTargetData.androidFileType;
            var bundleUrl = row.ImageTargetData.bundleAndroidUrl;

#if UNITY_IOS
            isAndroid = false;
            trackableType = row.ImageTargetData.iphoneFileType;
            bundleUrl = row.ImageTargetData.bundleiPhoneUrl;
#endif

            if (isAndroid)
            {
                Debug.Log("ShareResource: android trackableType=" + trackableType);
            }
            else
            {
                Debug.Log("ShareResource: iOS trackableType=" + trackableType);
            }

            Image thumbnailImage = null;
            bool isUrl = true;
            // Decide what to do considering the type
            var fileExists = false;
            destinationFilename = GetSharebleResourceUrl(row, ref contentType, trackableType, bundleUrl, ref thumbnailImage, ref isUrl, ref fileExists);

            Debug.Log("ShareResource: destinationFilename=" + destinationFilename);

            if (isUrl)
            {
                //// Download the information
                //WWW www = new WWW(destinationFilename); //TODO: This address should not be wired here... It needs a constant
                //Debug.Log("Descargar: " + destinationFilename);

                //yield return www;

                //Debug.Log("Código después del YIELD para descargar la imagen");
                //try
                //{
                //    //// Create a texture in DXT1 format
                //    Texture2D texture = new Texture2D(www.texture.width, www.texture.height);

                //    ////// assign the downloaded image to sprite
                //    www.LoadImageIntoTexture(texture);
                //    destinationFilename = DownloadToFile(texture);
                //}
                //catch (Exception e)
                //{
                //    Debug.LogError(e);
                //}
                // Download the image
                var shareableUrl = destinationFilename;
                var shareableAnimationFilename = System.IO.Path.GetFileName(shareableUrl);
                Debug.Log("shareableAnimationFilename=" + shareableAnimationFilename);

                destinationFilename = DataSetLoader.getFilePath(row.ImageTargetData.download) + "/" + shareableAnimationFilename;
                Debug.Log("destinationFilename=" + destinationFilename);

                DownloadToFile(shareableUrl, destinationFilename);

                //AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", destinationFilename);// Parse the filename
                //intentObject.Call<AndroidJavaObject>("setDataAndType", uriObject, "image/jpg");
            }


#if UNITY_ANDROID
            //execute the below lines if being run on a Android device
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");

            /**
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TITLE"), subject);
            **/
            //intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), body);

            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");

            AndroidJavaObject fileObject = new AndroidJavaObject("java.io.File", destinationFilename);
            AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("fromFile", fileObject);
            Debug.Log("Object to share:" + destinationFilename);

            intentObject.Call<AndroidJavaObject>("setType", contentType);
            bool fileExist = fileObject.Call<bool>("exists");
            if (fileExist)
            {
                Debug.Log("File exists: OK");
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject);
            }

            //                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_VIEW"));
            intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_ACTIVITY_CLEAR_WHEN_TASK_RESET"));
            intentObject.Call<AndroidJavaObject>("setFlags", intentClass.GetStatic<int>("FLAG_ACTIVITY_CLEAR_TOP"));
            intentObject.Call<AndroidJavaObject>("setFlags", intentClass.GetStatic<int>("FLAG_ACTIVITY_RESET_TASK_IF_NEEDED"));
            intentObject.Call<AndroidJavaObject>("setFlags", intentClass.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK"));

            //get the current activity
            AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            //start the activity by sending the intent data
            var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "EC: Choose an app to share your images");
            currentActivity.Call("startActivity", chooser);
#elif UNITY_IOS
			// ... share code for iOS
			// public void Share(string shareText, string imagePath, string url, string subject = "")
			var nativeShare = new NativeShare();
            //NativeShare.CallSocialShare("Title 1", "Message 2");
			nativeShare.Share(null, null, destinationFilename, null);
#endif

        }
        finally
        {
            //File.Delete(destination);
        }
    }

    /// <summary>
    /// Get the data from the database and generates a valid filename to send to android/iphone share engine
    /// </summary>
    /// <param name="row"></param>
    /// <param name="contentType"></param>
    /// <param name="trackableType"></param>
    /// <param name="bundleUrl"></param>
    /// <param name="thumbnailImage"></param>
    /// <param name="isUrl"></param>
    /// <param name="fileExists"></param>
    /// <returns></returns>
    private string GetSharebleResourceUrl(ProductDetailRow row, ref string contentType, string trackableType, string bundleUrl, ref Image thumbnailImage, ref bool isUrl, ref bool fileExists)
    {
        string destinationFilename;
        switch (trackableType)
        {
            case DataSetLoader.VIDEO_TRACKABLE_RESOURCE_TYPE:
                // Get the correct video MIME extension
                switch (Path.GetExtension(bundleUrl))
                {
                    case ".mp4":
                        contentType = "video/mp4";
                        break;
                    default:
                        contentType = "video/*";
                        break;
                }
                var videoFilename = System.IO.Path.GetFileName(bundleUrl);
                Debug.Log("videoFilename=" + videoFilename);

                destinationFilename = DataSetLoader.getFilePath(row.ImageTargetData.download) + "/" + videoFilename;
                isUrl = false;

                Debug.Log("Sharing destinationFilename => " + destinationFilename);
                fileExists = System.IO.File.Exists(destinationFilename);

                //var videoThumbnailUrl = row.ImageTargetData.videoThumbnailUrl;
                //var videoThumbnailFilename = System.IO.Path.GetFileName(videoThumbnailUrl);
                //var videoThumbnailDevicePath = DataSetLoader.getFilePath(row.ImageTargetData.download) + "/" + videoThumbnailFilename;
                //bool fileThumbnailExists = System.IO.File.Exists(videoThumbnailDevicePath);

                break;

            case DataSetLoader.PICTURE_TRACKABLE_RESOURCE_TYPE:

                var pictureFilename = System.IO.Path.GetFileName(bundleUrl);
                Debug.Log("pictureFilename=" + pictureFilename);

                destinationFilename = DataSetLoader.getFilePath(row.ImageTargetData.download) + "/" + pictureFilename;
                isUrl = false;

                Debug.Log("Sharing destinationFilename => " + destinationFilename);
                fileExists = System.IO.File.Exists(destinationFilename);

                break;

            case DataSetLoader.ANIMATION_TRACKABLE_RESOURCE_TYPE:

                // The resource is an animation, a video or a PDF file...
                if (string.IsNullOrEmpty(row.ImageTargetData.shareImageUrl))
                {
                    var images = row.GetComponentsInChildren<Image>();
                    thumbnailImage = images[THUMBNAIL_BUTTON_INDEX]; // Always get the first

                    destinationFilename = DownloadToFile(thumbnailImage.sprite.texture);
                    // The images comes from a local filename
                    isUrl = false;
                }
                else
                {
                    Debug.Log("Descargando una imagen desde la web" + row.ImageTargetData.shareImageUrl);
                    // the variable has the URL
                    destinationFilename = row.ImageTargetData.shareImageUrl;
                }
                break;
            default:
                // The resource is an image, so we need to send that image, not the shared one
                var bundleResFilename = System.IO.Path.GetFileName(bundleUrl);
                destinationFilename = DataSetLoader.getFilePath(row.ImageTargetData.download) + "/" + bundleResFilename;
                break;
        }

        return destinationFilename;
    }

    private IEnumerator downloadPDFCoroutineHandler = null;

    public void SharePDF(string PDFUrlFilename, bool isPreview = true)
    {
        Debug.Log("SharePDF: PDFUrlFilename=" + PDFUrlFilename + " isPreview=" + isPreview);
#if UNITY_ANDROID
        //execute the below lines if being run on a Android device
        try
        {
            Debug.Log("Comenzando a compartir el PDF: " + PDFUrlFilename);
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");

            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");

            intentObject.Call<AndroidJavaObject>("setType", "application/pdf");
            intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_ACTIVITY_NO_HISTORY"));
            if (isPreview)
            {
                Debug.Log("Se hará un preview del fichero PDF");
                AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", PDFUrlFilename);
                intentObject.Call<AndroidJavaObject>("setData", uriObject);
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_VIEW"));
            }
            else
            {
                Debug.Log("Se enviará el fichero PDF");
                // Create a new filename
                var fileToShare = BuildHashedDownloadFileName(PDFUrlFilename);
                // Download the PDF async and wait for the result
                DownloadToFile(PDFUrlFilename, fileToShare);
                AndroidJavaObject fileObject = new AndroidJavaObject("java.io.File", fileToShare);

                AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("fromFile", fileObject);

                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject);
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));

                intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_ACTIVITY_CLEAR_WHEN_TASK_RESET"));
                intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_ACTIVITY_CLEAR_TOP"));
                intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_ACTIVITY_RESET_TASK_IF_NEEDED"));
                intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK"));
            }
            //get the current activity
            AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");

            //start the activity by sending the intent data
            var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "EC: Choose an app to share your document");
            currentActivity.Call("startActivity", chooser);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
#elif UNITY_IOS
        Debug.Log("SharePDF (iOS): PDFUrlFilename=" + PDFUrlFilename + " isPreview=" + isPreview);

        if (isPreview)
        {
            // open PDF
            Application.OpenURL(PDFUrlFilename);
        }
        else
        {
            // share PDF

            // Create a new filename
            var pdfFile = BuildHashedDownloadFileName(PDFUrlFilename);
            Debug.Log("SharePDF (iOS): pdfFile=" + pdfFile);

            // Download the PDF async and wait for the result
            var downloadFile = DownloadToFile(PDFUrlFilename, pdfFile);
            Debug.Log("SharePDF (iOS): downloadFile=" + downloadFile);

            // public void Share(string shareText, string imagePath, string url, string subject = "")
            var nativeShare = new NativeShare();
            nativeShare.Share(null, null, pdfFile, null);
        }
#endif
    }

    private string BuildHashedDownloadFileName(string PDFFilePath)
    {
        string result;
        using (var md5 = MD5.Create())
        {
            var extension = Path.GetExtension(PDFFilePath);
            var imageHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(PDFFilePath))).Replace("-", string.Empty); // Convert into a HEX string
            result = Path.Combine(DataSetLoader.getFilePath(true), imageHash + extension); // Build a unique file name into the persistent file area
        }

        return result;
    }

    public string DownloadToFile(string url, string targetFilePath)
    {
        Debug.Log(string.Format("DownloadToFile: Reading url={0} and writing to path={1}", url, targetFilePath));

        if (!File.Exists(targetFilePath))
        {
            Debug.Log("DownloadToFile: openning url=" + url);
            var www = new WWW(url);
            while (!www.isDone)
                new WaitForSeconds(0.1f);

            Debug.Log("DownloadToFile: finished downloading");

            // Validates the directory where to write
            var directory = Path.GetDirectoryName(targetFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log(string.Format("DownloadToFile:D created directory={0}", directory));
            } else {
                Debug.Log("DownloadToFile: Directory '" + directory + "' exists");
            }

            byte[] content = www.bytes;
            File.WriteAllBytes(targetFilePath, content);
            Debug.Log(string.Format("DownloadToFile: written {0} bytes", content.Length));
        } else {
            Debug.Log("DownloadToFile: File " + targetFilePath + " does not exists");
        }

        return url;
    }

    /// <summary>
    /// Downloads an image into an Sprite2D
    /// </summary>
    /// <returns>The filename created</returns>
    public static string DownloadToFile(Texture2D texture)
    {
        string destinationFilename;
        byte[] dataToSave = texture.EncodeToPNG();
        Debug.Log("Storing " + dataToSave.Length.ToString() + " to image cache");
        using (var md5 = MD5.Create())
        {
            var imageHash = BitConverter.ToString(md5.ComputeHash(dataToSave)).Replace("-", string.Empty); // Convert into a HEX string
            destinationFilename = Path.Combine(Application.persistentDataPath, imageHash + ".png"); // Build a unique file name into the persistent file area
            Debug.Log("Storing image to file " + destinationFilename);
            if (!File.Exists(destinationFilename))
                File.WriteAllBytes(destinationFilename, dataToSave);
        }

        return destinationFilename;
    }


}
