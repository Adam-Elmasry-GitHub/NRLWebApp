using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using FirstWebApplication.Models.Obstacle;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NRLWebApp.Tests.Mocks;
using Xunit;

namespace NRLWebApp.Tests
{
    public class RegisterforerControllerTests
    {
        [Fact]
        public async Task ApproveObstacle_InvalidModel_RedirectsToReview()
        {
            // Arrange
            var db = Mocks.TestDbContext.Create();
            db.Database.EnsureCreated();

            var logger = Mock.Of<ILogger<RegisterforerController>>();
            var controller = new RegisterforerController(db, logger);

            controller.ModelState.AddModelError("ObstacleId", "Required");

            var model = new ApproveObstacleViewModel { ObstacleId = 1 };

            // Act
            var result = await controller.ApproveObstacle(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ReviewObstacle", redirect.ActionName);
        }

        [Fact]
        public async Task ApproveObstacle_ExistingObstacle_CreatesNewStatusAndRedirects()
        {
            // Arrange
            var db = Mocks.TestDbContext.Create();
            db.Database.EnsureCreated();

            // Create a user who will approve
            var approver = new ApplicationUser { Id = "approver-1", Email = "approver@test" };
            db.Users.Add(approver);

            // Create a registering user and an obstacle
            var registrant = new ApplicationUser { Id = "pilot-1", Email = "pilot@test" };
            db.Users.Add(registrant);

            var obstacle = new Obstacle
            {
                RegisteredByUserId = registrant.Id,
                Location = "POINT(10 59)",
                Name = "Test Obstacle"
            };
            db.Obstacles.Add(obstacle);
            await db.SaveChangesAsync();

            var logger = Mock.Of<ILogger<RegisterforerController>>();
            var controller = new RegisterforerController(db, logger);

            // Setup HttpContext with approver identity
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, approver.Id) }))
                }
            };

            var model = new ApproveObstacleViewModel { ObstacleId = obstacle.Id, Comments = "Looks good" };

            // Act
            var result = await controller.ApproveObstacle(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AllObstacles", redirect.ActionName);

            // Verify that a new ObstacleStatus exists with StatusTypeId == 3 (Approved)
            var status = db.ObstacleStatuses.FirstOrDefault(s => s.ObstacleId == obstacle.Id && s.IsActive);
            Assert.NotNull(status);
            Assert.Equal(3, status.StatusTypeId);
            Assert.Equal(approver.Id, status.ChangedByUserId);

            // Verify obstacle.CurrentStatusId is set
            var updated = db.Obstacles.Find(obstacle.Id);
            Assert.NotNull(updated);
            Assert.NotNull(updated.CurrentStatusId);
        }
    }
}
