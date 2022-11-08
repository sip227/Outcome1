#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LazuliSoftware
{
    public class DEWData : MonoBehaviour
    {
        //This class stores data for the editor window.
        [SerializeField]
        public List<DataNode> m_dataNodeList;
        [SerializeField]
        public DataNode m_startNode;

        public List<int> m_idOrder;

        public int GenUniqueID()
        {
            //This will generate a unique id compared to any node in the dataNodeList.
            int id = 0;
            while (DoesIDExist(id) == true)
            {
                id++;
            }
            return id;
        }

        bool DoesIDExist(int _id)
        {
            //Checks if id exists. Is used in GenUniqueID
            if (m_dataNodeList != null)
            {
                foreach (DataNode node in m_dataNodeList)
                {
                    if (node.ID == _id)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public DataNode GetNodeFromID(int _id)
        {
            //returns the DataNode with the ID provided if it exists.
            foreach (DataNode node in m_dataNodeList)
            {
                if (_id == node.ID)
                {
                    return node;
                }
            }
            return null;
        }

        public bool DoesNodeExist(NODETYPE _type, Vector2 _pos, string[] _textList)
        {
            //Checks if node exists by comparing data provided. Is used in loading when ID hasn't been stored.
            foreach (DataNode node in m_dataNodeList)
            {
                if (node.m_nodeType == _type && node.m_position == _pos && node.m_textList.Count == _textList.Length)
                {
                    bool diff = false;
                    for (int i = 0; i < node.m_textList.Count; i++)
                    {
                        if (node.m_textList[i] != _textList[i])
                        {
                            diff = true;
                            break;
                        }
                    }
                    if (diff == false)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool DoesNodeExist(NODETYPE _type, string[] _textList, out int _id)
        {
            //Checks if node exists by comparing data provided. Is used in loading when ID hasn't been stored.
            //This will return the id with 'out' so it can used.

            foreach (DataNode node in m_dataNodeList)
            {
                if (node.m_nodeType == _type && node.m_textList.Count == _textList.Length)
                {
                    bool diff = false;
                    for (int i = 0; i < node.m_textList.Count; i++)
                    {
                        if (node.m_textList[i] != _textList[i])
                        {
                            diff = true;
                            break;
                        }
                    }
                    if (diff == false)
                    {
                        _id = node.ID;
                        return true;
                    }
                }
            }
            _id = -1;
            return false;
        }

        [System.Serializable]
        public class DataNode
        {
            //This is the class which holds all the data for a node used in the editor window.
            //It reuses variables for different types of nodes instead of being a base for child classes.
            //This is because Serialization doesn't work well with inheritence in editor.
            public Vector2 m_position;
            public Vector2 m_size;
            public NODETYPE m_nodeType;
            public Vector2 m_oldPosition;
            public bool m_onMouse;
            public Vector2 m_mouseOffset;
            public int ID;
            public List<string> m_textList;
            public List<int> m_nextIDs;
            public int m_joiningID;
            public Vector2 m_baseArrowPos;
            public int m_baseArrowSpacing;
            public string m_name;
            public bool m_settingMode;
            public Color m_color;

            public DataNode(Vector2 _pos, int _id, NODETYPE _type)
            {
                m_position = _pos;
                m_size = Vector2.one * 75;
                ID = _id;
                m_nodeType = _type;
                m_joiningID = -1;
                m_settingMode = false;

                switch (_type)
                {
                    case NODETYPE.START:
                        Start();
                        break;
                    case NODETYPE.DIALOGUE:
                        Dialogue();
                        break;
                    case NODETYPE.CHOICE:
                        Choice();
                        break;
                    case NODETYPE.EVENT:
                        Event();
                        break;
                    default:
                        break;
                }

            }

            void Start()
            {
                m_nextIDs = new List<int>() { -1 };
                m_size = new Vector2(75, 66);
                m_name = "Start Node";
            }

            void Dialogue()
            {
                m_size = new Vector2(EditorPrefs.GetInt("DEW_WDTH_DLOG", 150), 81);
                m_textList = new List<string>() { "" };
                m_nextIDs = new List<int>() { -1 };
                m_name = "Dialogue";
            }

            void Choice()
            {
                m_textList = new List<string>() { "Yes", "No" };
                m_nextIDs = new List<int>() { -1, -1 };
                m_size = new Vector2(EditorPrefs.GetInt("DEW_WDTH_CHCE", 75), 75);
                m_baseArrowSpacing = 20;
                m_name = "Choice";
            }

            void Event()
            {
                m_textList = new List<string>() { "" };
                m_size = new Vector2(EditorPrefs.GetInt("DEW_WDTH_EVNT", 75), 66);
                m_name = "Event";
            }

            public void SetChoices(string[] _choices)
            {
                m_textList = new List<string>(_choices);
                m_nextIDs = new List<int>();
                for (int i = 0; i < _choices.Length; i++)
                {
                    m_nextIDs.Add(-1);
                    if (i > 1)
                    {
                        m_size.y += 20;
                    }
                }

            }
        }
    }

    public enum NODETYPE
    {
        START = 0,
        CHOICE,
        DIALOGUE,
        EVENT,
        NULL
    }
}
#endif