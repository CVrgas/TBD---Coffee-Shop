using Application.Auth.Dtos;
using Application.Auth.Interfaces;
using Application.Common.Abstractions.Envelope;

namespace Application.Auth.Services;

public class AuthQueryService(IAuthQueries queries) : IAuthQueryService
{
    public async Task<Envelope<UserMeDto>> GetMe()
    {
        var me = await queries.GetMe();
        return me == null ? Envelope<UserMeDto>.NotFound() : Envelope<UserMeDto>.Ok(me);
    }
}