using Microsoft.Extensions.Logging;
using OrderPaymentFsm.Enums;
using OrderPaymentFsm.Models;
using OrderPaymentFsm.Services;
using Stateless;

namespace OrderPaymentFsm.StateMachine
{
    internal partial class OrderStateMachine
    {
        private readonly IFsmTicketsOrderService _orderService;
        private readonly IFsmPayService _fsmPayService;
        private readonly OrderStateParams _stateParams;

        private CancellationToken _cancellationToken = CancellationToken.None;

        public OrderStateParams CurrentState => _stateParams;

        private readonly StateMachine<OrderState, OrderTrigger> _stateMachine;

        private readonly IOrderRepository _fsmOrderRepository;

        private readonly Dictionary<OrderState, OrderTrigger> _resumeTriggersDict;

        private readonly StateMachine<OrderState, OrderTrigger>.TriggerWithParameters<string?> _raiseLocalTransErrTrigger;
        private readonly StateMachine<OrderState, OrderTrigger>.TriggerWithParameters<string?> _transitToCustomTransErrTrigger;
        private readonly StateMachine<OrderState, OrderTrigger>.TriggerWithParameters<string?> _transitToGlobalErrTrigger;
        private readonly StateMachine<OrderState, OrderTrigger>.TriggerWithParameters<string?> _createTicketOrderErrTrigger;
        private readonly StateMachine<OrderState, OrderTrigger>.TriggerWithParameters<string?> _sellTicketsRefundErrTrigger;
        private readonly StateMachine<OrderState, OrderTrigger>.TriggerWithParameters<string?> _failSellTicketsTrigger;

        public OrderStateMachine(OrderStateParams stateParams, IFsmTicketsOrderService orderService, IOrderRepository orderRepository,
            IFsmPayService fsmPayService, ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));


