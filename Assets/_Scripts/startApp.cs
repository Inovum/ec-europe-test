using UnityEngine;
using Vuforia;
using System.Collections;

public class startApp : MonoBehaviour {
   
	// Use this for initialization
	void Start () {
        //check first time
        if (PlayerPrefs.GetInt("first_time", 0) == 0)
        {
            StartCoroutine(checkInfoFirstTime());
        }
        
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    IEnumerator checkInfoFirstTime()
    {
       // print(Time.time);
        yield return new WaitForSeconds(1);
        TrackerManager.Instance.GetTracker<ObjectTracker>().Stop();
        Canvas inf = GameObject.Find("InfoCanvas").GetComponent<Canvas>();
        inf.enabled = true;
        PlayerPrefs.SetInt("first_time", 1);
    }
}
