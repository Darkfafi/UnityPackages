using UnityEngine.Events;

namespace ModuleSystem.Core
{
	[System.Serializable]
	public class ModuleProcessorEvent : UnityEvent<ModuleAction, string>
	{

	}

	public interface IHaveModuleProcessor
	{
		ModuleProcessorEvent ActionProcessedEvent
		{
			get;
		}

		ModuleProcessorEvent ActionStackProcessedEvent
		{
			get;
		}

		ModuleProcessor Processor
		{
			get;
		}
	}
}