            _stateParams = stateParams ?? throw new ArgumentNullException(nameof(stateParams));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _fsmOrderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));

            _fsmPayService = fsmPayService ?? throw new ArgumentNullException(nameof(fsmPayService));

            _stateMachine = new StateMachine<OrderState, OrderTrigger>(() => _stateParams.State, s => _stateParams.State = s);
            _stateMachine.OnTransitionedAsync(async transition =>
            {
                if (transition.Destination != transition.Source)
                {
                    _stateParams.TransientErrCount = 0;
                }

                if ((transition.Destination.IsError() || transition.Destination.IsFailed() || transition.Destination == transition.Source) &&
                    transition.Parameters.Length > 0 && transition.Parameters[0] is string)
                {
                    _stateParams.ErrorMessage = transition.Parameters[0] as string;
                }
                else
                {
                    _stateParams.ErrorMessage = null;
                }

                logger.LogInformation("FSM Transition. OrderId={orderId} from {src} to {dest}. StateParams: {sp}", _stateParams.OrderId, transition.Source,
                    transition.Destination, _stateParams);
                await _fsmOrderRepository.SaveOrderStateParams(_stateParams, _cancellationToken);
                logger.LogInformation("FSM Transition to {dest} Saved. OrderId={orderId}", transition.Destination, _stateParams.OrderId);
            });

            _raiseLocalTransErrTrigger = _stateMachine.SetTriggerParameters<string?>(OrderTrigger.RaiseLocalTransErr);
            _transitToCustomTransErrTrigger = _stateMachine.SetTriggerParameters<string?>(OrderTrigger.TransitToCustomTransErr);
            _transitToGlobalErrTrigger = _stateMachine.SetTriggerParameters<string?>(OrderTrigger.TransitToGlobalErr);
            _createTicketOrderErrTrigger = _stateMachine.SetTriggerParameters<string?>(OrderTrigger.CreateTicketOrderErr);
            _sellTicketsRefundErrTrigger = _stateMachine.SetTriggerParameters<string?>(OrderTrigger.SellTicketsRefundErr);
            _failSellTicketsTrigger = _stateMachine.SetTriggerParameters<string?>(OrderTrigger.FailSellTickets);

            _resumeTriggersDict = GetResumeTriggersDict();

            _stateMachine.Configure(OrderState.Init)
                .Permit(OrderTrigger.CreateNewOrder, OrderState.NewOrder);

            _stateMachine.Configure(OrderState.NewOrder)
                .Permit(OrderTrigger.FindAndSaveReservation, OrderState.ReservedTicketSaved)
                .Permit(OrderTrigger.TransitToGlobalErr, OrderState.GlobalOrderErr)
                .PermitReentry(OrderTrigger.RaiseLocalTransErr)
                .Permit(OrderTrigger.TransitToCustomTransErr, OrderState.ReservationTransErr)
                .OnEntryAsync(async () => await FindAndSaveReservation())
                ;

            _stateMachine.Configure(OrderState.ReservedTicketSaved)
                .Permit(OrderTrigger.CreateTicketOrder, OrderState.TicketOrderCreated)
                .Permit(OrderTrigger.CreateTicketOrderErr, OrderState.TicketOrderErr)
                .PermitReentry(OrderTrigger.RaiseLocalTransErr)
                .Permit(OrderTrigger.TransitToCustomTransErr, OrderState.TicketOrderTransErr)
                .OnEntryAsync(async () => await CreateTicketOrder());

            _stateMachine.Configure(OrderState.TicketOrderCreated)
                .Permit(OrderTrigger.CreatePay, OrderState.PayLinkCreated)
                .Permit(OrderTrigger.TransitToGlobalErr, OrderState.GlobalOrderErr)
                .Permit(OrderTrigger.TransitToFullPricePromo, OrderState.FullPricePromo)
                .PermitReentry(OrderTrigger.RaiseLocalTransErr)
                .Permit(OrderTrigger.TransitToCustomTransErr, OrderState.PayTransErr)
                .OnEntryAsync(async () => await CreatePayOrder(false));

            _stateMachine.Configure(OrderState.TicketOrderErr)
                .Permit(OrderTrigger.FindRecentOrder, OrderState.TicketOrderCreated)
                .Permit(OrderTrigger.TransitToGlobalErr, OrderState.GlobalOrderErr)
                .PermitReentry(OrderTrigger.RaiseLocalTransErr)
                .Permit(OrderTrigger.TransitToCustomTransErr, OrderState.RecentOrderTransErr)
                .OnEntryAsync(async () => await FindRecentOrder());

            _stateMachine.Configure(OrderState.PayLinkCreated)
                .PermitReentry(OrderTrigger.RaiseLocalTransErr)
                .PermitReentry(OrderTrigger.RecreatePay)
                .OnEntryFromAsync(OrderTrigger.RecreatePay, async () => await CreatePayOrder(true))
                .Permit(OrderTrigger.CallbackSellOrder, OrderState.OrderSold)
                .Permit(OrderTrigger.CallbackFailOrder, OrderState.OrderSellFailed)
                .Permit(OrderTrigger.TransitToGlobalErr, OrderState.GlobalOrderErr)
                .Permit(OrderTrigger.TransitToCustomTransErr, OrderState.PayLinkCreatedTransErr); // todo tests и проверить повторы на реальных данных

            _stateMachine.Configure(OrderState.FullPricePromo)
                .PermitReentry(OrderTrigger.RaiseLocalTransErr)
                .Permit(OrderTrigger.TransitToGlobalErr, OrderState.GlobalOrderErr)
                .Permit(OrderTrigger.TransitToCustomTransErr, OrderState.FullPricePromoTransErr)
                .Permit(OrderTrigger.SellTickets, OrderState.TicketsSold)
                .Permit(OrderTrigger.FailSellTickets, OrderState.TicketsSellFailed)
                .OnEntryAsync(async () => await SellFullPricePromoTickets()); // todo tests

            _stateMachine.Configure(OrderState.OrderSold)
                .Permit(OrderTrigger.SellTickets, OrderState.TicketsSold)
                .Permit(OrderTrigger.SellTicketsRefundErr, OrderState.NeedRefund)
                .PermitReentry(OrderTrigger.RaiseLocalTransErr)
                .Permit(OrderTrigger.TransitToCustomTransErr, OrderState.SellTicketsTransErr)
                .OnEntryAsync(async () => await SellTickets());

            _stateMachine.Configure(OrderState.OrderSellFailed)
                .Permit(OrderTrigger.FailSellTickets, OrderState.TicketsSellFailed)
                .Permit(OrderTrigger.TransitToGlobalErr, OrderState.GlobalOrderErr)
                .PermitReentry(OrderTrigger.RaiseLocalTransErr)
                .Permit(OrderTrigger.TransitToCustomTransErr, OrderState.FailSellTicketsTransErr)
                .OnEntryAsync(async () => await FailSellTickets());

            ConfigureTransientErrorsTransitions(_stateMachine);
        }

        private static void ConfigureTransientErrorsTransitions(StateMachine<OrderState, OrderTrigger> stateMachine)
        {
            var resumeTransErrTriggersDict = GetResumeTransErrTriggersDict();
            foreach (var resumePair in resumeTransErrTriggersDict)
            {
                stateMachine.Configure(resumePair.Key)
                    .Permit(resumePair.Value.trigger, resumePair.Value.prevState);
            }
        }

        private static Dictionary<OrderState, OrderTrigger> GetResumeTriggersDict()
        {
            var triggersDict = new Dictionary<OrderState, OrderTrigger>();

            foreach (var keyValuePair in GetResumeTransErrTriggersDict())
            {
                triggersDict.Add(keyValuePair.Key, keyValuePair.Value.trigger);
            }

            return triggersDict;
        }

        private static Dictionary<OrderState, (OrderTrigger trigger, OrderState prevState)> GetResumeTransErrTriggersDict()
        {
            return new Dictionary<OrderState, (OrderTrigger, OrderState)>
            {
                {OrderState.ReservationTransErr, (OrderTrigger.FindAndSaveReservation, OrderState.NewOrder)},
                {OrderState.TicketOrderTransErr, (OrderTrigger.CreateTicketOrder, OrderState.ReservedTicketSaved)},
                {OrderState.PayTransErr, (OrderTrigger.CreatePay, OrderState.TicketOrderCreated)},
                {OrderState.RecentOrderTransErr, (OrderTrigger.FindRecentOrder, OrderState.TicketOrderErr)},
                {OrderState.PayLinkCreatedTransErr, (OrderTrigger.RecreatePay, OrderState.PayLinkCreated)},
                {OrderState.SellTicketsTransErr, (OrderTrigger.SellTickets, OrderState.OrderSold)},
                {OrderState.FailSellTicketsTransErr, (OrderTrigger.FailSellTickets, OrderState.OrderSellFailed)}
            };
        }
    }
}