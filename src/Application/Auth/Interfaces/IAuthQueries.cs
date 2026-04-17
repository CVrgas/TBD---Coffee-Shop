using Application.Auth.Dtos;
using Application.Auth.Services;

namespace Application.Auth.Interfaces;

public interface IAuthQueries
{
    Task<UserMeDto?> GetMe();
}