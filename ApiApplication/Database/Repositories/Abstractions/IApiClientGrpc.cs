using ProtoDefinitions;
using System.Threading.Tasks;

namespace ApiApplication.Database.Repositories.Abstractions
{
    public interface IApiClientGrpc
    {
        Task<showListResponse> GetAll();
        Task<showResponse> GetById(string id);
    }
}
