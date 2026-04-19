using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Application.DTOs;

public record RegisterRequestDto(
      string FullName,
      string Email,
      string Password,
      string City
  );
