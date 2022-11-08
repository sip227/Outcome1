using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LazuliSoftware
{
    public class DEWDialogue : ScriptableObject
    {
        //This class is a scriptable object.
        //It holds the dialogue tree as an array of Dialogue structs (defined at the bottom of this script).

        [SerializeField] private Dialogue[] m_dialogues;
        List<Dialogue> m_dialogueList;//the list is used for creating the tree and will be empty when saved.

        [SerializeField, HideInInspector] private int m_firstID;//The id of the Dialogue struct at the start of the tree.

        public void Clear()
        {
            //clears the dialogue list. This is used after the array m_dialogues has been saved.
            if (m_dialogueList != null)
            {
                m_dialogueList.Clear();
            }
        }

        public void AddDialogue(string[] _words, int _id, int[] _nextID, string[] _choices, Vector2 _pos, Vector2 _nextPos, string[] _eventKeys)
        {
            //Function for adding dialogue struct to dilaogue list.
            if (m_dialogueList == null)
            {
                m_dialogueList = new List<Dialogue>();
            }
            m_dialogueList.Add(new Dialogue(_words, _id, _nextID, _choices, _pos, _nextPos, _eventKeys));
        }

        public void Seal()//call this once adding has ended
        {
            //This is called after the dialogue list is fully populated.
            m_dialogues = m_dialogueList.ToArray();
            m_dialogueList.Clear();
        }

        public Dialogue GetDialogue(int _id)
        {
            //Finds and returns the dialogue struct with the id given.
            foreach (Dialogue tlk in m_dialogues)
            {
                if (tlk.GetID() == _id)
                {
                    return tlk;
                }
            }
            Debug.Log("Error: SODialogue returning empty struct");
            return new Dialogue();
        }

        public Dialogue GetFirstDialogue()
        {
            //Returns the first Dialogue struct in the tree.
            return GetDialogue(m_firstID);
        }

        public void SetFirstDialogue(int _id)
        {
            m_firstID = _id;
        }

        public Dialogue[] GetAllDialogue()
        {
            //returns the whole array.
            return m_dialogues;
        }

        public bool DoesIdExistInList(int _id)
        {
            //used for populating the dialogue list to prevent double ups.
            if (m_dialogueList == null)
            {
                m_dialogueList = new List<Dialogue>();
            }
            foreach (Dialogue tlk in m_dialogueList)
            {
                if (tlk.GetID() == _id)
                {
                    return true;
                }
            }
            return false;
        }

    }

    [System.Serializable]
    public struct Dialogue
    {
        //This is the struct which nodes are converted into.
        //It holds data for dialogue, choices and events.

        public Dialogue(string[] _words, int _id, int[] _nextID, string[] _choices, Vector2 _pos, Vector2 _nextPos, string[] _eventKeys)
        {
            //constructor
            m_words = _words;
            m_ID = _id;
            m_nextID = _nextID;
            m_position = new Vector4(_pos.x, _pos.y, _nextPos.x, _nextPos.y);
            m_eventKeys = _eventKeys;
            m_choices = _choices;
        }

        [SerializeField] private string[] m_words;//strings from dialogue node.
        [HideInInspector, SerializeField] private int m_ID;//id of struct.
        [HideInInspector, SerializeField] private int[] m_nextID;//all possible next structs.
        [HideInInspector, SerializeField] private Vector4 m_position;//used for loading struct in window.
        [SerializeField] private string[] m_eventKeys;//all possible event keys.
        [HideInInspector, SerializeField] private string[] m_choices;//all choices from choice node.

        public bool MultipleNext()
        {
            //returns true if dialogue node was connected to a choice node.
            return (m_nextID != null && m_nextID.Length > 1);
        }

        public bool IsLast()
        {
            //returns true if this struct does not connect to another.
            return (m_nextID == null || m_nextID.Length == 0
                || (m_nextID.Length == 1 && m_nextID[0] == -1));
        }

        public bool HasEvent()
        {
            //returns true if this sturct has any events.
            if (m_eventKeys == null)
            { return false; }
            foreach (string str in m_eventKeys)
            {
                if (str != null && str != "")
                { return true; }
            }
            return false;
        }

        public string[] GetWords() { return m_words; }
        public int GetID() { return m_ID; }
        public int[] GetNextID() { return m_nextID; }
        public Vector2 GetPos() { return new Vector2(m_position.x, m_position.y); }
        public Vector2 GetPos2() { return new Vector2(m_position.z, m_position.w); }
        public string[] GetEventKeys() { return m_eventKeys; }
        public string[] GetChoices() { return m_choices; }
    }
}