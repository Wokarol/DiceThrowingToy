using Wokarol.Common;
using Wokarol.GameSystemsLocator.Bootstrapping;
using Wokarol.GameSystemsLocator.Core;

public class GameConfig : ISystemConfiguration
{
    public void Configure(ServiceLocatorBuilder builder)
    {
        builder.PrefabPath = "Systems";

        // Plumbing
        builder.Add<InputBlocker>(required: true, createIfNotPresent: true);

        // Game
    }
}
