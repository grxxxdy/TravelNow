using System.Net;
using System.Text;
using System.Text.Json;
using contract_tests.ApiGatewayTests;
using Microsoft.AspNetCore.Mvc.Testing;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Matchers;
using Xunit.Abstractions;

namespace contract_tests.ConsumerTests;


public class ApiGatewayConsumer : IClassFixture<CustomApiGatewayFactory>
{
    private readonly CustomApiGatewayFactory _factory;
    private readonly ITestOutputHelper _output;

    public ApiGatewayConsumer(CustomApiGatewayFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Login_WhenValidRequest_ReturnsExpectedResponse()
    {
        // Arrange: Pact definition
        var pact = Pact.V4("API Gateway", "UserService", new PactConfig
        {
            PactDir = $"{Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.Parent!.FullName}/pacts",
            LogLevel = PactLogLevel.Debug,
            Outputters = new List<IOutput>
            {
                new XUnitOutput(_output)
            }
        });

        var pactBuilder = pact.WithHttpInteractions();

        pactBuilder
            .UponReceiving("A valid login request")
            .WithRequest(HttpMethod.Post, "/api/gateway/user/login")
            .WithHeader("Content-Type", "application/json")
            .WithJsonBody(new
            {
                email = "test@example.com",
                password = "password123"
            })
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(new
            {
                success = Match.Type(true),
                message = Match.Type("mocked-jwt-token")
            });
        
        var requestBody = new
        {
            email = "test@example.com",
            password = "password123"
        };

        // Act
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/gateway/user/login", new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"));

        await pactBuilder.VerifyAsync(async ctx =>
        {
            var с = new HttpClient
            {
                BaseAddress = ctx.MockServerUri
            };
            
            await с.PostAsync("/api/gateway/user/login", new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"));

            var content = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonElement>(content);
            
            _output.WriteLine($"Response: {responseJson}");
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(responseJson.GetProperty("success").GetBoolean());
            Assert.Equal("mocked-jwt-token", responseJson.GetProperty("message").GetString());
        });
    }
}

public class CustomApiGatewayFactory : WebApplicationFactory<api_gateway.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        
        builder.ConfigureServices(services =>
        {
            services.AddHostedService<MockUserServiceListener>();
        });
        
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
    }
}
