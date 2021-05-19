using ModuleSystem.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModuleSystem.Editor
{
	[ExecuteInEditMode]
	public class ModuleSystemDebugWindow : EditorWindow
	{
		#region Nested

		public enum Headers : int
		{
			Type = 0,
			Id = 1,
			DataMap = 2,
			Fields = 3,
			ProcessType = 4,
		}

		#endregion

		#region Variables

		[SerializeField]
		private ModuleProcessorViewState _state = null;

		private ModuleProcessorView _moduleProcessorTreeView;

		#endregion

		#region Public Methods

		[MenuItem("ModuleSystem/TreeView")]
		static void OpenWindow()
		{
			ModuleSystemDebugWindow window = GetWindow<ModuleSystemDebugWindow>();

			window.titleContent = new GUIContent("ModuleSystem TreeView");
			window.Show();
		}

		#endregion

		#region Lifecycle

		protected void Update()
		{
			if (_state.HasTarget)
			{
				_moduleProcessorTreeView.Reload();

				// Refresh Update Logics
				if(_state.IsRefreshed)
				{
					_moduleProcessorTreeView.multiColumnHeader.ResizeToFit();
					_state.IsRefreshed = false;
				}
			}
			Repaint();
		}

		protected void OnEnable()
		{
			if (_moduleProcessorTreeView == null)
			{
				if(_state == null)
				{
					_state = new ModuleProcessorViewState();
				}

				_moduleProcessorTreeView = new ModuleProcessorView(_state);
				EditorSceneManager.sceneLoaded += OnSceneLoaded;
				_state.RefreshTarget();
			}
		}

		protected void OnDisable()
		{
			if (_moduleProcessorTreeView != null)
			{
				_moduleProcessorTreeView = null;
				EditorSceneManager.sceneLoaded -= OnSceneLoaded;
			}
		}

		private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			_state.RefreshTarget();
		}

		public void RefreshTarget()
		{
			if (_moduleProcessorTreeView == null)
			{
				if (_state == null)
				{
					_state = new ModuleProcessorViewState();
				}

				_moduleProcessorTreeView = new ModuleProcessorView(_state);
			}

			UnityEngine.Object activeObject = Selection.activeObject;

			GameObject potentialTarget = _state.TargetContainer;

			if (activeObject != null)
			{
				if (activeObject is GameObject gameObject)
				{
					potentialTarget = gameObject;
				}
			}

			if (!_state.HasTarget)
			{
				_state.SetTargetContainer(potentialTarget, false);
			}

			if (!_state.HasTarget)
			{
				IHaveModuleProcessor[] processors = FindObjectsOfType<MonoBehaviour>().OfType<IHaveModuleProcessor>().ToArray();
				if (processors.Length > 0)
				{
					_state.SetTargetContainer(((MonoBehaviour)processors[0]).gameObject, false);
				}
			}

			if (_state.HasTarget)
			{
				if (_state.TargetContainer != null)
				{
					titleContent.text = $"Processor: {_state.TargetContainer.name}";
				}
				else
				{
					titleContent.text = "No Behaviour Processor";
				}
			}
			else
			{
				titleContent.text = "No Processor";
				EditorGUILayout.LabelField($"No Active {nameof(IHaveModuleProcessor)} Selected");
			}
		}

		private void OnRefreshData()
		{
			if (_moduleProcessorTreeView != null)
			{
				_moduleProcessorTreeView.multiColumnHeader.ResizeToFit();
			}
		}

		protected void OnGUI()
		{
			if (_moduleProcessorTreeView != null)
			{
				RefreshTarget();

				GameObject go = _state.TargetContainer;

				go = EditorGUILayout.ObjectField("Target: ", go, typeof(GameObject), true) as GameObject;

				if (go != _state.TargetContainer)
				{
					_state.SetTargetContainer(go, false);
				}

				if (_state.HasTarget)
				{
					_moduleProcessorTreeView.OnGUI(new Rect(0, 25, position.width, position.height));
				}
			}
		}

		#endregion

		#region Nested

		[Serializable]
		private class ModuleProcessorViewState : TreeViewState
		{
			[SerializeField]
			private GameObject _targetContainer;

			private List<ModuleActionData> _collectedCoreActions = new List<ModuleActionData>();

			public GameObject TargetContainer => _targetContainer;

			public bool HasTarget => Target != null;

			public IHaveModuleProcessor Target => _targetContainer != null ? _targetContainer.GetComponent<IHaveModuleProcessor>() : null;

			public bool IsRefreshed = false;

			#region Public Methods

			public ModuleActionData[] GetCoreActions()
			{
				return GetCollectedCoreActionsList().ToArray();
			}

			public void RemoveAction(ModuleActionData data)
			{
				GetCollectedCoreActionsList().Remove(data);
			}

			public void RefreshTarget()
			{
				SetTargetContainer(_targetContainer, true);
			}

			public void SetTargetContainer(GameObject targetContainer, bool force)
			{
				bool hasChange = _targetContainer != targetContainer;
				if(targetContainer == null || hasChange || force)
				{
					if(Target != null)
					{
						Target.ActionStackProcessedEvent.RemoveListener(OnActionStackProcessedEvent);
					}

					if(!force)
					{
						GetCollectedCoreActionsList().Clear();
					}

					_targetContainer = targetContainer;

					if (Target != null)
					{
						Target.ActionStackProcessedEvent.AddListener(OnActionStackProcessedEvent);
					}
				}
				
				if(hasChange)
				{
					IsRefreshed = true;
				}
			}

			private List<ModuleActionData> GetCollectedCoreActionsList()
			{
				if(_collectedCoreActions == null)
				{
					_collectedCoreActions = new List<ModuleActionData>();
				}

				return _collectedCoreActions;
			}

			private void OnActionStackProcessedEvent(ModuleAction coreAction, string layer)
			{
				Queue<(ModuleAction, ModuleActionData)> nextToConvert = new Queue<(ModuleAction, ModuleActionData)>();
				ModuleActionData coreActionData = new ModuleActionData();
				nextToConvert.Enqueue((coreAction, coreActionData));
				coreActionData.ProcessTypeString = "Root (Enqueued)";

				while (nextToConvert.Count > 0)
				{
					(ModuleAction convertingAction, ModuleActionData convertingData) = nextToConvert.Dequeue();
					List<string> options = new List<string>();

					options.Add("-- Marks -- ");
					foreach (var pair in convertingAction.DataMap.GetMarks())
					{
						foreach (var markSuffix in pair.Value)
						{
							options.Add(string.Format("{0}-{1}", pair.Key, markSuffix));
						}
					}

					options.Add("-- Data --");
					foreach (var pair in convertingAction.DataMap.GetDataMap())
					{
						options.Add(string.Format("{0}: {1}", pair.Key, pair.Value));
					}

					string[] dataMapOptions = options.ToArray();

					options.Clear();

					FieldInfo[] infos = convertingAction.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
					options.Add("-- Fields --");
					for (int j = 0; j < infos.Length; j++)
					{
						FieldInfo info = infos[j];
						object val = info.GetValue(convertingAction);
						string valString = val == null ? "NULL" : val.ToString();
						options.Add(string.Format("{0}: {1}", info.Name, valString));
					}

					string[] fieldsOptions = options.ToArray();

					convertingData.TypeString = convertingAction.GetType().Name;
					convertingData.UniqueIdentifierString = convertingAction.UniqueIdentifier;
					convertingData.DataMapOptions = dataMapOptions;
					convertingData.FieldOptions = fieldsOptions;

					// Chained Actions
					ModuleAction[] chainedActions = convertingAction.ChainedActions;
					ModuleActionData[] chainedActionsData = new ModuleActionData[chainedActions.Length];
					for (int i = 0; i < chainedActions.Length; i++)
					{
						ModuleActionData actionData = new ModuleActionData();
						nextToConvert.Enqueue((chainedActions[i], actionData));
						chainedActionsData[i] = actionData;
						actionData.ProcessTypeString = "Chained";
					}
					convertingData.ChainedActionsData = chainedActionsData;

					// Enqueued Actions
					ModuleAction[] enqueuedActions = convertingAction.EnqueuedActions;
					ModuleActionData[] enqueuedActionsData = new ModuleActionData[enqueuedActions.Length];
					for (int i = 0; i < enqueuedActions.Length; i++)
					{
						ModuleActionData actionData = new ModuleActionData();
						nextToConvert.Enqueue((enqueuedActions[i], actionData));
						enqueuedActionsData[i] = actionData;
						actionData.ProcessTypeString = "Enqueued";
					}
					convertingData.EnqueuedActionsData = enqueuedActionsData;
				}

				GetCollectedCoreActionsList().Add(coreActionData);
				IsRefreshed = true;
			}

			#endregion

			#region Nested

			public class ModuleActionData
			{
				public string TypeString;
				public string UniqueIdentifierString;
				public string[] DataMapOptions;
				public string[] FieldOptions;
				public string ProcessTypeString;
				public ModuleActionData[] ChainedActionsData;
				public ModuleActionData[] EnqueuedActionsData;
			}

			#endregion
		}

		private class ModuleProcessorView : TreeView
		{
			#region Variables

			private Dictionary<int, ModuleProcessorViewState.ModuleActionData> _idToAction = new Dictionary<int, ModuleProcessorViewState.ModuleActionData>();

			#endregion

			#region Properties

			private ModuleProcessorViewState _processorState = null;

			#endregion

			public static MultiColumnHeaderState.Column[] CreateColumnHeaders<T>() where T : Enum
			{
				Array e = Enum.GetValues(typeof(T));
				MultiColumnHeaderState.Column[] headers = new MultiColumnHeaderState.Column[e.Length];
				for (int i = 0; i < e.Length; i++)
				{
					headers[i] = new MultiColumnHeaderState.Column
					{
						headerContent = new GUIContent(e.GetValue(i).ToString()),
						autoResize = true,
					};
				}
				return headers;
			}

			public ModuleProcessorView(ModuleProcessorViewState state)
				: base(state, new MultiColumnHeader(new MultiColumnHeaderState(CreateColumnHeaders<Headers>())))
			{
				_processorState = state;
				rowHeight = 20;
				showAlternatingRowBackgrounds = true;
				showBorder = true;
				Reload();
			}

			#region Lifecycle

			protected override void RowGUI(RowGUIArgs args)
			{
				if (_idToAction.TryGetValue(args.item.id, out ModuleProcessorViewState.ModuleActionData action))
				{
					int numOfColumns = args.GetNumVisibleColumns();
					for (int i = 0; i < numOfColumns; i++)
					{
						Headers column = (Headers)args.GetColumn(i);
						Rect cellRect = args.GetCellRect(i);
						args.rowRect = cellRect;
						CenterRectUsingSingleLineHeight(ref cellRect);
						cellRect.x += depthIndentWidth * args.item.depth;

						switch (column)
						{
							case Headers.Type:
								args.label = action.TypeString;
								break;
							case Headers.Id:
								args.label = action.UniqueIdentifierString;
								break;
							case Headers.DataMap:
								EditorGUI.Popup(cellRect, 0, action.DataMapOptions);
								continue;
							case Headers.Fields:
								EditorGUI.Popup(cellRect, 0, action.FieldOptions);
								continue;
							case Headers.ProcessType:
								args.label = action.ProcessTypeString;
								break;
						}
						base.RowGUI(args);
					}
				}
				else
				{
					base.RowGUI(args);
				}
			}

			public override void OnGUI(Rect rect)
			{
				base.OnGUI(rect);

				Event e = Event.current;
				if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete && HasSelection())
				{
					foreach (var id in GetSelection())
					{
						if (_idToAction.TryGetValue(id, out ModuleProcessorViewState.ModuleActionData moduleAction))
						{
							_processorState.RemoveAction(moduleAction);
						}
					}
				}
			}

			protected override TreeViewItem BuildRoot()
			{
				int id = 0;
				_idToAction.Clear();

				TreeViewItem root = CreateTreeViewItem(null);
				root.depth = -1;

				ModuleProcessorViewState.ModuleActionData[] coreActions = _processorState.GetCoreActions();
				if (coreActions.Length > 0)
				{
					for (int i = 0; i < coreActions.Length; i++)
					{
						AddTreeItem(root, coreActions[i]);
					}
				}

				if (root.children == null || root.children.Count == 0)
				{
					root.AddChild(CreateTreeViewItem(null, "N/A"));
				}

				SetupDepthsFromParentsAndChildren(root);

				return root;

				void AddTreeItem(TreeViewItem parentTreeItem, ModuleProcessorViewState.ModuleActionData moduleAction)
				{
					TreeViewItem treeItem = CreateTreeViewItem(moduleAction);
					_idToAction.Add(id, moduleAction);
					parentTreeItem.AddChild(treeItem);

					ModuleProcessorViewState.ModuleActionData[] chainedChildren = moduleAction.ChainedActionsData;
					for (int i = 0; i < chainedChildren.Length; i++)
					{
						AddTreeItem(treeItem, chainedChildren[i]);
					}

					ModuleProcessorViewState.ModuleActionData[] enqueuedChildren = moduleAction.EnqueuedActionsData;
					for (int i = 0; i < enqueuedChildren.Length; i++)
					{
						AddTreeItem(treeItem, enqueuedChildren[i]);
					}
				}

				TreeViewItem CreateTreeViewItem(ModuleProcessorViewState.ModuleActionData moduleAction, string fallbackName = "-")
				{
					return new TreeViewItem
					{
						id = ++id,
						displayName = moduleAction != null ? moduleAction.UniqueIdentifierString : fallbackName,
					};
				}
			}

			#endregion
		}

		#endregion
	}
}