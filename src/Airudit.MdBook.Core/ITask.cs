
namespace Airudit.MdBook.Core;

using System.Threading;
using System.Threading.Tasks;

public interface ITask
{
    Task VisitAsync(PackageContext context, CancellationToken cancellationToken = default);
    Task VerifyAsync(PackageContext context, CancellationToken cancellationToken = default);
    Task RunAsync(PackageContext context, CancellationToken cancellationToken = default);
}
