using System;
using System.Collections;
using UnityEngine;

namespace ModuleSystem.Timeline
{
	public abstract class ModuleTimelineModuleBase : DelayedModuleBehaviourBase
	{
		public const string VisualizationStageMark = "_ModuleTimelineModuleBase_VisualizationStage";
		public const string ReservedByTimelineMark = "_ModuleTimelineModuleBase_ReservedByTimeline";

		public void SetVisualizationStage(ModuleAction action, VisualizationStage stage)
		{
			VisualizationStage oldStage = GetVisualizationStage(action);

			if (oldStage < stage)
			{
				action.DataMap.RemoveMark(VisualizationStageMark, oldStage.ToString());
				action.DataMap.Mark(VisualizationStageMark, stage.ToString());

				// Process action on different layer, so the children are processed on a new 'thread'
				switch(stage)
				{
					case VisualizationStage.ReadyToVisualize:
					case VisualizationStage.Visualized:
						action.DataMap.Mark(ReservedByTimelineMark, action.UniqueIdentifier);
						Processor.EnqueueAction(action, action.UniqueIdentifier);
						break;
				}
			}
		}

		public VisualizationStage GetVisualizationStage(ModuleAction action)
		{
			VisualizationStage result = VisualizationStage.None;
			if(action.DataMap.TryGetMarkSuffixes(VisualizationStageMark, out string[] suffixes))
			{
				for(int i = 0; i < suffixes.Length; i++)
				{
					if(Enum.TryParse(suffixes[i], out VisualizationStage stage) && result < stage)
					{
						result = stage;
					}
				}
			}
			return result;
		}


		#region Protected Methods

		protected override bool TryProcessInternal(ModuleAction action, Action unlockMethod)
		{
			if (GetVisualizationStage(action) >= VisualizationStage.Reserved)
			{
				return false;
			}

			ModuleAction moduleAction = action;

			Action internalUnlockMethod = new Action(() =>
			{
				SetVisualizationStage(moduleAction, VisualizationStage.Visualized);
				unlockMethod();
			});

			if (TryProcessTimeline(moduleAction, internalUnlockMethod))
			{
				SetVisualizationStage(moduleAction, VisualizationStage.Reserved);
				return true;
			}

			return false;
		}

		protected abstract bool TryProcessTimeline(ModuleAction action, Action unlockMethod);

		protected void DoCallbackAfterDelay(float delay, Action callback)
		{
			if (delay <= 0)
			{
				callback?.Invoke();
			}
			else
			{
				StartCoroutine(CallbackAfterDelayRoutine(delay, callback));
			}
		}

		protected void DoCallbackAfterCondition(Func<bool> condition, Action callback)
		{
			if (!condition())
			{
				callback();
			}
			else
			{
				StartCoroutine(CallbackAfterConditionRoutine(condition, callback));
			}
		}

		#endregion

		#region Private Methods

		private IEnumerator CallbackAfterDelayRoutine(float delay, Action callback)
		{
			yield return new WaitForSeconds(delay);
			callback?.Invoke();
		}

		private IEnumerator CallbackAfterConditionRoutine(Func<bool> condition, Action callback)
		{
			yield return new WaitWhile(condition);
			callback?.Invoke();
		}

		#endregion

	}
}