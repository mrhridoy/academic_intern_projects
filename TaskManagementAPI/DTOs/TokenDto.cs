using System;

namespace TaskManagementAPI.DTOs;

public class TokenDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}