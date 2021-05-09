using ModuleSystem.Core;
using System;

namespace ModuleSystem
{
	public abstract class DelayedModuleBase : ModuleBase
	{
		public override bool TryProcess(ModuleAction action, Action unlockAction)
		{
			return TryProcessInternal(action, unlockAction);
		}

		protected abstract bool TryProcessInternal(ModuleAction action, Action unlockMethod);
	}

	public abstract class DelayedModuleBase<T> : ModuleBase where T : ModuleAction
	{
		public override bool TryProcess(ModuleAction action, Action unlockAction)
		{
			return action is T castedAction && TryProcessInternal(castedAction, unlockAction);
		}

		protected abstract bool TryProcessInternal(T action, Action unlockMethod);
	}

	public abstract class BasicModuleBase : ModuleBase
	{
		public override bool TryProcess(ModuleAction action, Action unlockAction)
		{
			if(TryProcessInternal(action))
			{
				unlockAction();
				return true;
			}
			return false;
		}

		protected abstract bool TryProcessInternal(ModuleAction action);
	}

	public abstract class BasicModuleBase<T> : ModuleBase where T : ModuleAction
	{
		public override bool TryProcess(ModuleAction action, Action unlockAction)
		{
			if(action is T castedAction && TryProcessInternal(castedAction))
			{
				unlockAction();
				return true;
			}
			return false;
		}

		protected abstract bool TryProcessInternal(T action);
	}
}

namespace ModuleSystem.Core
{
	public abstract class ModuleBase : IModule
	{
		public ModuleProcessor Processor
		{
			get; private set;
		}

		public string UniqueIdentifier
		{
			get; private set;
		}

		public ModuleBase()
		{
			UniqueIdentifier = Guid.NewGuid().ToString();
		}

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
	}
}