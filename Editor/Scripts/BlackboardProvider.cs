﻿using System;
using System.Collections.Generic;
using System.Linq;
using NodeEditor.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeEditor.Scripts
{
	public class BlackboardProvider
	{
		readonly AbstractNodeGraph m_Graph;
		readonly Texture2D m_ExposedIcon;
		readonly Dictionary<Guid, BlackboardRow> m_PropertyRows;
		readonly BlackboardSection m_Section;
		//WindowDraggable m_WindowDraggable;
		//ResizeBorderFrame m_ResizeBorderFrame;
		public Blackboard blackboard { get; private set; }
		Label m_PathLabel;
		TextField m_PathLabelTextField;
		bool m_EditPathCancelled = false;

		//public Action onDragFinished
		//{
		//    get { return m_WindowDraggable.OnDragFinished; }
		//    set { m_WindowDraggable.OnDragFinished = value; }
		//}

		//public Action onResizeFinished
		//{
		//    get { return m_ResizeBorderFrame.OnResizeFinished; }
		//    set { m_ResizeBorderFrame.OnResizeFinished = value; }
		//}

		public string assetName
		{
			get { return blackboard.title; }
			set
			{
				blackboard.title = value;
			}
		}

		public BlackboardProvider(AbstractNodeGraph graph)
		{
			m_Graph = graph;
			m_ExposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
			m_PropertyRows = new Dictionary<Guid, BlackboardRow>();

			blackboard = new Blackboard()
			{
				scrollable = true,
				subTitle = FormatPath(graph.path),
				editTextRequested = EditTextRequested,
				addItemRequested = AddItemRequested,
				moveItemRequested = MoveItemRequested
			};

			m_PathLabel = blackboard.hierarchy.ElementAt(0).Q<Label>("subTitleLabel");
			m_PathLabel.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

			m_PathLabelTextField = new TextField { visible = false };
			m_PathLabelTextField.RegisterCallback<FocusOutEvent>(e => { OnEditPathTextFinished(); });
			m_PathLabelTextField.RegisterCallback<KeyDownEvent>(OnPathTextFieldKeyPressed);
			blackboard.hierarchy.Add(m_PathLabelTextField);

			// m_WindowDraggable = new WindowDraggable(blackboard.shadow.Children().First().Q("header"));
			// blackboard.AddManipulator(m_WindowDraggable);

			// m_ResizeBorderFrame = new ResizeBorderFrame(blackboard) { name = "resizeBorderFrame" };
			// blackboard.shadow.Add(m_ResizeBorderFrame);

			m_Section = new BlackboardSection { headerVisible = false };
			foreach (var property in graph.properties)
				AddProperty(property);
			blackboard.Add(m_Section);
		}

		void OnMouseDownEvent(MouseDownEvent evt)
		{
			if (evt.clickCount == 2 && evt.button == (int)MouseButton.LeftMouse)
			{
				StartEditingPath();
				evt.PreventDefault();
			}
		}

		void StartEditingPath()
		{
			m_PathLabelTextField.visible = true;

			m_PathLabelTextField.value = m_PathLabel.text;
			m_PathLabelTextField.style.position = Position.Absolute;
			var rect = m_PathLabel.ChangeCoordinatesTo(blackboard, new Rect(Vector2.zero, m_PathLabel.layout.size));
			m_PathLabelTextField.style.left = rect.xMin;
			m_PathLabelTextField.style.top = rect.yMin;
			m_PathLabelTextField.style.width = rect.width;
			m_PathLabelTextField.style.fontSize = 11;
			m_PathLabelTextField.style.marginLeft = 0;
			m_PathLabelTextField.style.marginRight = 0;
			m_PathLabelTextField.style.marginTop = 0;
			m_PathLabelTextField.style.marginBottom = 0;

			m_PathLabel.visible = false;

			m_PathLabelTextField.Focus();
			m_PathLabelTextField.SelectAll();
		}

		void OnPathTextFieldKeyPressed(KeyDownEvent evt)
		{
			switch (evt.keyCode)
			{
				case KeyCode.Escape:
					m_EditPathCancelled = true;
					m_PathLabelTextField.Blur();
					break;
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					m_PathLabelTextField.Blur();
					break;
				default:
					break;
			}
		}

		void OnEditPathTextFinished()
		{
			m_PathLabel.visible = true;
			m_PathLabelTextField.visible = false;

			var newPath = m_PathLabelTextField.text;
			if (!m_EditPathCancelled && (newPath != m_PathLabel.text))
			{
				newPath = SanitizePath(newPath);
			}

			m_Graph.path = newPath;
			m_PathLabel.text = FormatPath(newPath);
			m_EditPathCancelled = false;
		}

		static string FormatPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return "—";
			return path;
		}

		static string SanitizePath(string path)
		{
			var splitString = path.Split('/');
			List<string> newStrings = new List<string>();
			foreach (string s in splitString)
			{
				var str = s.Trim();
				if (!string.IsNullOrEmpty(str))
				{
					newStrings.Add(str);
				}
			}

			return string.Join("/", newStrings.ToArray());
		}

		void MoveItemRequested(Blackboard blackboard, int newIndex, VisualElement visualElement)
		{
			var property = visualElement.userData as INodeProperty;
			if (property == null)
				return;
			m_Graph.owner.RegisterCompleteObjectUndo("Move Property");
			m_Graph.MoveShaderProperty(property, newIndex);
		}

		void AddItemRequested(Blackboard blackboard)
		{
			var gm = new GenericMenu();
			gm.AddItem(new GUIContent("Boolean"),false,()=> AddProperty(new ValueProperty<bool>(),true));
			gm.AddItem(new GUIContent("Float"),false,()=> AddProperty(new ValueProperty<float>(),true));
			gm.AddItem(new GUIContent("Int"),false,()=> AddProperty(new ValueProperty<int>(),true));
			gm.AddItem(new GUIContent("String"),false,()=> AddProperty(new ValueProperty<string>(),true));
			gm.AddItem(new GUIContent("Vector4"),false,()=> AddProperty(new ValueProperty<Vector4>(),true));
			gm.AddItem(new GUIContent("Vector3"),false,()=> AddProperty(new ValueProperty<Vector3>(),true));
			gm.AddItem(new GUIContent("Vector2"),false,()=> AddProperty(new ValueProperty<Vector2>(),true));
			gm.AddItem(new GUIContent("Matrix4x4"),false,()=> AddProperty(new ValueProperty<Matrix4x4>(),true));
			gm.AddItem(new GUIContent("Quaternion"),false,()=> AddProperty(new ValueProperty<Quaternion>(),true));
			gm.AddItem(new GUIContent("Color"),false,()=> AddProperty(new ValueProperty<Color>(),true));
			gm.AddItem(new GUIContent("Bounds"),false,()=> AddProperty(new ValueProperty<Bounds>(),true));
			gm.AddItem(new GUIContent("Rect"),false,()=> AddProperty(new ValueProperty<Rect>(),true));
			gm.AddItem(new GUIContent("Texture2D"),false,()=> AddProperty(new ValueProperty<Texture2D>(),true));
			gm.AddItem(new GUIContent("Cubemap"),false,()=> AddProperty(new ValueProperty<Cubemap>(),true));
			gm.AddItem(new GUIContent("Gradient"),false,()=> AddProperty(new ValueProperty<Gradient>(),true));
			gm.AddItem(new GUIContent("AnimationCureve"),false,()=> AddProperty(new ValueProperty<AnimationCurve>(),true));
			gm.ShowAsContext();
		}

		void EditTextRequested(Blackboard blackboard, VisualElement visualElement, string newText)
		{
			var field = (BlackboardField)visualElement;
			var property = (INodeProperty)field.userData;
			if (!string.IsNullOrEmpty(newText) && newText != property.displayName)
			{
				m_Graph.owner.RegisterCompleteObjectUndo("Edit Property Name");
				newText = m_Graph.SanitizePropertyName(newText, property.guid);
				property.displayName = newText;
				field.text = newText;
				DirtyNodes();
			}
		}

		public void HandleGraphChanges()
		{
			foreach (var propertyGuid in m_Graph.removedProperties)
			{
				BlackboardRow row;
				if (m_PropertyRows.TryGetValue(propertyGuid, out row))
				{
					row.RemoveFromHierarchy();
					m_PropertyRows.Remove(propertyGuid);
				}
			}

			foreach (var property in m_Graph.addedProperties)
				AddProperty(property, index: m_Graph.GetShaderPropertyIndex(property));

			if (m_Graph.movedProperties.Any())
			{
				foreach (var row in m_PropertyRows.Values)
					row.RemoveFromHierarchy();

				foreach (var property in m_Graph.properties)
					m_Section.Add(m_PropertyRows[property.guid]);
			}
		}

		void AddProperty(INodeProperty property, bool create = false, int index = -1)
		{
			if (m_PropertyRows.ContainsKey(property.guid))
				return;

			if (create)
				property.displayName = m_Graph.SanitizePropertyName(property.displayName);

			var field = new BlackboardField(m_ExposedIcon, property.displayName, property.propertyType.ToString()) { userData = property };
			var row = new BlackboardRow(field, new BlackboardFieldPropertyView(m_Graph, property));
			row.userData = property;
			if (index < 0)
				index = m_PropertyRows.Count;
			if (index == m_PropertyRows.Count)
				m_Section.Add(row);
			else
				m_Section.Insert(index, row);
			m_PropertyRows[property.guid] = row;

			if (create)
			{
				row.expanded = true;
				m_Graph.owner.RegisterCompleteObjectUndo("Create Property");
				m_Graph.AddShaderProperty(property);
				field.OpenTextEditor();
			}
		}

		void DirtyNodes()
		{
			foreach (var node in m_Graph.GetNodes<PropertyNode>())
			{
				node.OnEnable();
				node.Dirty(ModificationScope.Node);
			}
		}
	}
}