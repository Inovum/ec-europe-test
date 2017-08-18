using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Vuforia;
using System;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.CognitoIdentity;
using Amazon;

public class DataSetLoader : MonoBehaviour
{
    public const string PICTURE_TRACKABLE_RESOURCE_TYPE = "PICTURE";
    public const string ANIMATION_TRACKABLE_RESOURCE_TYPE = "ANIMATION";
    public const string PDF_TRACKABLE_RESOURCE_TYPE = "PDF";
    public const string VIDEO_TRACKABLE_RESOURCE_TYPE = "VIDEO";

    public string vuforiaResourceName;
    public List<string> localResourcesPath;

    private DataSet dataSet = null;
    private Button imagePopupButton = null;
    private Button videoPopupButton = null;
    private UnityEngine.UI.Image videoThumbnail = null;
    public int imagePopupCounter = 0;
    public int videoPopupCounter = 0;
    private float imagePopupWidth;
    private float imagePopupHeight;
    private int MAX_GAME_OBJECTS = 20;
    // private float zoomSensitivity = 0.000001f;  // 0.005f started with
    private float zoomSensitivity = 0.001f; // started with
    private float resourceCheckSeconds = 10;

    private string productName;

    private AppResponseJson.ProductLineJson productJsonObject;
    private AppResponseJson.ImageTargetLineJson imageTargetLineJson;

    private Canvas mainCanvas;
    public ObjectTracker tracker;

    // Creates and initializes a new ArrayList.
    private ArrayList fileLoadingList = new ArrayList();

    // 0 - protrait 1 - horizontal
    private int lastDeviceOrientation = 0;

    private bool isVideoPlaying = false;
    public string videoPath = null;

    // amazon private vars
    private string IdentityPoolId = "us-east-1:0aee2c38-fa8a-46e1-ba00-4004a2e41785";
    private string CognitoIdentityRegion = "us-east-1";
    public string S3Region = "eu-central-1";
    private string S3BucketName = "ec-europe";
    private string AWSResourceFile = null;
    private IAmazonS3 S3Client;
    private AWSCredentials Credentials;
    private GameObject[] gobs;
    private int currentGOB = 0;
    public int activeGOB = 0;


