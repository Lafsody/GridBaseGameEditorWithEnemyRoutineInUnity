﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

enum EditOption {Null, Add, Delete, Select}

public class GridEditorWindow : EditorWindow
{
	public  static bool isEnabled;
	private static EditOption selected;

	Rect headerSection;
	Rect bodySection;
	Rect modeSection;
	Rect infoSection;

	GUISkin skin;

	static GridController selectedGrid;
	static Vector3 formerPosition;

	Tool lastTool = Tool.None;

	public static void OpenWindow()
	{
		var window = GetWindow(typeof(GridEditorWindow)) as GridEditorWindow;
		window.minSize = new Vector2(400, 400);
		window.Show();
	}

	void OnEnable()
	{
		isEnabled = true;
		skin = Resources.Load<GUISkin>("GUIStyle/ObjectsEditorSkin");
		Editor.CreateInstance(typeof(SceneViewEventHandler));
		
		lastTool = Tools.current;
		Tools.current = Tool.None;
    	Tools.hidden = true;
	}

	void OnDestroy()
	{
		isEnabled = false;
		Tools.current = lastTool;
    	Tools.hidden = false;
	}

	void OnGUI()
	{
		DrawLayout();
		DrawHeader();
		DrawBody();
	}

	void DrawLayout()
	{
		headerSection.x = 0;
		headerSection.y = 0;
		headerSection.width = Screen.width;
		headerSection.height = 50;

		bodySection.x = 0;
		bodySection.y =  headerSection.height;
		bodySection.width = Screen.width;
		bodySection.height = Screen.height - headerSection.height;
	}

	void DrawHeader()
	{
		GUILayout.BeginArea(headerSection, skin.GetStyle("Header"));
		GUILayout.Label("Grid Editor", skin.GetStyle("Header"));
		GUILayout.EndArea();
	}

	void DrawBody()
	{
		GUILayout.BeginArea(bodySection, skin.GetStyle("Body"));
		GUILayout.Label("Create Grid [Ctrl + Click]", skin.GetStyle("BodyText"));
		GUILayout.Label("Delete Grid [Shift + Click]", skin.GetStyle("BodyText"));
		GUILayout.EndArea();
	}

	public class SceneViewEventHandler : Editor
	{
		static SceneViewEventHandler()
		{
			SceneView.onSceneGUIDelegate = OnSceneGUI;
		}

		static void OnSceneGUI(SceneView aView)
		{
			if (!isEnabled)
				return;

			Tools.current = Tool.None;
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

			Event e = Event.current;
			if (CheckCtrlClick())
			{
				AddNewGrid();
			}
			else if (CheckShiftClick())
			{
				DeleteGrid();
			}
			else{

				if (CheckNormalClick())
				{
					SelectGrid();
				}

				if (CheckDrag())
				{
					DragSelectedGrid();
				}

				if (CheckMouseUp())
				{
					ReleaseGrid();
				}
			}
		}
	}
	
	static bool CheckCtrlClick()
	{
		Event e = Event.current;
 		return e.control && e.type == EventType.MouseDown && e.button == 0;
	}

	static bool CheckAltClick()
	{
		Event e = Event.current;
		return e.alt && e.type == EventType.MouseDown && e.button == 0;
	}

	static bool CheckShiftClick()
	{
		Event e = Event.current;
		return e.shift && e.type == EventType.MouseDown && e.button == 0;
	}

	static bool CheckNormalClick()
	{
		Event e = Event.current;
		return (!e.alt && !e.shift && !e.control) && e.type == EventType.MouseDown && e.button == 0;
	}

	static bool CheckNormalDrag()
	{
		Event e = Event.current;
		return (!e.alt && !e.shift && !e.control) && CheckDrag();
	}

	static bool CheckDrag()
	{
		Event e = Event.current;
		return e.type == EventType.MouseDrag && e.button == 0;
	}

	static bool CheckMouseUp()
	{
		Event e = Event.current;
		return e.type == EventType.MouseUp && e.button == 0;
	}

	static void AddNewGrid()
	{
		Vector3 mouseWorldPos = GetMouseWorldPos();

		if (mouseWorldPos.x < 0 || mouseWorldPos.y < 0)
			return;
		
		if (!HasGrid())
		{
			CreateGrid(mouseWorldPos);
		}
	}

	static void CreateGrid(Vector3 position)
	{
		var gridContainer = GetGridContainer();

		Object prefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/SceneObjects/Floor.prefab", typeof(GameObject));
		GameObject clone = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
		
		clone.transform.position = position;
		clone.transform.parent = gridContainer.transform;

		Undo.RegisterCreatedObjectUndo(clone, "Create " + clone.name);
		Undo.IncrementCurrentGroup();
	}

