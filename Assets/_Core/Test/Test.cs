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
		});

		for (int i = 0; i < 6; i++)
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

	private void OnActionStackProcessedEvent(ModuleAction moduleAction)
	{
		ActionStackProcessedEvent.Emit(moduleAction);
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
