using ModuleSystem.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.Reflection;
using System.Text;
using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;

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
			if (_moduleProcessorTreeView != null)
			{
				_moduleProcessorTreeView.RefreshTargetProcessor();
				if(_moduleProcessorTreeView.HasTarget)
				{
					_moduleProcessorTreeView.Reload();
				}
				Repaint();
			}
		}

		protected void OnEnable()
		{
			if (_moduleProcessorTreeView == null)
			{
				_moduleProcessorTreeView = new ModuleProcessorView(new TreeViewState());
				EditorSceneManager.sceneLoaded += OnSceneLoaded;
			}
		}

		protected void OnDisable()
		{
			_moduleProcessorTreeView = null;
			EditorSceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (!_moduleProcessorTreeView.HasTarget)
			{
				IHaveModuleProcessor[] processors = FindObjectsOfType<MonoBehaviour>().OfType<IHaveModuleProcessor>().ToArray();
				if (processors.Length > 0)
				{
					_moduleProcessorTreeView.SetTarget(processors[0]);
				}
			}
			_moduleProcessorTreeView.RefreshTargetProcessor();
		}

		protected void OnGUI()
		{
			if (_moduleProcessorTreeView != null)
			{
				UnityEngine.Object activeObject = Selection.activeObject;

				IHaveModuleProcessor potentialTarget = null;
				
				if(activeObject != null)
				{
					if(activeObject is GameObject gameObject)
					{
						potentialTarget = gameObject.GetComponent<IHaveModuleProcessor>();
					}
					else
					{
						potentialTarget = activeObject as IHaveModuleProcessor;
					}
				}

				if (!_moduleProcessorTreeView.HasTarget || potentialTarget != _moduleProcessorTreeView.Target && potentialTarget != null)
				{
					_moduleProcessorTreeView.SetTarget(potentialTarget);
				}

				if (_moduleProcessorTreeView.HasTarget)
				{
					_moduleProcessorTreeView.OnGUI(new Rect(0, 0, position.width, position.height));
				}
				else
				{
					EditorGUILayout.LabelField($"No Active {nameof(IHaveModuleProcessor)} Selected");
				}
			}
		}

		#endregion

		#region Nested

		private class ModuleProcessorView : TreeView
		{
			#region Variables

			private List<ModuleActionData> _collectedCoreActions = new List<ModuleActionData>();
			private Dictionary<int, ModuleActionData> _idToAction = new Dictionary<int, ModuleActionData>();

			#endregion

			#region Properties

			public bool HasTarget => Target != null;

			public IHaveModuleProcessor Target
			{
				get; private set;
			}

			private ModuleProcessor _targetProcessor = null;

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

			public ModuleProcessorView(TreeViewState state)
				: base(state, new MultiColumnHeader(new MultiColumnHeaderState(CreateColumnHeaders<Headers>())))
			{
				rowHeight = 20;
				showAlternatingRowBackgrounds = true;
				showBorder = true;
				Reload();
			}

			#region Public Methods

			public void RefreshTargetProcessor()
			{
				if(Target == null || Target.Processor != _targetProcessor)
				{
					_collectedCoreActions.Clear();
					if (_targetProcessor != null)
					{
						_targetProcessor.ActionStackProcessedEvent -= OnStackProcessed;
					}

					_targetProcessor = Target?.Processor;

					if (_targetProcessor != null)
					{
						_targetProcessor.ActionStackProcessedEvent += OnStackProcessed;
					}
				}

			}

			public void SetTarget(IHaveModuleProcessor moduleProcessorHolder)
			{
				if(Target != moduleProcessorHolder)
				{
					if (_targetProcessor != null)
					{
						_targetProcessor.ActionStackProcessedEvent -= OnStackProcessed;
						_targetProcessor = null;
					}

					_collectedCoreActions.Clear();

					Target = moduleProcessorHolder;

					RefreshTargetProcessor();

					multiColumnHeader.ResizeToFit();
					Reload();
				}
			}

			#endregion

			#region Lifecycle

			protected override void RowGUI(RowGUIArgs args)
			{
				if (_idToAction.TryGetValue(args.item.id, out ModuleActionData action))
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
						if (_idToAction.TryGetValue(id, out ModuleActionData moduleAction))
						{
							_collectedCoreActions.Remove(moduleAction);
						}
					}

					if (!HasTarget)
					{
						SetTarget(null);
					}
				}
			}

			protected override TreeViewItem BuildRoot()
			{
				int id = 0;
				_idToAction.Clear();

				TreeViewItem root = CreateTreeViewItem(null);
				root.depth = -1;

				ModuleActionData[] coreActions = _collectedCoreActions.ToArray();
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

				void AddTreeItem(TreeViewItem parentTreeItem, ModuleActionData moduleAction)
				{
					TreeViewItem treeItem = CreateTreeViewItem(moduleAction);
					_idToAction.Add(id, moduleAction);
					parentTreeItem.AddChild(treeItem);

					ModuleActionData[] chainedChildren = moduleAction.ChainedActionsData;
					for (int i = 0; i < chainedChildren.Length; i++)
					{
						AddTreeItem(treeItem, chainedChildren[i]);
					}

					ModuleActionData[] enqueuedChildren = moduleAction.EnqueuedActionsData;
					for (int i = 0; i < enqueuedChildren.Length; i++)
					{
						AddTreeItem(treeItem, enqueuedChildren[i]);
					}
				}

				TreeViewItem CreateTreeViewItem(ModuleActionData moduleAction, string fallbackName = "-")
				{
					return new TreeViewItem
					{
						id = ++id,
						displayName = moduleAction != null ? moduleAction.UniqueIdentifierString : fallbackName,
					};
				}
			}

			#endregion

			#region Private Methods

			private void OnStackProcessed(ModuleAction coreAction, uint layer)
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
					for(int i = 0; i < chainedActions.Length; i++)
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

				_collectedCoreActions.Add(coreActionData);
			}


			#endregion

			#region Nested

			private class ModuleActionData
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

		#endregion
	}
}