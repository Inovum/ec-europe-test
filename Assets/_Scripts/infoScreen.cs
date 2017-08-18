using UnityEngine;
using System.Collections;

public class infoScreen : MonoBehaviour {
    private bool isFirtsTime = false;
    public OpenInfoPage op;

	// Use this for initialization
	void Start () {
        isFirtsTime = true;
    }
	
	// Update is called once per frame
	void Update () {
        if (isFirtsTime)
        {
          //  OpenInfoPage op = info.GetComponent("OpenInfoPage") as OpenInfoPage;
            op.OpenModal();
            isFirtsTime = false;
        }
    }
}
