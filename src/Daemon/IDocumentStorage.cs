using System.Collections.Generic;
using System.Threading.Tasks;

namespace Idb.Sec.Convergence.Daemon
{
    public interface IDocumentStorage
    {
        Task<IEnumerable<Document>> SearchByCodeAsync(string code);
    }
}