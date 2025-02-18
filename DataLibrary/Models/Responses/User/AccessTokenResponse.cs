﻿using System.Text.Json.Serialization;

namespace DataLibrary.Models.Responses.User;

public class AccessTokenResponse
{
    /// <summary>
    /// The value is always "Bearer" which indicates this response provides a "Bearer" token
    /// in the form of an opaque <see cref="AccessToken"/>.
    /// </summary>
    /// <remarks>
    /// This is serialized as "token_type": "Bearer" using System.Text.Json.
    /// </remarks>
    public string TokenType { get; } = "Bearer";

    /// <summary>
    /// The opaque bearer token to send as part of the Authorization request header.
    /// </summary>
    /// <remarks>
    /// This is serialized as "access_token": "{AccessToken}" using System.Text.Json.
    /// </remarks>
    public required string AccessToken { get; init; }

    /// <summary>
    /// The number of seconds before the <see cref="AccessToken"/> expires.
    /// </summary>
    /// <remarks>
    /// This is serialized as "expires_in": "{ExpiresInSeconds}" using System.Text.Json.
    /// </remarks>
    public required long ExpiresInSeconds { get; init; }

    /// <summary>
    /// If set, this provides the ability to get a new access_token after it expires using a refresh endpoint.
    /// </summary>
    /// <remarks>
    /// This is serialized as "refresh_token": "{RefreshToken}" using System.Text.Json.
    /// </remarks>
    public required string RefreshToken { get; init; }
}