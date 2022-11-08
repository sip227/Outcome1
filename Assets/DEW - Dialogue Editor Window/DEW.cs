#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LazuliSoftware
{
    public class DEW : EditorWindow
    {
        DEWData m_data;
        DEWDialogue m_dialogueFile;

        bool m_drawNodeMenu = false;
        Vector2 m_nodeMenuPos;
        readonly Vector2 m_nodeMenuSize = new Vector2(60, 82);

        bool m_mainPage = true;
        DEWData.DataNode[] m_settingNodes;
        Color[] m_settingColors;
        [SerializeField] int[] m_settingWidths;
        [SerializeField] bool m_settingClearStart = false;
        GUIStyle m_txtStyle;

        [SerializeField]
        public Color[] m_colorPallete;//0: header, 1: start, 2: choice, 3: dialogue, 4: event, 5: outline, 6: arrow, 7: arrowhead, 8: text 

        #region defaultColors
        readonly Color r_colHead = Color.white;
        readonly Color r_colStart = new Color(1, 0.5f, 0.5f);
        readonly Color r_colChoice = new Color(0.5f, 1, 0.5f);
        readonly Color r_colDialogue = new Color(1, 1, 0.5f);
        readonly Color r_colEvent = new Color(0.5f, 1, 1);
        readonly Color r_colOutline = Color.black;
        readonly Color r_colArrow = Color.black;
        readonly Color r_colArrowHead = Color.white;
        readonly Color r_colText = Color.black;
        #endregion

        [MenuItem("Window/DEW")]
        static void Init()
        {
            DEW window = (DEW)EditorWindow.GetWindow(typeof(DEW));
            window.Show();
            EditorStyles.textArea.wordWrap = true;
            EditorStyles.textField.wordWrap = true;
            window.minSize = new Vector2(450, 450);
        }

        private void OnEnable()
        {
            this.titleContent.text = "DEW";
            //load data from DEW Data prefab
            string m_assetRoot = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            m_assetRoot = System.IO.Directory.GetParent(m_assetRoot).ToString();
            int index = m_assetRoot.IndexOf(m_assetRoot.Contains("Assets/") ? "Assets/" : "Assets\\");
            m_assetRoot = m_assetRoot.Substring(index);
            GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(m_assetRoot + "/DEW Data.prefab", typeof(GameObject));
            m_data = go.GetComponent<DEWData>();

            m_colorPallete = new Color[] {//load settings from EditorPrefs
            StrToColor(EditorPrefs.GetString("DEW_COL_HEAD", r_colHead.ToString()), r_colHead),
            StrToColor(EditorPrefs.GetString("DEW_COL_STRT", r_colStart.ToString()), r_colStart),
            StrToColor(EditorPrefs.GetString("DEW_COL_CHCE", r_colChoice.ToString()), r_colChoice),
            StrToColor(EditorPrefs.GetString("DEW_COL_DLOG", r_colDialogue.ToString()), r_colDialogue),
            StrToColor(EditorPrefs.GetString("DEW_COL_EVNT", r_colEvent.ToString()), r_colEvent),
            StrToColor(EditorPrefs.GetString("DEW_COL_OTLN", r_colOutline.ToString()), r_colOutline),
            StrToColor(EditorPrefs.GetString("DEW_COL_ARRW", r_colArrow.ToString()), r_colArrow),
            StrToColor(EditorPrefs.GetString("DEW_COL_AHED", r_colArrowHead.ToString()), r_colArrowHead),
            StrToColor(EditorPrefs.GetString("DEW_COL_TEXT", r_colText.ToString()), r_colText)
            };
            m_settingClearStart = EditorPrefs.GetBool("DEW_BOOL_CLST", false);
            m_txtStyle = new GUIStyle();
            m_txtStyle.normal.textColor = m_colorPallete[8];

            if (m_data.m_dataNodeList == null || m_data.m_dataNodeList.Count == 0)
            {
                //If data is empty create initail Start node.
                m_data.m_dataNodeList = new List<DEWData.DataNode>();
                m_data.m_dataNodeList.Add(new DEWData.DataNode(new Vector2(5, 10), m_data.GenUniqueID(), NODETYPE.START));
                m_data.m_startNode = m_data.m_dataNodeList[0];
                m_data.m_idOrder = null;
            }
            else
            {
                //find and assign the startNode
                foreach (DEWData.DataNode node in m_data.m_dataNodeList)
                {
                    if (node.m_nodeType == NODETYPE.START)
                    {
                        m_data.m_startNode = node;
                        break;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            EditorUtility.SetDirty(m_data);
            Undo.ClearUndo(m_data);
            if (m_settingClearStart)
            {
                //Clear data on close if setting is true.
                m_data.m_dataNodeList = new List<DEWData.DataNode>();
                m_data.m_dataNodeList.Add(new DEWData.DataNode(new Vector2(5, 10), m_data.GenUniqueID(), NODETYPE.START));
                m_data.m_startNode = m_data.m_dataNodeList[0];
                m_data.m_idOrder = null;
            }
        }

        private void OnGUI()
        {
            if (m_mainPage)
            {
                MainUpdate();
                MainDraw();
            }
            else
            {
                SettingsUpdate();
                SettingsDraw();
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void MainUpdate()
        {
            if (m_data.m_idOrder == null || m_data.m_idOrder.Count == 0)
            {
                //if id order is empty, create a new one.
                m_data.m_idOrder = new List<int>();
                for (int i = 0; i < m_data.m_dataNodeList.Count; i++)
                {
                    m_data.m_idOrder.Add(m_data.m_dataNodeList[i].ID);
                }
            }

            List<DEWData.DataNode> m_nodeList = m_data.m_dataNodeList;
            bool clicked = false;
            int clickIndex = -1;
            for (int i = m_data.m_idOrder.Count -1; i > -1; i--)
            {
                //update each node in reverse order
                if (clicked)//only one node can be clicked per update.
                {
                    UpdateNode(m_data.GetNodeFromID(m_data.m_idOrder[i]), true);
                }
                else
                {
                    clicked = UpdateNode(m_data.GetNodeFromID(m_data.m_idOrder[i]), false);
                    clickIndex = i;
                }
            }

            if (clicked)
            {
                //move the clicked node to back of order so it is rendered last.
                int id = m_data.m_idOrder[clickIndex];
                m_data.m_idOrder.RemoveAt(clickIndex);
                m_data.m_idOrder.Add(id);
                GUI.FocusControl(null);
            }

            Event e = Event.current;

            if (e.button == 1 && e.type == EventType.MouseDown)
            {
                //Right click opens node menu.
                m_drawNodeMenu = true;
                m_nodeMenuPos = e.mousePosition;
            }
            else if (m_drawNodeMenu)
            {
                if (e.button == 0 && e.isMouse == true
                && PointinBox(e.mousePosition, new Rect(m_nodeMenuPos, m_nodeMenuSize)) == false)
                {
                    //close node menu if user clicks outside of it.
                    m_drawNodeMenu = false;
                }
            }
        }

        private void MainDraw()
        {
            bool load = DrawHeader();
            if (load)
            {
                //return if loading to prevent errors with data changing.
                return;
            }

            for (int i = 0; i < m_data.m_idOrder.Count; i++)
            {
                //draw each node by id order.
                DrawNode(m_data.GetNodeFromID(m_data.m_idOrder[i]));
            }

            foreach (DEWData.DataNode node in m_data.m_dataNodeList)
            {
                //Draw every arrow.
                if (node.m_nextIDs != null)
                {
                    for (int j = 0; j < node.m_nextIDs.Count; j++)
                    {
                        int nextID = node.m_nextIDs[j];
                        if (nextID != -1)
                        {
                            DrawArrow(node.m_baseArrowPos + new Vector2(0, j * node.m_baseArrowSpacing),
                                 m_data.GetNodeFromID(nextID).m_position + new Vector2(7, 8));
                        }
                        else if (j == node.m_joiningID)
                        {
                            DrawArrow(node.m_baseArrowPos + new Vector2(0, j * node.m_baseArrowSpacing),
                                Event.current.mousePosition);
                        }
                    }
                }
            }

            if (m_drawNodeMenu)
            {
                DrawNodeMenu();
            }
        }

        //Returns true if loading from file.
        bool DrawHeader()
        {
            //Draw header area in main screen.
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 50), Color.gray);
            Handles.color = Color.black;
            Handles.DrawLine(new Vector2(0, 50), new Vector2(position.width, 50));
            Handles.DrawLine(new Vector2(0, 2), new Vector2(position.width, 2));

            //object field
            m_dialogueFile = (DEWDialogue)EditorGUI.ObjectField(new Rect(15, 17, 100, 16), m_dialogueFile, typeof(DEWDialogue), false);

            if (GUI.Button(new Rect(125, 17, 50, 16), "Load"))
            {
                if (m_dialogueFile == null)
                {
                    string loadPath = EditorUtility.OpenFilePanel(
                        "Load a DEW File",
                        //EditorPrefs.GetString("DEW_LASTPATH", "Assets"),
                        "Assets",
                        "asset");

                    if (loadPath.Length != 0)
                    {
                        EditorPrefs.SetString("DEW_LASTPATH", loadPath);
                        int index = loadPath.IndexOf("/Assets");
                        if (index != -1)
                        {
                            loadPath = loadPath.Substring(index + 1);
                        }
                        m_dialogueFile = (DEWDialogue)AssetDatabase.LoadAssetAtPath(loadPath, typeof(DEWDialogue));
                    }
                }

                if (m_dialogueFile != null)
                {
                    LoadDialogue(m_dialogueFile);
                    return true;
                }
            }

            if (GUI.Button(new Rect(197, 17, 45, 16), "Save"))
            {
                if (m_dialogueFile == null)
                {
                    SaveDialogueAs();
                }
                else
                {
                    SaveDialogue(ref m_dialogueFile);
                    EditorUtility.SetDirty(m_dialogueFile);
                }
            }

            if(GUI.Button(new Rect(242, 17, 26, 16), "As"))
            {
                SaveDialogueAs();
            }

            if (GUI.Button(new Rect(290, 17, 50, 16), "Clear"))
            {
                Undo.RecordObject(m_data, "Cleared workspace");
                m_data.m_dataNodeList = new List<DEWData.DataNode>();
                m_data.m_dataNodeList.Add(new DEWData.DataNode(new Vector2(5, 10), m_data.GenUniqueID(), NODETYPE.START));
                m_data.m_startNode = m_data.m_dataNodeList[0];
                m_data.m_idOrder = null;
                return true;
            }

            Handles.DrawLine(new Vector2(353f, 8), new Vector2(353f, 42));

            if (GUI.Button(new Rect(365, 17, 65, 16), "Settings"))
            {
                m_mainPage = false;
                GoToSettings();
            }
            return false;
        }

        private void SettingsUpdate()
        {
            //Update function for Settings Page.
            if (m_settingWidths[0] < 75) { m_settingWidths[0] = 75; }
            else if (m_settingWidths[0] > 175) { m_settingWidths[0] = 175; }

            if (m_settingWidths[1] < 90) { m_settingWidths[1] = 90; }
            else if (m_settingWidths[1] > 250) { m_settingWidths[1] = 250; }

            if (m_settingWidths[2] < 75) { m_settingWidths[2] = 75; }
            else if (m_settingWidths[2] > 175) { m_settingWidths[2] = 175; }

            foreach (DEWData.DataNode node in m_settingNodes)
            {
                //update node widths with changes in settings.
                if (node.m_nodeType == NODETYPE.CHOICE)
                {
                    node.m_size.x = m_settingWidths[0];
                }
                else if (node.m_nodeType == NODETYPE.DIALOGUE)
                {
                    node.m_size.x = m_settingWidths[1];
                }
                else if (node.m_nodeType == NODETYPE.EVENT)
                {
                    node.m_size.x = m_settingWidths[2];
                }
            }
        }

        private void SettingsDraw()
        {
            //Draws Settings page.
            //Drawing Header area.
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 50), Color.gray);
            Handles.color = Color.black;
            Handles.DrawLine(new Vector2(0, 50), new Vector2(position.width, 50));
            Handles.DrawLine(new Vector2(0, 2), new Vector2(position.width, 2));

            if (GUI.Button(new Rect(25, 16, 50, 18), "Back"))
            {
                //return to main page, revert unsaved settings
                m_mainPage = true;
                m_colorPallete = new List<Color>(m_settingColors).ToArray();
                m_txtStyle.normal.textColor = m_colorPallete[8];
            }

            Color[] tempCol = new Color[9];
            EditorGUI.BeginChangeCheck();
            tempCol[0] = EditorGUI.ColorField(new Rect(10, 60, 180, 18), new GUIContent("Header Color"), m_colorPallete[0], false, false, false);
            tempCol[1] = EditorGUI.ColorField(new Rect(10, 85, 180, 18), new GUIContent("Start Color"), m_colorPallete[1], false, false, false);
            tempCol[2] = EditorGUI.ColorField(new Rect(10, 110, 180, 18), new GUIContent("Choice Color"), m_colorPallete[2], false, false, false);
            tempCol[3] = EditorGUI.ColorField(new Rect(10, 135, 180, 18), new GUIContent("Dialogue Color"), m_colorPallete[3], false, false, false);
            tempCol[4] = EditorGUI.ColorField(new Rect(10, 160, 180, 18), new GUIContent("Event Color"), m_colorPallete[4], false, false, false);
            tempCol[5] = EditorGUI.ColorField(new Rect(10, 185, 180, 18), new GUIContent("Outline Color"), m_colorPallete[5], false, false, false);
            tempCol[6] = EditorGUI.ColorField(new Rect(10, 210, 180, 18), new GUIContent("Arrow Color"), m_colorPallete[6], false, false, false);
            tempCol[7] = EditorGUI.ColorField(new Rect(10, 235, 180, 18), new GUIContent("Arrow Head Color"), m_colorPallete[7], false, false, false);
            tempCol[8] = EditorGUI.ColorField(new Rect(10, 260, 180, 18), new GUIContent("Text Color"), m_colorPallete[8], false, false, false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Changed color");
                for (int i = 0; i < tempCol.Length; i++)
                {
                    m_colorPallete[i] = tempCol[i];
                }
                m_txtStyle.normal.textColor = m_colorPallete[8];
            }

            EditorGUI.BeginChangeCheck();
            int cInt = EditorGUI.IntField(new Rect(10, 285, 180, 18), "Choice Width", m_settingWidths[0]);
            int dInt = EditorGUI.IntField(new Rect(10, 310, 180, 18), "Dialogue Width", m_settingWidths[1]);
            int eInt = EditorGUI.IntField(new Rect(10, 335, 180, 18), "Event Width", m_settingWidths[2]);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Changed Width");
                m_settingWidths[0] = cInt;
                m_settingWidths[1] = dInt;
                m_settingWidths[2] = eInt;
            }

            EditorGUI.BeginChangeCheck();
            bool tempB = EditorGUI.Toggle(new Rect(10, 360, 180, 18), "Clear On Open", m_settingClearStart);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Setting Toggle");
                m_settingClearStart = tempB;
            }

            if (GUI.Button(new Rect(10, 385, 180, 18), "Save Settings"))
            {
                SaveSettings();
            }

            if (GUI.Button(new Rect(10, 410, 180, 18), "Restore Defaults"))
            {
                Undo.RecordObject(this, "Settings Defaults");
                m_colorPallete[0] = r_colHead;
                m_colorPallete[1] = r_colStart;
                m_colorPallete[2] = r_colChoice;
                m_colorPallete[3] = r_colDialogue;
                m_colorPallete[4] = r_colEvent;
                m_colorPallete[5] = r_colOutline;
                m_colorPallete[6] = r_colArrow;
                m_colorPallete[7] = r_colArrowHead;
                m_colorPallete[8] = r_colText;

                m_txtStyle.normal.textColor = m_colorPallete[8];

                m_settingWidths[0] = 75;
                m_settingWidths[1] = 150;
                m_settingWidths[2] = 75;

                m_settingClearStart = false;
            }

            for (int i = 0; i < m_settingNodes.Length; i++)
            {
                //draw each node in settings menu
                DrawNode(m_settingNodes[i]);
            }
            DrawArrow(m_settingNodes[0].m_baseArrowPos, m_settingNodes[0].m_baseArrowPos + new Vector2(100, 0));
        }

        private void SaveSettings()
        {
            //Saves all settings to EditorPrefs.
            EditorPrefs.SetBool("DEW_BOOL_CLST", m_settingClearStart);

            EditorPrefs.SetInt("DEW_WDTH_CHCE", m_settingWidths[0]);
            EditorPrefs.SetInt("DEW_WDTH_DLOG", m_settingWidths[1]);
            EditorPrefs.SetInt("DEW_WDTH_EVNT", m_settingWidths[2]);

            EditorPrefs.SetString("DEW_COL_HEAD", m_colorPallete[0].ToString());
            EditorPrefs.SetString("DEW_COL_STRT", m_colorPallete[1].ToString());
            EditorPrefs.SetString("DEW_COL_CHCE", m_colorPallete[2].ToString());
            EditorPrefs.SetString("DEW_COL_DLOG", m_colorPallete[3].ToString());
            EditorPrefs.SetString("DEW_COL_EVNT", m_colorPallete[4].ToString());
            EditorPrefs.SetString("DEW_COL_OTLN", m_colorPallete[5].ToString());
            EditorPrefs.SetString("DEW_COL_ARRW", m_colorPallete[6].ToString());
            EditorPrefs.SetString("DEW_COL_AHED", m_colorPallete[7].ToString());
            EditorPrefs.SetString("DEW_COL_TEXT", m_colorPallete[8].ToString());

            m_settingColors = new List<Color>(m_colorPallete).ToArray();

            foreach (DEWData.DataNode node in m_data.m_dataNodeList)
            {
                if (node.m_nodeType == NODETYPE.CHOICE)
                {
                    node.m_size.x = m_settingWidths[0];
                }
                else if (node.m_nodeType == NODETYPE.DIALOGUE)
                {
                    node.m_size.x = m_settingWidths[1];
                }
                else if (node.m_nodeType == NODETYPE.EVENT)
                {
                    node.m_size.x = m_settingWidths[2];
                }
            }
        }

        private void GoToSettings()
        {
            //Creates data for settings menu.
            m_settingNodes = new DEWData.DataNode[] {
            new DEWData.DataNode(new Vector2(250, 60), -1, NODETYPE.START),
            new DEWData.DataNode(new Vector2(250, 136), -1, NODETYPE.CHOICE),
            new DEWData.DataNode(new Vector2(250, 221), -1, NODETYPE.DIALOGUE),
            new DEWData.DataNode(new Vector2(250, 312), -1, NODETYPE.EVENT)
            };

            m_settingColors = new List<Color>(m_colorPallete).ToArray();

            m_settingWidths = new int[] {
            EditorPrefs.GetInt("DEW_WDTH_CHCE", 75),
            EditorPrefs.GetInt("DEW_WDTH_DLOG", 150),
            EditorPrefs.GetInt("DEW_WDTH_EVNT", 75)
            };

            m_settingClearStart = EditorPrefs.GetBool("DEW_BOOL_CLST", false);

            foreach (DEWData.DataNode node in m_settingNodes)
            {
                node.m_settingMode = true;
            }
        }

        void AddNode(Vector2 _pos, NODETYPE _type)
        {
            //Adds node to data
            Undo.RegisterCompleteObjectUndo(m_data, "Added node");
            GUI.FocusControl(null);
            int id = m_data.GenUniqueID();
            m_data.m_dataNodeList.Add(new DEWData.DataNode(_pos, id, _type));
            m_data.m_idOrder.Add(id);
        }

        void RemoveNode(DEWData.DataNode _node)
        {
            //Removes node from data.
            Undo.RecordObject(m_data, "Removed node");
            UnconnectNodesPointingAt(_node.ID);
            m_data.m_dataNodeList.Remove(_node);
            m_data.m_idOrder.Remove(_node.ID);
        }

        void UnconnectNodesPointingAt(int _id)
        {
            //Resets any nextIDs if it is pointing at given _id.
            foreach (DEWData.DataNode node in m_data.m_dataNodeList)
            {
                if (node.m_nextIDs == null) { continue; }
                for (int i = 0; i < node.m_nextIDs.Count; i++)
                {
                    if (node.m_nextIDs[i] == _id)
                    {
                        node.m_nextIDs[i] = -1;
                    }
                }
            }
        }

        void UnconnectArrows()
        {
            //Resets any nodes that have arrows pointing at the mouse.
            foreach (DEWData.DataNode node in m_data.m_dataNodeList)
            {
                node.m_joiningID = -1;
            }
        }

        void DrawNode(DEWData.DataNode _node)
        {
            //Draws the base of every node.
            EditorGUI.DrawRect(new Rect(_node.m_position, _node.m_size), m_colorPallete[5]);//outline
            EditorGUI.DrawRect(new Rect(_node.m_position + new Vector2(1, 16), _node.m_size - new Vector2(2, 17)), GetTypeColor(_node.m_nodeType));//node
            EditorGUI.DrawRect(new Rect(_node.m_position + new Vector2(1, 1), new Vector2(_node.m_size.x - 2, 14)),
                _node.m_onMouse ? Color.Lerp(Color.black, m_colorPallete[0], 0.8f) : m_colorPallete[0]);//header
            EditorGUI.LabelField(new Rect(_node.m_position + new Vector2(2, 1), new Vector2(75, 18)), (_node.m_nodeType != NODETYPE.START ? "◯ " : "") + _node.m_name, m_txtStyle);

            switch (_node.m_nodeType)//Draws the rest of the node.
            {
                case NODETYPE.DIALOGUE:
                    DrawDialogueNode(_node);
                    break;
                case NODETYPE.START:
                    DrawStartNode(_node);
                    break;
                case NODETYPE.EVENT:
                    DrawEventNode(_node);
                    break;
                case NODETYPE.CHOICE:
                    DrawChoiceNode(_node);
                    break;
                default:
                    break;
            }

            if (_node.m_nodeType != NODETYPE.START)
            {
                if (GUI.Button(new Rect(_node.m_position + new Vector2(_node.m_size.x - 13, 1), new Vector2(12, 14)), "X", m_txtStyle))
                {
                    //Draws the close button on the nodes.
                    if (_node.m_settingMode) { return; }
                    RemoveNode(_node);
                }
            }
        }

        void DrawDialogueNode(DEWData.DataNode _node)
        {
            //Draws the dialogue node
            for (int i = 0; i < _node.m_textList.Count; i++)
            {
                EditorGUI.BeginChangeCheck();
                string txt = EditorGUI.TextArea(new Rect(_node.m_position + new Vector2(5, 20 + (i * 60)),
                    new Vector2(_node.m_size.x - 21, 56)), _node.m_textList[i]);
                if (EditorGUI.EndChangeCheck())
                {
                    if (_node.m_settingMode) { continue; }
                    Undo.RecordObject(m_data, "Dialogue text");
                    _node.m_textList[i] = txt;
                }
            }

            if (GUI.Button(new Rect(_node.m_position + new Vector2(_node.m_size.x - 17, 65), new Vector2(16, 12)), " +", m_txtStyle))
            {
                if (_node.m_textList.Count < 8 && _node.m_settingMode == false)
                {
                    Undo.RecordObject(m_data, "Added dialogue box");
                    _node.m_textList.Add("");
                    _node.m_size.y += 61;
                }
            }
            else if (GUI.Button(new Rect(_node.m_position + new Vector2(_node.m_size.x - 16, 52), new Vector2(15, 12)), " –", m_txtStyle))
            {
                if (_node.m_textList.Count > 1)
                {
                    Undo.RecordObject(m_data, "Removed dialogue box");
                    _node.m_textList.RemoveAt(_node.m_textList.Count - 1);
                    _node.m_size.y -= 61;
                }
            }

            _node.m_baseArrowPos = _node.m_position + new Vector2(_node.m_size.x - 4, 29);
            DrawJoiner(_node, 0, _node.m_position + new Vector2(_node.m_size.x - 13, 21));
        }

        void DrawStartNode(DEWData.DataNode _node)
        {
            //Draws the start node.
            EditorGUI.LabelField(new Rect(_node.m_position + new Vector2(7, 31), new Vector2(40, 16)), "Start", m_txtStyle);

            _node.m_baseArrowPos = _node.m_position + new Vector2(_node.m_size.x - 4, 38);
            DrawJoiner(_node, 0, _node.m_position + new Vector2(_node.m_size.x - 13, 30));
        }

        void DrawEventNode(DEWData.DataNode _node)
        {
            //Draws the event node.
            EditorGUI.LabelField(new Rect(_node.m_position + new Vector2(7, 22), new Vector2(65, 16)), "Event Key", m_txtStyle);

            EditorGUI.BeginChangeCheck();
            string txt = EditorGUI.TextField(new Rect(_node.m_position + new Vector2(5, 40), new Vector2(_node.m_size.x - 10, 16)), _node.m_textList[0]);
            if (EditorGUI.EndChangeCheck())
            {
                if (_node.m_settingMode) { return; }
                Undo.RecordObject(m_data, "Event key text");
                _node.m_textList[0] = txt;
            }
        }

        void DrawChoiceNode(DEWData.DataNode _node)
        {
            //Draws the choice node.
            for (int i = 0; i < _node.m_textList.Count; i++)
            {
                EditorGUI.BeginChangeCheck();
                string txt = EditorGUI.TextField(new Rect(_node.m_position + new Vector2(5, 21 + (i * 20)),
                    new Vector2(_node.m_size.x - 21, 16)), _node.m_textList[i]);
                if (EditorGUI.EndChangeCheck())
                {
                    if (_node.m_settingMode) { continue; }
                    Undo.RecordObject(m_data, "Choice node text");
                    _node.m_textList[i] = txt;
                }
            }

            for (int i = 0; i < _node.m_textList.Count; i++)
            {
                DrawJoiner(_node, i, _node.m_position + new Vector2(_node.m_size.x - 13, 21 + (i * 20)));
            }
            _node.m_baseArrowPos = _node.m_position + new Vector2(_node.m_size.x - 4, 29);

            if (GUI.Button(new Rect(_node.m_position + new Vector2(_node.m_size.x - 13, _node.m_size.y - 16),
                new Vector2(12, 14)), "+", m_txtStyle))
            {
                if (_node.m_textList.Count < 16 && _node.m_settingMode == false)
                {
                    Undo.RecordObject(m_data, "Added choice");
                    _node.m_textList.Add("CHOICE");
                    _node.m_nextIDs.Add(-1);
                    _node.m_size.y += 20;
                }
            }
            else if (GUI.Button(new Rect(_node.m_position + new Vector2(_node.m_size.x - 26, _node.m_size.y - 16),
                new Vector2(12, 14)), "–", m_txtStyle))
            {
                if (_node.m_textList.Count > 2)
                {
                    Undo.RecordObject(m_data, "Removed choice");
                    _node.m_textList.RemoveAt(_node.m_textList.Count - 1);
                    _node.m_nextIDs.RemoveAt(_node.m_nextIDs.Count - 1);
                    _node.m_size.y -= 20;
                }
            }
        }

        public void DrawArrow(Vector2 _start, Vector2 _end)
        {
            //Draws arrow between two points using the handles class
            Vector2 dir = (_end - _start).normalized;
            Vector2 perp = Vector2.Perpendicular(dir);

            Handles.color = m_colorPallete[6];
            Handles.DrawLine(_start, _end - dir * 10);

            Handles.color = m_colorPallete[7];
            for (int i = 9; i > 0; i--)
            {
                Vector2 bottom = _end - dir * i;
                Vector2 perpHalf = perp * Mathf.Ceil(i / 2);
                Handles.DrawLine(bottom + perpHalf, bottom - perpHalf);
            }

            Handles.color = m_colorPallete[6];
            Vector2 a = _end - dir * 10 + perp * 5;
            Vector2 b = _end - dir * 10 - perp * 5;
            Handles.DrawPolyLine(new Vector3[] { a, b, _end, a });
        }

        void DrawJoiner(DEWData.DataNode _node, int _index, Vector2 _pos)
        {
            //Draws the joiner button used on nodes.
            if (GUI.Button(new Rect(_pos, new Vector2(12, 12)), _node.m_nextIDs[_index] == -1 ? "▷" : "▶", m_txtStyle))
            {
                if (_node.m_settingMode) { return; }
                UnconnectArrows();
                _node.m_joiningID = _index;
            }
            if (_node.m_joiningID == _index)
            {
                _node.m_nextIDs[_index] = -1;
            }
        }

        void DrawNodeMenu()
        {
            //Draws the menu that appears from right clicks.
            EditorGUI.DrawRect(new Rect(m_nodeMenuPos, m_nodeMenuSize), Color.black);
            Vector2 blockSize = new Vector2(m_nodeMenuSize.x - 2, 20);
            EditorGUI.DrawRect(new Rect(m_nodeMenuPos + new Vector2(1, 1), new Vector2(m_nodeMenuSize.x - 2, 16)), Color.white);
            EditorGUI.DrawRect(new Rect(m_nodeMenuPos + new Vector2(1, 19), blockSize), Color.white);
            EditorGUI.DrawRect(new Rect(m_nodeMenuPos + new Vector2(1, 40), blockSize), Color.white);
            EditorGUI.DrawRect(new Rect(m_nodeMenuPos + new Vector2(1, 61), blockSize), Color.white);
            EditorGUI.LabelField(new Rect(m_nodeMenuPos + new Vector2(1, 1), blockSize), "Create...");

            Vector2 mouse = Event.current.mousePosition;
            if (PointinBox(mouse, new Rect(m_nodeMenuPos, m_nodeMenuSize)))
            {
                for (int i = 0; i < 3; i++)
                {
                    //highlights the button the mouse is hovering over in the menu
                    Rect check = new Rect(m_nodeMenuPos + new Vector2(1, 19 + (21 * i)), blockSize);
                    if (PointinBox(mouse, check))
                    {
                        EditorGUI.DrawRect(check, Color.yellow);
                        break;
                    }
                }
            }

            if (GUI.Button(new Rect(m_nodeMenuPos + new Vector2(1, 19), new Vector2(m_nodeMenuSize.x - 2, 20)), "", GUIStyle.none))
            {
                AddNode(m_nodeMenuPos, NODETYPE.DIALOGUE);
                m_drawNodeMenu = false;
            }
            else if (GUI.Button(new Rect(m_nodeMenuPos + new Vector2(1, 40), new Vector2(m_nodeMenuSize.x - 2, 20)), "", GUIStyle.none))
            {
                AddNode(m_nodeMenuPos, NODETYPE.CHOICE);
                m_drawNodeMenu = false;
            }
            else if (GUI.Button(new Rect(m_nodeMenuPos + new Vector2(1, 61), new Vector2(m_nodeMenuSize.x - 2, 20)), "", GUIStyle.none))
            {
                AddNode(m_nodeMenuPos, NODETYPE.EVENT);
                m_drawNodeMenu = false;
            }

            GUI.Label(new Rect(m_nodeMenuPos + new Vector2(3, 22), new Vector2(m_nodeMenuSize.x - 2, 16)), "Dialogue", GUIStyle.none);
            GUI.Label(new Rect(m_nodeMenuPos + new Vector2(3, 43), new Vector2(m_nodeMenuSize.x - 2, 16)), "Choice", GUIStyle.none);
            GUI.Label(new Rect(m_nodeMenuPos + new Vector2(3, 64), new Vector2(m_nodeMenuSize.x - 2, 16)), "Event", GUIStyle.none);
        }

        bool UpdateNode(DEWData.DataNode _node, bool _skipMove)
        {
            //Updates the nodes, returns true if the node was clicked for ordering purposes.
            bool clicked = false;
            Event e = Event.current;
            if (_skipMove == false)
            {
                if (e.type == EventType.MouseDown && e.button == 0
                && PointinBox(e.mousePosition, new Rect(_node.m_position, _node.m_size)))
                {
                    clicked = true;
                }

                if (PointinBox(e.mousePosition, new Rect(_node.m_position, new Vector2(_node.m_size.x, 15)))
                && e.type == EventType.MouseDown && e.button == 0)
                {
                    //connect node to mouse to move it.
                    _node.m_onMouse = true;
                    _node.m_oldPosition = _node.m_position;
                    _node.m_mouseOffset = _node.m_position - e.mousePosition;
                }

                if (_node.m_onMouse)
                {
                    if (e.type == EventType.MouseUp && e.button == 0)
                    {
                        //removes node from mouse.
                        _node.m_onMouse = false;
                        _node.m_position = _node.m_oldPosition;
                        Undo.RecordObject(m_data, "Moved Node");
                    }
                    _node.m_position = e.mousePosition + _node.m_mouseOffset;
                }
            }

            if (_node.m_nextIDs != null)
            {
                for (int i = 0; i < _node.m_nextIDs.Count; i++)
                {
                    //Updates each joiner on the node.
                    if (_node.m_joiningID == i)
                    {
                        UpdateJoiner(_node, i);
                    }
                }
            }

            StayInCanvas(_node);
            return clicked;
        }

        void UpdateJoiner(DEWData.DataNode _node, int _nextIdIndex)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                if (e.button == 0)
                {
                    Vector2 mouse = e.mousePosition;
                    for (int i = 0; i < m_data.m_dataNodeList.Count; i++)
                    {
                        DEWData.DataNode dataNode = m_data.m_dataNodeList[i];
                        if (_node.ID == dataNode.ID)
                        { continue; }

                        if (JointAllowed(_node.m_nodeType, dataNode.m_nodeType)
                            && PointinBox(e.mousePosition, new Rect(dataNode.m_position, new Vector2(16, 16))))
                        {
                            //connects joiner to another node if clicked on.
                            _node.m_joiningID = -1;
                            Undo.RecordObject(m_data, "Connected Node");
                            _node.m_nextIDs[_nextIdIndex] = dataNode.ID;
                            break;
                        }
                    }
                }
                UnconnectArrows();
            }
        }

        private bool JointAllowed(NODETYPE _fromType, NODETYPE _nodeType)
        {
            //determines if a node can connect to another one.
            if (_fromType == NODETYPE.START)
            {
                return _nodeType == NODETYPE.DIALOGUE;
            }
            else if (_fromType == NODETYPE.CHOICE)
            {
                return _nodeType == NODETYPE.DIALOGUE
                    || _nodeType == NODETYPE.EVENT;
            }
            else if (_fromType == NODETYPE.DIALOGUE)
            {
                return _nodeType == NODETYPE.CHOICE
                    || _nodeType == NODETYPE.DIALOGUE
                    || _nodeType == NODETYPE.EVENT;
            }
            else
            {
                return false;
            }
        }

        void StayInCanvas(DEWData.DataNode _node)
        {
            //keeps nodes in borders of the window.
            if (_node.m_position.x < 5)
            {
                _node.m_position.x = 5;
            }
            else if (_node.m_position.x > position.width - 15)
            {
                _node.m_position.x = position.width - 15;
            }

            if (_node.m_position.y < 55)
            {
                _node.m_position.y = 55;
            }
            else if (_node.m_position.y > position.height - 15)
            {
                _node.m_position.y = position.height - 15;
            }
        }

        bool PointinBox(Vector2 _p, Rect _box)
        {
            //function for doing point in rect calculation.
            return (_p.x > _box.x && _p.x < _box.x + _box.width
            && _p.y > _box.y && _p.y < _box.y + _box.height);
        }

        void LoadDialogue(DEWDialogue _file)
        {
            //Loads nodes from a DEWDialogue file and joins them together.
            if (_file.GetAllDialogue().Length == 0)
            {//file empty
                Debug.Log("DEW cannot open empty file: " + _file.name);
                return;
            }

            Undo.RecordObject(m_data, "Loaded file");
            RemoveAllNodes();
            m_data.m_dataNodeList = new List<DEWData.DataNode>();

            List<DEWData.DataNode> dNodeList = new List<DEWData.DataNode>();
            List<DEWData.DataNode> cNodeList = new List<DEWData.DataNode>();
            List<DEWData.DataNode> eNodeList = new List<DEWData.DataNode>();

            Dialogue[] allTalks = _file.GetAllDialogue();
            float dWidth = 0;
            for (int i = 0; i < allTalks.Length; i++)
            {
                //Load all Dialogue nodes.
                DEWData.DataNode dNode = new DEWData.DataNode(allTalks[i].GetPos(), allTalks[i].GetID(), NODETYPE.DIALOGUE);
                dNode.m_textList = new List<string>(allTalks[i].GetWords());
                dNode.m_size.y += 61 * (dNode.m_textList.Count - 1);
                m_data.m_dataNodeList.Add(dNode);
                dNodeList.Add(dNode);
                dWidth = dNode.m_size.x;
            }

            //Create StartNode.
            DEWData.DataNode sNode = new DEWData.DataNode(new Vector2(5, 55), m_data.GenUniqueID(), NODETYPE.START);
            m_data.m_dataNodeList.Add(sNode);
            m_data.m_startNode = sNode;

            //Creates choice and event nodes.
            for (int i = 0; i < allTalks.Length; i++)
            {
                if (allTalks[i].MultipleNext())
                {
                    if (m_data.DoesNodeExist(NODETYPE.CHOICE, allTalks[i].GetPos2(), allTalks[i].GetChoices()))
                    { continue; }

                    //create Choice Node.
                    DEWData.DataNode cNode = new DEWData.DataNode(allTalks[i].GetPos2(), m_data.GenUniqueID(), NODETYPE.CHOICE);
                    cNode.SetChoices(allTalks[i].GetChoices());
                    m_data.m_dataNodeList.Add(cNode);
                    cNodeList.Add(cNode);

                    if (allTalks[i].HasEvent())
                    {
                        string[] keys = allTalks[i].GetEventKeys();//there can be multiple events from a choice node
                        for (int e = 0; e < keys.Length; e++)
                        {
                            if (allTalks[i].GetEventKeys()[e] == "")
                            {
                                continue;
                            }
                            else if (m_data.DoesNodeExist(NODETYPE.EVENT, new string[] { keys[e] }, out int outID))
                            {
                                cNode.m_nextIDs[e] = outID;
                                continue;
                            }

                            //Create and connect Event Node.
                            Vector2 pos = cNode.m_position + new Vector2(cNode.m_size.x + 10, e * 76);
                            DEWData.DataNode eNode = new DEWData.DataNode(pos, m_data.GenUniqueID(), NODETYPE.EVENT);
                            eNode.m_textList[0] = keys[e];
                            m_data.m_dataNodeList.Add(eNode);
                            eNodeList.Add(eNode);
                            cNode.m_nextIDs[e] = eNode.ID;
                        }
                    }
                }
                else if (allTalks[i].HasEvent())
                {
                    Vector2 pos = allTalks[i].GetPos() + new Vector2(dWidth + 10, 0);
                    if (m_data.DoesNodeExist(NODETYPE.EVENT, new string[] { allTalks[i].GetEventKeys()[0] }, out int outID))
                    {
                        dNodeList[i].m_nextIDs[0] = outID;
                        continue;
                    }

                    //Create and connect Event node.
                    DEWData.DataNode eNode = new DEWData.DataNode(pos, m_data.GenUniqueID(), NODETYPE.EVENT);
                    eNode.m_textList = new List<string>() { allTalks[i].GetEventKeys()[0] };
                    m_data.m_dataNodeList.Add(eNode);
                    eNodeList.Add(eNode);
                    dNodeList[i].m_nextIDs[0] = eNode.ID;
                }
            }
            //all nodes loaded 

            sNode.m_nextIDs[0] = _file.GetFirstDialogue().GetID();

            //Join all nodes.
            for (int i = 0; i < allTalks.Length; i++)
            {
                if (allTalks[i].IsLast())
                { continue; }

                if (allTalks[i].MultipleNext())
                {
                    //Confirm choicenode exists.
                    int choiceLength = allTalks[i].GetChoices().Length - 1;
                    DEWData.DataNode cNode = cNodeList.Find(x => x.m_position == allTalks[i].GetPos2() &&
                    x.m_textList[x.m_textList.Count - 1] == allTalks[i].GetChoices()[choiceLength]);
                    if (cNode == null)
                    {
                        continue;
                    }

                    //Join dialogue node to choice node.
                    dNodeList[i].m_nextIDs[0] = cNode.ID;

                    //join choice node to dialogue nodes.
                    for (int j = 0; j < allTalks[i].GetNextID().Length; j++)
                    {
                        foreach (DEWData.DataNode dNode in dNodeList)
                        {
                            if (dNode.ID == allTalks[i].GetNextID()[j])
                            {
                                cNode.m_nextIDs[j] = dNode.ID;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    //connect Dialogue node to dialogue node.
                    foreach (DEWData.DataNode dNode in dNodeList)
                    {
                        if (dNode.ID == allTalks[i].GetNextID()[0])
                        {
                            dNodeList[i].m_nextIDs[0] = dNode.ID;
                            break;
                        }
                    }
                }
            }

        }

        void SaveDialogue(ref DEWDialogue _file)
        {
            //Function to save dialogue to DEWDialogue file.
            _file.Clear();
            DEWData.DataNode surfNode = m_data.GetNodeFromID(m_data.m_startNode.m_nextIDs[0]);
            if (surfNode == null)
            {
                //Will not save empty dialogue tree.
                return;
            }

            _file.SetFirstDialogue(surfNode.ID);
            int[] nextID = new int[] { -1 };
            Vector2 nextPos = Vector2.zero;
            string[] eventKeys = null;
            string[] choices = null;

            List<DEWData.DataNode> dNodeList = new List<DEWData.DataNode>() { surfNode };
            while (true)
            {
                for (int i = 0; i < dNodeList.Count; i++)
                {
                    if (_file.DoesIdExistInList(dNodeList[i].ID) == false)
                    {
                        surfNode = dNodeList[i];
                        break;
                    }
                    else
                    {
                        dNodeList.RemoveAt(i);
                        i--;
                    }
                }

                if (dNodeList.Count == 0)
                {
                    //Exit loop when all dialogue nodes are accounted for.
                    break;
                }

                DEWData.DataNode IDNode = m_data.GetNodeFromID(surfNode.m_nextIDs[0]);
                if (IDNode == null)
                {
                    nextID = new int[] { -1 };
                }
                else if (IDNode.m_nodeType == NODETYPE.DIALOGUE)
                {
                    nextID = new int[] { IDNode.ID };
                    dNodeList.Add(IDNode);
                }
                else if (IDNode.m_nodeType == NODETYPE.EVENT)
                {
                    nextID = new int[] { -1 };
                    eventKeys = new string[] { IDNode.m_textList[0] };
                }
                else//next is CHOICENODE
                {
                    List<int> intList = new List<int>();
                    List<string> eventList = new List<string>();
                    bool hasEvent = false;

                    DEWData.DataNode nextNode;
                    for (int i = 0; i < IDNode.m_nextIDs.Count; i++)
                    {
                        nextNode = m_data.GetNodeFromID(IDNode.m_nextIDs[i]);
                        if (nextNode == null)
                        {
                            intList.Add(-1);
                            eventList.Add("");
                        }
                        else if (nextNode.m_nodeType == NODETYPE.DIALOGUE)
                        {
                            intList.Add(nextNode.ID);
                            dNodeList.Add(nextNode);
                            eventList.Add("");
                        }
                        else if (nextNode.m_nodeType == NODETYPE.EVENT)
                        {
                            intList.Add(-1);
                            eventList.Add(nextNode.m_textList[0]);
                            hasEvent = true;
                        }
                    }

                    choices = IDNode.m_textList.ToArray();
                    nextID = intList.ToArray();
                    eventKeys = hasEvent ? eventList.ToArray() : null;
                    nextPos = IDNode.m_position;
                }

                _file.AddDialogue(surfNode.m_textList.ToArray(), surfNode.ID, nextID, choices, surfNode.m_position, nextPos, eventKeys);
                dNodeList.Remove(surfNode);

                nextPos = Vector2.zero;
                eventKeys = null;
                choices = null;
            }
            _file.Seal();
        }

        void SaveDialogueAs()
        {
            //open filepicker if no file is on object field
            DEWDialogue tempfile = (DEWDialogue)CreateInstance(typeof(DEWDialogue));
            string path = EditorUtility.SaveFilePanelInProject(
                "Save DEW Dialogue File",
                "",
                "asset",
                "Enter a file name to save the DEW Dialogue to",
                EditorPrefs.GetString("DEW_LASTPATH", "Assets"));

            if (path.Length != 0)
            {
                EditorPrefs.SetString("DEW_LASTPATH", path);
                SaveDialogue(ref tempfile);
                AssetDatabase.CreateAsset(tempfile, path);
                EditorUtility.SetDirty(tempfile);
                m_dialogueFile = tempfile;
            }
        }

        void RemoveAllNodes()
        {
            //clears nodes in data for loading.
            foreach (DEWData.DataNode node in m_data.m_dataNodeList)
            {
                node.m_nextIDs = null;
                node.m_joiningID = -1;
            }
            m_data.m_dataNodeList.Clear();
            m_data.m_idOrder = null;
        }

        static public Color StrToColor(string _str, Color _default)
        {
            //used to get colors that are saved as strings in EditorPrefs.
            try
            {
                float r = float.Parse(_str.Substring(5, 5));
                float g = float.Parse(_str.Substring(12, 5));
                float b = float.Parse(_str.Substring(19, 5));
                return new Color(r, g, b);
            }
            catch
            {
                return _default;
            }
        }

        Color GetTypeColor(NODETYPE _type)
        {
            //returns color of node for function DrawNode.
            switch (_type)
            {
                case NODETYPE.START:
                    return m_colorPallete[1];
                case NODETYPE.CHOICE:
                    return m_colorPallete[2];
                case NODETYPE.DIALOGUE:
                    return m_colorPallete[3];
                case NODETYPE.EVENT:
                    return m_colorPallete[4];
                default:
                    return Color.white;
            }
        }
    }
}
#endif