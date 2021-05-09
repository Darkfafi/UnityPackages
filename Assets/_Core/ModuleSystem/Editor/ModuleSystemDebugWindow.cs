using ModuleSystem.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.Reflection;
using System.Text;
using System;

namespace ModuleSystem.Editor
{
	public class ModuleSystemDebugWindow : EditorWindow
	{
		#region Nested

		public enum Headers : int
		{
			Type = 0,
			Id = 1,
			DataMap = 2,
			Fields = 3,
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
			if (_moduleProcessorTreeView != null && _moduleProcessorTreeView.HasTarget)
			{
				_moduleProcessorTreeView.Reload();
				Repaint();
			}
		}

		protected void OnEnable()
		{
			_moduleProcessorTreeView = new ModuleProcessorView(new TreeViewState());
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

			private List<ModuleAction> _collectedCoreActions = new List<ModuleAction>();
			private Dictionary<int, ModuleAction> _idToAction = new Dictionary<int, ModuleAction>();

			#endregion

			#region Properties

			public bool HasTarget => Target?.Processor != null;

			public IHaveModuleProcessor Target
			{
				get; private set;
			}

			public IReadOnlyDictionary<int, ModuleAction> IdToAction => _idToAction;

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

			public void SetTarget(IHaveModuleProcessor moduleProcessorHolder)
			{
				if(Target != null && Target.Processor != null)
				{
					Target.Processor.ActionStackProcessedEvent -= OnStackProcessed;
				}

				if(Target != moduleProcessorHolder)
				{
					_collectedCoreActions.Clear();
					Target = moduleProcessorHolder;
					if (Target != null)
					{
						Target.Processor.ActionStackProcessedEvent += OnStackProcessed;
					}
					multiColumnHeader.ResizeToFit();
					Reload();
				}
			}

			#endregion

			#region Lifecycle

			protected override void RowGUI(RowGUIArgs args)
			{
				if (IdToAction.TryGetValue(args.item.id, out ModuleAction action))
				{
					int numOfColumns = args.GetNumVisibleColumns();
					for (int i = 0; i < numOfColumns; i++)
					{
						Headers column = (Headers)args.GetColumn(i);
						Rect cellRect = args.GetCellRect(i);
						args.rowRect = cellRect;
						CenterRectUsingSingleLineHeight(ref cellRect);
						switch (column)
						{
							case Headers.Type:
								args.label = action.GetType().Name;
								break;
							case Headers.Id:
								args.label = action.UniqueIdentifier;
								break;
							case Headers.DataMap:
								GUI.TextArea(cellRect, action.DataMap.ToString());
								continue;
							case Headers.Fields:
								FieldInfo[] infos = action.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
								StringBuilder sb = new StringBuilder();
								for(int j = 0; j < infos.Length; j++)
								{
									FieldInfo info = infos[j];
									object val = info.GetValue(action);
									string valString = val == null ? "NULL" : val.ToString();
									sb.AppendLine(string.Format("{0}: {1}", info.Name, valString));
								}
								GUI.TextArea(cellRect, sb.ToString());
								continue;
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
						if (IdToAction.TryGetValue(id, out ModuleAction moduleAction))
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

				ModuleAction[] coreActions = _collectedCoreActions.ToArray();
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

				void AddTreeItem(TreeViewItem parentTreeItem, ModuleAction moduleAction)
				{
					TreeViewItem treeItem = CreateTreeViewItem(moduleAction);
					_idToAction.Add(id, moduleAction);
					parentTreeItem.AddChild(treeItem);
					ModuleAction[] children = moduleAction.ChainedActions;
					for (int i = 0; i < children.Length; i++)
					{
						AddTreeItem(treeItem, children[i]);
					}
				}

				TreeViewItem CreateTreeViewItem(ModuleAction moduleAction, string fallbackName = "-")
				{
					return new TreeViewItem
					{
						id = ++id,
						displayName = moduleAction != null ? moduleAction.UniqueIdentifier : fallbackName,
					};
				}
			}

			#endregion

			#region Private Methods

			private void OnStackProcessed(ModuleAction coreAction)
			{
				_collectedCoreActions.Add(coreAction);
			}

			#endregion
		}

		#endregion
	}
}