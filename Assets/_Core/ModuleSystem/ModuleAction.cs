using System;
using System.Collections.Generic;

namespace ModuleSystem
{
	public class ModuleAction
	{
		#region Variables

		public readonly string UniqueIdentifier;
		private readonly List<ModuleAction> _chainedActions = new List<ModuleAction>();
		private readonly List<ModuleAction> _enqueuedActions = new List<ModuleAction>();
		private ModuleAction _cachedRoot = null;

		#endregion

		#region Properties

		public ModuleAction Source
		{
			get; private set;
		}

		public DataMap DataMap
		{
			get; private set;
		}

		public ModuleAction[] ChainedActions => _chainedActions.ToArray();

		public ModuleAction[] EnqueuedActions => _enqueuedActions.ToArray();

		#endregion

		public ModuleAction()
		{
			UniqueIdentifier = Guid.NewGuid().ToString();
			DataMap = new DataMap();
		}

		#region Public Methods

		public void EnqueueAction(ModuleAction action)
		{
			if(action.Source != null)
			{
				action.Source._enqueuedActions.Remove(action);
			}

			action.Source = this;

			_enqueuedActions.Add(action);
		}

		public void ChainAction(ModuleAction action)
		{
			if (action.Source != null)
			{
				action.Source._chainedActions.Remove(action);
			}

			action.Source = this;

			_chainedActions.Add(action);
		}

		public bool HasUpwards<T>(Predicate<T> predicate, Predicate<ModuleAction> chainBlockade)
		{
			return TryFindUpwards(predicate, chainBlockade, out _);
		}

		public bool TryFindUpwards<T>(Predicate<T> predicate, Predicate<ModuleAction> chainBlockade, out T result)
		{
			predicate = predicate ?? new Predicate<T>(x => true);
			chainBlockade = chainBlockade ?? new Predicate<ModuleAction>(x => false);
			ModuleAction source = Source;
			while (source != null)
			{
				if(chainBlockade(source))
				{
					break;
				}

				if(source is T castedSource && predicate(castedSource))
				{
					result = castedSource;
					return true;
				}
				source = source.Source;
			}
			result = default;
			return false;
		}

		public T[] FindAllUpwards<T>(Predicate<T> predicate)
		{
			predicate = predicate ?? new Predicate<T>(x => true);
			List<T> results = new List<T>();
			ModuleAction source = Source;
			while (source != null)
			{
				if (source is T castedSource && predicate(castedSource))
				{
					results.Add(castedSource);
				}
				source = source.Source;
			}
			return results.ToArray();
		}

		public bool TryFindDirectChainAction<T>(Predicate<T> predicate, out T result)
		{
			predicate = predicate ?? new Predicate<T>(x => true);
			for(int i = 0; i < _chainedActions.Count; i++)
			{
				ModuleAction chainedAction = _chainedActions[i];
				if(chainedAction is T castedChainedAction && predicate(castedChainedAction))
				{
					result = castedChainedAction;
					return true;
				}
			}
			result = default;
			return false;
		}

		public bool HasDownwards<T>(Predicate<T> predicate, bool inclSelf)
		{
			return TryFindDownwards(predicate, null, inclSelf, out _);
		}

		public bool TryFindDownwards<T>(Predicate<T> predicate, Predicate<ModuleAction> chainBlockade, bool inclSelf, out T result)
		{
			Queue<ModuleAction> chainedActions = new Queue<ModuleAction>(ChainedActions);
			predicate = predicate ?? new Predicate<T>(x => true);
			chainBlockade = chainBlockade ?? new Predicate<ModuleAction>(x => false);

			if(inclSelf && this is T castedSelf && predicate(castedSelf))
			{
				result = castedSelf;
				return true;
			}

			while (chainedActions.Count > 0)
			{
				ModuleAction action = chainedActions.Dequeue();
				if(action is T castedAction && predicate(castedAction))
				{
					result = castedAction;
					return true;
				}

				if (!chainBlockade(action))
				{
					for (int i = 0, c = action.ChainedActions.Length; i < c; i++)
					{
						chainedActions.Enqueue(action.ChainedActions[i]);
					}
				}
			}
			result = default;
			return false;
		}

		public T[] FindAllDownwards<T>(Predicate<T> predicate, Predicate<ModuleAction> chainBlockade)
			where T : ModuleAction
		{
			List<T> results = new List<T>();
			Queue<ModuleAction> chainedActions = new Queue<ModuleAction>(ChainedActions);
			predicate = predicate ?? new Predicate<T>(x => true);
			chainBlockade = chainBlockade ?? new Predicate<ModuleAction>(x => false);

			while (chainedActions.Count > 0)
			{
				ModuleAction action = chainedActions.Dequeue();
				if (action is T castedAction && predicate(castedAction))
				{
					results.Add(castedAction);
				}

				if(!chainBlockade(action))
				{
					for (int i = 0, c = action.ChainedActions.Length; i < c; i++)
					{
						chainedActions.Enqueue(action.ChainedActions[i]);
					}
				}
			}
			return results.ToArray();
		}

		public ModuleAction GetRoot()
		{
			if(_cachedRoot != null)
			{
				return _cachedRoot;
			}

			ModuleAction root = this;
			while(root.Source != null)
			{
				root = root.Source;
			}
			
			if(root != this)
			{
				_cachedRoot = root;
			}

			return root;
		}

		#endregion
	}
}