    // Use this for initialization
    void Awake()
    {
        imagePopupWidth = 300.0f;
        imagePopupHeight = 250.0f;

        localResourcesPath = new List<string>();

        // Obtain the imagePopupButton reference
        mainCanvas = GameObject.Find("MainCanvas").GetComponent<Canvas>();
        Button[] buttons = mainCanvas.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (button.name == "ImagePopupButton")
            {
                imagePopupButton = button;
            }
            if (button.name == "VideoPopupButton")
            {
                videoPopupButton = button;
            }
        }
        UnityEngine.UI.Image[] images = mainCanvas.GetComponentsInChildren<UnityEngine.UI.Image>();
        foreach (UnityEngine.UI.Image image in images)
        {
            if (image.name == "VideoThumbnail")
            {
                videoThumbnail = image;
            }
        }
    }
    
    // Use this for initialization
    void Start () {
        //init gobs
        gobs = new GameObject[MAX_GAME_OBJECTS];
        // Amazon init
        UnityInitializer.AttachToGameObject(this.gameObject);
        Credentials = new CognitoAWSCredentials(IdentityPoolId, RegionEndpoint.GetBySystemName(CognitoIdentityRegion));
        S3Client = new AmazonS3Client(Credentials, RegionEndpoint.GetBySystemName(S3Region));

        Debug.Log("----------------------------DataSetLoader Start" + vuforiaResourceName);
        
        // Get product from stored json
        this.productJsonObject = getProductJsonObject(this.vuforiaResourceName);

        this.Enable();
      //  string tf = Application.persistentDataPath + "/versionfiles/" + "prueba.txt";
       // GetFileFromAmazon("prueba.txt", tf);

    }

    void Update()
    {
     
     //// rotate object from X and Y coords

      if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {            
         Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
         if (gobs[activeGOB] != null)
            {
               gobs[activeGOB].transform.Rotate(-touchDeltaPosition.y * 5, -touchDeltaPosition.x * 5, 1);
              //  gobs[activeGOB].transform.Rotate(Vector3.up * -touchDeltaPosition.x * Time.deltaTime * 20f);

            }
      } else
        {
            // pinch zoom
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                if (touch0.phase == TouchPhase.Moved && touch1.phase == TouchPhase.Moved)
                {
                    Vector2 prevDist = (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition);
                    Vector2 curDist = touch0.position - touch1.position;
                    float delta = curDist.magnitude - prevDist.magnitude;
                    if (delta > 0)
                    {
                        if (gobs[activeGOB] != null)
                            gobs[activeGOB].transform.localScale += new Vector3(zoomSensitivity, zoomSensitivity, zoomSensitivity);
                    }
                    else
                    {
                        if (delta < 0)
                        {
                            if (gobs[activeGOB] != null)
                                gobs[activeGOB].transform.localScale -= new Vector3(zoomSensitivity, zoomSensitivity, zoomSensitivity);
                        }
                    }
                }
            }
        }
    }



    private void GetFileFromAmazon(string fileName, string destPath)
    {
        Debug.Log("GetFileFromAmazon FILE:" + fileName);
        Debug.Log("GetFileFromAmazon DEST:" + destPath);

        S3Client.GetObjectAsync(S3BucketName, fileName, (responseObj) =>
        {
            var response = responseObj.Response;           
            if (response.ResponseStream != null)
            {
                byte[] data = new byte[response.ResponseStream.Length];
                string nfp = destPath;
                response.ResponseStream.Read(data, 0, (int)response.ResponseStream.Length);
                System.IO.File.WriteAllBytes(nfp, data);
            }
            else
            {
#if UNITY_ANDROID
                ShowToast.Show(Localization.GetTranslations().error.downloadFromAmazonFailed);
#endif
            }

        });
    }


    public void Enable()
    {
        this.enabled = true;
        PlayerPrefs.SetString(Globals.getProductIsEnabledKey(this.vuforiaResourceName), "true");

        StartCoroutine(SaveAndLoadDataSet());

        // Show message product was enabled
        ShowToast.Show(Localization.GetTranslations().message.productEnabled);
    }

    public IEnumerator SaveAndLoadDataSet()
    {
        // Check device version against downloaded json
        this.productName = this.productJsonObject.vuforiaProductTrackableName;
        int deviceProductVersion = PlayerPrefs.GetInt(Globals.getProductVersionKey(this.productName));
        int downloadedProductVersion = this.productJsonObject.vuforiaProductDatabaseVersion;

        // Retrieve XML and DAT files
        string xmlProductURL = AppStartupController.SERVER_DOMAIN + AppStartupController.SERVER_URI_GETPRODUCTXML + this.productJsonObject.vuforiaProductDatabaseId;
        string datProductURL = AppStartupController.SERVER_DOMAIN + AppStartupController.SERVER_URI_GETPRODUCTDAT + this.productJsonObject.vuforiaProductDatabaseId;
        
        // if file.exists and file.version == bundleVersion
        var xmlDeviceProductPath = getFilePath(true) + "/" + this.productJsonObject.vuforiaProductDatabaseId + ".xml";
        // XML and DAT names MUST match
        var datDeviceProductPath = getFilePath(true) + "/" + this.productJsonObject.vuforiaProductDatabaseId + ".dat";

        if (deviceProductVersion == downloadedProductVersion && System.IO.File.Exists(xmlDeviceProductPath) && System.IO.File.Exists(datDeviceProductPath))
        {
            LoadDataSet(xmlDeviceProductPath);
        }
        else
        {
            if (deviceProductVersion != downloadedProductVersion)
            {
                Debug.Log("*************************************************** PRODUCT VERSION WAS CHANGED!");
                // Delete All resources of this product
                // check if file exist and delete it
                for (int a = 0; a < this.productJsonObject.imageTargets.Length; a++)
                {
                    this.imageTargetLineJson = this.productJsonObject.imageTargets[a];
                    var presource = this.imageTargetLineJson.bundleAndroidUrl;
#if UNITY_IOS
                presource = this.imageTargetLineJson.bundleiPhoneUrl;

#elif UNITY_ANDROID
                    presource = this.imageTargetLineJson.bundleAndroidUrl;
#endif
                    var rfilename = string.Empty;
                    string[] splitFileName = presource.Split(new string[] { ":" }, StringSplitOptions.None);

                    Debug.Log("THE PRESOURCE IS: " + presource + "." + " Params: " + String.Join(",", splitFileName));
                    if (splitFileName.Length > 0 && splitFileName[0].Contains("http"))
                    {
                        //extract last element
                        string[] fElements = splitFileName[1].Split(new string[] { "/" }, StringSplitOptions.None);
                        rfilename = fElements[fElements.Length - 1];
                    }
                    else if (splitFileName.Length > 1)
                    {
                        string[] fElements = splitFileName[1].Split(new string[] { "/" }, StringSplitOptions.None);

                        rfilename = fElements[fElements.Length - 1];
                    }

                    Debug.Log("THE FILENAME TO DELETE IS " + rfilename);
                    if (rfilename.Length > 0 && System.IO.File.Exists(Application.persistentDataPath + "/versionfiles/" + rfilename))
                    {
                        Debug.Log("FILENAME EXISTS" + Application.persistentDataPath + "/versionfiles/" + rfilename);
                        System.IO.File.Delete(Application.persistentDataPath + "/versionfiles/" + rfilename);

                    }
                    else
                    {
                        Debug.Log("FILENAME DOESNT EXISTS" + Application.persistentDataPath + "/versionfiles/" + rfilename);
                    }
                    // Directory.Exists(Application.persistentDataPath + Globals.VERSION_FILES_URI)
                }
            }

            // End Delete


            // Download the file from the URL. It will not be saved in the Cache
            using (WWW wwwXml = new WWW(xmlProductURL))
            {
                yield return wwwXml;
                if (wwwXml.error != null)
                {
                    Debug.Log("wwwXml download had an error:" + wwwXml.error);
                    ProductLoadAllShowError();
                    yield break;
                }
                Debug.Log("Save vuforia file from: " + xmlProductURL);

                // Save data into user's folder
                System.IO.File.WriteAllBytes(xmlDeviceProductPath, wwwXml.bytes);
                Debug.Log("Save vuforia file - Asset was saved: " + xmlDeviceProductPath);

                using (WWW wwwDat = new WWW(datProductURL))
                {
                    yield return wwwDat;
                    if (wwwDat.error != null)
                    {
                        Debug.Log("wwwDat download had an error:" + wwwXml.error);
                        ProductLoadAllShowError();
                        yield break;
                    }
                    Debug.Log("Save vuforia file from: " + datProductURL);

                    // Save data into user's folder
                    System.IO.File.WriteAllBytes(datDeviceProductPath, wwwDat.bytes);
                    Debug.Log("Save vuforia file - Asset was saved: " + datDeviceProductPath);

                    // Update deviceProductVersion
                    PlayerPrefs.SetInt(Globals.getProductVersionKey(this.productName), downloadedProductVersion);

                    
                }
            }  // memory is freed from the web stream (www.Dispose() gets called implicitly)
            LoadDataSet(xmlDeviceProductPath);
        }
    }
    
    public void LoadDataSet(string xmlDeviceProductPath)
    {
        tracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        dataSet = tracker.CreateDataSet();
        
        if (dataSet.Load(xmlDeviceProductPath, VuforiaUnity.StorageType.STORAGE_ABSOLUTE))
        {
            Debug.Log("CustomImageTargetBehaviour - Dataset loaded! " + dataSet.ToString());

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

            //StartCoroutine(LoadTrackableGameObjects());
            LoadTrackableGameObjects();
        }
    }

    private void LoadTrackableGameObjects()
    {
        string imgUrl = "";
        Debug.Log("LoadTrackableGameObjects");
        int counter = 0;
        IEnumerable<TrackableBehaviour> tbs = TrackerManager.Instance.GetStateManager().GetTrackableBehaviours();
        foreach (TrackableBehaviour tb in tbs)
        {
            Debug.Log("LoadTrackableGameObjects name: " + tb.name);
            if (tb != null)
            {
                if (tb.name == "New Game Object")
                {
                    AppResponseJson.ImageTargetLineJson trackObject = FindImageTargetFromProduct(tb);
                    // change generic name to include trackable name
                    tb.gameObject.name = string.Format("{0}|{1}|{2}", trackObject.idImageTarget, trackObject.trackableName, trackObject.title);
                    //tb.gameObject.transform = gameObject.transform;
                    tb.gameObject.transform.SetParent(gameObject.transform);

                    // add additional script components for trackable
                    tb.gameObject.AddComponent<DefaultTrackableEventHandler>();
                    tb.gameObject.AddComponent<TurnOffBehaviour>();
                }

                if (productJsonObject.imageTargets != null)
                {
                    // Find the trackable name in the data json
                    foreach (AppResponseJson.ImageTargetLineJson imageTargetLineJson in this.productJsonObject.imageTargets)
                    {
                        var trackableType = string.Empty;

#if UNITY_IOS
                        trackableType = imageTargetLineJson.iphoneFileType;
#elif UNITY_ANDROID
                        trackableType = imageTargetLineJson.androidFileType;
#endif

                        Debug.Log("imageTargetLineJson name: " + imageTargetLineJson.trackableName + " - " + trackableType + " - " + imageTargetLineJson.download);
                        if (tb.TrackableName == imageTargetLineJson.trackableName && trackableType == ANIMATION_TRACKABLE_RESOURCE_TYPE)
                        {
                            if (imageTargetLineJson.download)
                            {
                                StartCoroutine(SaveAndLoadAssetBundleWWW(imageTargetLineJson, this.productJsonObject.vuforiaProductDatabaseVersion, tb, null));
                            }
                            else
                            {
                                // file not exists and imageTargetLineJson.download == false
                                tb.RegisterTrackableEventHandler(new FileOnDemandItemTrackable(this, tb, trackableType, imageTargetLineJson, this.productJsonObject.vuforiaProductDatabaseVersion));
                            }
                        }
                        else if (tb.TrackableName == imageTargetLineJson.trackableName && trackableType == PICTURE_TRACKABLE_RESOURCE_TYPE)
                        {

                            // Obtain url and device path
#if UNITY_EDITOR
                            // Editor Debuging
                            imgUrl = imageTargetLineJson.bundleAndroidUrl;
#endif
#if UNITY_IOS
                        imgUrl = imageTargetLineJson.bundleiPhoneUrl;
                        Debug.Log("THE IMAGE URL IS:" + imgUrl);
#elif UNITY_ANDROID
                            imgUrl = imageTargetLineJson.bundleAndroidUrl;
#endif
                            var imgFilename = System.IO.Path.GetFileName(imgUrl);
                            var imgDevicePath = getFilePath(imageTargetLineJson.download) + "/" + imgFilename;
                            localResourcesPath.Add(imgDevicePath);
                            bool fileExists = System.IO.File.Exists(imgDevicePath);

                            if (imageTargetLineJson.download && !fileExists)
                            {
                                Debug.Log("Attempt to download image from " + imgUrl);
                                //  StartCoroutine(LoadImageWWW(imgUrl, imgDevicePath, tb, null));
                                StartCoroutine(LoadImageAmazon(imgUrl, imgDevicePath, tb, null));
                            }
                            else if (fileExists)
                            {
                                Debug.Log("Image file exists " + imgDevicePath);
                                // Image file exists - load bytes into texture
                                var bytes = System.IO.File.ReadAllBytes(imgDevicePath);
                                Texture2D texture = new Texture2D(1, 1);
                                texture.LoadImage(bytes);

                                // Add GUI button to open image
                                if (imagePopupButton != null)
                                {
                                    tb.RegisterTrackableEventHandler(new ImageItemTrackable(this, texture, imagePopupButton));
                                    //imagePopupButton.onClick.AddListener(showImagePopup);
                                }
                            }
                            else
                            {
                                Debug.Log("file not exists and imageTargetLineJson.download == false " + imgUrl);
                                // file not exists and imageTargetLineJson.download == false
                                tb.RegisterTrackableEventHandler(new FileOnDemandItemTrackable(this, tb, trackableType, imgUrl, imgDevicePath, imagePopupButton));
                            }
                            continue;
                        }
                        else if (tb.TrackableName == imageTargetLineJson.trackableName && trackableType == DataSetLoader.VIDEO_TRACKABLE_RESOURCE_TYPE)
                        {
                            var videoUrl = imageTargetLineJson.bundleAndroidUrl;
#if UNITY_IOS
                        videoUrl = imageTargetLineJson.bundleiPhoneUrl;
                         Debug.Log("THE VIDEO URL IS:" + videoUrl);
#elif UNITY_ANDROID
                            videoUrl = imageTargetLineJson.bundleAndroidUrl;
#endif
                            var videoFilename = System.IO.Path.GetFileName(videoUrl);
                            var videoDevicePath = getFilePath(imageTargetLineJson.download) + "/" + videoFilename;
                            localResourcesPath.Add(videoDevicePath);
                            bool fileExists = System.IO.File.Exists(videoDevicePath);

                            var videoThumbnailUrl = imageTargetLineJson.videoThumbnailUrl;
                            var videoThumbnailFilename = System.IO.Path.GetFileName(videoThumbnailUrl);
                            var videoThumbnailDevicePath = getFilePath(imageTargetLineJson.download) + "/" + videoThumbnailFilename;
                            bool fileThumbnailExists = System.IO.File.Exists(videoThumbnailDevicePath);

                            if (imageTargetLineJson.download && (!fileExists || !fileThumbnailExists))
                            {
                                Debug.Log("Attempt to download video from " + videoUrl);
                                StartCoroutine(LoadVideoThumbnailAmazon(videoThumbnailUrl, videoThumbnailDevicePath, videoUrl, videoDevicePath, tb, null));
                            }
                            else if (fileExists && fileThumbnailExists)
                            {
                                Debug.Log("Video File exists " + videoDevicePath);

                                // Thumbnail image file exists - load bytes into texture
                                var bytes = System.IO.File.ReadAllBytes(videoThumbnailDevicePath);
                                Texture2D textureThumbnail = new Texture2D(1, 1);
                                textureThumbnail.LoadImage(bytes);

                                // Add GUI button to open video
                                if (videoPopupButton != null && videoThumbnail != null)
                                {
                                    tb.RegisterTrackableEventHandler(new VideoItemTrackable(this, textureThumbnail, videoDevicePath, videoThumbnail, videoPopupButton));
                                }
                            }
                            else
                            {
                                Debug.Log("file not exists and imageTargetLineJson.download == false " + videoUrl);
                                // file not exists and imageTargetLineJson.download == false
                                tb.RegisterTrackableEventHandler(new FileOnDemandItemTrackable(this, tb, trackableType, videoThumbnailUrl, videoThumbnailDevicePath, videoUrl, videoDevicePath, videoPopupButton));
                            }
                            continue;
                        }
                        else if (trackableType == DataSetLoader.PDF_TRACKABLE_RESOURCE_TYPE)
                        {
                            var PDFUrl = imageTargetLineJson.videoThumbnailUrl;

                        }
                        else
                        {
                            continue;
                        }
                    } // foreach
                } // if 
            }
        }
    }

    /// <summary>
    /// Find an imageTarget inside the list provided by the JSON data of the product
    /// </summary>
    private AppResponseJson.ImageTargetLineJson FindImageTargetFromProduct(TrackableBehaviour tb)
    {
        AppResponseJson.ImageTargetLineJson trackObject = new AppResponseJson.ImageTargetLineJson();
        // Find the product from the json
        if (productJsonObject.imageTargets != null && productJsonObject.imageTargets.Length > 0)
        {
            foreach (var item in productJsonObject.imageTargets)
            {
                if (item.trackableName == tb.TrackableName)
                {
                    trackObject = item;
                    break;
                }
            }
        }

        return trackObject;
    }

    public void Disable()
    {
        this.enabled = false;
        PlayerPrefs.SetString(Globals.getProductIsEnabledKey(this.vuforiaResourceName), "false");

        tracker = TrackerManager.Instance.GetTracker<ObjectTracker>();

        // Destroy TrackableBehaviours from stateManager first
        StateManager stateManager = TrackerManager.Instance.GetStateManager();
        var tbList = new List<TrackableBehaviour>(stateManager.GetTrackableBehaviours());
        foreach (TrackableBehaviour tb in tbList)
        {
            if (dataSet.Contains(tb.Trackable))
            {
                stateManager.DestroyTrackableBehavioursForTrackable(tb.Trackable, true);
            }
        }

        // Destroy loaded DataSet
        tracker.DeactivateDataSet(dataSet);
        tracker.DestroyDataSet(dataSet, false);

        // Show message product was disabled
        ShowToast.Show(Localization.GetTranslations().message.productDisabled);
    }

    public void showImagePopup(Texture2D texture2D)
    {

        Debug.Log("showImagePopup");
        tracker.Stop();
        Canvas imageCanvas = GameObject.Find("ImageCanvas").GetComponent<Canvas>();
        imageCanvas.enabled = true;
        GameObject.Find("ImageCloseButton").GetComponentInChildren<Text>().text = Localization.GetTranslations().close;

        // Draw image
        //  Sprite sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 40);
        //   GameObject.Find("ImageTexturePanel").GetComponentInChildren<UnityEngine.UI.Image>().sprite = sprite;
        RawImage rim = GameObject.Find("ImageTexturePanel").GetComponent<RawImage>();
        rim.texture = (Texture) texture2D;
          // Add button listener
          Button closeButton = GameObject.Find("ImageBottomPanel").GetComponentInChildren<Button>();
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() =>
        {
            // Close button
            Debug.Log("showImagePopup ImageCloseButton");
            imageCanvas.enabled = false;
            tracker.Start();
        });
    }

    public IEnumerator showVideoPopup(string filePath, bool postActivateMainCanvas = true)
    {
        Debug.Log("showVideoPopup " + filePath);
        /*  //ScreenOrientation currentOrientation = Screen.orientation;*/
        // Screen.orientation = ScreenOrientation.Landscape;
        isVideoPlaying = true;
        //   Handheld.PlayFullScreenMovie(filePath, Color.black, FullScreenMovieControlMode.Minimal);//, FullScreenMovieScalingMode.AspectFit);
        //    Handheld.PlayFullScreenMovie(filePath, Color.black, FullScreenMovieControlMode.CancelOnInput);

        var playfile = filePath;
#if UNITY_IOS
        playfile = "file://" + filePath;
#elif UNITY_ANDROID
        playfile =  filePath;
#endif

        Debug.Log("SHOWIMAGEPOPUP : playfile:" + playfile);
        //  yield return StartCoroutine(playVideoPopup(playfile));
        //Screen.autorotateToLandscapeLeft = true;
        //Screen.orientation = ScreenOrientation.LandscapeLeft;
        mainCanvas.enabled = false;
        yield return StartCoroutine(playVideoPopup(playfile));
        yield return new WaitForSeconds(1);
        if (postActivateMainCanvas)
            mainCanvas.enabled = true;

        Screen.orientation = ScreenOrientation.Portrait;
        //playVideoPopup(playfile);        
        yield return null;

    }

    public IEnumerator playVideoPopup(string filePath)
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        yield return new WaitForSeconds(1);
        Handheld.PlayFullScreenMovie(filePath, Color.black, FullScreenMovieControlMode.Minimal);
       
        yield return null;
    }




    public void RequestLoadVideo(FileOnDemandItemTrackable fileOnDemandItemTrackable, string videoThumbnailUrl, string videoThumbnailDevicePath, string videoUrl, string videoDevicePath, TrackableBehaviour tb)
    {
#if UNITY_IOS
          Debug.Log("REQUESTLOADVIDEO : videoDevicePath :" + videoDevicePath);
          Debug.Log("REQUESTLOADVIDEO : videoUrl :" + videoUrl);
#elif UNITY_ANDROID
        // Show message file is loading
        ShowToast.Show(Localization.GetTranslations().message.fileLoading);
#endif
        StartCoroutine(isResourcedDownloaded(videoThumbnailDevicePath, videoThumbnailUrl));
        StartCoroutine(LoadVideoThumbnailAmazon(videoThumbnailUrl, videoThumbnailDevicePath, videoUrl, videoDevicePath, tb, fileOnDemandItemTrackable));
    }

    public void RequestLoadImage(FileOnDemandItemTrackable fileOnDemandItemTrackable, string imgUrl, string imgDevicePath, TrackableBehaviour tb)
    {
        // Show message file is loading
        ShowToast.Show(Localization.GetTranslations().message.fileLoading);

        StartCoroutine(isResourcedDownloaded(imgDevicePath,imgUrl));

        StartCoroutine(LoadImageAmazon(imgUrl, imgDevicePath, tb, fileOnDemandItemTrackable));
    }

    public void RequestLoadAnimation(FileOnDemandItemTrackable fileOnDemandItemTrackable, AppResponseJson.ImageTargetLineJson imageTargetLineJson, int bundleVersion, TrackableBehaviour tb)
    {
        // Show message file is loading
        ShowToast.Show(Localization.GetTranslations().message.fileLoading);

        
        StartCoroutine(SaveAndLoadAssetBundleWWW(imageTargetLineJson, bundleVersion, tb, fileOnDemandItemTrackable));
    }

    IEnumerator isResourcedDownloaded(string fpath, string resourceName)
    {
        //  ShowToast.Show(Localization.GetTranslations().message.fileLoading);
       // ShowToast.Show("Empieza Timeout para comprobar: " + fpath);
        yield return new WaitForSeconds(resourceCheckSeconds);        
        if (! System.IO.File.Exists(fpath))
        {
            string t = Localization.GetTranslations().error.failToDownloadResource + " " + resourceName;
#if UNITY_ANDROID
            ShowToast.Show(t);
#endif
            ShowSpinner.Hide();
        } 

    }


   IEnumerator LoadImageAmazon(string imgUrl, string imgDevicePath, TrackableBehaviour tb, FileOnDemandItemTrackable fileOnDemandItemTrackable)
    {
        Debug.Log("Inside of LoadImageAmazon");
        string strippedFileName;
        // Show spinner if resource is not cached
        bool isPreload = fileOnDemandItemTrackable == null;
        if (!isPreload)
            showLoadingSpinner();




        //split image name
        string[] splitFileName = imgUrl.Split(new string[] { ":" }, StringSplitOptions.None);
        if (splitFileName[0].Contains("http"))
        {
            StartCoroutine(LoadImageWWW(imgUrl, imgDevicePath, tb, fileOnDemandItemTrackable));
        }
        else
        {

            strippedFileName = splitFileName[splitFileName.Length - 1];
            string Bucket = splitFileName[0];

            Debug.Log("Image URL: " + imgUrl);
            Debug.Log("Ready to load image file:" + strippedFileName[1]);
            Debug.Log("From Bucket:" + Bucket);
            S3Client.GetObjectAsync(Bucket, strippedFileName, (responseObj) =>
            {
                var response = responseObj.Response;
                if (response.ResponseStream != null)
                {
                    byte[] data = new byte[response.ResponseStream.Length];
                    string nfp = imgDevicePath;
                    response.ResponseStream.Read(data, 0, (int)response.ResponseStream.Length);
                    System.IO.File.WriteAllBytes(nfp, data);

                // Set image bytes to texture
                Texture2D texture = new Texture2D(1, 1);
                    texture.LoadImage(data);

                // Add GUI button to open image
                if (imagePopupButton != null)
                    {
                        tb.RegisterTrackableEventHandler(new ImageItemTrackable(this, texture, imagePopupButton));
                    }

                // Hide spinner if it was being shown
                hideLoadingSpinner();
                }
                else
                {
#if UNITY_ANDROID
                    ShowToast.Show(Localization.GetTranslations().error.downloadFromAmazonFailed);
#endif
                }

            });

        }
        yield return null;
    }



    IEnumerator LoadImageWWW(string imgUrl, string imgDevicePath, TrackableBehaviour tb, FileOnDemandItemTrackable fileOnDemandItemTrackable)
    {
        // Show spinner if resource is not cached
        bool isPreload = fileOnDemandItemTrackable == null;
        if (!isPreload)
            showLoadingSpinner();

        
        // Download the file from the URL.
        using (WWW www = new WWW(imgUrl))
        {
            yield return www;
            if (www.error != null)
            {
                if (isPreload)
                {
                    ProductLoadAllShowError();
                }
                else
                {
                    Canvas warningCanvas = GameObject.Find("WarningCanvas").GetComponent<Canvas>();
                    AppStartupController.OpenWarningModal(Localization.GetTranslations().error.loadResourceFailed, Localization.GetTranslations().retry, () =>
                    {
                        // Retry button
                        warningCanvas.enabled = false;

                        StartCoroutine(LoadImageWWW(imgUrl, imgDevicePath, tb, fileOnDemandItemTrackable));
                    }, Localization.GetTranslations().ignore, () =>
                    {
                        // Ignore button
                        warningCanvas.enabled = false;

                        // Allow download to start again if triggered in the future
                        if (fileOnDemandItemTrackable != null)
                            fileOnDemandItemTrackable.isDownloading = false;
                    });
                }

                // When resource download was false
                if (!isPreload)
                {
                    // Hide spinner if it was being shown
                    hideLoadingSpinner();

                    // Show message file was downloaded
                    ShowToast.Show(Localization.GetTranslations().message.fileLoaded);
                }

                yield break;
            }

            Debug.Log("LoadImageWWW - Loaded resource from: " + imgUrl);

            // Save file
            System.IO.File.WriteAllBytes(imgDevicePath, www.bytes);
            Debug.Log("LoadImageWWW - Image was saved: " + imgDevicePath);

            // Set image bytes to texture
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(www.bytes);

            // Add GUI button to open image
            if (imagePopupButton != null)
            {
                tb.RegisterTrackableEventHandler(new ImageItemTrackable(this, texture, imagePopupButton));
            }

            // Hide spinner if it was being shown
            hideLoadingSpinner();
          // yield return null;
        } // memory is freed from the web stream (www.Dispose() gets called implicitly)
    }


    IEnumerator LoadVideoThumbnailAmazon(string videoThumbnailURL, string videoThumbnailDevicePath, string videoURL, string videoDevicePath, TrackableBehaviour tb, FileOnDemandItemTrackable fileOnDemandItemTrackable)
    {
      
        Debug.Log("Inside of LoadVideoThumbnailAmazon");
        string strippedFileName;
        // Show spinner if resource is not cached
        bool isPreload = fileOnDemandItemTrackable == null;
        if (!isPreload)
            showLoadingSpinner();


       
        //split name
        string[] splitFileName = videoThumbnailURL.Split(new string[] { ":" }, StringSplitOptions.None);
        if (splitFileName[0].Contains("http"))
        {
            StartCoroutine(LoadVideoThumbnailWWW(videoThumbnailURL, videoThumbnailDevicePath, videoURL, videoDevicePath, tb,fileOnDemandItemTrackable));
        }
        else
        {
            //split  name            
            strippedFileName = splitFileName[splitFileName.Length - 1];
            string Bucket = splitFileName[0];

            //GetFileFromAmazon(strippedFileName, videoThumbnailDevicePath);

            S3Client.GetObjectAsync(Bucket, strippedFileName, (responseObj) =>
            {
                var response = responseObj.Response;
                if (response.ResponseStream != null)
                {
                    byte[] data = new byte[response.ResponseStream.Length];
                    string nfp = videoThumbnailDevicePath;
                    response.ResponseStream.Read(data, 0, (int)response.ResponseStream.Length);
                    System.IO.File.WriteAllBytes(nfp, data);

                // Set image bytes to texture
                Texture2D textureThumbnail = new Texture2D(1, 1);
                 textureThumbnail.LoadImage(data);
                   
                // Load video after Thumbnail
                StartCoroutine(LoadVideoAmazon(textureThumbnail, videoURL, videoDevicePath, tb, fileOnDemandItemTrackable));
                }
                else
                {
#if UNITY_ANDROID
                    ShowToast.Show(Localization.GetTranslations().error.downloadFromAmazonFailed);
#endif
                }

            });
        }
            

        yield return null;
    }


