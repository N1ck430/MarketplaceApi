﻿namespace DataLibrary.Models.Requests.User;

public class RegisterRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string Email { get; init; }
}