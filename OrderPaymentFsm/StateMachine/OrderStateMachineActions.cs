using OrderPaymentFsm.Enums;
using OrderPaymentFsm.Exceptions;
using OrderPaymentFsm.Models;
using OrderPaymentFsm.Models.Payments;
using OrderPaymentFsm.Models.Tickets;

namespace OrderPaymentFsm.StateMachine
{
    internal partial class OrderStateMachine
    {
        public async Task StartFromInitState(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            await _stateMachine.FireAsync(OrderTrigger.CreateNewOrder);
        }

        public async Task ResumeFromCurrentState(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            if (_stateParams.State == OrderState.Init)
            {
                await _stateMachine.FireAsync(OrderTrigger.CreateNewOrder);
            }
            else if (_stateParams.State == OrderState.PayLinkCreated)
            {
                await _stateMachine.FireAsync(OrderTrigger.RecreatePay);
            }
            else if (_stateParams.State.IsTransientErr() && _resumeTriggersDict.TryGetValue(_stateParams.State, out var currentTrigger))
            {
                await _stateMachine.FireAsync(currentTrigger);
            }
            else
            {
                await _stateMachine.FireAsync(OrderTrigger.RaiseLocalTransErr);
            }
        }

        public async Task CallBackSellOrder(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            await _stateMachine.FireAsync(OrderTrigger.CallbackSellOrder);
        }

        public async Task CallBackFailOrder(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            await _stateMachine.FireAsync(OrderTrigger.CallbackFailOrder);
        }

        private async Task FindAndSaveReservation()
        {
            await FsmActionExecute(
                async () => await _orderService.FindAndSaveReservation(_stateParams, _cancellationToken),
                async _ => await _stateMachine.FireAsync(OrderTrigger.FindAndSaveReservation));
        }

        private async Task CreateTicketOrder()
        {
            await FsmActionExecute(
                async () => await _orderService.CreateTicketOrder(_stateParams.Request, _cancellationToken),
                async orderResult =>
                {
                    _stateParams.TicketOrderId = orderResult.Result!.Id;
                    await _stateMachine.FireAsync(OrderTrigger.CreateTicketOrder);
                },
                async orderResult => { await _stateMachine.FireAsync(_createTicketOrderErrTrigger, orderResult.ErrorMessage); });
        }

        private async Task FindRecentOrder()
        {
            await FsmActionExecute(
                async () => await _orderService.FindRecentOrder(_stateParams.BookedTicket!, _cancellationToken),
                async recentOrderResult =>
                {
                    _stateParams.TicketOrderId = recentOrderResult.Result!.Id;
                    await _stateMachine.FireAsync(OrderTrigger.FindRecentOrder);
                });
        }

        private async Task CreatePayOrder(bool isRecreating)
        {
            if (isRecreating && _stateParams.PayResponse?.PayOrderId != null) // todo tests
            {
                var cancelPayResult = await _fsmPayService.CancelNewPay(_stateParams.PayResponse!.PayOrderId, _cancellationToken);
                await FsmProcessResult(cancelPayResult, async _ => await GetOrderInfoAndInitPay(isRecreating));
            }
            else
            {
                await GetOrderInfoAndInitPay(isRecreating);
            }
        }

        private async Task GetOrderInfoAndInitPay(bool isRecreating)
        {
            await FsmActionExecute(
                async () => await _orderService.GetOrderInfo(_stateParams.TicketOrderId!.Value, _cancellationToken),
                async orderResult =>
                {
                    if (orderResult.Result!.TicketsSum > 0)
                    {
                        await InitPay(orderResult, isRecreating);
                    }
                    else
                    {
                        await _stateMachine.FireAsync(OrderTrigger.TransitToFullPricePromo);
                    }
                });
        }

        private async Task InitPay(FsmServiceResponse<FsmOrderInfo> orderResult, bool isRecreating)
        {
            await FsmActionExecute(
                async () =>
                {
                    if (orderResult.Result!.TicketOrderStatus == TicketOrderStatusEnum.Sold || orderResult.Result.TicketOrderStatus == TicketOrderStatusEnum.Canceled)
                    {
                        return new FsmServiceResponse<FsmPayResponse>
                            {Status = FsmResponseStatus.Fail, ErrorMessage = $"{Constants.CantInitPay}: {orderResult.Result.TicketOrderStatus}"};
                    }

                    return await _fsmPayService.InitPay(
                        new PayRequest
                        {
                            TicketOrderId = _stateParams.TicketOrderId!.Value,
                            Amount = orderResult.Result.TicketsSum,
                            Description = orderResult.Result.Description,
                            Tickets = orderResult.Result.Tickets
                        }, _cancellationToken);
                },
                async payResult =>
                {
                    _stateParams.PayResponse = payResult.Result;
                    if (isRecreating) // todo тесты на проверку сохранения, когда не вызывается переход состояния
                    {
                        await _fsmOrderRepository.SaveOrderStateParams(_stateParams, _cancellationToken);
                    }
                    else
                    {
                        await _stateMachine.FireAsync(OrderTrigger.CreatePay);
                    }
                });
        }

