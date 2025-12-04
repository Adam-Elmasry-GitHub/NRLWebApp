using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using FirstWebApplication.Entities;
using FirstWebApplication.Services;
using NRLWebApp.Tests.Mocks;
using Xunit;

namespace NRLWebApp.Tests
{
    public class ExportServiceTests
    {
        [Fact]
        public void GenerateObstaclesExcel_ReturnsValidExcel()
        {
            // Arrange
            var db = TestDbContext.Create();

            var user = new ApplicationUser { Id = "u1", Email = "u1@test" };
            db.Users.Add(user);

            var type = new ObstacleType { Id = 1, Name = "Tower" };
            db.ObstacleTypes.Add(type);

            var obstacle = new Obstacle
            {
                RegisteredByUserId = user.Id,
                RegisteredByUser = user,
                ObstacleType = type,
                Name = "Test Obs",
                Location = "POINT(10 59)",
                RegisteredDate = System.DateTime.Now
            };
            db.Obstacles.Add(obstacle);
            db.SaveChanges();

            var service = new ExportService();

            var obstacles = db.Obstacles.Include(o => o.ObstacleType).Include(o => o.RegisteredByUser).ToList();

            // Act
            var bytes = service.GenerateObstaclesExcel(obstacles);

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            using var ms = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(ms);
            var ws = workbook.Worksheets.First();
            Assert.Equal("ID", ws.Cell(1, 1).GetString());
            Assert.Equal("Name", ws.Cell(1, 2).GetString());
        }
    }
}
