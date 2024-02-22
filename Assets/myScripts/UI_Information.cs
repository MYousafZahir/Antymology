using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Information : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<TextMeshProUGUI>().fontSize = 20;
    }

    // Update is called once per frame
    void Update() {
        int antCount = GameObject.FindGameObjectsWithTag("Ant").Length + 1;
        int nestCount = GameObject.FindGameObjectWithTag("Queen").GetComponent<antLogic>().nestPositions.Count;
        
        // make ant count and nest count text
        string displayText = "ants: " + antCount + "\n" + "nest blocks: " + nestCount;
        
        // update TextMeshPro text
        GetComponent<TextMeshProUGUI>().text = displayText; 
        
    }
}