        private async Task FsmActionExecute<T>(Func<Task<T>> action, Func<T, Task>? successAction, Func<T, Task>? failAction = null)
            where T : FsmServiceResponse
        {
            var actionResult = await action.Invoke();
            switch (actionResult.Status)
            {
                case FsmResponseStatus.Success:
                    if (successAction != null)
                    {
                        await successAction.Invoke(actionResult);
                    }

                    break;
                case FsmResponseStatus.Fail:
                    if (failAction == null)
                    {
                        await TransitToGlobalError(actionResult.ErrorMessage);
                    }
                    else
                    {
                        await failAction(actionResult);
                    }

                    break;
                case FsmResponseStatus.Transient:
                    await TransientTriggerRetry(actionResult.ErrorMessage);
                    break;
                default:
                    throw new UnsupportedResponseStatusException();
            }
        }

        private async Task FsmProcessResult<T>(T actionResult, Func<T, Task> successAction, Func<T, Task>? failAction = null) where T : FsmServiceResponse
        {
            switch (actionResult.Status)
            {
                case FsmResponseStatus.Success:
                    await successAction.Invoke(actionResult);
                    break;
                case FsmResponseStatus.Fail:
                    if (failAction == null)
                    {
                        await TransitToGlobalError(actionResult.ErrorMessage);
                    }
                    else
                    {
                        await failAction(actionResult);
                    }

                    break;
                case FsmResponseStatus.Transient:
                    await TransientTriggerRetry(actionResult.ErrorMessage);
                    break;
                default:
                    throw new UnsupportedResponseStatusException();
            }
        }

        private async Task TransitToGlobalError(string? errorMessage)
        {
            await _stateMachine.FireAsync(_transitToGlobalErrTrigger, errorMessage);
        }

        private async Task TransientTriggerRetry(string? errorMessage)
        {
            _stateParams.TransientErrCount++;
            if (_stateParams.TransientErrCount < 5 && !_cancellationToken.IsCancellationRequested) // todo из опций
            {
                await _stateMachine.FireAsync(_raiseLocalTransErrTrigger, errorMessage);
            }
            else
            {
                _stateParams.TransientErrCount = 0;
                await _stateMachine.FireAsync(_transitToCustomTransErrTrigger, errorMessage);
            }
        }

        private async Task FailSellTickets()
        {
            await FsmActionExecute(
                async () =>
                {
                    var ticketOrderId = _stateParams.TicketOrderId!.Value;
                    return await _orderService.ConfirmCancelOrder(ticketOrderId, _cancellationToken);
                },
                async _ => await _stateMachine.FireAsync(OrderTrigger.FailSellTickets));
        }

        private async Task SellTickets()
        {
            await FsmActionExecute(
                async () =>
                {
                    var confirmSellOrderResponse = await _orderService.ConfirmSellOrder(_stateParams.OrderId, _stateParams.TicketOrderId!.Value,
                        _stateParams.PayResponse!.Amount!.Value,
                        _cancellationToken);
                    return confirmSellOrderResponse;
                },
                async _ => await _stateMachine.FireAsync(OrderTrigger.SellTickets),
                async result =>
                {
                    await _stateMachine.FireAsync(_sellTicketsRefundErrTrigger, result.ErrorMessage);
                }); // todo отработать, если невозможно сменить статус заказа
        }

        private async Task SellFullPricePromoTickets()
        {
            await FsmActionExecute(
                async () => await _orderService.ConfirmSellOrder(_stateParams.OrderId, _stateParams.TicketOrderId!.Value, 0, _cancellationToken),
                async _ =>
                {
                    _stateParams.PayResponse = new FsmPayResponse
                    {
                        IsFullPricePromo = true,
                        WebViewLink = new Uri("https://zenit.page.link/PaymentSuccess") // todo from options
                    };
                    await _stateMachine.FireAsync(OrderTrigger.SellTickets);
                },
                async result =>
                {
                    _stateParams.PayResponse = new FsmPayResponse
                    {
                        IsFullPricePromo = true,
                        WebViewLink = new Uri("https://zenit.page.link/PaymentFailed") // todo from options
                    };
                    await _stateMachine.FireAsync(_failSellTicketsTrigger, result.ErrorMessage);
                }
            );
        }
    }
}