IEnumerator LoadVideoThumbnailWWW(string videoThumbnailURL, string videoThumbnailDevicePath, string videoURL, string videoDevicePath, TrackableBehaviour tb, FileOnDemandItemTrackable fileOnDemandItemTrackable)
    {
        // Show spinner if resource is not cached
        bool isPreload = fileOnDemandItemTrackable == null;
        if (!isPreload)
            showLoadingSpinner();

        // Download the file from the URL.
        Debug.Log("LoadVideoThumbnailWWW " + videoThumbnailURL);
        using (WWW www = new WWW(videoThumbnailURL))
        {
            yield return www;
            if (www.error != null)
            {
                Debug.Log("LoadVideoThumbnailWWW failed " + www.error);
                if (isPreload)
                {
                    ProductLoadAllShowError();
                }
                else
                {
                    Canvas warningCanvas = GameObject.Find("WarningCanvas").GetComponent<Canvas>();
                    AppStartupController.OpenWarningModal(Localization.GetTranslations().error.loadResourceFailed, Localization.GetTranslations().retry, () =>
                    {
                        // Retry button
                        warningCanvas.enabled = false;

                        StartCoroutine(LoadVideoThumbnailWWW(videoThumbnailURL, videoThumbnailDevicePath, videoURL, videoDevicePath, tb, fileOnDemandItemTrackable));
                    }, Localization.GetTranslations().ignore, () =>
                    {
                        // Ignore button
                        warningCanvas.enabled = false;

                        // Allow download to start again if triggered in the future
                        if (fileOnDemandItemTrackable != null)
                            fileOnDemandItemTrackable.isDownloading = false;
                    });
                }

                yield break;
            }

            Debug.Log("LoadVideoThumbnailWWW - Loaded resource from: " + videoThumbnailURL);

            // Save file
            System.IO.File.WriteAllBytes(videoThumbnailDevicePath, www.bytes);
            Debug.Log("LoadVideoThumbnailWWW - Video was saved: " + videoThumbnailDevicePath);

            // Set image bytes to texture
            Texture2D textureThumbnail = new Texture2D(1, 1);
            textureThumbnail.LoadImage(www.bytes);

            showLoadingSpinner();
            // Load video after Thumbnail
            StartCoroutine(LoadVideoAmazon(textureThumbnail, videoURL, videoDevicePath, tb, fileOnDemandItemTrackable));

        } // memory is freed from the web stream (www.Dispose() gets called implicitly)
    }




    IEnumerator LoadVideoAmazon(Texture2D textureThumbnail, string videoURL, string videoDevicePath, TrackableBehaviour tb, FileOnDemandItemTrackable fileOnDemandItemTrackable)
    {

        Debug.Log("Inside of LoadVideoAmazon");
        string strippedFileName;

        // Show spinner if resource is not cached
        bool isPreload = fileOnDemandItemTrackable == null;
        if (!isPreload)
            showLoadingSpinner();
              

            Debug.Log("LoadVideoAmazon - Loaded resource from: " + videoURL);

        StartCoroutine(isResourcedDownloaded(videoDevicePath, videoURL));


        //split name
        string[] splitFileName = videoURL.Split(new string[] { ":" }, StringSplitOptions.None);
        if (splitFileName[0].Contains("http"))
        {
            StartCoroutine(LoadVideoWWW(textureThumbnail, videoURL, videoDevicePath, tb, fileOnDemandItemTrackable));
        }
        else
        {

            //split image name
            //  string[] splitFileName = videoURL.Split(new string[] { ":" }, StringSplitOptions.None);
            strippedFileName = splitFileName[splitFileName.Length - 1];
            string Bucket = splitFileName[0];

            showLoadingSpinner();
            S3Client.GetObjectAsync(Bucket, strippedFileName, (responseObj) =>
            {
                var response = responseObj.Response;
                if (response.ResponseStream != null)
                {
                    byte[] data = new byte[response.ResponseStream.Length];
                    string nfp = videoDevicePath;
                    response.ResponseStream.Read(data, 0, (int)response.ResponseStream.Length);
                    System.IO.File.WriteAllBytes(nfp, data);

                    // Add GUI button to open image
                    if (videoPopupButton != null && videoThumbnail != null)
                    {
                        tb.RegisterTrackableEventHandler(new VideoItemTrackable(this, textureThumbnail, videoDevicePath, videoThumbnail, videoPopupButton));
                    }

                    // Hide spinner if it was being shown
                    hideLoadingSpinner();

                }
                else
                {
#if UNITY_ANDROID
                    ShowToast.Show(Localization.GetTranslations().error.downloadFromAmazonFailed);
#endif
                }

            });

            Debug.Log("LoadVideoWWW - Video was saved: " + videoDevicePath);

           
            // memory is freed from the web stream (www.Dispose() gets called implicitly)
        }
        yield return null;
    }



    IEnumerator LoadVideoWWW(Texture2D textureThumbnail, string videoURL, string videoDevicePath, TrackableBehaviour tb, FileOnDemandItemTrackable fileOnDemandItemTrackable)
    {
        // Show spinner if resource is not cached
        bool isPreload = fileOnDemandItemTrackable == null;
        if (!isPreload)
            showLoadingSpinner();

        // Download the file from the URL.
        using (WWW www = new WWW(videoURL))
        {
            yield return www;
            if (www.error != null)
            {
                if (isPreload)
                {
                    ProductLoadAllShowError();
                }
                else
                {
                    Canvas warningCanvas = GameObject.Find("WarningCanvas").GetComponent<Canvas>();
                    AppStartupController.OpenWarningModal(Localization.GetTranslations().error.loadResourceFailed, Localization.GetTranslations().retry, () =>
                    {
                        // Retry button
                        warningCanvas.enabled = false;

                        StartCoroutine(LoadVideoWWW(textureThumbnail, videoURL, videoDevicePath, tb, fileOnDemandItemTrackable));
                    }, Localization.GetTranslations().ignore, () =>
                    {
                        // Ignore button
                        warningCanvas.enabled = false;

                        // Allow download to start again if triggered in the future
                        if (fileOnDemandItemTrackable != null)
                            fileOnDemandItemTrackable.isDownloading = false;
                    });
                }

                // When resource download was false
                if (!isPreload)
                {
                    // Hide spinner if it was being shown
                    hideLoadingSpinner();

                    // Show message file was downloaded
                    ShowToast.Show(Localization.GetTranslations().message.fileLoaded);
                }

                yield break;
            }

            Debug.Log("LoadVideoWWW - Loaded resource from: " + videoURL);
            
            // Save file
            System.IO.File.WriteAllBytes(videoDevicePath, www.bytes);
            Debug.Log("LoadVideoWWW - Video was saved: " + videoDevicePath);

            // Add GUI button to open image
            if (videoPopupButton != null && videoThumbnail != null)
            {
                tb.RegisterTrackableEventHandler(new VideoItemTrackable(this, textureThumbnail, videoDevicePath, videoThumbnail, videoPopupButton));
            }

            // Hide spinner if it was being shown
            hideLoadingSpinner();
        } // memory is freed from the web stream (www.Dispose() gets called implicitly)
    }
    
    IEnumerator SaveAndLoadAssetBundleWWW(AppResponseJson.ImageTargetLineJson imageTargetLineJson, int bundleVersion, TrackableBehaviour tb, FileOnDemandItemTrackable fileOnDemandItemTrackable)
    {
        Debug.Log("SaveAndLoadAssetBundleWWW - Start");
       
        string strippedFileName;
        var bundleUrl = imageTargetLineJson.bundleAndroidUrl;
#if UNITY_IPHONE
        bundleUrl = imageTargetLineJson.bundleiPhoneUrl;
#elif UNITY_ANDROID
        bundleUrl = imageTargetLineJson.bundleAndroidUrl;
#endif

        int deviceBundleVersion = PlayerPrefs.GetInt(bundleUrl);
        
        // if file.exists and file.version == bundleVersion
        var bundleFilename = System.IO.Path.GetFileName(bundleUrl);
        var devicePath = getFilePath(imageTargetLineJson.download) + "/" + bundleFilename;

        StartCoroutine(isResourcedDownloaded(devicePath, bundleUrl));

        //add resourcepath  
        localResourcesPath.Add(devicePath);

        var scale = imageTargetLineJson.scale;

        if (deviceBundleVersion == bundleVersion && System.IO.File.Exists(devicePath))
        {
            StartCoroutine(LoadAssetBundleFromDevice("file://" + devicePath, bundleVersion, scale, tb, this));
        }
        else
        {
            // Show spinner if resource is not cached
            bool isPreload = fileOnDemandItemTrackable == null;
            if (!isPreload)
                showLoadingSpinner();

     
            //split name
            string[] splitFileName = bundleUrl.Split(new string[] { ":" }, StringSplitOptions.None);
            if (splitFileName[0].Contains("http"))
            {
                ////////////// LOAD FROM AMAZON WITH DIRECT URL


                // Download the file from the URL. It will not be saved in the Cache
                using (WWW www = new WWW(bundleUrl))
                {
                    yield return www;
                    if (www.error != null)
                    {
                        if (isPreload)
                        {
                            ProductLoadAllShowError();
                        }
                        else
                        {
                            Canvas warningCanvas = GameObject.Find("WarningCanvas").GetComponent<Canvas>();
                            AppStartupController.OpenWarningModal(Localization.GetTranslations().error.loadResourceFailed, Localization.GetTranslations().retry, () =>
                            {
                                  // Retry button
                                  warningCanvas.enabled = false;

                                StartCoroutine(SaveAndLoadAssetBundleWWW(imageTargetLineJson, bundleVersion, tb, fileOnDemandItemTrackable));
                            }, Localization.GetTranslations().ignore, () =>
                            {
                                  // Ignore button
                                  warningCanvas.enabled = false;

                                  // Allow download to start again if triggered in the future
                                  if (fileOnDemandItemTrackable != null)
                                    fileOnDemandItemTrackable.isDownloading = false;
                            });
                        }

                        // Hide spinner if it was being shown
                        hideLoadingSpinner();

                        yield break;
                    }

                    // Download Bundle from Amazon

                    Debug.Log("SaveAndLoadAssetBundleWWW - Loaded resource from: " + bundleUrl);

                    // Save data into user's folder
                    System.IO.File.WriteAllBytes(devicePath, www.bytes);
                    Debug.Log("SaveAndLoadAssetBundleWWW - Asset was saved: " + devicePath);
                    PlayerPrefs.SetInt(bundleUrl, bundleVersion);
                    StartCoroutine(LoadAssetBundleFromDevice("file://" + devicePath, bundleVersion, scale, tb, this));

                }
                //////////// END LOAD FROM AMAZON
            }  else    {
                  //  string[] splitFileName = bundleUrl.Split(new string[] { ":" }, StringSplitOptions.None);
                    strippedFileName = splitFileName[splitFileName.Length - 1];
                    string Bucket = splitFileName[0];

                   Debug.Log("FILE stripped: " + strippedFileName);

                    S3Client.GetObjectAsync(Bucket, strippedFileName, (responseObj) =>
                    {
                        var response = responseObj.Response;
                        if (response.ResponseStream != null)
                        {
                            byte[] data = new byte[response.ResponseStream.Length];
                            string nfp = devicePath;
                            response.ResponseStream.Read(data, 0, (int)response.ResponseStream.Length);
                            System.IO.File.WriteAllBytes(nfp, data);

                        }
                        else
                        {
#if UNITY_ANDROID
                            ShowToast.Show(Localization.GetTranslations().error.downloadFromAmazonFailed);
#endif
                        }
                        Debug.Log("SaveAndLoadAssetBundleWWW - Loaded resource from: " + bundleUrl);

                        Debug.Log("SaveAndLoadAssetBundleWWW - Asset was saved: " + devicePath);
                        PlayerPrefs.SetInt(bundleUrl, bundleVersion);
                        StartCoroutine(LoadAssetBundleFromDevice("file://" + devicePath, bundleVersion, scale, tb, this));
                    });
                }

              

                // Update deviceBundleVersion
               

                // When resource download was false
                if (!isPreload)
                {
                    // Hide spinner if it was being shown
                    hideLoadingSpinner();

                    // Show message file was downloaded
                    ShowToast.Show(Localization.GetTranslations().message.fileLoaded);
                }
            }
            yield return null;
        }

    public static IEnumerator LoadAssetBundleFromDevice(
        string devicePath,
        int bundleVersion,
        float scale,
        TrackableBehaviour tb,
        DataSetLoader loaderInstance = null,
        Action<GameObject> callbackAction =  null) {
            // Download the file from the URL. It will not be saved in the Cache
            Debug.Log("LoadAssetBundleFromDevice - Start");
            using (WWW www = WWW.LoadFromCacheOrDownload(devicePath, bundleVersion))
            {
                yield return www;
                if (www.error != null)
                {
                    throw new Exception("WWW download had an error: " + www.error);
                }
                Debug.Log("LoadAssetBundleFromDevice - Loaded resource from: " + devicePath);

                AssetBundle bundle = www.assetBundle;
                Debug.Log("LoadAssetBundleFromDevice - bundle == null?: " + (bundle == null)); // + " mainAsset: " + bundle.mainAsset);

                //GameObject child = Instantiate(bundle.LoadAsset("firefighter")) as GameObject;
                GameObject child = null;
                Material mat = null;
                Texture tex = null;
                Avatar ava = null;
                AnimationClip ani = null;
                RuntimeAnimatorController rac = null;
                //Debug.Log("LoadAssetBundleFromDevice - bundle.mainAsset == null?: " + (bundle.mainAsset == null)); // + " mainAsset: " + bundle.mainAsset);
                foreach (UnityEngine.Object obj in bundle.LoadAllAssets())
                {
                    if ("UnityEngine.GameObject".Equals(obj.GetType().ToString()) && child == null)
                    {
                        child = Instantiate(bundle.LoadAsset<GameObject>(obj.name));                  
                    }
                    if ("UnityEngine.Material".Equals(obj.GetType().ToString()) && mat == null)
                    {
                        mat = bundle.LoadAsset<Material>(obj.name);
                    }
                    if ("UnityEngine.Texture2D".Equals(obj.GetType().ToString()) && tex == null)
                    {
                        tex = bundle.LoadAsset<Texture>(obj.name);
                    }
                    if ("UnityEngine.Avatar".Equals(obj.GetType().ToString()) && ava == null)
                    {
                        ava = obj as Avatar;
                    }
                    if ("UnityEngine.AnimationClip".Equals(obj.GetType().ToString()))
                    {
                        ani = obj as AnimationClip;
                        ani.legacy = true;
                    }
                    if ("UnityEditor.Animations.AnimatorController".Equals(obj.GetType().ToString()) && rac == null)
                    {
                        rac = obj as RuntimeAnimatorController;
                    }
                }
            if (child != null)
            {
                if (mat != null)
                {
                    // Find renderer component
                    if (child.GetComponent<Renderer>() != null)
                    {
                        child.GetComponent<Renderer>().material = mat;
                        if (tex != null)
                            child.GetComponent<Renderer>().material.SetTexture("myTexture", tex);
                    }
                    else if (child.GetComponent<MeshRenderer>() != null)
                    {
                        child.GetComponent<MeshRenderer>().material = mat;
                        if (tex != null)
                            child.GetComponent<MeshRenderer>().material.SetTexture("myTexture", tex);
                    }
                }

                if (ava != null && ani != null && child.GetComponent<Animator>() != null)
                {
                    //AvatarTarget avatarTarget = new AvatarTarget();

                    //child.GetComponent<Animator>().avatar = ava;

                    child.AddComponent<Animation>();

                    child.GetComponent<Animation>().clip = ani;
                    child.GetComponent<Animation>().AddClip(ani, ani.name);
                    child.GetComponent<Animation>().playAutomatically = true;
                    //child.GetComponent<Animation>().Play();

                    Animator animator = child.GetComponent<Animator>();

                    // Empty Runtime Animator Controller from Resources folder
                    if (rac == null)
                    {
                        rac = (RuntimeAnimatorController)RuntimeAnimatorController.Instantiate(Resources.Load("MyAnimatorController", typeof(RuntimeAnimatorController)));
                    }
                    // Do swap clip in override controller here
                    AnimatorOverrideController overrideController = new AnimatorOverrideController();
                    overrideController.runtimeAnimatorController = rac;
                    overrideController.name = "MyOverrideController";
                    overrideController["walk"] = ani;
                    foreach (AnimationClipPair animPair in overrideController.clips)
                    {
                        animPair.overrideClip = ani;
                    }

                    //overrideController["walk"]
                    animator.runtimeAnimatorController = overrideController;

                    // Save animator state
                    AnimatorStateInfo[] layerInfo = new AnimatorStateInfo[animator.layerCount];
                    for (int i = 0; i < animator.layerCount; i++)
                    {
                        Debug.Log("animator.layerCount " + i);
                        layerInfo[i] = animator.GetCurrentAnimatorStateInfo(i);
                    }

                    // Force an update
                    animator.Update(0.0f);

                    // Push back state
                    for (int i = 0; i < animator.layerCount; i++)
                    {
                        Debug.Log("push back animator.layerCount " + i);
                        animator.Play(layerInfo[i].fullPathHash, i, layerInfo[i].normalizedTime);
                    }
                }

                //child.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                child.transform.localScale = new Vector3(0.05f * scale, 0.05f * scale, 0.05f * scale);
                child.transform.localPosition = new Vector3(0, 0.005f, 0);
                child.transform.localRotation = Quaternion.identity;
                if (child.GetComponent<MeshRenderer>() != null)
                    child.GetComponent<MeshRenderer>().enabled = true;
                Destroy(child.GetComponent<BoxCollider>());

                if (loaderInstance != null)
                    loaderInstance.NewTrackableBundle(tb, child);

                if (callbackAction != null)
                    callbackAction(child);
            }

            // Unload the AssetBundles compressed contents to conserve memory
            bundle.Unload(false);

           } // memory is freed from the web stream (www.Dispose() gets called implicitly)
        }

    public void NewTrackableBundle(TrackableBehaviour tb, GameObject child)
    {
        // Add as a trackable target
        if (tb != null)
        {
            // Set position reference to parent
            child.transform.parent = tb.transform;

            child.name = "ob_" + currentGOB.ToString();
            gobs[currentGOB] = child;
            currentGOB++;
            tb.RegisterTrackableEventHandler(new AssetBundleItemTrackable(child, this));
        }
    }

    public static string getFilePath(bool isPermanent)
        {
            if (isPermanent)
                return Application.persistentDataPath + Globals.VERSION_FILES_URI;
            else
                return Application.persistentDataPath + Globals.CACHE_FILES_URI;
        }

        void ProductLoadAllShowError()
        {
            Canvas warningCanvas = GameObject.Find("WarningCanvas").GetComponent<Canvas>();
            var content = Localization.GetTranslations().error.loadAllResourcesFromProductFailed.Replace("{0}", this.productName);
            AppStartupController.OpenWarningModal(content, Localization.GetTranslations().retry, () =>
            {
                Debug.Log("---------***-------OpenConfirmModal OpenModal AcceptButton");
                warningCanvas.enabled = false;

                // Load resources
                LoadTrackableGameObjects();
            }, Localization.GetTranslations().disable, () =>
            {
                Debug.Log("---------***-------OpenConfirmModal OpenModal CancelButton");
                warningCanvas.enabled = false;

                // Disable product
                Disable();

                GameObject[] loadedDataSetControllers = GameObject.FindGameObjectsWithTag("DataSetController");
                foreach (GameObject loadedDataSetController in loadedDataSetControllers)
                {
                    if (loadedDataSetController.GetComponent<DataSetLoader>().vuforiaResourceName == this.productName)
                    {
                        // Destroy unneeded objects
                        Destroy(loadedDataSetController);
                        break;
                    }
                }
            });
        }

        public void showLoadingSpinner()
        {
            /*
#if UNITY_IPHONE
            Handheld.SetActivityIndicatorStyle(iOS.ActivityIndicatorStyle.Gray);
#elif UNITY_ANDROID
            Handheld.SetActivityIndicatorStyle(AndroidActivityIndicatorStyle.Small);
#endif

            Handheld.StartActivityIndicator();*/
                ShowSpinner.Show();
    }

    public void hideLoadingSpinner()
    {
        //Handheld.StopActivityIndicator();
        ShowSpinner.Hide();
    }

    // Find product from stored json
    public AppResponseJson.ProductLineJson getProductJsonObject(string pname)
    {
        // Retrieve data from device
        string data = PlayerPrefs.GetString(Globals.APP_JSONDATA_KEY);
        AppResponseJson json = JsonHelper.getJson<AppResponseJson>(data);
        foreach (AppResponseJson.ProductLineJson product in json.products) {
            if (product.vuforiaProductTrackableName == pname)
            {
                return product;
            }
        }

        return new AppResponseJson.ProductLineJson();
    }
   
}

