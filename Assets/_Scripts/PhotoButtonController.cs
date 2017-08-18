using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.IO;

public class PhotoButtonController : MonoBehaviour
{
    private UnityEngine.Events.UnityAction unityAction;
    private bool prepareTakePicture, takePicture, endTakePicture = false;
    private string directory = "/sdcard/DCIM/ec-europe/";

    private Texture2D m_Texture;

    // Use this for initialization
    void Awake()
    {
        // Button translated text
        LocaleTranslationJson localeJson = Localization.GetTranslations();
        //gameObject.GetComponentInChildren<Text>().text = localeJson.takePhoto;

        // onClick event
        gameObject.GetComponent<Button>().onClick.AddListener(ButtonPressed);

        m_Texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, true);
    }

    // Update is called once per frame
    void Update()
    {
        if (prepareTakePicture)
        {
            // Hide UI
            gameObject.GetComponentInParent<Canvas>().enabled = false;
            prepareTakePicture = false;
            takePicture = true;
        }
        else if (takePicture)
        {
            // Take Picture
            StartCoroutine(TakeScreenShot());
            takePicture = false;
            endTakePicture = true;
        }
        else if (endTakePicture)
        {
            // Restore UI
            gameObject.GetComponentInParent<Canvas>().enabled = true;
            endTakePicture = false;
        }
    }

    void ButtonPressed()
    {
        Debug.Log("PhotoButtonController ButtonPressed");
#if UNITY_IOS
        ScreenshotManager.SaveScreenshot(generateFileName(480,800),"ec_europe");
        Debug.Log(" After ScreenshotManager.SaveScreenshot!");
#endif
#if UNITY_ANDROID
        prepareTakePicture = true;
#endif

    }

    private string generateFileName(int width, int height)
    {
        return string.Format("screen_{0}x{1}_{2}.png",
                              width, height,
                              System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    // Save screenshot
    public IEnumerator TakeScreenShot()
    {
        yield return new WaitForEndOfFrame();

        m_Texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, true);
        m_Texture.Apply();

        // Encode texture into PNG
        byte[] bytes = m_Texture.EncodeToPNG();

        // save in memory
        string filename = generateFileName(Convert.ToInt32(m_Texture.width), Convert.ToInt32(m_Texture.height));
        
        //string directory = "/mnt/sdcard/DCIM/ec-europe/";
       //  directory = "/sdcard/DCIM/ec-europe/";
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        
        string path = directory + filename;
        System.IO.File.WriteAllBytes(path, bytes);

#if UNITY_ANDROID && !UNITY_EDITOR
        //REFRESHING THE ANDROID PHONE PHOTO GALLERY IS BEGUN
        try
        {
            AndroidJavaClass classPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject objActivity = classPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass classUri = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject objIntent = new AndroidJavaObject("android.content.Intent", new object[2] { "android.intent.action.MEDIA_SCANNER_SCAN_FILE", classUri.CallStatic<AndroidJavaObject>("parse", "file://" + path) });
            objActivity.Call("sendBroadcast", objIntent);
        }
        catch (Exception e) {
            Debug.Log(e.Message);
        }
        //REFRESHING THE ANDROID PHONE PHOTO GALLERY IS COMPLETE
#endif


        // Show message
        ShowToast.Show(Localization.GetTranslations().message.photoSaved);
    }
}
