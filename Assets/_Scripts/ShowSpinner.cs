using UnityEngine;

public class ShowSpinner {
    
    private static Canvas loadingCanvas;
    private static UnityEngine.UI.Image spinnerImage;
    private static bool show = false;
    
    static ShowSpinner()
    {
        loadingCanvas = GameObject.Find("LoadingCanvas").GetComponent<Canvas>();
        spinnerImage = loadingCanvas.GetComponentInChildren<UnityEngine.UI.Image>();
    }

    // Update is called externally
    public static void Update () {
        if (IsShowing())
        {
            spinnerImage.transform.Rotate(Vector3.back * 180 * Time.deltaTime);
        }
    }

    public static void Show()
    {
        show = true;
        loadingCanvas.enabled = true;
    }

    public static void Hide()
    {
        show = false;
        loadingCanvas.enabled = false;
    }

    public static bool IsShowing()
    {
        return show;
    }
    
}
