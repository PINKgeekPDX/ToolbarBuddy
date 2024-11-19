using System.Threading;
using System.Threading.Tasks;

namespace ToolBarApp.Services.Interfaces
{
    public interface IToolbarPlugin
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        string Version { get; }
        Task InitializeAsync();
        Task<string> ExecuteAsync(IDictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
        Task ShutdownAsync();
        bool IsCompatible();
    }
}
