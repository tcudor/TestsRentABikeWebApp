using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RentABikeWebApp.Controllers;
using RentABikeWebApp.Data;
using RentABikeWebApp.Data.Services;
using RentABikeWebApp.Data.ViewModels;
using RentABikeWebApp.Models;

namespace TestsRentABikeWebApp.ReservationsTests
{
    [TestClass]
    public class ReservationsControllerTests
    {
        private ApplicationDbContext? _context;
        private ReservationsController? _controller;
        private UserManager<IdentityUser>? _userManager;
        private ICustomersService? _customersService;

        [TestInitialize]
        public void Initialize()
        {
            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDB")
                .Options;
            _context = new ApplicationDbContext(opts);
            _context.Database.EnsureCreated();

            _context.Bikes.AddRange(
                new Bike { Id = 1, Type = BikeType.Simple, PricePerHour = 10m },
                new Bike { Id = 2, Type = BikeType.Hybrid, PricePerHour = 8m }
            );

            _context.Customers.AddRange(
                new Customer { Id = 1, Name = "John Doe", UserId = null },
                new Customer { Id = 2, Name = "Jane Roe", UserId = null }
            );

            _context.Reservations.Add(new Reservation
            {
                Id = 1,
                BikeId = 1,
                CustomerId = 1,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            });

            _context.SaveChanges();

            var store = new UserStore<IdentityUser>(_context);
            _userManager = new UserManager<IdentityUser>(
                store, null!,
                new PasswordHasher<IdentityUser>(),
                Array.Empty<IUserValidator<IdentityUser>>(),
                Array.Empty<IPasswordValidator<IdentityUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(), null!, null!
            );

            _customersService = new CustomersService(_context);
            var svc = new ReservationsService(_context);
            _controller = new ReservationsController(svc, _customersService, _userManager);

            var identity = new ClaimsIdentity(new[]{
                new Claim(ClaimTypes.NameIdentifier, "test-user"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task Index_ReturnsAllReservations()
        {
            var result = await _controller.Index() as ViewResult;
            Assert.IsNotNull(result);
            var model = result.Model as IQueryable<Reservation> ?? ((IEnumerable<Reservation>)result.Model!).AsQueryable();
            Assert.AreEqual(1, model.Count());
        }

        [TestMethod]
        public async Task Create_GET_PopulatesFormVM()
        {

            var result = await _controller.Create(1) as ViewResult;

            Assert.IsNotNull(result);
            var vm = result.Model as ReservationFormVM;
            Assert.IsNotNull(vm);
            Assert.AreEqual(2, vm.Bikes.Count);                         
            Assert.AreEqual(2, vm.Customers.Count);                     
            Assert.AreEqual(10m, vm.PricePerHour);                     
            Assert.AreEqual(1, vm.ActiveReservations.Count());          
        }

        [TestMethod]
        public async Task Create_POST_Redirects_WhenValid()
        {
            var vm = new ReservationFormVM
            {
                Reservation = new Reservation
                {
                    BikeId = 1,
                    CustomerId = 1,
                    StartDate = DateTime.Today.AddDays(5),
                    EndDate = DateTime.Today.AddDays(6)
                }
            };

            var result = await _controller.Create(vm) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }

        [TestMethod]
        public async Task Create_POST_ShowsOverlapError_WhenConflict()
        {
           
            var vm = new ReservationFormVM
            {
                Reservation = new Reservation
                {
                    BikeId = 1,
                    CustomerId = 1,
                    StartDate = DateTime.Today.AddDays(1).AddHours(10),
                    EndDate = DateTime.Today.AddDays(1).AddHours(11)
                }
            };

            var result = await _controller.Create(vm) as ViewResult;
            Assert.IsNotNull(result);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.IsTrue(result.ViewData.ModelState.ContainsKey("ReservationOverlap"));
        }

        [TestMethod]
        public async Task Edit_GET_PopulatesFormVM()
        {
            var result = await _controller.Edit(1) as ViewResult;
            Assert.IsNotNull(result);
            var vm = result.Model as ReservationFormVM;
            Assert.IsNotNull(vm);
            Assert.AreEqual(2, vm.Bikes.Count);
            Assert.AreEqual(2, vm.Customers.Count);
            Assert.AreEqual(0, vm.ActiveReservations.Count()); 
        }

        [TestMethod]
        public async Task Edit_POST_Redirects_WhenValid()
        {
            var existing = await _context.Reservations.FindAsync(1);
            var vm = new ReservationFormVM
            {
                Reservation = existing!
            };
            vm.Reservation.StartDate = vm.Reservation.EndDate.AddDays(1);
            vm.Reservation.EndDate = vm.Reservation.EndDate.AddDays(2);

            var result = await _controller.Edit(1, vm) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }

        [TestMethod]
        public async Task Edit_POST_ShowsOverlapError_WhenConflict()
        {
            _context.Reservations.Add(new Reservation
            {
                Id = 2,
                BikeId = 1,
                CustomerId = 2,
                StartDate = DateTime.Today.AddDays(1).AddHours(11),
                EndDate = DateTime.Today.AddDays(1).AddHours(13)
            });
            _context.SaveChanges();
            var existing = await _context.Reservations.FindAsync(1);
            existing!.StartDate = DateTime.Today.AddDays(1).AddHours(12);
            existing.EndDate = DateTime.Today.AddDays(1).AddHours(14);
            var vm = new ReservationFormVM { Reservation = existing };

            var result = await _controller.Edit(1, vm) as ViewResult;
            Assert.IsNotNull(result);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.IsTrue(result.ViewData.ModelState.ContainsKey("ReservationOverlap"));
        }

        [TestMethod]
        public async Task GetData_ReturnsCorrectJson()
        {
            var result = await _controller.GetData(1) as JsonResult;
            Assert.IsNotNull(result);
            var val = result!.Value!;
            var t = val.GetType();
            var priceProp = t.GetProperty("pricePerHour");
            Assert.IsNotNull(priceProp, "pricePerHour property missing");
            var price = (decimal)priceProp.GetValue(val)!;
            Assert.AreEqual(10m, price);
            var activeProp = t.GetProperty("activeReservations");
            Assert.IsNotNull(activeProp, "activeReservations property missing");
            var activeList = (IEnumerable<object>)activeProp.GetValue(val)!;
            Assert.IsTrue(activeList.Any(), "Expected at least one active reservation");
        }



        [TestMethod]
        public async Task Details_ReturnsViewResult_WithReservationModel()
        {
            var result = await _controller.Details(1) as ViewResult;
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Reservation));
        }

        [TestMethod]
        public async Task Delete_GET_ReturnsViewResult_WhenReservationExists()
        {
            var result = await _controller.Delete(1) as ViewResult;
            Assert.IsNotNull(result);
            Assert.IsNull(result.ViewName);
        }

        [TestMethod]
        public async Task Delete_GET_ReturnsNotFound_WhenReservationDoesNotExist()
        {
            var result = await _controller.Delete(-1) as ViewResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("NotFound", result.ViewName);
        }

        [TestMethod]
        public async Task DeleteConfirmed_DeletesReservationFromDatabase()
        {
            await _controller.DeleteConfirmed(1);
            var deleted = await _context.Reservations.FindAsync(1);
            Assert.IsNull(deleted);
        }
    }
}
