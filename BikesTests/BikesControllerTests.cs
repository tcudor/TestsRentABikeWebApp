using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RentABikeWebApp.Controllers;
using RentABikeWebApp.Data;
using RentABikeWebApp.Models;
using RentABikeWebApp.Data.Services;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TestsRentABikeWebApp.BikesTests
{
    [TestClass]
    public class BikesControllerTests
    {
        private ApplicationDbContext _context;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            SeedDatabase();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void SeedDatabase()
        {
            var bikes = new[]
            {
                new Bike { Id = 1, Type = BikeType.Double, PricePerHour = 20.00m },
                new Bike { Id = 2, Type = BikeType.Mountain, PricePerHour = 25.00m }
            };
            _context.Bikes.AddRange(bikes);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task Index_ReturnsViewResultWithAllBikes()
        {
            var controller = new BikesController(new BikesService(_context));
            var result = await controller.Index() as ViewResult;
            Assert.IsNotNull(result);
            var model = result.Model as IEnumerable<Bike>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count());
        }

        [TestMethod]
        public async Task Create_Post_ReturnsRedirectToActionResult_WhenModelStateIsValid()
        {
            var controller = new BikesController(new BikesService(_context));
            var bike = new Bike { Type = BikeType.Simple, PricePerHour = 30.00m };
            var image = new FormFile(new MemoryStream(new byte[] { }), 0, 0, "imageFile", "image.jpg");
            var result = await controller.Create(bike, image) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }

        [TestMethod]
        public async Task Create_Post_ReturnsViewResultWithModel_WhenModelStateIsInvalid()
        {
            var controller = new BikesController(new BikesService(_context));
            var bike = new Bike { Type = BikeType.Simple };
            var image = new FormFile(new MemoryStream(new byte[] { }), 0, 0, "imageFile", "image.jpg");
            controller.ModelState.AddModelError("PricePerHour", "Price per hour is required");
            var result = await controller.Create(bike, image) as ViewResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(bike, result.Model);
        }

        [TestMethod]
        public async Task Details_ReturnsViewResultWithBike_WhenBikeExists()
        {
            var controller = new BikesController(new BikesService(_context));
            var result = await controller.Details(1) as ViewResult;
            Assert.IsNotNull(result);
            var model = result.Model as Bike;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Id);
        }

        [TestMethod]
        public async Task Details_ReturnsNotFound_WhenBikeDoesNotExist()
        {
            var controller = new BikesController(new BikesService(_context));
            var result = await controller.Details(100) as ViewResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("NotFound", result.ViewName);
        }

        [TestMethod]
        public async Task Edit_ReturnsNotFound_WhenBikeIsNull()
        {
            var controller = new BikesController(new BikesService(_context));
            var result = await controller.Edit(100);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.AreEqual("NotFound", viewResult.ViewName);
        }

        [TestMethod]
        public async Task Edit_ReturnsViewResult_WhenBikeExists()
        {
            var controller = new BikesController(new BikesService(_context));
            var result = await controller.Edit(1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
        }

        [TestMethod]
        public async Task Edit_UpdatesBikeInDatabase_WhenModelStateIsValid()
        {
            var controller = new BikesController(new BikesService(_context));
            var existingBike = await _context.Bikes.FindAsync(1);
            var image = new FormFile(new MemoryStream(new byte[] { }), 0, 0, "imageFile", "image.jpg");
            existingBike.Type = BikeType.Simple;
            existingBike.PricePerHour = 30.00m;
            var result = await controller.Edit(existingBike.Id, existingBike, image);
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectToActionResult = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirectToActionResult.ActionName);
            var editedBike = await _context.Bikes.FindAsync(existingBike.Id);
            Assert.AreEqual(existingBike.Type, editedBike.Type);
            Assert.AreEqual(existingBike.PricePerHour, editedBike.PricePerHour);
        }



        [TestMethod]
        public async Task Delete_ReturnsNotFound_WhenBikeIsNull()
        {
            var controller = new BikesController(new BikesService(_context));
            var result = await controller.Delete(100);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.AreEqual("NotFound", viewResult.ViewName);
        }

        [TestMethod]
        public async Task Delete_ReturnsViewResult_WhenBikeExists()
        {
            var controller = new BikesController(new BikesService(_context));
            var result = await controller.Delete(1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsNull(viewResult.ViewName);
        }

        [TestMethod]
        public async Task DeleteConfirmed_DeletesBikeFromDatabase()
        {
            var controller = new BikesController(new BikesService(_context));
            await controller.DeleteConfirmed(1);
            var deletedBike = await _context.Bikes.FindAsync(1);
            Assert.IsNull(deletedBike);
        }

        [TestMethod]
        public async Task Edit_ReturnsNotFound_WhenIdIsInvalid()
        {
            var controller = new BikesController(new BikesService(_context));
            var result = await controller.Edit(-1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.AreEqual("NotFound", viewResult.ViewName);
        }

        [TestMethod]
        public async Task Delete_ReturnsNotFound_WhenIdIsInvalid()
        {
            var controller = new BikesController(new BikesService(_context));
            var result = await controller.Delete(-1);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.AreEqual("NotFound", viewResult.ViewName);
        }

    }
}
