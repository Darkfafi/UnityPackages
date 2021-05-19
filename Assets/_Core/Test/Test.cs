using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ModuleSystem;
using ModuleSystem.Core;
using ModuleSystem.Timeline;
using System;
using UnityEngine.Events;

public class Test : MonoBehaviour, IHaveModuleProcessor
{
	[SerializeField]
	private ModuleTimelineProcessor _timelineProcessor = null;

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
		Processor = new ModuleProcessor(false, 
			new BasicLambdaModule<MatchAction>((action, processor) => 
			{
				if(action.Iteration % 2 == 0)
				{
					action.EnqueueAction(new MatchAction());
				}

				for(int i = 0; i < action.Iteration * 2; i++)
				{
					action.ChainAction(new HitAction());
				}
				return true;
			}),
			new BasicLambdaModule<HitAction>((action, processor) =>
			{
				action.ChainAction(new RemoveAction());
				return true;
			}),
			new BasicLambdaModule<RemoveAction>((action, processor) =>
			{
				Debug.Log("Removed Tile");
				return true;
			})
		);

		for (int i = 0; i < 6; i++)
		{
			Processor.EnqueueAction(new MatchAction() { Iteration = i });
		}

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

	private void OnActionProcessedEvent(ModuleAction moduleAction, string layer)
	{
		ActionProcessedEvent?.Invoke(moduleAction, layer);
	}

	private void OnActionStackProcessedEvent(ModuleAction moduleAction, string layer)
	{
		ActionStackProcessedEvent?.Invoke(moduleAction, layer);
		_timelineProcessor.Visualize(moduleAction);
	}

	public class MatchAction : ModuleAction
	{
		public int Iteration = 1;
	}

	public class HitAction : ModuleAction
	{

	}

	public class RemoveAction : ModuleAction
	{

	}
}
