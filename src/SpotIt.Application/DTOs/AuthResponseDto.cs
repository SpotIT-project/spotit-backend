using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Application.DTOs;

public record AuthResponseDto(
      string UserId,
      string Email,
      string FullName,
      string Role
  );
