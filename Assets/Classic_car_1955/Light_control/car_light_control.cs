using UnityEngine;
using System.Collections;

public class car_light_control : MonoBehaviour
{

    private new Light light;
    private Light interior_light;
    public bool interior_light_bool; // We set "true" in inspector, only for "interior light" gameobject

    // Use this for initialization
    void Start ()
	{
	    light = gameObject.GetComponent<Light>();  // We get "light" component from our gameobject
        interior_light = gameObject.GetComponent<Light>();  // We get "light" component from our gameobject
        light.enabled = false;   // Light always disabled, when scene start
        interior_light.enabled = false;  // Light always disabled, when scene start
    }
	
	// Update is called once per frame
	void Update () {

	    if (Input.GetKeyDown(KeyCode.Q) && interior_light_bool == false) // Headlight on/off switch
	        light.enabled = !light.enabled;


	    if (Input.GetKeyDown(KeyCode.T) && interior_light_bool == true)  // Interior light on/off switch
            interior_light.enabled = !interior_light.enabled;



    }
}
