using ModuleSystem.Core;
using System;
using System.Collections.Generic;

namespace ModuleSystem
{
	public class ModuleProcessor : IDisposable
	{
		#region Consts

		public const string DefaultLayer = "__Default__";

		private const string ProcessedByModuleKey = "ProcessedByModule";
		private const string ChainedByProcessorKey = "ChainedByProcessor";
		private const string EnqueuedByProcessorKey = "EnqueuedByProcessor";

		#endregion

		#region Events

		public delegate void ModuleActionHandler(ModuleAction moduleAction, string layer);
		public event ModuleActionHandler ActionProcessedEvent;
		public event ModuleActionHandler ActionStackProcessedEvent;

		#endregion

		#region Variables

		private Dictionary<string, ProcessLayer> _processLayers = new Dictionary<string, ProcessLayer>();
		private List<IModule> _modules;
		private bool _started = false;

		#endregion

		#region Properties

		public string UniqueIdentifier
		{
			get;
		}

		public bool IsDisabled
		{
			get; private set;
		}

		public bool IsProcessing
		{
			get; private set;
		}

		#endregion

		public ModuleProcessor(bool startModules, params IModule[] modules)
		{
			UniqueIdentifier = Guid.NewGuid().ToString();
			_started = false;
			_modules = new List<IModule>(modules);

			for (int i = 0; i < _modules.Count; i++)
			{
				_modules[i].Init(this);
			}

			if(startModules)
			{
				StartModules();
			}
		}

		#region Public Methods

		public void AddModule(IModule module)
		{
			if(!_modules.Contains(module))
			{
				_modules.Add(module);
				module.Init(this);
				if(_started)
				{
					module.StartModule();
				}
			}
		}

		public void RemoveModule(IModule module)
		{
			if(_modules.Remove(module))
			{
				module.Deinit();
			}
		}

		public void AddModules(IModule[] modules)
		{
			List<IModule> addedModules = new List<IModule>();
			for(int i = 0; i < modules.Length; i++)
			{
				IModule module = modules[i];
				if (!_modules.Contains(module))
				{
					_modules.Add(module);
					module.Init(this);
					addedModules.Add(module);
				}
			}

			if (_started)
			{
				for (int i = 0; i < addedModules.Count; i++)
				{
					IModule module = addedModules[i];
					if (_modules.Contains(module))
					{
						module.StartModule();
					}
				}
			}
		}

		public void StartModules()
		{
			if (!_started)
			{
				for (int i = 0; i < _modules.Count; i++)
				{
					_modules[i].StartModule();
				}

				_started = true;
				TryProcessStack();
			}
		}

		public IModule[] GetModules()
		{
			return _modules.ToArray();
		}

		public void EnqueueAction(ModuleAction action, string layer = DefaultLayer)
		{
			if(string.IsNullOrEmpty(layer) || string.IsNullOrWhiteSpace(layer))
			{
				layer = DefaultLayer;
			}

			GetOrCreateLayer(layer).EnqueueAction(action);
		}

		public void SetDisabled(bool isDisabled)
		{
			if (IsDisabled != isDisabled)
			{
				IsDisabled = isDisabled;
			}
		}

		public void Dispose()
		{
			foreach(var pair in _processLayers)
			{
				pair.Value.Dispose();
			}

			_processLayers.Clear();

			for (int i = _modules.Count - 1; i >= 0; i--)
			{
				RemoveModule(_modules[i]);
			}

			_modules.Clear();

			_started = false;
			_modules = null;
			_processLayers = null;
		}

		#endregion

		#region Private Methods

		private ProcessLayer GetOrCreateLayer(string layer)
		{
			if (!_processLayers.TryGetValue(layer, out ProcessLayer processLayer))
			{
				_processLayers[layer] = processLayer = new ProcessLayer(layer, this, OnActionProcessed, OnActionStackProcessed);
			}
			return processLayer;
		}

