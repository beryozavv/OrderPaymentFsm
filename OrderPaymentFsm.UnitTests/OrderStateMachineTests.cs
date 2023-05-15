using Microsoft.Extensions.Logging;
using Moq;
using OrderPaymentFsm.Enums;
using OrderPaymentFsm.Models;
using OrderPaymentFsm.Models.Payments;
using OrderPaymentFsm.Models.Tickets;
using OrderPaymentFsm.Services;
using OrderPaymentFsm.StateMachine;
using Xunit;
using Xunit.Abstractions;

namespace OrderPaymentFsm.UnitTests
{
    public class OrderStateMachineTests
    {
        private readonly ITestOutputHelper _testOutput;
        private readonly ILogger _logger;

        public OrderStateMachineTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput ?? throw new ArgumentNullException(nameof(testOutput));
            var mock = new Mock<ILogger>();
            _logger = mock.Object;
        }

        // Todo Verify the stateParams props in tests

        [Fact]
        public async Task StartNewFSM_Success()
        {
            var stateParams = GetDefaultOrderStateParams();

            var orderId = 123123L;

            var (ticketOrderServiceMock, orderRepositoryMock, payServiceMock) =
                ConfigureServicesMocks(stateParams, orderId, FsmResponseStatus.Success, FsmResponseStatus.Success, FsmResponseStatus.Success, FsmResponseStatus.Success);

            var orderStateMachine =
                new OrderStateMachine(stateParams, ticketOrderServiceMock.Object, orderRepositoryMock.Object, payServiceMock.Object, _logger);

            await orderStateMachine.StartFromInitState(CancellationToken.None);

            Assert.Equal(OrderState.PayLinkCreated, orderStateMachine.CurrentState.State);
            ticketOrderServiceMock.Verify(service => service.FindAndSaveReservation(stateParams, CancellationToken.None), Times.AtMostOnce);
            ticketOrderServiceMock.Verify(service => service.CreateTicketOrder(stateParams.Request, CancellationToken.None), Times.AtMostOnce);
            orderRepositoryMock.Verify(service => service.SaveOrderStateParams(stateParams, CancellationToken.None), Times.Exactly(4));
        }

        [Theory]
        [InlineData(FsmResponseStatus.Success, FsmResponseStatus.Fail, FsmResponseStatus.Success, FsmResponseStatus.Success, OrderState.PayLinkCreated, 5)]
        [InlineData(FsmResponseStatus.Success, FsmResponseStatus.Fail, FsmResponseStatus.Transient, FsmResponseStatus.Success, OrderState.RecentOrderTransErr, 8)]
        [InlineData(FsmResponseStatus.Success, FsmResponseStatus.Fail, FsmResponseStatus.Fail, FsmResponseStatus.Success, OrderState.GlobalOrderErr, 4)]
        public async Task StartNewFSM_RecentOrder(FsmResponseStatus reservationStatus, FsmResponseStatus createTicketStatus,
            FsmResponseStatus findRecentStatus, FsmResponseStatus createPayStatus, OrderState finalOrderState, int savedCount)
        {
            var stateParams = GetDefaultOrderStateParams();

            var orderId = 123123L;

            var (ticketOrderServiceMock, orderRepositoryMock, payServiceMock) =
                ConfigureServicesMocks(stateParams, orderId, reservationStatus, createTicketStatus, findRecentStatus, createPayStatus,
                    "Ошибка поиска текущего заказа в недавних");

            var orderStateMachine =
                new OrderStateMachine(stateParams, ticketOrderServiceMock.Object, orderRepositoryMock.Object, payServiceMock.Object, _logger);

            await orderStateMachine.StartFromInitState(CancellationToken.None);

            Assert.Equal(finalOrderState, orderStateMachine.CurrentState.State);
            ticketOrderServiceMock.Verify(service => service.FindAndSaveReservation(stateParams, CancellationToken.None), Times.AtMostOnce);
            ticketOrderServiceMock.Verify(service => service.CreateTicketOrder(stateParams.Request, CancellationToken.None), Times.AtMostOnce);
            ticketOrderServiceMock.Verify(service => service.FindRecentOrder(stateParams.BookedTicket!, CancellationToken.None),
                Times.Exactly(findRecentStatus == FsmResponseStatus.Transient ? 5 : 1));
            orderRepositoryMock.Verify(service => service.SaveOrderStateParams(stateParams, CancellationToken.None), Times.Exactly(savedCount));
        }

