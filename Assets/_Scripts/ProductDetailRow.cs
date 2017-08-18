using UnityEngine;
using System;
using UnityEngine.UI;
using Vuforia;
using System.Collections.Generic;
using System.Collections;

public delegate void SelectedProductEvent();

public class ProductDetailRow : MonoBehaviour
{

    public event SelectedProductEvent onSelected;

    /// <summary>
    /// Keeps the information attached to this image target, loaded from the database
    /// </summary>
    [NonSerialized]
    public AppResponseJson.ImageTargetLineJson ImageTargetData;

    [NonSerialized]
    public Transform targetAnimationObject;

    [NonSerialized]
    public string vuforiaResourceName;

    // Use this for initialization
    void Start()
    {
        var rowButtons = gameObject.GetComponentsInChildren<Button>();
        var previewButton = rowButtons[0];
        var shareButton = rowButtons[1];

        previewButton.onClick.AddListener(delegate { StartCoroutine(animateTarget()); });
    }

    public IEnumerator animateTarget()
    {
        var datasets = GameObject.FindObjectsOfType<DataSetLoader>();
        DataSetLoader currentDataset = null;
        foreach (DataSetLoader ds in datasets)
            if (ds.name == vuforiaResourceName)
                currentDataset = ds;

        //IEnumerable<TrackableBehaviour> tbs = TrackerManager.Instance.GetStateManager().GetTrackableBehaviours();
        //foreach (TrackableBehaviour tb in tbs)
        //{
        //    if (tb.transform == this.targetAnimationObject)
        //    {
        //        var eventHandler = tb.gameObject.GetComponentInChildren<DefaultTrackableEventHandler>();
        //        eventHandler.OnTrackableStateChanged(TrackableBehaviour.Status.UNDEFINED, TrackableBehaviour.Status.DETECTED);

        //        foreach (Transform tbChild in tb.transform)
        //        {
        //            datasetCanvas.enabled = false;

        //            tbChild.gameObject.SetActive(true);
        //            string[] arr = tbChild.name.Split('_');
        //            currentDataset.activeGOB = int.Parse(arr[1]);
        //            currentDataset.showLoadingSpinner();

        //            break;
        //        }
        //        break;
        //    }
        //}
        var isAndroid = true;
        var trackableType = ImageTargetData.androidFileType;
        var bundleUrl = ImageTargetData.bundleAndroidUrl;

#if UNITY_IOS
        isAndroid = false;
        trackableType = ImageTargetData.iphoneFileType;
        bundleUrl = ImageTargetData.bundleiPhoneUrl;
#endif
        Canvas productDetailsCanvas = GameObject.Find("ProductDetailCanvas").GetComponent<Canvas>();
        string bundleResFilename = null;
        string bundleResDevicePath = null;

        var mTrackableBehaviour = this.targetAnimationObject.GetComponent<TrackableBehaviour>();

        if (isAndroid)
        {
            Debug.Log("android trackableType=" + trackableType);
        }
        else
        {
            Debug.Log("ios trackableType=" + trackableType);
        }

        switch (trackableType)
        {
            case DataSetLoader.ANIMATION_TRACKABLE_RESOURCE_TYPE:
                 StartCoroutine(DataSetLoader.LoadAssetBundleFromDevice(
                     bundleUrl,
                     0,
                     1f,
                     null,
                     null,
                     child => previewAssetBundle(child, ImageTargetData)));
                break;
            case DataSetLoader.VIDEO_TRACKABLE_RESOURCE_TYPE:
                bundleResFilename = System.IO.Path.GetFileName(bundleUrl);
                bundleResDevicePath = DataSetLoader.getFilePath(ImageTargetData.download) + "/" + bundleResFilename;

                Debug.Log("VIDEO_TRACKABLE_RESOURCE_TYPE bundleResFilename=" + bundleResFilename);
                Debug.Log("VIDEO_TRACKABLE_RESOURCE_TYPE bundleResDevicePath=" + bundleResDevicePath);

                //productDetailsCanvas.transform.localScale = new Vector3(0, 0, 0);
                yield return StartCoroutine(previewVideo(bundleResDevicePath));

                Debug.Log("Returned from video");
                //productDetailsCanvas.transform.localScale = new Vector3(1, 1, 1);

                break;
            case DataSetLoader.PDF_TRACKABLE_RESOURCE_TYPE:
                break;
            case DataSetLoader.PICTURE_TRACKABLE_RESOURCE_TYPE:
                bundleResFilename = System.IO.Path.GetFileName(bundleUrl);
                Debug.Log("PICTURE_TRACKABLE_RESOURCE_TYPE bundleResFilename=" + bundleResFilename);
                bundleResDevicePath = DataSetLoader.getFilePath(ImageTargetData.download) + "/" + bundleResFilename;
                Debug.Log("PICTURE_TRACKABLE_RESOURCE_TYPE bundleResDevicePath=" + bundleResDevicePath);

                var bytes = System.IO.File.ReadAllBytes(bundleResDevicePath);
                Texture2D texture = new Texture2D(1, 1);
                texture.LoadImage(bytes);

                var previewPopup = showPreviewWindowsPopup(ImageTargetData);

                var images = previewPopup.GetComponentsInChildren<UnityEngine.UI.Image>();
                foreach (var image in images)
                {
                    if (image.name == "ContentImage")
                    {
                        // Now change the height of container

                        yield return new WaitForSeconds(0.15f);
                        var backgroundContainerRect = (RectTransform)GameObject.Find("BackgroundContentPanel").transform;
                        var newHeight = (Convert.ToSingle(texture.height) / Convert.ToSingle(texture.width)) * backgroundContainerRect.rect.width + 140f;
                        Debug.Log("PICTURE_TRACKABLE_RESOURCE_TYPE newHeight=" + newHeight);
                        Debug.Log("PICTURE_TRACKABLE_RESOURCE_TYPE Screen.height=" + Screen.height);
#if UNITY_IOS
                        var yPositionDelta = (Screen.height / 2 - newHeight); // (Screen.height - newHeight);
                        Debug.Log("PICTURE_TRACKABLE_RESOURCE_TYPE yPositionDelta=" + yPositionDelta);
                        backgroundContainerRect.anchoredPosition = new Vector2(backgroundContainerRect.anchoredPosition.x, yPositionDelta);
                        backgroundContainerRect.sizeDelta = new Vector2(0, newHeight);
#else
                        var yPositionDelta = (Screen.height - newHeight);
                        Debug.Log("PICTURE_TRACKABLE_RESOURCE_TYPE yPositionDelta=" + yPositionDelta);
                        backgroundContainerRect.anchoredPosition = new Vector2(backgroundContainerRect.anchoredPosition.x, yPositionDelta);
                        backgroundContainerRect.sizeDelta = new Vector2(0, newHeight);
#endif

                        // Assign the image
                        image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0f, 0f));
                        break;
                    }
                }
                break;
        }

        //Transform obj = Instantiate(this.targetAnimationObject);
        //Animation anim = obj.GetComponentInChildren<Animation>();

        //GameObject trackableObj = GameObject.Find(this.targetAnimationObject.name);
        //if (trackableObj == null)
        //    obj = Instantiate(this.targetAnimationObject);
        //else
        //    obj = trackableObj.transform;

        //GameObject productDetailCanvas = GameObject.Find("ProductDetailCanvas");

        //obj.parent = productDetailCanvas.transform;
        //obj.gameObject.SetActive(true);
        //obj.SetAsFirstSibling();
        //anim.Play();

        //Destroy(obj.gameObject);

        //int l = ac.layers != null ? ac.layers.Length : 0;
        //var la = ac.layers[0];

        //int newState = -1;
        //var info1 = animator.GetCurrentAnimatorStateInfo(0);
        //if (newState != info1.shortNameHash)
        //{
        //    animator.Play(newState, -1, 0f);
        //    var info2 = animator.GetCurrentAnimatorStateInfo(0);
        //    if (info1.shortNameHash == info2.shortNameHash)
        //    {
        //        Debug.LogWarning("State not changed");
        //    }
        //}

        //GameObject canvasDemo = productDetailCanvas.transform.FindChild("CanvasDemo").gameObject;
        //Canvas insideCanvas = canvasDemo.GetComponentInChildren<Canvas>();

        //// Download the information
        //WWW www = new WWW("http://www.modelocontrato.net/wp-content/uploads/2009/10/periodo-de-prueba.jpg"); //TODO: This address should not be wired here... It needs a constant
        //yield return www;

        //try
        //{
        //    //// Create a texture in DXT1 format
        //    Texture2D texture = new Texture2D(www.texture.width, www.texture.height);

        //    ////// assign the downloaded image to sprite
        //    www.LoadImageIntoTexture(texture);
        //    UnityEngine.UI.Image img = insideCanvas.GetComponentInChildren<UnityEngine.UI.Image>();
        //    img.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(1f, 1f), 100f);
        //    (img.transform as RectTransform).sizeDelta = new Vector2(texture.width, texture.height);

        //}
        //catch (Exception e)
        //{
        //    Debug.LogError(e);
        //}

        //canvasDemo.SetActive(true);
        //canvasDemo.transform.SetAsLastSibling();
    }

    private void previewAssetBundle(GameObject bundleObj, AppResponseJson.ImageTargetLineJson imageTargetData)
    {
        if (bundleObj != null)
        {

            // Get the UI camera
            var uiCamera = GameObject.Find("UI Camera").GetComponent<Camera>();

            Color defaultBackgroundColor = Color.white;
             ColorUtility.TryParseHtmlString(imageTargetData.backgroundColor, out defaultBackgroundColor);
            uiCamera.backgroundColor = defaultBackgroundColor;


            var animationPreviewCanvas = GameObject.Find("CameraPreviewCanvas");

            // Change the values after the insertion on parent
            bundleObj.name = "currentBundleAnimation";
            bundleObj.transform.localPosition = new Vector3(0f, 0f, 20f);
            bundleObj.transform.localScale = new Vector3(imageTargetData.scale, imageTargetData.scale, imageTargetData.scale);
            bundleObj.transform.localRotation = Quaternion.Euler(-100f, 0, 0);
            bundleObj.SetActive(true);

            // Now, change the render mode of the current canvas to show the 3D object
            var productDetailCanvas = GameObject.Find("ProductDetailCanvas");
            productDetailCanvas.GetComponentInChildren<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
            productDetailCanvas.SetActive(false);

            animationPreviewCanvas.GetComponentInParent<Canvas>().enabled = true;
            var buttonClose = animationPreviewCanvas.GetComponentInChildren<Button>();
            // Set the event to handle the button
            buttonClose.onClick.AddListener(() =>
            {
                animationPreviewCanvas.GetComponentInParent<Canvas>().enabled = false;

                productDetailCanvas.GetComponentInChildren<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                uiCamera.depth = 0;

                DestroyImmediate(bundleObj);
                DestroyImmediate(bundleObj);

                productDetailCanvas.SetActive(true);

                Debug.Log("Click on close button");
            });

            bundleObj.transform.parent = animationPreviewCanvas.transform;
            uiCamera.depth = 1;

        }

    }

    private GameObject showPreviewWindowsPopup(AppResponseJson.ImageTargetLineJson ImageTargetJson)
    {
        var result = GameObject.Find("PicturePreviewCanvas");
        Canvas picturePreviewCanvas = result.GetComponent<Canvas>();

        //Set the background color if it is set from the database
        //if (!String.IsNullOrEmpty(ImageTargetData.backgroundColor))
        //{
        //    RawImage containerImage = picturePreviewCanvas.GetComponentInChildren<RawImage>();

        //    Color backgroundColor = containerImage.color;
        //    ColorUtility.TryParseHtmlString(ImageTargetData.backgroundColor, out backgroundColor);
        //    containerImage.color = backgroundColor;
        //}

        picturePreviewCanvas.enabled = true;

        return result;
    }

    public IEnumerator previewVideo(string deviceFilePath)
    {
        Debug.Log("previewVideo deviceFilePath=" + deviceFilePath);

        if (System.IO.File.Exists(deviceFilePath))
        {
            Debug.Log("previewVideo file exists");

#if UNITY_IOS
            deviceFilePath = "file://" + deviceFilePath;
            Debug.Log("previewVideo update for iOS deviceFilePath=" + deviceFilePath);
#endif
			// new code
			yield return StartCoroutine(playVideo(deviceFilePath));
			yield return new WaitForSeconds(.5f);

			Screen.orientation = ScreenOrientation.Portrait;    
			yield return null;

            // original code
            //Screen.orientation = ScreenOrientation.LandscapeLeft;
            //Handheld.PlayFullScreenMovie(deviceFilePath, Color.black, FullScreenMovieControlMode.Full);
            //yield return new WaitForSeconds(1);
            //Screen.orientation = ScreenOrientation.Portrait;
		} else {
            Debug.Log("previewVideo file doesn't exists");
            yield return null;
        }
    }

	public IEnumerator playVideo(string filePath)
	{
		Screen.orientation = ScreenOrientation.LandscapeLeft;
		
        yield return new WaitForSeconds(.5f);

        Handheld.PlayFullScreenMovie(filePath, Color.black, FullScreenMovieControlMode.Minimal);

		yield return null;
	}

    // Update is called once per frame
    void Update()
    {

	}

}