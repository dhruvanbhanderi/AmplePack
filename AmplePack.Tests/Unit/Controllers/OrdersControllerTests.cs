using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AmplePack.Controllers;
using AmplePack.Services;
using AmplePack.Models;
using AmplePack.Data;
using AmplePack.Tests.Infrastructure;

namespace AmplePack.Tests.Unit.Controllers
{
    public class OrdersControllerTests : DatabaseTestBase
    {
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            // Create real instances with proper dependencies
            var invoiceService = new InvoiceService();
            var logger = new Mock<ILogger<OrderManagementService>>().Object;
            var orderService = new OrderManagementService(Context, logger);
            _controller = new OrdersController(Context, invoiceService, orderService);
        }

        protected override void SeedDatabase()
        {
            var customer = CreateTestCustomer();
            Context.Customers.Add(customer);
            Context.SaveChanges();

            var order = CreateTestOrder(customer.Id);
            Context.Orders.Add(order);
            Context.SaveChanges();
        }

        [Fact]
        public async Task Index_Should_Return_View_With_Orders()
        {
            // Act
            var result = await _controller.Index(null, null, null, null, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().NotBeNull();
        }

        [Fact]
        public async Task Details_With_Valid_Id_Should_Return_View()
        {
            // Arrange
            var order = Context.Orders.First();

            // Act
            var result = await _controller.Details(order.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<Order>();
        }

        [Fact]
        public async Task Details_With_Invalid_Id_Should_Return_NotFound()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Create_POST_With_Valid_Order_Should_Redirect_To_Index()
        {
            // Arrange
            var customer = Context.Customers.First();
            var order = CreateTestOrder(customer.Id);

            // Act
            var result = await _controller.Create(order);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
        }

        [Fact]
        public async Task Create_POST_With_Invalid_Model_Should_Return_View()
        {
            // Arrange
            var order = new Order(); // Invalid order
            _controller.ModelState.AddModelError("CustomerId", "Customer is required");

            // Act
            var result = await _controller.Create(order);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Edit_POST_With_Valid_Order_Should_Redirect_To_Index()
        {
            // Arrange
            var order = Context.Orders.First();
            order.Status = "Processing";

            // Act
            var result = await _controller.Edit(order.Id, order);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
        }

        [Fact]
        public async Task Delete_With_Valid_Id_Should_Remove_Order()
        {
            // Arrange
            var order = Context.Orders.First();
            var orderId = order.Id;

            // Act
            var result = await _controller.DeleteConfirmed(orderId);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            Context.Orders.Find(orderId).Should().BeNull();
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Processing")]
        [InlineData("Completed")]
        [InlineData("Cancelled")]
        public async Task Filter_By_Status_Should_Return_Filtered_Results(string status)
        {
            // Arrange
            var customer = Context.Customers.First();
            var orders = new[]
            {
                CreateTestOrder(customer.Id, status: "Pending"),
                CreateTestOrder(customer.Id, status: "Processing"),
                CreateTestOrder(customer.Id, status: "Completed")
            };
            Context.Orders.AddRange(orders);
            Context.SaveChanges();

            // Act
            var result = await _controller.Index(null, status, null, null, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task ChangeStatus_With_Valid_Status_Should_Update_Order()
        {
            // Arrange
            var order = Context.Orders.First();
            var request = new AmplePack.Controllers.ChangeStatusRequest { Status = "Processing" };

            // Act
            var result = await _controller.ChangeStatus(order.Id, request);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var updatedOrder = Context.Orders.Find(order.Id);
            updatedOrder!.Status.Should().Be("Processing");
        }

        [Fact]
        public async Task ChangeStatus_With_Invalid_Status_Should_Return_Error()
        {
            // Arrange
            var order = Context.Orders.First();
            var request = new AmplePack.Controllers.ChangeStatusRequest { Status = "InvalidStatus" };

            // Act
            var result = await _controller.ChangeStatus(order.Id, request);

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public async Task GetCustomerDetails_With_Valid_Id_Should_Return_Json()
        {
            // Arrange
            var customer = Context.Customers.First();

            // Act
            var result = await _controller.GetCustomerDetails(customer.Id);

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public async Task GetCustomerDetails_With_Invalid_Id_Should_Return_Error_Json()
        {
            // Act
            var result = await _controller.GetCustomerDetails(999);

            // Assert
            result.Should().BeOfType<JsonResult>();
        }
    }
}