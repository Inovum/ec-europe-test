using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OpenConfirmModal : MonoBehaviour
{

    private Canvas confirmCanvas;

    // Use this for initialization
    void Awake()
    {
        confirmCanvas = GameObject.Find("ConfirmCanvas").GetComponent<Canvas>();

        gameObject.GetComponent<Button>().onClick.AddListener(OpenModal);
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OpenModal()
    {
        Debug.Log("---------***-------OpenConfirmModal OpenModal");
        confirmCanvas.enabled = true;
    }
}