using System.Net;
using System.Text.Json;
using InventorySystem.Server.Authorization;
using InventorySystem.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace unit_testing;

/// <summary>
/// Unit tests for <see cref="AuthController"/>.
/// Verifies the token proxy endpoint handles successful authentication,
/// Keycloak error responses, and connectivity failures gracefully.
/// </summary>
public class AuthControllerTests
{
    private readonly IOptions<KeycloakAuthorizationOptions> _options;

    public AuthControllerTests()
    {
        _options = Options.Create(new KeycloakAuthorizationOptions
        {
            Authority = "http://localhost:8080/realms/inventory-realm",
            Audience = "inventory-client"
        });
    }

    [Fact]
    public async Task GetToken_ReturnsOk_WithAccessToken_WhenKeycloakReturnsToken()
    {
        // Arrange
        var tokenResponseJson = JsonSerializer.Serialize(new
        {
            access_token = "mocked-jwt-access-token",
            expires_in = 300,
            token_type = "Bearer"
        });

        var handlerMock = CreateHttpMessageHandlerMock(HttpStatusCode.OK, tokenResponseJson);
        var httpClient = new HttpClient(handlerMock.Object);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var controller = new AuthController(factoryMock.Object, _options);
        var loginRequest = new LoginRequest { Username = "staff", Password = "12345" };

        // Act
        var result = await controller.GetToken(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var jsonElement = Assert.IsType<JsonElement>(okResult.Value);
        Assert.Equal("mocked-jwt-access-token", jsonElement.GetProperty("access_token").GetString());
    }

    [Fact]
    public async Task GetToken_ReturnsBadRequest_WhenKeycloakReturnsInvalidGrant()
    {
        // Arrange
        var errorResponseJson = JsonSerializer.Serialize(new
        {
            error = "invalid_grant",
            error_description = "Invalid user credentials"
        });

        var handlerMock = CreateHttpMessageHandlerMock(HttpStatusCode.BadRequest, errorResponseJson);
        var httpClient = new HttpClient(handlerMock.Object);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var controller = new AuthController(factoryMock.Object, _options);
        var loginRequest = new LoginRequest { Username = "staff", Password = "wrong-password" };

        // Act
        var result = await controller.GetToken(loginRequest);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetToken_Returns502BadGateway_WhenKeycloakIsUnreachable()
    {
        // Arrange: Mock handler throwing HttpRequestException
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var controller = new AuthController(factoryMock.Object, _options);
        var loginRequest = new LoginRequest { Username = "staff", Password = "12345" };

        // Act
        var result = await controller.GetToken(loginRequest);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, statusResult.StatusCode);
    }

    private static Mock<HttpMessageHandler> CreateHttpMessageHandlerMock(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });

        return handlerMock;
    }
}