public class JsonHelper
{
    public static T[] getJsonArray<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    public static T getJson<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}

public class PDFArchiveViewable
{

}

public class VideoItemTrackable : ITrackableEventHandler
{
    private DataSetLoader parentObject;
    private UnityEngine.UI.Image videoThumbnail;
    private Sprite sprite;
    private Button button;
    private UnityEngine.Events.UnityAction listener;

    public VideoItemTrackable(DataSetLoader parentObject, Texture2D textureThumbnail, string videoDevicePath, UnityEngine.UI.Image videoThumbnail, Button button)
    {
        this.parentObject = parentObject;
        this.videoThumbnail = videoThumbnail;
        this.button = button;

        // Draw image
        sprite = Sprite.Create(textureThumbnail, new Rect(0, 0, textureThumbnail.width, textureThumbnail.height), new Vector2(0.5f, 0.5f), 40);
        parentObject.videoPath = videoDevicePath;

        listener = (() => { parentObject.StartCoroutine(parentObject.showVideoPopup(videoDevicePath)); });
    }

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        var detected = (newStatus == TrackableBehaviour.Status.DETECTED || newStatus == TrackableBehaviour.Status.TRACKED);

        if (detected)
        {
            // Add button listener
            button.onClick.AddListener(listener);
            parentObject.videoPopupCounter++;
        }
        else
        {
            // Remove button listener
            button.onClick.RemoveListener(listener);
            parentObject.videoPopupCounter--;
            if (parentObject.videoPopupCounter < 0)
                parentObject.videoPopupCounter = 0;
        }

