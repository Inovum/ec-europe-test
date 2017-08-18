using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShowToast : MonoBehaviour {

    private int seconds = 3;

    public static void Show(string toastString)
    {
        if (Application.platform == RuntimePlatform.Android)
        {
           AndroidToast toast = new AndroidToast();
            toast.showToastOnUiThread(toastString);
        }

          if (Application.platform == RuntimePlatform.IPhonePlayer)
          {
            ShowToast NewScript = new GameObject().AddComponent<ShowToast>();
            NewScript.iosToast(toastString);                        
        }

    }

    private void iosToast(string toastString)
    {
        StartCoroutine(runIOSToast(toastString));
    }

    IEnumerator runIOSToast(string toastString)
    {
        Canvas iosToast = GameObject.Find("ShowToastIOS").GetComponent<Canvas>();
        Text texto = GameObject.Find("toastText").GetComponent<Text>();
        iosToast.enabled = true;
        texto.text = toastString;

        yield return new WaitForSeconds(seconds);

        iosToast.enabled = false;
    }

    class AndroidToast {

        string toastString;
        AndroidJavaObject currentActivity;

        public void showToastOnUiThread(string toastString)
        {
            AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            this.toastString = toastString;

            currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(showToast));
        }

        private void showToast()
        {
            Debug.Log("Running on UI thread");
            AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
            AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", toastString);
            AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString, Toast.GetStatic<int>("LENGTH_SHORT"));
            toast.Call("show");
        }
    }
}
