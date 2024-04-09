using Microsoft.EntityFrameworkCore;
using RentABikeWebApp.Data;
using RentABikeWebApp.Data.Services;
using RentABikeWebApp.Models;

namespace RentABikeWebApp.Tests
{
    [TestClass]
    public class BikesServiceTests
    {
        [TestMethod]
        public async Task AddAsync_ShouldAddBikeToDatabase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "AddBikeToDatabase")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var service = new BikesService(context);
                var bike = new Bike
                {
                    Id = 1,
                    Type = BikeType.Mountain,
                    PricePerHour = 15.00m,
                    Status = "Available",
                    Image = new byte[] { 0x01, 0x02, 0x03 },
                };

                await service.AddAsync(bike);

                var result = await context.Bikes.FindAsync(1);
                Assert.IsNotNull(result);
                Assert.AreEqual(bike.Id, result.Id);
                Assert.AreEqual(bike.Type, result.Type);
                Assert.AreEqual(bike.PricePerHour, result.PricePerHour);
                Assert.AreEqual(bike.Status, result.Status);
                CollectionAssert.AreEqual(bike.Image, result.Image);
            }
        }

        [TestMethod]
        public async Task DeleteAsync_ShouldDeleteBikeFromDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "DeleteBikeFromDatabase")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var bike = new Bike { Id = 1 };
                await context.Bikes.AddAsync(bike);
                await context.SaveChangesAsync();

                var service = new BikesService(context);

                // Act
                await service.DeleteAsync(1);

                // Assert
                var result = await context.Bikes.FindAsync(1);
                Assert.IsNull(result);
            }
        }

        [TestMethod]
        public async Task GetAllAsync_ShouldReturnAllBikesFromDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "GetAllBikesFromDatabase")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var bikes = new List<Bike>
                {
                    new Bike { Id = 1, Type = BikeType.Simple },
                    new Bike { Id = 2, Type = BikeType.Mountain },
                    new Bike { Id = 3, Type = BikeType.Double }
                };
                await context.Bikes.AddRangeAsync(bikes);
                await context.SaveChangesAsync();

                var service = new BikesService(context);

                // Act
                var result = await service.GetAllAsync();

                // Assert
                CollectionAssert.AreEqual(bikes, result.ToList());
            }
        }

        [TestMethod]
        public async Task GetByIdAsync_ShouldReturnBikeWithGivenId()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "GetBikeByIdFromDatabase")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var bikes = new List<Bike>
                {
                    new Bike { Id = 1, Type = BikeType.Simple },
                    new Bike { Id = 2, Type = BikeType.Mountain },
                    new Bike { Id = 3, Type = BikeType.Double }
                };
                await context.Bikes.AddRangeAsync(bikes);
                await context.SaveChangesAsync();

                var service = new BikesService(context);

                // Act
                var result = await service.GetByIdAsync(2);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(bikes[1], result);
            }
        }

        [TestMethod]
        public async Task UpdateAsync_ShouldUpdateBikeInDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "UpdateBikeInDatabase")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var bike = new Bike { Id = 1, Type = BikeType.Simple, PricePerHour = 15.00m };
                await context.Bikes.AddAsync(bike);
                await context.SaveChangesAsync();
            }

            using (var context = new ApplicationDbContext(options))
            {
                var updatedBike = new Bike { Id = 1, Type = BikeType.Mountain, PricePerHour = 20.00m };

                var service = new BikesService(context);

                // Act
                await service.UpdateAsync(1, updatedBike);

                // Assert
                using (var assertContext = new ApplicationDbContext(options))
                {
                    var result = await assertContext.Bikes.FindAsync(1);
                    Assert.IsNotNull(result);
                    Assert.AreEqual(updatedBike.Type, result.Type);
                    Assert.AreEqual(updatedBike.PricePerHour, result.PricePerHour);
                }
            }
        }

    }
}