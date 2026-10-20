using DS.Domain.DTOs;
using DS.Domain.Models;

namespace DS.Application.Services.Groups
{
    public record GroupPreSignupDetails(int Id, string Name, District District, UpdateGroupPreSignupDto Counts);
}
