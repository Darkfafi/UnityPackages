using ModuleSystem.Core;
using System;
using UnityEngine;

namespace ModuleSystem
{
	public abstract class DelayedModuleSOBase : ModuleSOBase
	{
		public override bool TryProcess(ModuleAction action, Action unlockAction)
		{
			return TryProcessInternal(action, unlockAction);
		}

		protected abstract bool TryProcessInternal(ModuleAction action, Action unlockMethod);
	}

	public abstract class BasicModuleSOBase : ModuleSOBase
	{
		public override bool TryProcess(ModuleAction action, Action unlockAction)
		{
			if (TryProcessInternal(action))
			{
				unlockAction();
				return true;
			}
			return false;
		}

		protected abstract bool TryProcessInternal(ModuleAction action);
	}
}

namespace ModuleSystem.Core
{
	public abstract class ModuleSOBase : ScriptableObject, IModule
	{
		#region Properties

		public string UniqueIdentifier => string.Concat("ModuleSO-UUID:", GetInstanceID());

		public ModuleProcessor Processor
		{
			get; private set;
		}

		#endregion

		#region Public Methods

		public virtual void Init(ModuleProcessor parent)
		{
			Processor = parent;
		}

		public virtual void StartModule()
		{

		}

		public virtual void Deinit()
		{
			Processor = null;
		}

		public virtual void OnResolvedStack(ModuleAction coreAction)
		{

		}

		public abstract bool TryProcess(ModuleAction action, Action unlockAction);

		#endregion
	}
}
