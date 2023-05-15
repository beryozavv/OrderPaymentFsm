using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderPaymentFsm.Enums;
using OrderPaymentFsm.Models;
using OrderPaymentFsm.Models.Payments;
using OrderPaymentFsm.Services;
using TinkoffFsmPayService.Exceptions;
using TinkoffFsmPayService.Tinkoff;
using PaymentStatus = TinkoffFsmPayService.Tinkoff.PaymentStatus;

namespace TinkoffFsmPayService
{
    internal class TinkoffFsmPayService : IFsmPayService
    {
        private readonly ITinkoffApiClient _tinkoffApi;
        private readonly ILogger<TinkoffFsmPayService> _logger;
        private readonly IMapper _mapper;
        private readonly ITinkoffTokenService _tinkoffTokenService;
        private readonly TinkoffApiOptions _options;
        private readonly PaymentStatus[] _canCancelStatuses = {PaymentStatus.NEW, PaymentStatus.FORM_SHOWED, PaymentStatus.DEADLINE_EXPIRED};

        public TinkoffFsmPayService(ITinkoffApiClient tinkoffApi, ILogger<TinkoffFsmPayService> logger, IOptions<TinkoffApiOptions> options, IMapper mapper,
            ITinkoffTokenService tinkoffTokenService)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _tinkoffApi = tinkoffApi ?? throw new ArgumentNullException(nameof(tinkoffApi));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _tinkoffTokenService = tinkoffTokenService ?? throw new ArgumentNullException(nameof(tinkoffTokenService));
            _options = options.Value;
        }

        public async Task<FsmServiceResponse<FsmPayResponse>> InitPay(PayRequest? request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Call {0} ({1})", nameof(InitPay), request);
            if (request == null || request.TicketOrderId == 0 || request.Amount == 0)
            {
                return new FsmServiceResponse<FsmPayResponse>
                    {Status = FsmResponseStatus.Fail, ErrorMessage = $"Invalid Pay Request. {request}"};
            }

            try
            {
                // todo _mapper and nullable
                var paymentRequest = new CreatePaymentRequest
                {
                    Amount = request.Amount,
                    Description = $"Покупка билетов на событие {request.Description}",
                    OrderId = request.TicketOrderId.ToString(),
                    NotificationUrl = _options.InitPaymentOptions.NotificationUrl,
                    SuccessUrl = _options.InitPaymentOptions.SuccessUrl,
                    FailUrl = _options.InitPaymentOptions.FailUrl,
                    Data = null
                };

                paymentRequest.Token = _tinkoffTokenService.CalculateToken(paymentRequest);

                var paymentResponse = await _tinkoffApi.InitPayment(paymentRequest, cancellationToken);

                var fsmPayResponse = _mapper.Map<FsmPayResponse>(paymentResponse);
                return new FsmServiceResponse<FsmPayResponse>(fsmPayResponse)
                {
                    Status = FsmResponseStatus.Success
                };
            }
            catch (TinkoffApiException ex) when
                // todo https://github.com/icerockdev/tinkoff-merchant-api/blob/master/ERROR_CODES.md
                (ex.ErrorCode.HasValue && _options.PermanentErrors.Contains(ex.ErrorCode!.Value))
            {
                _logger.LogError(ex, "{method} ({0}) ErrorCode = {1}. Message: {2}", nameof(InitPay), request, ex.ErrorCode, ex.Message);
                return new FsmServiceResponse<FsmPayResponse> {Status = FsmResponseStatus.Fail, ErrorMessage = ex.Message};
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{method} ({0}) Message: {1}", nameof(InitPay), request, ex.Message);
                return new FsmServiceResponse<FsmPayResponse> {Status = FsmResponseStatus.Transient, ErrorMessage = ex.Message};
            }
        }

        public async Task<FsmServiceResponse> CancelNewPay(long? payOrderId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Call {0} ({1})", nameof(CancelNewPay), payOrderId);
            if (!payOrderId.HasValue || payOrderId == 0)
            {
                _logger.LogInformation($"Invalid payOrderId Request. {payOrderId}");
                return new FsmServiceResponse<FsmPayResponse>
                    {Status = FsmResponseStatus.Fail, ErrorMessage = $"Invalid payOrderId Request. {payOrderId}"};
            }

            try
            {
                var paymentState = await GetPaymentState(payOrderId, cancellationToken);

                if (paymentState.Status == PaymentStatus.REJECTED)
                {
                    _logger.LogInformation($"Payment {payOrderId} has status {PaymentStatus.REJECTED}");
                    return new FsmServiceResponse {Status = FsmResponseStatus.Success};
                }

                if (!_canCancelStatuses.Contains(paymentState.Status))
                {
                    _logger.LogInformation($"Can't cancel pay in status {paymentState.Status}. Recreate of order can cancel only NewPay");
                    return new FsmServiceResponse<FsmPayResponse>
                    {
                        Status = FsmResponseStatus.Fail,
                        ErrorMessage = $"Can't cancel pay in status {paymentState.Status}. Recreate of order can cancel only NewPay"
                    };
                }

                var cancelPaymentRequest = new CancelPaymentRequest
                {
                    PaymentId = payOrderId.Value
                };

                cancelPaymentRequest.Token = _tinkoffTokenService.CalculateToken(cancelPaymentRequest);

                await _tinkoffApi.CancelPayment(cancelPaymentRequest, cancellationToken);
                _logger.LogInformation("Call {0} ({1}) passed successfully", nameof(CancelNewPay), payOrderId);

                return new FsmServiceResponse {Status = FsmResponseStatus.Success};
            }
            catch (TinkoffApiException ex) when (ex.ErrorCode == 4) // todo codes?
            {
                _logger.LogError(ex, "{Method} ({0}) ErrorCode = {1}. Message: {2}", nameof(CancelNewPay), payOrderId, ex.ErrorCode, ex.Message);
                return new FsmServiceResponse<FsmPayResponse> {Status = FsmResponseStatus.Fail, ErrorMessage = ex.Message};
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method} ({0}) Message: {1}", nameof(CancelNewPay), payOrderId, ex.Message);
                return new FsmServiceResponse<FsmPayResponse> {Status = FsmResponseStatus.Transient, ErrorMessage = ex.Message};
            }
        }

        private async Task<CancelPaymentResponse> GetPaymentState([DisallowNull] long? payOrderId, CancellationToken cancellationToken)
        {
            var getStatePaymentRequest = new GetStatePaymentRequest
            {
                PaymentId = payOrderId.Value
            };
            getStatePaymentRequest.Token = _tinkoffTokenService.CalculateToken(getStatePaymentRequest);

            var getStateResponse = await _tinkoffApi.GetState(getStatePaymentRequest, cancellationToken);
            return getStateResponse;
        }
    }
}