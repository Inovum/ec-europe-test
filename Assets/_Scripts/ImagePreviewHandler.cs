using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public delegate void OnClosePreviewPopupEvent(bool confirmStatus);

public class ImagePreviewHandler : MonoBehaviour
{
    /// <summary>
    /// Used to call an user code after the user clicks the close button
    /// </summary>
    public OnClosePreviewPopupEvent OnClosePreviewCallback;

    // Use this for initialization
    void Start()
    {
        //  Set listeners
        var previewDialog = GameObject.Find("PicturePreviewCanvas").GetComponent<Canvas>();
        var buttons = GetComponentsInChildren<Button>();
        foreach (var button in buttons)
        {
            switch (button.name.ToUpper())
            {
                case "OKBUTTON":
                    button.onClick.AddListener(delegate
                   {
                       previewDialog.enabled = false;
                       if (OnClosePreviewCallback != null)
                           OnClosePreviewCallback(true);
                   });
                    break;
                case "CANCELBUTTON":
                    button.onClick.AddListener(delegate
                    {
                        previewDialog.enabled = false;
                        if (OnClosePreviewCallback != null)
                            OnClosePreviewCallback(false);
                    });
                    break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
