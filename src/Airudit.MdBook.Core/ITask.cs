
namespace Airudit.MdBook.Core;

using System;

public interface ITask
{
    void Visit(PackageContext context);
    void Verify(PackageContext context);
    void Run(PackageContext context);
}