	static void DeleteGrid()
	{
		Vector3 mouseWorldPos = GetMouseWorldPos();
		GridController[] allgo = GameObject.FindObjectsOfType(typeof (GridController)) as GridController[];

		for (int i = 0; i < allgo.Length;i++)
		{
			if (Mathf.Approximately(allgo[i].transform.position.x, mouseWorldPos.x) && Mathf.Approximately(allgo[i].transform.position.y, mouseWorldPos.y) && Mathf.Approximately(allgo[i].transform.position.z, mouseWorldPos.z))
			{
				Undo.DestroyObjectImmediate(allgo[i].gameObject);
				Undo.IncrementCurrentGroup();
			}
		}
	}

	static void SelectGrid()
	{
		Vector3 mouseWorldPos = GetMouseWorldPos();
		GridController[] allgo = GameObject.FindObjectsOfType(typeof (GridController)) as GridController[];

		for (int i = 0; i < allgo.Length;i++)
		{
			if (Mathf.Approximately(allgo[i].transform.position.x, mouseWorldPos.x) && Mathf.Approximately(allgo[i].transform.position.y, mouseWorldPos.y) && Mathf.Approximately(allgo[i].transform.position.z, mouseWorldPos.z))
			{
				selectedGrid = allgo[i];
				formerPosition = selectedGrid.transform.position;
				return;
			}
		}
	}

	static void DragSelectedGrid()
	{
		if (selectedGrid == null)
			return;

		Vector3 mouseWorldPos = GetMouseWorldPos();
		selectedGrid.transform.position = mouseWorldPos;
	}

	static void ReleaseGrid()
	{
		if (selectedGrid == null)
			return;
		
		if (GridCount() > 1)
		{
			selectedGrid.transform.position = formerPosition;
		}

		selectedGrid = null;
		formerPosition = default(Vector3);
	}

	static Vector3 GetMouseWorldPos()
	{
		Vector2 mousePos = Event.current.mousePosition;
		mousePos.y = SceneView.currentDrawingSceneView.camera.pixelHeight - mousePos.y;

		Vector3 mouseWorldPos = SceneView.currentDrawingSceneView.camera.ScreenPointToRay(mousePos).origin;

		var gridSize = new Vector2(1f, 1f);
		if (EditorPrefs.HasKey(GridLineWindow.gridSizeXPrefs) && EditorPrefs.HasKey(GridLineWindow.gridSizeYPrefs))
		{
			gridSize.x = EditorPrefs.GetFloat(GridLineWindow.gridSizeXPrefs);
			gridSize.y = EditorPrefs.GetFloat(GridLineWindow.gridSizeYPrefs);
		}
		var shiftToMiddle = false;
		if (EditorPrefs.HasKey(GridLineWindow.shiftToMiddlePrefs))
		{
			shiftToMiddle = EditorPrefs.GetBool(GridLineWindow.shiftToMiddlePrefs);
		}

		mouseWorldPos.y = 0;
		if (gridSize.x > 0.05f && gridSize.y > 0.05f)
		{
			if (shiftToMiddle)
			{
				mouseWorldPos.x = Mathf.Floor((mouseWorldPos.x + gridSize.x/2)/ gridSize.x) * gridSize.x;
				mouseWorldPos.z = Mathf.Floor((mouseWorldPos.z + gridSize.y/2)/ gridSize.y) * gridSize.y;
			}
			else
			{
				mouseWorldPos.x = Mathf.Floor((mouseWorldPos.x)/ gridSize.x) * gridSize.x + gridSize.x/2;
				mouseWorldPos.z = Mathf.Floor((mouseWorldPos.z)/ gridSize.y) * gridSize.y + gridSize.y/2;
			}
		}

		return mouseWorldPos;
	}

	static bool HasGrid()
	{
		return GridCount() > 0;
	}

	static int GridCount()
	{
		Vector3 mouseWorldPos = GetMouseWorldPos();
		GridController[] allgo = GameObject.FindObjectsOfType(typeof (GridController)) as GridController[];

		int brk = 0;
		for (int i = 0; i < allgo.Length;i++)
		{
			if (Mathf.Approximately(allgo[i].transform.position.x, mouseWorldPos.x) && Mathf.Approximately(allgo[i].transform.position.y, mouseWorldPos.y) && Mathf.Approximately(allgo[i].transform.position.z, mouseWorldPos.z))
			{
				brk++;
			}
		}

		return brk;
	}

	static GameObject GetGridContainer()
	{
		var container = MapEditorWindow.GetContainer();
		var gridContainer = container.transform.Find("Grid");

		if (gridContainer == null)
		{
			var go = new GameObject();
			go.name = "Grid";
			go.transform.position = Vector3.zero;
			go.transform.parent = container.transform;
			gridContainer = go.transform;

			Undo.RegisterCreatedObjectUndo(go, "GridContainer");
		}

		return gridContainer.gameObject;
	}
}