        // Show button if at least one video is detected
        videoThumbnail.enabled = (parentObject.videoPopupCounter > 0);
        videoThumbnail.GetComponentInChildren<UnityEngine.UI.Image>().sprite = sprite;
        //GameObject.Find("VideoThumbnail").GetComponentInChildren<UnityEngine.UI.Image>().sprite = sprite;
        button.GetComponent<UnityEngine.UI.Image>().enabled = (parentObject.videoPopupCounter > 0);
        button.GetComponent<UnityEngine.UI.Button>().enabled = (parentObject.videoPopupCounter > 0);
        //button.GetComponentInChildren<Text>().enabled = (parentObject.videoPopupCounter > 0);
    }
}

public class ImageItemTrackable : ITrackableEventHandler
{
    private DataSetLoader parentObject;
    private Button button;
    private UnityEngine.Events.UnityAction listener;

    public ImageItemTrackable(DataSetLoader parentObject, Texture2D texture2D, Button button)
    {
        this.parentObject = parentObject;
        this.button = button;

        listener = (() => { parentObject.showImagePopup(texture2D); });
    }

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        var detected = (newStatus == TrackableBehaviour.Status.DETECTED || newStatus == TrackableBehaviour.Status.TRACKED);

        if (detected)
        {
            // Add button listener
            button.onClick.AddListener(listener);
            parentObject.imagePopupCounter++;
        }
        else
        {
            // Remove button listener
            button.onClick.RemoveListener(listener);
            parentObject.imagePopupCounter--;
            if (parentObject.imagePopupCounter < 0)
                parentObject.imagePopupCounter = 0;
        }
        
