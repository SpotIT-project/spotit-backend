using SpotIt.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace SpotIt.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccesToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal?GetPrincipalFromExpiredToken(string token);
}
