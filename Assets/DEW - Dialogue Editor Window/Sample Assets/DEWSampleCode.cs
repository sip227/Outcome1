using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LazuliSoftware
{
    public class DEWSampleCode : MonoBehaviour
    {
        //This class shows how to read & use the data stored in a DEWDialogue file
        //It will use the Unity Editor console to go through the dialogue tree.

        [System.Serializable]
        public struct DEWEvent
        {
            //This struct is used to connect the key to an event in the editor.
            public string m_key;
            public UnityEvent m_event;
        }

        public DEWDialogue _dew;
        public DEWEvent[] m_eventArray;

        void Start()
        {
            if (_dew != null)
            {
                StartCoroutine(ParseDEW(_dew));
            }
        }

        IEnumerator ParseDEW(DEWDialogue _dialogue)
        {
            //This Coroutine will loop through the dialogue tree. 

            Dialogue current = _dialogue.GetFirstDialogue();//assign the first Dialogue struct to a variable
            Debug.Log("Dialogue tree start");
            while (true)
            {
                foreach (string str in current.GetWords())//looping through the current dialogue node
                {
                    Debug.Log(str);//write string

                    while (Input.anyKeyDown == false)//wait for input
                    { yield return null; }
                    yield return null;
                }

                if (current.IsLast())//the dialogue struct does not point to any more dialogue structs
                {
                    if (current.HasEvent())//dialogue node is connected to event node
                    {
                        //trigger this event //current.GetEventKeys()[0];
                        TriggerEvent(current.GetEventKeys()[0]);
                    }
                    break;//end of while loop
                }
                else if (current.MultipleNext())//dialogue node is connected to choice node
                {
                    //diaplay choices to player
                    DrawChoices(current.GetChoices());

                    //wait until choice has been selected, assign it to choiceIndex
                    int choiceIndex = -1;
                    while (choiceIndex == -1)
                    {
                        for (int i = 0; i < current.GetChoices().Length; i++)
                        {
                            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                            {
                                choiceIndex = i;
                                Debug.Log(choiceIndex);
                                break;
                            }
                        }
                        yield return null;
                    }

                    int nextID = current.GetNextID()[choiceIndex];
                    if (nextID == -1)//similar to isLast, Doesnt connect to another dialogue struct. This is done so the tree can end on a choice node.
                    {
                        if (current.HasEvent() && current.GetEventKeys()[choiceIndex] != "")
                        {
                            //trigger this event//current.GetEventKeys()[choiceIndex];
                            TriggerEvent(current.GetEventKeys()[choiceIndex]);
                        }
                        break;//end of while loop
                    }

                    current = _dialogue.GetDialogue(current.GetNextID()[choiceIndex]);//reassign the Dialogue struct variable and continue the loop
                }
                else //dialogue node is connected to dialogue node
                {
                    current = _dialogue.GetDialogue(current.GetNextID()[0]);//reassign the Dialogue struct variable and continue the loop.
                }
                yield return null;
            }
            Debug.Log("dialogue tree has ended");
        }

        void DrawChoices(string[] _choices)
        {
            //This function just writes the choices in the Editor console
            string str = "";
            for (int i = 0; i < _choices.Length; i++)
            {
                str += (i + ": " + _choices[i] + ", ");
            }

            Debug.Log(str);
        }

        void TriggerEvent(string _key)
        {
            //This function looks throught the event array and calls the event that has the event key.
            for (int i = 0; i < m_eventArray.Length; i++)
            {
                if (_key == m_eventArray[i].m_key)
                {
                    m_eventArray[i].m_event.Invoke();
                }
            }
        }

        public void NoQuestionEvent()
        {
            //Place holder event.
            Debug.Log("end of cont event");
        }

    }
}