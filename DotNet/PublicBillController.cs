using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Showcase.Api.Helpers;
using Showcase.Application.Bills.Commands;
using Showcase.Application.Bills.Queries;
using Showcase.Application.Dtos;
using Showcase.Application.Public.Models;
using Showcase.Application.Public.Models.Bill.V1;
using Showcase.Application.Public.Helpers;
using Showcase.Domain.Models;
using Showcase.Cloud.Models.CloudConfiguration;
using Showcase.Cloud.Models.DateTime;
using Showcase.Cloud.Models.Session;
using Showcase.Domain.Settings;

namespace Showcase.Api.Controllers.Public
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PublicBillController : BaseController
    {
        private readonly string _apiName = "bill";
        private readonly AppSettings _appSettings;
        private readonly string _billDateFormat = "MM-dd-yyyy";
        private readonly CloudConfigurationSetting _cloudConfigurationSetting;
        private readonly IWebHostEnvironment _environment;
        private readonly IbillderContext _ibillderEntities;
        private readonly ILogger<PublicBillController> _logger;
        private readonly IMapper _mapper;
        private readonly PublicAuthHelper _publicAuthHelper;

        private readonly string controllerName = typeof(PublicBillController).Name;

        public PublicBillController(
            IOptions<AppSettings> appSettings,
            CloudConfigurationSetting cloudConfigurationSetting,
            IWebHostEnvironment environment,
            IbillderContext ibillderEntities,
            ILogger<PublicBillController> logger,
            IMapper mapper)
        {
            DateTime cstDateTime = DateTimeHelper.GetOsSpecificCstDateTime();
            apiInstanceVersion =
                $"V1.{cstDateTime.Year}.{cstDateTime.Month.ToString().PadLeft(2, '0')}.{cstDateTime.Day.ToString().PadLeft(2, '0')}";

            _appSettings = appSettings.Value;
            _cloudConfigurationSetting = cloudConfigurationSetting;
            _environment = environment;
            _ibillderEntities = ibillderEntities;
            _logger = logger;
            _mapper = mapper;
            _publicAuthHelper = new PublicAuthHelper(_appSettings, _cloudConfigurationSetting, _environment, _ibillderEntities);
        }

        //GET: api/v1/publicbill/health
        [HttpGet]
        [Route("health")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public IActionResult HealthCheck()
        {
            DateTime cstDateTime = DateTimeHelper.GetOsSpecificCstDateTime();
            var dateTimeOutput = $"{cstDateTime.ToShortDateString()} {cstDateTime.ToLongTimeString()}";

            //var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
            //ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
            //if (!validTokenInfo.IsValid)
            //{
            //    Console.WriteLine($"{_apiName} API Health.Check: Unauthorized, apiInstanceVersion: {apiInstanceVersion}, cstDateTime: {dateTimeOutput}");
            //    return Unauthorized($"{_apiName} API Health.Check: Unauthorized, apiInstanceVersion: {apiInstanceVersion}, tokenErrorMessage: {validTokenInfo.ErrorMessage}");
            //}

            Console.WriteLine($"{_apiName} API Health.Check: OK, apiInstanceVersion: {apiInstanceVersion}, cstDateTime: {dateTimeOutput}");
            return Ok($"{_apiName} API Health.Check: OK, apiInstanceVersion: {apiInstanceVersion}, cstDateTime: {dateTimeOutput}");
        }

        //GET: api/v1/publicbill/get
        [HttpGet]
        [Route("get")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(BillResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(BillResponse))]
        public async Task<IActionResult> GetAsync(CancellationToken ct)
        {
            _logger.LogInformation($"{controllerName}.GetAsync() called");

            BillResponse response = new();
            response.Data = new List<BillDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorBillResponse(validTokenInfo, response);
                }
                else
                {
                    response.Data = await GetBillsAsync(validTokenInfo.UserProfileSession);
                    response.ResponseDetails.ReturnCode = 200;
                }

                DateTime responseDateTime = DateTime.UtcNow;
                response.ResponseDetails.ResponseDateTime = responseDateTime.ToString(apiDateTimeStringFormat);
                response.ResponseDetails.DurationInMilliseconds = (int)(responseDateTime - requestDateTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, ex.Message);
                LogControllerException(ex, RouteData.Values);

                response.ResponseDetails.ProblemDetails = new ProblemDetails
                {
                    Status = 500,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Unexpected")
                };
                response.ResponseDetails.ReturnCode = 500;
            }

            return StatusCode(response.ResponseDetails.ReturnCode, response);
        }

        //POST: api/v1/publicbill/getwithfilter
        [HttpPost]
        [Route("getwithfilter")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(BillResponse))]
        [SwaggerRequestExample(typeof(BillRequestExample), typeof(BillResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(BillResponse))]
        public async Task<IActionResult> GetWithFilterAsync(CancellationToken ct, BillRequest? billRequest = null)
        {
            _logger.LogInformation($"{controllerName}.GetWithFilterAsync() called");

            BillResponse response = new();
            response.Data = new List<BillDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorBillResponse(validTokenInfo, response);
                }
                else
                {
                    if (!RequestHelper.DoesRequestContainFilters(billRequest))
                    {
                        response.Data = await GetBillsAsync(validTokenInfo.UserProfileSession);
                        response.ResponseDetails.ReturnCode = 200;
                    }
                    else
                    {
                        response.Data = await GetBillsWithFilterAsync(billRequest.Filters, validTokenInfo.UserProfileSession);
                        if (response.Data.Count == 0)
                        {
                            response.ResponseDetails.ProblemDetails = new ProblemDetails
                            {
                                Status = 404,
                                Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "NoDataFoundWithFilter")
                            };
                            response.ResponseDetails.ReturnCode = 404;
                        }
                        else
                        {
                            response.ResponseDetails.ReturnCode = 200;
                        }
                    }
                }

                DateTime responseDateTime = DateTime.UtcNow;
                response.ResponseDetails.ResponseDateTime = responseDateTime.ToString(apiDateTimeStringFormat);
                response.ResponseDetails.DurationInMilliseconds = (int)(responseDateTime - requestDateTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, ex.Message);
                LogControllerException(ex, RouteData.Values);

                response.ResponseDetails.ProblemDetails = new ProblemDetails
                {
                    Status = 500,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Unexpected")
                };
                response.ResponseDetails.ReturnCode = 500;
            }

            return StatusCode(response.ResponseDetails.ReturnCode, response);
        }

        //POST: api/v1/publicbill/create
        [HttpPost]
        [Route("create")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(BillResponse))]
        //[SwaggerRequestExample(typeof(BillRequestExample), typeof(BillResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(BillResponse))]
        public async Task<IActionResult> CreateAsync(CancellationToken ct, CreateBillRequest? createBillRequest = null)
        {
            _logger.LogInformation($"{controllerName}.CreateAsync() called");

            BillResponse response = new();
            response.Data = new List<BillDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorBillResponse(validTokenInfo, response);
                }
                else
                {
                    if (createBillRequest != null)
                    {
                        ProblemDetails problemDetails = ValidateCreateBillRequestFields(createBillRequest);
                        if (problemDetails.Status.HasValue)
                        {
                            response.ResponseDetails.ProblemDetails = problemDetails;
                            response.ResponseDetails.ReturnCode = 400;
                        }
                        else
                        {
                            var createBillResult = await CreateBillAsync(createBillRequest, validTokenInfo.UserProfileSession);
                            if (!createBillResult.IsSuccess)
                            {
                                response.ResponseDetails.ProblemDetails = new ProblemDetails
                                {
                                    Status = 400,
                                    Detail = createBillResult.Message
                                };

                                response.ResponseDetails.ReturnCode = 400;
                            }
                            else
                            {
                                var filters = new List<RequestFilter>();
                                filters.Add(new RequestFilter { Key = "BillReference", Value = createBillRequest.BillReference });
                                response.Data = await GetBillsWithFilterAsync(filters, validTokenInfo.UserProfileSession);
                                response.ResponseDetails.ReturnCode = 200;
                            }
                        }
                    }
                    else
                    {
                        response.ResponseDetails.ProblemDetails = new ProblemDetails
                        {
                            Status = 400,
                            Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "BadRequest")
                        };
                        response.ResponseDetails.ReturnCode = 400;
                    }
                }

                DateTime responseDateTime = DateTime.UtcNow;
                response.ResponseDetails.ResponseDateTime = responseDateTime.ToString(apiDateTimeStringFormat);
                response.ResponseDetails.DurationInMilliseconds = (int)(responseDateTime - requestDateTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, ex.Message);
                LogControllerException(ex, RouteData.Values);

                response.ResponseDetails.ProblemDetails = new ProblemDetails
                {
                    Status = 500,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Unexpected")
                };
                response.ResponseDetails.ReturnCode = 500;
            }

            return StatusCode(response.ResponseDetails.ReturnCode, response);
        }

        //POST: api/v1/publicbill/update
        [HttpPost]
        [Route("update")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(BillResponse))]
        //[SwaggerRequestExample(typeof(BillRequestExample), typeof(BillResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(BillResponse))]
        public async Task<IActionResult> UpdateAsync(CancellationToken ct, UpdateBillRequest? updateBillRequest = null)
        {
            _logger.LogInformation($"{controllerName}.UpdateAsync() called");

            BillResponse response = new();
            response.Data = new List<BillDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorBillResponse(validTokenInfo, response);
                }
                else
                {
                    if (updateBillRequest != null)
                    {
                        ProblemDetails problemDetails = ValidateUpdateBillRequestFields(updateBillRequest);
                        if (problemDetails.Status.HasValue)
                        {
                            response.ResponseDetails.ProblemDetails = problemDetails;
                            response.ResponseDetails.ReturnCode = 400;
                        }
                        else
                        {
                            var updateBillResult = await UpdateBillAsync(updateBillRequest, validTokenInfo.UserProfileSession);
                            if (!updateBillResult.IsSuccess)
                            {
                                response.ResponseDetails.ProblemDetails = new ProblemDetails
                                {
                                    Status = 400,
                                    Detail = updateBillResult.Message
                                };

                                response.ResponseDetails.ReturnCode = 400;
                            }
                            else
                            {
                                var filters = new List<RequestFilter>();
                                filters.Add(new RequestFilter { Key = "BillReference", Value = updateBillRequest.BillReference });
                                response.Data = await GetBillsWithFilterAsync(filters, validTokenInfo.UserProfileSession);
                                response.ResponseDetails.ReturnCode = 200;
                            }
                        }
                    }
                    else
                    {
                        response.ResponseDetails.ProblemDetails = new ProblemDetails
                        {
                            Status = 400,
                            Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "BadRequest")
                        };
                        response.ResponseDetails.ReturnCode = 400;
                    }
                }

                DateTime responseDateTime = DateTime.UtcNow;
                response.ResponseDetails.ResponseDateTime = responseDateTime.ToString(apiDateTimeStringFormat);
                response.ResponseDetails.DurationInMilliseconds = (int)(responseDateTime - requestDateTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, ex.Message);
                LogControllerException(ex, RouteData.Values);

                response.ResponseDetails.ProblemDetails = new ProblemDetails
                {
                    Status = 500,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Unexpected")
                };
                response.ResponseDetails.ReturnCode = 500;
            }

            return StatusCode(response.ResponseDetails.ReturnCode, response);
        }

        //PUT: api/v1/publicbill/archive
        [HttpPut]
        [Route("archive")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(BillResponse))]
        //[SwaggerRequestExample(typeof(BillRequestExample), typeof(BillResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(BillResponse))]
        public async Task<IActionResult> ArchiveAsync(CancellationToken ct, ArchiveBillRequest? archiveBillRequest = null)
        {
            _logger.LogInformation($"{controllerName}.ArchiveAsync() called");

            BillResponse response = new();
            response.Data = new List<BillDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorBillResponse(validTokenInfo, response);
                }
                else
                {
                    if (archiveBillRequest != null)
                    {
                        var archiveBillResult = await ArchiveBillAsync(archiveBillRequest, validTokenInfo.UserProfileSession);
                        if (!archiveBillResult.IsSuccess)
                        {
                            response.ResponseDetails.ProblemDetails = new ProblemDetails
                            {
                                Status = 400,
                                Detail = archiveBillResult.Message
                            };

                            response.ResponseDetails.ReturnCode = 400;
                        }
                        else
                        {
                            response.ResponseDetails.ReturnCode = 200;
                        }
                    }
                    else
                    {
                        response.ResponseDetails.ProblemDetails = new ProblemDetails
                        {
                            Status = 400,
                            Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "BadRequest")
                        };
                        response.ResponseDetails.ReturnCode = 400;
                    }
                }

                DateTime responseDateTime = DateTime.UtcNow;
                response.ResponseDetails.ResponseDateTime = responseDateTime.ToString(apiDateTimeStringFormat);
                response.ResponseDetails.DurationInMilliseconds = (int)(responseDateTime - requestDateTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, ex.Message);
                LogControllerException(ex, RouteData.Values);

                response.ResponseDetails.ProblemDetails = new ProblemDetails
                {
                    Status = 500,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Unexpected")
                };
                response.ResponseDetails.ReturnCode = 500;
            }

            return StatusCode(response.ResponseDetails.ReturnCode, response);
        }

        private async Task<ResultDto<Unit>> ArchiveBillAsync(ArchiveBillRequest archiveBillRequest, UserProfileSession userProfileSession)
        {
            // Map ArchiveBillRequest to DeleteBillCommand
            DeleteBillCommand command = new();
            command = _mapper.Map<DeleteBillCommand>(archiveBillRequest);

            command.UserProfileSession = userProfileSession;

            var archiveBillResult = await Mediator.Send(command);

            return archiveBillResult;
        }

        private async Task<ResultDto<Unit>> CreateBillAsync(CreateBillRequest createBillRequest, UserProfileSession userProfileSession)
        {
            // Create CreateBillCommand and map CreateBillRequest to CreateBillCommand
            CreateBillCommand command = new();
            command = _mapper.Map<CreateBillCommand>(createBillRequest);
            command.UserProfileSession = userProfileSession;

            command.ApiSource = "PublicBillController";
            command.DueDate = DateTime.ParseExact(createBillRequest.DueDate, _billDateFormat, CultureInfo.InvariantCulture);
            command.InvoiceDate = DateTime.ParseExact(createBillRequest.InvoiceDate, _billDateFormat, CultureInfo.InvariantCulture);

            var createBillResult = await Mediator.Send(command);

            return createBillResult;
        }

        private List<BillDto> FilterBills(List<BillDto> billDtoList,
            List<RequestFilter> filters)
        {
            List<BillDto> dataFiltered = new();
            foreach (var billDto in billDtoList)
            {
                foreach (var filter in filters)
                {
                    if (!string.IsNullOrEmpty(filter.Key) && !string.IsNullOrEmpty(filter.Value))
                    {
                        foreach (PropertyInfo pi in billDto.GetType().GetProperties())
                        {
                            if (pi.PropertyType == typeof(string))
                            {
                                if (!string.IsNullOrEmpty((string)pi.GetValue(billDto)))
                                {
                                    if (pi.Name.ToLower() == filter.Key.ToLower() &&
                                        ((string)pi.GetValue(billDto)).TrimEnd().ToLower().Equals(filter.Value.TrimEnd().ToLower(),
                                            StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        dataFiltered.Add(billDto);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return dataFiltered;
        }

        private async Task<List<BillDto>> GetBillsAsync(UserProfileSession userProfileSession)
        {
            List<BillDto> billList = new();

            var billResult = await Mediator.Send(new GetSingleBillListQuery { UserProfileSession = userProfileSession });

            // Map mediator BillDto to response.Data
            BillDto billDto = null;
            foreach (var bill in billResult.Data)
            {
                billDto = new BillDto();
                billList.Add(_mapper.Map<BillDto>(bill));
            }

            return billList;
        }

        private async Task<List<BillDto>> GetBillsWithFilterAsync(List<RequestFilter> filters,
            UserProfileSession userProfileSession)
        {
            List<BillDto> allBillList = await GetBillsAsync(userProfileSession);

            return FilterBills(allBillList, filters);
        }

        private async Task<ResultDto<Unit>> UpdateBillAsync(UpdateBillRequest updateBillRequest, UserProfileSession userProfileSession)
        {
            // Map UpdateBillRequest to UpdateBillCommand
            UpdateBillCommand command = new();
            command = _mapper.Map<UpdateBillCommand>(updateBillRequest);
            command.UserProfileSession = userProfileSession;

            command.ApiSource = "PublicBillController";

            var updateBillResult = await Mediator.Send(command);

            return updateBillResult;
        }

        private ProblemDetails ValidateCreateBillRequestFields(CreateBillRequest createBillRequest)
        {
            ProblemDetails problemDetails = new();

            // Check required fields
            if (string.IsNullOrEmpty(createBillRequest.BillReference))
            {
                problemDetails = new ProblemDetails
                {
                    Status = 400,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Required.Bill.Create.BillReference")
                };
            }
            else if (string.IsNullOrEmpty(createBillRequest.DueDate))
            {
                problemDetails = new ProblemDetails
                {
                    Status = 400,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Required.Bill.Create.DueDate")
                };
            }
            else if (string.IsNullOrEmpty(createBillRequest.InvoiceDate))
            {
                problemDetails = new ProblemDetails
                {
                    Status = 400,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Required.Bill.Create.InvoiceDate")
                };
            }
            else if (string.IsNullOrEmpty(createBillRequest.InvoiceNumber))
            {
                problemDetails = new ProblemDetails
                {
                    Status = 400,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Required.Bill.Create.InvoiceNumber")
                };
            }
            else if (string.IsNullOrEmpty(createBillRequest.VendorReference))
            {
                problemDetails = new ProblemDetails
                {
                    Status = 400,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Required.Bill.Create.VendorReference")
                };
            }
            // Check field formats
            else if (!DateTime.TryParseExact(createBillRequest.DueDate, _billDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime dueDate))
            {
                problemDetails = new ProblemDetails
                {
                    Status = 400,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "InvalidFormat.Bill.Create.DueDate.MM-dd-yyyy")
                };
            }
            else if (createBillRequest.InvoiceAmount <= 0)
            {
                problemDetails = new ProblemDetails
                {
                    Status = 400,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "InvalidValue.Bill.Create.InvoiceAmount.must be greater than 0")
                };
            }
            else if (!DateTime.TryParseExact(createBillRequest.InvoiceDate, _billDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime invoiceDate))
            {
                problemDetails = new ProblemDetails
                {
                    Status = 400,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "InvalidFormat.Bill.Create.InvoiceDate.MM-dd-yyyy")
                };
            }

            return problemDetails;
        }

        private ProblemDetails ValidateUpdateBillRequestFields(UpdateBillRequest updateBillRequest)
        {
            ProblemDetails problemDetails = new();

            // Check required fields
            if (string.IsNullOrEmpty(updateBillRequest.BillReference))
            {
                problemDetails = new ProblemDetails
                {
                    Status = 400,
                    Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "Required.Bill.Update.BillReference")
                };
            }
            // Check field formats
            else if (!string.IsNullOrEmpty(updateBillRequest.DueDate))
            {
                if (!DateTime.TryParseExact(updateBillRequest.DueDate, _billDateFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dueDate))
                {
                    problemDetails = new ProblemDetails
                    {
                        Status = 400,
                        Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "InvalidFormat.Bill.Update.DueDate.MM-dd-yyyy")
                    };
                }
            }
            else if (!string.IsNullOrEmpty(updateBillRequest.InvoiceDate))
            {
                if (!DateTime.TryParseExact(updateBillRequest.InvoiceDate, _billDateFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime invoiceDate))
                {
                    problemDetails = new ProblemDetails
                    {
                        Status = 400,
                        Detail = ApiErrorHelper.GenerateApiError(_apiName, apiInstanceVersion, "InvalidFormat.Bill.Update.InvoiceDate.MM-dd-yyyy")
                    };
                }
            }

            return problemDetails;
        }

    }
}