        // Show button if at least one image is detected
        button.GetComponent<UnityEngine.UI.Image>().enabled = (parentObject.imagePopupCounter > 0);
        button.GetComponent<UnityEngine.UI.Button>().enabled = (parentObject.imagePopupCounter > 0);
        //button.GetComponentInChildren<Text>().enabled = (parentObject.imagePopupCounter > 0);
    }
}

public class FileOnDemandItemTrackable : ITrackableEventHandler
{
    private DataSetLoader parentObject;
    private TrackableBehaviour tb;
    private string type;
    private string fileThumbnailUrl;
    private string fileThumbnailDevicePath;
    private string fileUrl;
    private string fileDevicePath;
    private Button button;

    // AssetBundle fields
    private AppResponseJson.ImageTargetLineJson imageTargetLineJson;
    private int bundleVersion;
    
    public bool isDownloading = false;

    public FileOnDemandItemTrackable(DataSetLoader parentObject, TrackableBehaviour tb, string type, string fileUrl, string fileDevicePath, Button button)
    {
        this.parentObject = parentObject;
        this.tb = tb;
        this.type = type;
        this.fileUrl = fileUrl;
        this.fileDevicePath = fileDevicePath;
        this.button = button;
    }

    public FileOnDemandItemTrackable(DataSetLoader parentObject, TrackableBehaviour tb, string type, string fileThumbnailUrl, string fileThumbnailDevicePath, string fileUrl, string fileDevicePath, Button button)
    {
        this.parentObject = parentObject;
        this.tb = tb;
        this.type = type;
        this.fileThumbnailUrl = fileThumbnailUrl;
        this.fileThumbnailDevicePath = fileThumbnailDevicePath;
        this.fileUrl = fileUrl;
        this.fileDevicePath = fileDevicePath;
        this.button = button;
    }