		private void TryProcessStack()
		{
			Dictionary<string, ProcessLayer> cachedLayers = new Dictionary<string, ProcessLayer>(_processLayers);
			foreach (var pair in cachedLayers)
			{
				pair.Value.TryProcessStack();
			}
		}

		private void OnActionProcessed(ModuleAction moduleAction, string layer)
		{
			ActionProcessedEvent?.Invoke(moduleAction, layer);
		}

		private void OnActionStackProcessed(ModuleAction moduleAction, string layer)
		{
			ActionStackProcessedEvent?.Invoke(moduleAction, layer);

			if (_processLayers.TryGetValue(layer, out ProcessLayer processLayer))
			{
				if(processLayer.IsProcessingLastAction)
				{
					_processLayers.Remove(layer);
				}
			}
		}

		#endregion

		#region Nested

		private class ProcessLayer : IDisposable
		{
			#region Variables
			private ModuleProcessor _processor = null;
			private bool _isProcessing = false;
			private IModule _lockingModule = null;

			private ModuleAction _initialAction = null;

			// Actions Chained / Enqueued by Action
			private Stack<ModuleAction> _executionStack = new Stack<ModuleAction>();
			private Queue<ModuleAction> _executionQueue = new Queue<ModuleAction>();

			// Actions Enqueued to execute when execution stack / queue is completely processed
			private Queue<ModuleAction> _nextActions = new Queue<ModuleAction>();

			private ModuleActionHandler _actionProcessedCallback = null;
			private ModuleActionHandler _actionStackProcessedCallback = null;

			#endregion

			#region Properties

			public string Layer
			{
				get; private set;
			}

			public bool IsProcessingLastAction => IsProcessing && _nextActions.Count == 0;
			public bool IsProcessing => _isProcessing || _lockingModule != null;

			#endregion

			public ProcessLayer(string layer, ModuleProcessor processor, ModuleActionHandler actionProcessedCallback, ModuleActionHandler actionStackProcessedCallback)
			{
				Layer = layer;
				_processor = processor;
				_actionProcessedCallback = actionProcessedCallback;
				_actionStackProcessedCallback = actionStackProcessedCallback;
			}

			#region Public Methods

			public void EnqueueAction(ModuleAction action)
			{
				if (!_processor._started)
				{
					_nextActions.Enqueue(action);
					return;
				}

				if (!_processor.IsDisabled || IsProcessing)
				{
					_nextActions.Enqueue(action);
					TryProcessStack();
				}
			}

