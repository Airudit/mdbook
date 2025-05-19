
namespace Airudit.MdBook.Core.Internals;

using System;

public interface ITask
{
    void Visit(PackageContext context);
    void Verify(PackageContext context);
    void Run(PackageContext context);
}