    public FileOnDemandItemTrackable(DataSetLoader parentObject, TrackableBehaviour tb, string type, AppResponseJson.ImageTargetLineJson imageTargetLineJson, int bundleVersion)
    {
        this.parentObject = parentObject;
        this.tb = tb;
        this.type = type;
        this.imageTargetLineJson = imageTargetLineJson;
        this.bundleVersion = bundleVersion;
    }

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        var detected = (newStatus == TrackableBehaviour.Status.DETECTED || newStatus == TrackableBehaviour.Status.TRACKED);
        bool fileExists = System.IO.File.Exists(fileDevicePath);

        if (detected && !fileExists && !isDownloading)
        {
            Debug.Log("FileOnDemandItemTrackable OnTrackableStateChanged proceed to request file --------- " + fileDevicePath);
            isDownloading = true;

            // Download resource
            if (type == DataSetLoader.PICTURE_TRACKABLE_RESOURCE_TYPE)
            {
                parentObject.RequestLoadImage(this, fileUrl, fileDevicePath, tb);
            }
            else if (type == DataSetLoader.VIDEO_TRACKABLE_RESOURCE_TYPE)
            {
                parentObject.RequestLoadVideo(this, fileThumbnailUrl, fileThumbnailDevicePath, fileUrl, fileDevicePath, tb);
            }
            else if (type == DataSetLoader.ANIMATION_TRACKABLE_RESOURCE_TYPE)
            {
                parentObject.RequestLoadAnimation(this, imageTargetLineJson, bundleVersion, tb);
            }
        }
    }
}

public class AssetBundleItemTrackable :  ITrackableEventHandler
{
    private GameObject go;
    private DataSetLoader parentObject;
    
 

public AssetBundleItemTrackable(GameObject go, DataSetLoader parentObject)
    {
        this.go = go;
        this.parentObject = parentObject;   
    }

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        var active = (newStatus == TrackableBehaviour.Status.DETECTED || newStatus == TrackableBehaviour.Status.TRACKED);        
        go.SetActive(active);
     //   ShowToast.Show("3d Object Detected " + go.name);
        string[] arr = go.name.Split('_');
        this.parentObject.activeGOB = int.Parse(arr[1]);

        ///////// here is the entry point to rotate object

    }

 }

public struct FileLoading
{
    public string url;
    public bool isDownloading;
    public bool downloadError;
    public bool downloadSuccess;
}