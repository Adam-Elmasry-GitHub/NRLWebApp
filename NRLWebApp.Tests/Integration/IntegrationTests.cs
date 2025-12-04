using System.Net;
using System.Net.Http.Headers;
using FirstWebApplication.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NRLWebApp.Tests.Integration
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public IntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DB context with in-memory for tests
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("IntegrationTestsDb");
                    });
                });
            });
        }

        [Fact]
        public async Task CspMiddlewareAddsSecurityHeaders()
        {
            // Simple smoke test: verify CSP middleware is working and adds headers
            var client = _factory.CreateClient();

            // Act - test home page (no auth required, [AllowAnonymous])
            var response = await client.GetAsync("/");

            // Assert - should return OK or redirect (302 if authenticated)
            Assert.True(
                response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Redirect,
                $"Expected OK/Redirect, got {response.StatusCode}"
            );

            // Check for security headers
            Assert.True(
                response.Headers.Contains("Content-Security-Policy"),
                "CSP header should be present"
            );
            
            Assert.True(
                response.Headers.Contains("X-Content-Type-Options"),
                "X-Content-Type-Options header should be present"
            );

            Assert.True(
                response.Headers.Contains("X-Frame-Options"),
                "X-Frame-Options header should be present"
            );
        }
    }
}
