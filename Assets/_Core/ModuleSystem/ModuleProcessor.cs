using ModuleSystem.Core;
using System;
using System.Collections.Generic;

namespace ModuleSystem
{
	public class ModuleProcessor : IDisposable
	{
		#region Consts

		private const string ProcessedByModuleKey = "ProcessedByModule";
		private const string ChainedByProcessorKey = "ChainedByProcessor";

		#endregion

		#region Events

		public delegate void ModuleActionHandler(ModuleAction moduleAction, uint layer);
		public event ModuleActionHandler ActionProcessedEvent;
		public event ModuleActionHandler ActionStackProcessedEvent;

		#endregion

		#region Variables

		private Dictionary<uint, ProcessLayer> _processLayers = new Dictionary<uint, ProcessLayer>();
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

		public ProcessorSettings Settings
		{
			get; private set;
		}

		#endregion

		public ModuleProcessor(ProcessorSettings settings, params IModule[] modules)
		{
			Settings = settings;
			UniqueIdentifier = Guid.NewGuid().ToString();
			_started = false;
			_modules = new List<IModule>(modules);

			for (int i = 0; i < _modules.Count; i++)
			{
				_modules[i].Init(this);
			}

			if (Settings.StartModules)
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

		public void EnqueueAction(ModuleAction action, uint layer = 0)
		{
			if(!_processLayers.TryGetValue(layer, out ProcessLayer processLayer))
			{
				processLayer = new ProcessLayer(layer, this, OnActionProcessed, OnActionStackProcessed);
			}
			processLayer.EnqueueAction(action);
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
				_modules[i].Deinit();
			}

			_modules = null;
			_started = false;
			_processLayers = null;
		}

		#endregion

		#region Private Methods

		private void ProcessAction(ModuleAction action, uint layer)
		{
			if (!_processLayers.TryGetValue(layer, out ProcessLayer processLayer))
			{
				processLayer = new ProcessLayer(layer, this, OnActionProcessed, OnActionStackProcessed);
			}
			processLayer.ProcessAction(action);
		}

		private void TryProcessStack()
		{
			foreach (var pair in _processLayers)
			{
				pair.Value.TryProcessStack();
			}
		}

		private void OnActionProcessed(ModuleAction moduleAction, uint layer)
		{
			ActionProcessedEvent?.Invoke(moduleAction, layer);
		}

		private void OnActionStackProcessed(ModuleAction moduleAction, uint layer)
		{
			ActionStackProcessedEvent?.Invoke(moduleAction, layer);
		}

		#endregion

		#region Nested

		public struct ProcessorSettings
		{
			public bool StartModules;
			public uint ChainActionLayerOffset;

			public ProcessorSettings(bool startModules, uint chainActionLayerOffset)
			{
				StartModules = startModules;
				ChainActionLayerOffset = chainActionLayerOffset;
			}
		}

		private class ProcessLayer : IDisposable
		{
			#region Variables

			private uint _layer = 0;
			private ModuleProcessor _processor = null;
			private bool _isProcessing = false;
			private IModule _lockingModule = null;

			private ModuleAction _initialAction = null;
			private Stack<ModuleAction> _executionStack = new Stack<ModuleAction>();
			private Queue<ModuleAction> _nextActions = new Queue<ModuleAction>();

			private ModuleActionHandler _actionProcessedCallback = null;
			private ModuleActionHandler _actionStackProcessedCallback = null;

			#endregion

			#region Properties

			public bool IsProcessing => _isProcessing || _lockingModule != null;

			#endregion

			public ProcessLayer(uint layer, ModuleProcessor processor, ModuleActionHandler actionProcessedCallback, ModuleActionHandler actionStackProcessedCallback)
			{
				_layer = layer;
				_processor = processor;
				_actionProcessedCallback = actionProcessedCallback;
				_actionStackProcessedCallback = actionStackProcessedCallback;
			}

			#region Public Methods

			public void ProcessAction(ModuleAction action)
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

				// If the stack is empty, but the queue is not, then place the first of the queue on top of the stack
				if (_executionStack.Count == 0 && _nextActions.Count > 0)
				{
					_executionStack.Push(_nextActions.Dequeue());
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
					if (_executionStack.Peek() != action)
					{
						continue;
					}

					_executionStack.Pop();

					_actionProcessedCallback?.Invoke(action, _layer);

					// If the Stack is completely resolved
					if (_executionStack.Count == 0)
					{
						if (_initialAction != null)
						{
							ModuleAction actionBase = _initialAction;
							_initialAction = null;

							for (int i = 0; i < modules.Length; i++)
							{
								modules[i].OnResolvedStack(actionBase);
							}

							_actionStackProcessedCallback?.Invoke(actionBase, _layer);
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
				_executionStack.Clear();

				_lockingModule = null;
				_initialAction = null;
				_isProcessing = false;
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
				// Stack Chain Actions after source is processed completely
				for (int i = source.ChainedActions.Length - 1; i >= 0; i--)
				{
					ModuleAction chainedAction = source.ChainedActions[i];
					if (!chainedAction.DataMap.HasMark(ChainedByProcessorKey, _processor.UniqueIdentifier))
					{
						_processor.ProcessAction(chainedAction, _layer + _processor.Settings.ChainActionLayerOffset);
						chainedAction.DataMap.Mark(ChainedByProcessorKey, _processor.UniqueIdentifier);
					}
				}
			}

			#endregion
		}

		#endregion
	}
}