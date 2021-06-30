using System.Collections.Generic;
using UnityEngine;
using ModuleSystem.Core;
using UnityEngine.Events;

namespace ModuleSystem.Timeline
{
	public class ModuleTimelineProcessor : MonoBehaviour, IHaveModuleProcessor
	{
		public const string TimelineDefaultLayer = "__Timeline-Default__";

		[SerializeField]
		private ModuleProcessorEvent _actionProcessedEvent = null;

		[SerializeField]
		private ModuleProcessorEvent _actionStackProcessedEvent = null;

		public ModuleProcessorEvent ActionProcessedEvent => _actionProcessedEvent;
		public ModuleProcessorEvent ActionStackProcessedEvent => _actionStackProcessedEvent;

		public ModuleProcessor Processor
		{
			get; private set;
		}

		protected void Awake()
		{
			ModuleTimelineModuleBase[] modules = GetComponentsInChildren<ModuleTimelineModuleBase>();
			Processor = new ModuleProcessor(false, modules);

			Processor.ActionProcessedEvent += OnActionProcessedEvent;
			Processor.ActionStackProcessedEvent += OnActionStackProcessedEvent;
		}

		protected void Start()
		{
			Processor.StartModules();
		}

		protected void OnDestroy()
		{
			Processor.ActionProcessedEvent -= OnActionProcessedEvent;
			Processor.ActionStackProcessedEvent -= OnActionStackProcessedEvent;
			Processor.Dispose();
			Processor = null;
		}

		public void Visualize(ModuleAction coreAction, string layer)
		{
			if (Processor != null)
			{
				Processor.EnqueueAction(coreAction, layer);
			}
		}

		public void Visualize(ModuleAction coreAction)
		{
			Visualize(coreAction, TimelineDefaultLayer);
		}

		private void OnActionProcessedEvent(ModuleAction moduleAction, string layer)
		{
			ActionProcessedEvent?.Invoke(moduleAction, layer);
		}

		private void OnActionStackProcessedEvent(ModuleAction moduleAction, string layer)
		{
			ActionStackProcessedEvent?.Invoke(moduleAction, layer);
		}
	}

	public enum VisualizationStage : int
	{
		None = 0,
		ReadyToVisualize = 1,
		Reserved = 2,
		Visualized = 3
	}
}