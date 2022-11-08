using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeText : MonoBehaviour
{
    public Text changingText;


    public GameObject changingTextTwo;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TextChange() {
        changingText.text = "2";
        changingTextTwo.GetComponent<Text>().text = "2";
    }
}