        [Theory]
        [InlineData(FsmResponseStatus.Fail, FsmResponseStatus.Success, FsmResponseStatus.Success, FsmResponseStatus.Success, OrderState.GlobalOrderErr, 2)]
        [InlineData(FsmResponseStatus.Success, FsmResponseStatus.Fail, FsmResponseStatus.Fail, FsmResponseStatus.Success, OrderState.GlobalOrderErr, 4)]
        [InlineData(FsmResponseStatus.Success, FsmResponseStatus.Success, FsmResponseStatus.Success, FsmResponseStatus.Fail, OrderState.GlobalOrderErr, 4)]
        public async Task StartNewFSM_GlobalErr(FsmResponseStatus reservationStatus, FsmResponseStatus createTicketStatus,
            FsmResponseStatus findRecentStatus, FsmResponseStatus createPayStatus, OrderState finalOrderState, int savedCount)
        {
            var stateParams = GetDefaultOrderStateParams();

            var orderId = 123123L;

            var (ticketOrderServiceMock, orderRepositoryMock, payServiceMock) =
                ConfigureServicesMocks(stateParams, orderId, reservationStatus, createTicketStatus, findRecentStatus, createPayStatus,
                    "Ошибка. Продолжение невозможно");

            var orderStateMachine =
                new OrderStateMachine(stateParams, ticketOrderServiceMock.Object, orderRepositoryMock.Object, payServiceMock.Object, _logger);

            await orderStateMachine.StartFromInitState(CancellationToken.None);

            Assert.Equal(finalOrderState, orderStateMachine.CurrentState.State);
            ticketOrderServiceMock.Verify(service => service.FindAndSaveReservation(stateParams, CancellationToken.None), Times.AtMostOnce);
            orderRepositoryMock.Verify(service => service.SaveOrderStateParams(stateParams, CancellationToken.None), Times.Exactly(savedCount));
        }

        [Theory]
        [InlineData(FsmResponseStatus.Transient, FsmResponseStatus.Success, FsmResponseStatus.Success, FsmResponseStatus.Success, OrderState.ReservationTransErr, 4)]
        [InlineData(FsmResponseStatus.Success, FsmResponseStatus.Transient, FsmResponseStatus.Success, FsmResponseStatus.Success, OrderState.TicketOrderTransErr, 3)]
        [InlineData(FsmResponseStatus.Success, FsmResponseStatus.Success, FsmResponseStatus.Success, FsmResponseStatus.Transient, OrderState.PayTransErr, 2)]
        public async Task StartNewStateMachine_TransientErrResume(FsmResponseStatus reservationStatus, FsmResponseStatus createTicketStatus,
            FsmResponseStatus findRecentStatus, FsmResponseStatus createPayStatus, OrderState transientErrorState, int savedCount)
        {
            var stateParams = GetDefaultOrderStateParams();

            var orderId = 123123L;

            var (ticketOrderServiceMock, orderRepositoryMock, payServiceMock) =
                ConfigureServicesMocks(stateParams, orderId, reservationStatus, createTicketStatus, findRecentStatus, createPayStatus,
                    "Transient Error connect to server");

            var orderStateMachine =
                new OrderStateMachine(stateParams, ticketOrderServiceMock.Object, orderRepositoryMock.Object, payServiceMock.Object, _logger);

            await orderStateMachine.StartFromInitState(CancellationToken.None);

            Assert.Equal(transientErrorState, orderStateMachine.CurrentState.State);

            if (reservationStatus == FsmResponseStatus.Transient)
            {
                ticketOrderServiceMock.Verify(service => service.FindAndSaveReservation(stateParams, CancellationToken.None), Times.Exactly(5));
            }

            if (createTicketStatus == FsmResponseStatus.Transient)
            {
                ticketOrderServiceMock.Verify(service => service.CreateTicketOrder(stateParams.Request, CancellationToken.None), Times.Exactly(5));
            }

            (ticketOrderServiceMock, orderRepositoryMock, payServiceMock) =
                ConfigureServicesMocks(stateParams, orderId, FsmResponseStatus.Success, FsmResponseStatus.Success, FsmResponseStatus.Success, FsmResponseStatus.Success);

            orderStateMachine = new OrderStateMachine(stateParams, ticketOrderServiceMock.Object, orderRepositoryMock.Object, payServiceMock.Object, _logger);

            _testOutput.WriteLine("Resume FSM!");

            await orderStateMachine.ResumeFromCurrentState(CancellationToken.None);
            Assert.Equal(OrderState.PayLinkCreated, orderStateMachine.CurrentState.State);
            ticketOrderServiceMock.Verify(service => service.FindAndSaveReservation(stateParams, CancellationToken.None), Times.AtMostOnce);
            ticketOrderServiceMock.Verify(service => service.CreateTicketOrder(stateParams.Request, CancellationToken.None), Times.AtMostOnce);
            orderRepositoryMock.Verify(service => service.SaveOrderStateParams(stateParams, CancellationToken.None), Times.Exactly(savedCount));
        }