			public void TryProcessStack()
			{
				if (IsProcessing)
				{
					return;
				}

				_isProcessing = true;

				// If the stack is empty
				if(_executionStack.Count == 0)
				{
					// But the execution queue is not, then place the first of the queue on top of the stack
					if (_executionQueue.Count > 0)
					{
						_executionStack.Push(_executionQueue.Dequeue());
					}
					//  But the queue is not, then place the first of the queue on top of the stack
					else if (_nextActions.Count > 0)
					{
						_executionStack.Push(_nextActions.Dequeue());
					}
				}

				// Stack Resolve Loop
				while (_executionStack.Count > 0)
				{
					ModuleAction action = _executionStack.Peek();

					if (_initialAction == null)
					{
						_initialAction = action;
					}

					IModule[] modules = _processor.GetModules();
					for (int i = 0; i < modules.Length; i++)
					{
						IModule module = modules[i];
						_lockingModule = module;
						if (!action.DataMap.HasMark(ProcessedByModuleKey, module.UniqueIdentifier))
						{
							// Processing Callback
							if (action is CallbackModuleAction callbackModule && callbackModule.ModuleSource == module)
							{
								callbackModule.DataMap.Mark(ProcessedByModuleKey, module.UniqueIdentifier);
								callbackModule?.ModuleCallback(callbackModule);
								break;
							}
							// Processing Module
							else if (module.TryProcess(action, () =>
							{
								Unlock(module);
							}))
							{
								action.DataMap.Mark(ProcessedByModuleKey, module.UniqueIdentifier);
								if (_lockingModule != null)
								{
									_isProcessing = false;
									return;
								}
								else
								{
									ChainActions(action);
									EnqueueActions(action);

									// If a new actions are on the stack, process those before finishing the processing of the source
									if (_executionStack.Peek() != action)
									{
										break;
									}
									else
									{
										i = -1;
										continue;
									}
								}
							}
						}

						_lockingModule = null;
					}

					// After the action processing is done, check for chain reactions, if any are added, process them before closing this action
					ChainActions(action);
					EnqueueActions(action);

					if (_executionStack.Peek() != action)
					{
						continue;
					}

					_executionStack.Pop();

					_actionProcessedCallback?.Invoke(action, Layer);

					// If the Stack is completely resolved
					if (_executionStack.Count == 0)
					{
						// Process next in execution queue, causing the next stack flow on the execution stack
						if (_executionQueue.Count > 0)
						{
							_executionStack.Push(_executionQueue.Dequeue());
							// Continue for the 'stack' is not fully processed yet. For the Exeuction Queue is part of it.
							continue;
						}

						if (_initialAction != null)
						{
							ModuleAction actionBase = _initialAction;
							_initialAction = null;

							for (int i = 0; i < modules.Length; i++)
							{
								modules[i].OnResolvedStack(actionBase);
							}

							_actionStackProcessedCallback?.Invoke(actionBase, Layer);
						}
						
						// Process next in queue, causing the next stack flow on the execution stack
						if (_nextActions.Count > 0)
						{
							_executionStack.Push(_nextActions.Dequeue());
						}
					}
				}

				_isProcessing = false;
			}

			public void Dispose()
			{
				_nextActions.Clear();
				_executionQueue.Clear();
				_executionStack.Clear();

				_lockingModule = null;
				_initialAction = null;
				_isProcessing = false;
			}

			#endregion

			#region Internal Methods

			internal void InternalStackProcessAction(ModuleAction action)
			{
				if (!_processor._started)
				{
					_executionStack.Push(action);
					return;
				}

				if (!_processor.IsDisabled || IsProcessing)
				{
					_executionStack.Push(action);
					TryProcessStack();
				}
			}

			internal void InternalStackEnqueueAction(ModuleAction action)
			{
				if (!_processor._started)
				{
					_executionQueue.Enqueue(action);
					return;
				}

				if (!_processor.IsDisabled || IsProcessing)
				{
					_executionQueue.Enqueue(action);
					TryProcessStack();
				}
			}

			#endregion

			#region Private Methods

			private bool IsLockingModule(IModule module)
			{
				return _lockingModule == module;
			}

			private void Unlock(IModule module)
			{
				if (IsLockingModule(module))
				{
					_lockingModule = null;
					TryProcessStack();
				}
			}

			private void ChainActions(ModuleAction source)
			{
				ModuleAction[] chainedActions = source.ChainedActions;
				for (int i = chainedActions.Length - 1; i >= 0; i--)
				{
					ModuleAction chainedAction = chainedActions[i];
					if (!chainedAction.DataMap.HasMark(ChainedByProcessorKey, _processor.UniqueIdentifier))
					{
						InternalStackProcessAction(chainedAction);
						chainedAction.DataMap.Mark(ChainedByProcessorKey, _processor.UniqueIdentifier);
					}
				}
			}

			private void EnqueueActions(ModuleAction source)
			{
				ModuleAction[] enqueuedActions = source.EnqueuedActions;
				for (int i = 0; i < enqueuedActions.Length; i++)
				{
					ModuleAction enqueuedAction = enqueuedActions[i];
					if (!enqueuedAction.DataMap.HasMark(EnqueuedByProcessorKey, _processor.UniqueIdentifier))
					{
						InternalStackEnqueueAction(enqueuedAction);
						enqueuedAction.DataMap.Mark(EnqueuedByProcessorKey, _processor.UniqueIdentifier);
					}
				}
			}

			#endregion
		}

		#endregion
	}
}