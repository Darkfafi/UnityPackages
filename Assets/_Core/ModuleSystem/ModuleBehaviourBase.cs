﻿using ModuleSystem.Core;
using System;
using UnityEngine;

namespace ModuleSystem
{
	public abstract class DelayedModuleBehaviourBase : ModuleBehaviourBase
	{
		public override bool TryProcess(ModuleAction action)
		{
			return TryProcessInternal(action, Unlock);
		}

		protected abstract bool TryProcessInternal(ModuleAction action, Action unlockMethod);
	}

	public abstract class BasicModuleBehaviourBase : ModuleBehaviourBase
	{
		public override bool TryProcess(ModuleAction action)
		{
			if (TryProcessInternal(action))
			{
				Unlock();
				return true;
			}
			return false;
		}

		protected abstract bool TryProcessInternal(ModuleAction action);
	}
}

namespace ModuleSystem.Core
{
	public abstract class ModuleBehaviourBase : MonoBehaviour, IModule
	{
		#region Properties

		public ModuleProcessor Processor
		{
			get; private set;
		}

		public string UniqueIdentifier => string.Concat("ModuleBehaviour-UUID:", GetInstanceID());

		public bool IsLocking => Processor != null && Processor.IsLockingModule(this);

		#endregion

		#region Public Methods

		public void Unlock()
		{
			Processor.Unlock(this);
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

		public abstract bool TryProcess(ModuleAction action);

		#endregion
	}
}
