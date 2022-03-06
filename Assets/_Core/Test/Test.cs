using ModuleSystem;
using ModuleSystem.Core;
using UnityEngine;

public class Test : MonoBehaviour
{
	[SerializeField]
	private ActionStackProcessedEvent _actionStackProcessedEvent = null;

	public ActionStackProcessedEvent ActionStackProcessedEvent => _actionStackProcessedEvent;

	public ModuleProcessor Processor
	{
		get; private set;
	}

	protected void Awake()
	{
		Processor = new ModuleProcessor(false, new IModule[] 
		{
			new BasicLambdaModule<MatchAction>((action, processor) =>
			{
				if(action.Iteration % 2 == 0)
				{
					action.ChainAction(new MatchAction());
				}
				action.ChainAction(new HitAction());
				return true;
			}),
			new BasicLambdaModule<HitAction>((action, processor) =>
			{
				if(action.ReactionCount < 5)
				{
					action.ReactionCount = action.ReactionCount + 1;
					action.ChainAction(new RemoveAction());
					return true;
				}
				return false;
			}, true),
			new BasicLambdaModule<RemoveAction>((action, processor) =>
			{
				return true;
			})
		});

		for (int i = 0; i < 1; i++)
		{
			Processor.EnqueueAction(new MatchAction() { Iteration = i });
		}

		Processor.ActionStackProcessedEvent += OnActionStackProcessedEvent;
	}

	protected void Start()
	{
		Processor.StartModules();
	}

	protected void OnDestroy()
	{
		if (Processor != null)
		{
			Processor.ActionStackProcessedEvent -= OnActionStackProcessedEvent;
			Processor.Dispose();
			Processor = null;
		}
	}

	private void OnActionStackProcessedEvent(ModuleAction moduleAction, ModuleProcessor processor)
	{
		ActionStackProcessedEvent.Emit(moduleAction);
	}

	public class MatchAction : ModuleAction
	{
		public int Iteration = 1;
	}

	public class HitAction : ModuleAction
	{
		public int ReactionCount = 0;
	}

	public class RemoveAction : ModuleAction
	{

	}
}
