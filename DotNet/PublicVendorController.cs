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
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Showcase.Api.Helpers;
using Showcase.Application.CustomerVendors.Commands;
using Showcase.Application.CustomerVendors.Dtos;
using Showcase.Application.CustomerVendors.Queries;
using Showcase.Application.Dtos;
using Showcase.Application.Public.Helpers;
using Showcase.Application.Public.Models;
using Showcase.Application.Public.Models.Vendor.V1;
using Showcase.Domain.Models;
using Showcase.Domain.Settings;
using Showcase.Cloud.Models.CloudConfiguration;
using Showcase.Cloud.Models.DateTime;
using Showcase.Cloud.Models.Session;

namespace Showcase.Api.Controllers.Public
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PublicVendorController : BaseController
    {
        private readonly string _apiName = "vendor";
        private readonly AppSettings _appSettings;
        private readonly CloudConfigurationSetting _cloudConfigurationSetting;
        private readonly IWebHostEnvironment _environment;
        private readonly IbillderContext _ibillderEntities;
        private readonly ILogger<PublicVendorController> _logger;
        private readonly IMapper _mapper;
        private readonly PublicAuthHelper _publicAuthHelper;

        private readonly string controllerName = typeof(PublicVendorController).Name;

        public PublicVendorController(
            IOptions<AppSettings> appSettings,
            CloudConfigurationSetting cloudConfigurationSetting,
            IWebHostEnvironment environment,
            IbillderContext ibillderEntities,
            ILogger<PublicVendorController> logger,
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

        //GET: api/v1/publicvendor/health
        [HttpGet]
        [Route("health")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public IActionResult HealthCheck()
        {
            DateTime cstDateTime = DateTimeHelper.GetOsSpecificCstDateTime();
            var dateTimeOutput = $"{cstDateTime.ToShortDateString()} {cstDateTime.ToLongTimeString()}";

            Console.WriteLine($"{_apiName} API Health.Check: OK, apiInstanceVersion: {apiInstanceVersion}, cstDateTime: {dateTimeOutput}");

            return Ok($"{_apiName} API Health.Check: OK, apiInstanceVersion: {apiInstanceVersion}, cstDateTime: {dateTimeOutput}");
        }

        //GET: api/v1/publicvendor/get
        [HttpGet]
        [Route("get")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(VendorResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(VendorResponse))]
        public async Task<IActionResult> GetAsync(CancellationToken ct)
        {
            _logger.LogInformation($"{controllerName}.GetAsync() called");

            VendorResponse response = new();
            response.Data = new List<VendorDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorVendorResponse(validTokenInfo, response);
                }
                else
                {
                    response.Data = await GetVendorsAsync(validTokenInfo.UserProfileSession);
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

        //POST: api/v1/publicvendor/getwithfilter
        [HttpPost]
        [Route("getwithfilter")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(VendorResponse))]
        [SwaggerRequestExample(typeof(VendorRequestExample), typeof(VendorResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(VendorResponse))]
        public async Task<IActionResult> GetWithFilterAsync(CancellationToken ct, VendorRequest? vendorRequest = null)
        {
            _logger.LogInformation($"{controllerName}.GetWithFilterAsync() called");

            VendorResponse response = new();
            response.Data = new List<VendorDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorVendorResponse(validTokenInfo, response);
                }
                else
                {
                    if (!RequestHelper.DoesRequestContainFilters(vendorRequest))
                    {
                        response.Data = await GetVendorsAsync(validTokenInfo.UserProfileSession);
                        response.ResponseDetails.ReturnCode = 200;
                    }
                    else
                    {
                        response.Data = await GetVendorsWithFilterAsync(vendorRequest.Filters, validTokenInfo.UserProfileSession);
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

        //POST: api/v1/publicvendor/create
        [HttpPost]
        [Route("create")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(VendorResponse))]
        //[SwaggerRequestExample(typeof(VendorRequestExample), typeof(VendorResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(VendorResponse))]
        public async Task<IActionResult> CreateAsync(CancellationToken ct, CreateVendorRequest? createVendorRequest = null)
        {
            _logger.LogInformation($"{controllerName}.CreateAsync() called");

            VendorResponse response = new();
            response.Data = new List<VendorDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorVendorResponse(validTokenInfo, response);
                }
                else
                {
                    if (createVendorRequest != null)
                    {
                        var createVendorResult = await CreateVendorAsync(createVendorRequest, validTokenInfo.UserProfileSession);
                        if (!createVendorResult.IsSuccess)
                        {
                            response.ResponseDetails.ProblemDetails = new ProblemDetails
                            {
                                Status = 400,
                                Detail = createVendorResult.Message
                            };

                            response.ResponseDetails.ReturnCode = 400;
                        }
                        else
                        {
                            var filters = new List<RequestFilter>();
                            filters.Add(new RequestFilter { Key = "VendorReference", Value = createVendorRequest.VendorReference });
                            response.Data = await GetVendorsWithFilterAsync(filters, validTokenInfo.UserProfileSession);
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

        //POST: api/v1/publicvendor/update
        [HttpPost]
        [Route("update")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(VendorResponse))]
        //[SwaggerRequestExample(typeof(VendorRequestExample), typeof(VendorResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(VendorResponse))]
        public async Task<IActionResult> UpdateAsync(CancellationToken ct, UpdateVendorRequest? updateVendorRequest = null)
        {
            _logger.LogInformation($"{controllerName}.UpdateAsync() called");

            VendorResponse response = new();
            response.Data = new List<VendorDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorVendorResponse(validTokenInfo, response);
                }
                else
                {
                    if (updateVendorRequest != null)
                    {
                        var updateVendorResult = await UpdateVendorAsync(updateVendorRequest, validTokenInfo.UserProfileSession);
                        if (!updateVendorResult.IsSuccess)
                        {
                            response.ResponseDetails.ProblemDetails = new ProblemDetails
                            {
                                Status = 400,
                                Detail = updateVendorResult.Message
                            };

                            response.ResponseDetails.ReturnCode = 400;
                        }
                        else
                        {
                            var filters = new List<RequestFilter>();
                            filters.Add(new RequestFilter { Key = "VendorReference", Value = updateVendorRequest.VendorReference });
                            response.Data = await GetVendorsWithFilterAsync(filters, validTokenInfo.UserProfileSession);
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

        //PUT: api/v1/publicvendor/archive
        [HttpPut]
        [Route("archive")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(VendorResponse))]
        //[SwaggerRequestExample(typeof(VendorRequestExample), typeof(VendorResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(VendorResponse))]
        public async Task<IActionResult> ArchiveAsync(CancellationToken ct, ArchiveVendorRequest? archiveVendorRequest = null)
        {
            _logger.LogInformation($"{controllerName}.ArchiveAsync() called");

            VendorResponse response = new();
            response.Data = new List<VendorDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorVendorResponse(validTokenInfo, response);
                }
                else
                {
                    if (archiveVendorRequest != null)
                    {
                        var updateVendorResult = await ArchiveVendorAsync(archiveVendorRequest, validTokenInfo.UserProfileSession);
                        if (!updateVendorResult.IsSuccess)
                        {
                            response.ResponseDetails.ProblemDetails = new ProblemDetails
                            {
                                Status = 400,
                                Detail = updateVendorResult.Message
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

        private async Task<ResultDto<Unit>> ArchiveVendorAsync(ArchiveVendorRequest archiveVendorRequest, UserProfileSession userProfileSession)
        {
            // Map ArchiveVendorRequest to ArchiveVendorCommand
            ArchiveVendorCommand command = new();
            command = _mapper.Map<ArchiveVendorCommand>(archiveVendorRequest);

            command.UserProfileSession = userProfileSession;

            var archiveVendorResult = await Mediator.Send(command);

            return archiveVendorResult;
        }

        private async Task<ResultDto<Unit>> CreateVendorAsync(CreateVendorRequest createVendorRequest, UserProfileSession userProfileSession)
        {
            // Create CreateVendorCommand and map CreateVendorRequest to CreateVendorDto
            CreateVendorCommand command = new();
            CreateVendorDto createVendorDto = new();
            createVendorDto = _mapper.Map<CreateVendorDto>(createVendorRequest);
            command.Vendors = new List<CreateVendorDto>
            {
                createVendorDto
            };

            command.UserProfileSession = userProfileSession;

            var createVendorResult = await Mediator.Send(command);

            return createVendorResult;
        }

        private List<VendorDto> FilterVendors(List<VendorDto> vendorDtoList, List<RequestFilter> filters)
        {
            List<VendorDto> dataFiltered = new();
            foreach (var vendorDto in vendorDtoList)
            {
                foreach (var filter in filters)
                {
                    if (!string.IsNullOrEmpty(filter.Key) && !string.IsNullOrEmpty(filter.Value))
                    {
                        foreach (PropertyInfo pi in vendorDto.GetType().GetProperties())
                        {
                            if (pi.PropertyType == typeof(string))
                            {
                                if (!string.IsNullOrEmpty((string)pi.GetValue(vendorDto)))
                                {
                                    if (pi.Name.ToLower() == filter.Key.ToLower() &&
                                        ((string)pi.GetValue(vendorDto)).TrimEnd().ToLower().Equals(filter.Value.TrimEnd().ToLower(),
                                            StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        dataFiltered.Add(vendorDto);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return dataFiltered;
        }

        private async Task<List<VendorDto>> GetVendorsAsync(UserProfileSession userProfileSession)
        {
            List<VendorDto> vendorList = new();

            var vendorResult = await Mediator.Send(new GetVendorListQuery { UserProfileSession = userProfileSession });

            // Map mediator vendorResult to response.Data
            VendorDto vendorDto = null;
            foreach (var vendor in vendorResult.Data)
            {
                vendorDto = new VendorDto();
                vendorList.Add(_mapper.Map<VendorDto>(vendor));
            }

            return vendorList;
        }

        private async Task<List<VendorDto>> GetVendorsWithFilterAsync(List<RequestFilter> filters, UserProfileSession userProfileSession)
        {
            List<VendorDto> allVendorList = await GetVendorsAsync(userProfileSession);

            return FilterVendors(allVendorList, filters);
        }

        private async Task<ResultDto<Unit>> UpdateVendorAsync(UpdateVendorRequest updateVendorRequest, UserProfileSession userProfileSession)
        {
            // Map UpdateVendorRequest to UpdateVendorByCustomerCommand
            UpdateVendorByCustomerCommand command = new();
            command = _mapper.Map<UpdateVendorByCustomerCommand>(updateVendorRequest);

            command.UserProfileSession = userProfileSession;

            var updateVendorResult = await Mediator.Send(command);

            return updateVendorResult;
        }

    }
}
