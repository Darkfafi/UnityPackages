namespace ModuleSystem.Core
{
	public interface IModule
	{
		string UniqueIdentifier
		{
			get;
		}

		bool TryProcess(ModuleAction action);
		void OnResolvedStack(ModuleAction coreAction);
		void Init(ModuleProcessor parent);
		void StartModule();
		void Deinit();
	}
}