using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ModuleSystem;
using ModuleSystem.Core;

public class Test : MonoBehaviour, IHaveModuleProcessor
{
	public ModuleProcessor Processor
	{
		get; private set;
	}

	protected void Awake()
	{
		Processor = new ModuleProcessor(true, 
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
	}

	protected void Start()
	{
		for (int i = 0; i < 6; i++)
		{
			Processor.EnqueueAction(new MatchAction() { Iteration = i });
		}
	}

	protected void OnDestroy()
	{
		Processor.Dispose();
		Processor = null;
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