        private (Mock<IFsmTicketsOrderService> ticketOrderServiceMock, Mock<IOrderRepository> orderRepositoryMock, Mock<IFsmPayService> payServiceMock)
            ConfigureServicesMocks(OrderStateParams stateParams, long orderId, FsmResponseStatus reservationStatus, FsmResponseStatus createTicketStatus,
                FsmResponseStatus findRecentStatus, FsmResponseStatus createPayStatus, string? errorMessage = null)
        {
            var ticketOrderService = new Mock<IFsmTicketsOrderService>();

            var bookedTicketId = 147L;

            ticketOrderService.Setup(service => service.FindAndSaveReservation(stateParams, CancellationToken.None))
                .Callback(() =>
                {
                    stateParams.BookedTicket = GetBookedTicket(bookedTicketId);
                    _testOutput.WriteLine("FindAndSaveReservation called");
                })
                .Returns(Task.FromResult(new FsmServiceResponse {Status = reservationStatus, ErrorMessage = errorMessage}));

            ticketOrderService.Setup(service => service.CreateTicketOrder(stateParams.Request, CancellationToken.None))
                .Callback(() => _testOutput.WriteLine("CreateTicketOrder called"))
                .Returns(Task.FromResult(new FsmServiceResponse<CreatedOrderId>
                {
                    Status = createTicketStatus,
                    Result = new CreatedOrderId {Id = orderId},
                    ErrorMessage = errorMessage
                }));

            ticketOrderService.Setup(service => service.FindRecentOrder(GetBookedTicket(bookedTicketId), CancellationToken.None))
                .Callback(() => _testOutput.WriteLine("FindRecentOrder called"))
                .Returns(Task.FromResult(new FsmServiceResponse<CreatedOrderId>
                {
                    Status = findRecentStatus,
                    Result = new CreatedOrderId {Id = orderId},
                    ErrorMessage = errorMessage
                }));

            ticketOrderService.Setup(service => service.GetOrderInfo(orderId, CancellationToken.None))
                .Callback(() => _testOutput.WriteLine("GetOrderInfo called"))
                .Returns(Task.FromResult(new FsmServiceResponse<FsmOrderInfo>(new FsmOrderInfo(orderId, 123100, TicketOrderStatusEnum.Reservation))));

            var orderRepository = new Mock<IOrderRepository>();

            orderRepository.Setup(service => service.SaveOrderStateParams(stateParams, CancellationToken.None))
                .Callback(() => _testOutput.WriteLine($"Transition Saved: {stateParams}"))
                .Returns(Task.CompletedTask);

            var payServiceMock = new Mock<IFsmPayService>();
            payServiceMock.Setup(service => service.InitPay(new PayRequest {TicketOrderId = orderId, Amount = 123100}, CancellationToken.None))
                .Callback(() => _testOutput.WriteLine("InitPay called"))
                .Returns(Task.FromResult(new FsmServiceResponse<FsmPayResponse>(new FsmPayResponse
                    {PayOrderId = 78964156, WebViewLink = new Uri("https://business.tinkoff.ru/openapi/")})
                {
                    Status = createPayStatus,
                    ErrorMessage = errorMessage
                }));

            return (ticketOrderService, orderRepository, payServiceMock);
        }

        private OrderStateParams GetDefaultOrderStateParams()
        {
            return new OrderStateParams
            {
                OrderId = Guid.NewGuid(),
                State = OrderState.Init,
                Request = new CreateOrderRequest {Email = "test@gmail.com"}
            };
        }

        private BookedTicketItem GetBookedTicket(long id)
        {
            return new BookedTicketItem
            {
                Id = id,
                Status = TicketStatusEnum.Booked
            };
        }
    }
}