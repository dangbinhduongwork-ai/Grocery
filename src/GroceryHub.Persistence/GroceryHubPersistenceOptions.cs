using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Persistence;

public sealed class GroceryHubPersistenceOptions
{
    public GroceryHubPersistenceOptions()
    {
    }

    public GroceryHubPersistenceOptions(params Assembly[] configurationAssemblies)
    {
        ArgumentNullException.ThrowIfNull(configurationAssemblies);
        foreach (Assembly assembly in configurationAssemblies)
        {
            ConfigurationAssemblies.Add(assembly);
        }
    }

    public IList<Assembly> ConfigurationAssemblies { get; } = new List<Assembly>();

    public Action<ModelBuilder>? ConfigureModel { get; set; }
}
