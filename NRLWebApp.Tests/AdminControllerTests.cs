using System.Collections.Generic;
using System.Threading.Tasks;
using FirstWebApplication.Controllers;
using FirstWebApplication.Entities;
using FirstWebApplication.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NRLWebApp.Tests.Mocks;
using Xunit;

namespace NRLWebApp.Tests
{
    public class AdminControllerTests
    {
        [Fact]
        public async Task AssignRole_CallsAddToRoleAndRedirects()
        {
            // Arrange
            var mockUserManager = MockUserManager.Create();
            var mockRoleManager = MockRoleManager.Create();
            var mockRoleService = new Mock<UserRoleService>(mockUserManager.Object, mockRoleManager.Object);

            var db = Mocks.TestDbContext.Create();

            var controller = new AdminController(mockUserManager.Object, mockRoleService.Object, db, new ExportService());
            // Setup TempData to avoid NullReference when controller sets TempData in methods
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            // Act
            var result = await controller.AssignRole("some-id", "Pilot");

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Pilot"), Times.Once);
        }

        [Fact]
        public async Task RemoveRole_DoesNotRemoveLastAdmin_ReturnsRedirect()
        {
            // Arrange
            var mockUserManager = MockUserManager.Create();
            var mockRoleManager = MockRoleManager.Create();
            var mockRoleService = new Mock<UserRoleService>(mockUserManager.Object, mockRoleManager.Object);

            // Simuler at det kun finnes én admin
            mockRoleService
                .Setup(s => s.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<ApplicationUser> { new ApplicationUser { Id = "only-admin", Email = "admin@test" } });

            var db = Mocks.TestDbContext.Create();

            var controller = new AdminController(mockUserManager.Object, mockRoleService.Object, db, new ExportService());
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            // Act
            var result = await controller.RemoveRole("some-id", "Admin");

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            mockUserManager.Verify(um => um.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), "Admin"), Times.Never);
        }
    }
}
