using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Application.DTOs;

public record LoginRequestDto(
      string Email,
      string Password
  );
