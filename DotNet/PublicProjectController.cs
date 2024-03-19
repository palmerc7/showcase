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
using Showcase.Application.BillPayProjects.Queries;
using Showcase.Application.BillPayProjects.Commands;
using Showcase.Application.Dtos;
using Showcase.Application.Public.Models;
using Showcase.Application.Public.Models.Project.V1;
using Showcase.Application.Public.Helpers;
using Showcase.Domain.Models;
using Showcase.Domain.Settings;
using Showcase.Cloud.Models.CloudConfiguration;
using Showcase.Cloud.Models.DateTime;
using Showcase.Cloud.Models.Session;

namespace Showcase.Api.Controllers.Public
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PublicProjectController : BaseController
    {
        private readonly string _apiName = "project";
        private readonly AppSettings _appSettings;
        private readonly CloudConfigurationSetting _cloudConfigurationSetting;
        private readonly IWebHostEnvironment _environment;
        private readonly IbillderContext _ibillderEntities;
        private readonly ILogger<PublicProjectController> _logger;
        private readonly IMapper _mapper;
        private readonly PublicAuthHelper _publicAuthHelper;

        private readonly string controllerName = typeof(PublicProjectController).Name;

        public PublicProjectController(
            IOptions<AppSettings> appSettings,
            CloudConfigurationSetting cloudConfigurationSetting,
            IWebHostEnvironment environment,
            IbillderContext ibillderEntities,
            ILogger<PublicProjectController> logger,
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

        //GET: api/v1/publicproject/health
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

        //GET: api/v1/publicproject/get
        [HttpGet]
        [Route("get")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ProjectResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(ProjectResponse))]
        public async Task<IActionResult> GetAsync(CancellationToken ct)
        {
            _logger.LogInformation($"{controllerName}.GetAsync() called");

            ProjectResponse response = new();
            response.Data = new List<Application.Public.Models.Project.V1.ProjectDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorProjectResponse(validTokenInfo, response);
                }
                else
                {
                    response.Data = await GetProjectsAsync(validTokenInfo.UserProfileSession);
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

        //POST: api/v1/publicproject/getwithfilter
        [HttpPost]
        [Route("getwithfilter")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ProjectResponse))]
        [SwaggerRequestExample(typeof(ProjectRequestExample), typeof(ProjectResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(ProjectResponse))]
        public async Task<IActionResult> GetWithFilterAsync(CancellationToken ct, ProjectRequest? projectRequest = null)
        {
            _logger.LogInformation($"{controllerName}.GetWithFilterAsync() called");

            ProjectResponse response = new();
            response.Data = new List<Application.Public.Models.Project.V1.ProjectDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorProjectResponse(validTokenInfo, response);
                }
                else
                {
                    if (!RequestHelper.DoesRequestContainFilters(projectRequest))
                    {
                        response.Data = await GetProjectsAsync(validTokenInfo.UserProfileSession);
                        response.ResponseDetails.ReturnCode = 200;
                    }
                    else
                    {
                        response.Data = await GetProjectsWithFilterAsync(projectRequest.Filters, validTokenInfo.UserProfileSession);
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

        //POST: api/v1/publicproject/create
        [HttpPost]
        [Route("create")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ProjectResponse))]
        //[SwaggerRequestExample(typeof(ProjectRequestExample), typeof(ProjectResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(ProjectResponse))]
        public async Task<IActionResult> CreateAsync(CancellationToken ct, CreateProjectRequest? createProjectRequest = null)
        {
            _logger.LogInformation($"{controllerName}.CreateAsync() called");

            ProjectResponse response = new();
            response.Data = new List<Application.Public.Models.Project.V1.ProjectDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorProjectResponse(validTokenInfo, response);
                }
                else
                {
                    if (createProjectRequest != null)
                    {
                        var createProjectResult = await CreateProjectAsync(createProjectRequest, validTokenInfo.UserProfileSession);
                        if (!createProjectResult.IsSuccess)
                        {
                            response.ResponseDetails.ProblemDetails = new ProblemDetails
                            {
                                Status = 400,
                                Detail = createProjectResult.Message
                            };

                            response.ResponseDetails.ReturnCode = 400;
                        }
                        else
                        {
                            var filters = new List<RequestFilter>();
                            filters.Add(new RequestFilter { Key = "ProjectReference", Value = createProjectRequest.ProjectReference });
                            response.Data = await GetProjectsWithFilterAsync(filters, validTokenInfo.UserProfileSession);
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

        //POST: api/v1/publicproject/update
        [HttpPost]
        [Route("update")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ProjectResponse))]
        //[SwaggerRequestExample(typeof(ProjectRequestExample), typeof(ProjectResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(ProjectResponse))]
        public async Task<IActionResult> UpdateAsync(CancellationToken ct, UpdateProjectRequest? updateProjectRequest = null)
        {
            _logger.LogInformation($"{controllerName}.UpdateAsync() called");

            ProjectResponse response = new();
            response.Data = new List<Application.Public.Models.Project.V1.ProjectDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorProjectResponse(validTokenInfo, response);
                }
                else
                {
                    if (updateProjectRequest != null)
                    {
                        var updateProjectResult = await UpdateProjectAsync(updateProjectRequest, validTokenInfo.UserProfileSession);
                        if (!updateProjectResult.IsSuccess)
                        {
                            response.ResponseDetails.ProblemDetails = new ProblemDetails
                            {
                                Status = 400,
                                Detail = updateProjectResult.Message
                            };

                            response.ResponseDetails.ReturnCode = 400;
                        }
                        else
                        {
                            var filters = new List<RequestFilter>();
                            filters.Add(new RequestFilter { Key = "ProjectReference", Value = updateProjectRequest.ProjectReference });
                            response.Data = await GetProjectsWithFilterAsync(filters, validTokenInfo.UserProfileSession);
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

        //PUT: api/v1/publicproject/archive
        [HttpPut]
        [Route("archive")]
        [Produces("application/json")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ProjectResponse))]
        //[SwaggerRequestExample(typeof(ProjectRequestExample), typeof(ProjectResponse))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, Type = typeof(ProjectResponse))]
        public async Task<IActionResult> ArchiveAsync(CancellationToken ct, ArchiveProjectRequest? archiveProjectRequest = null)
        {
            _logger.LogInformation($"{controllerName}.ArchiveAsync() called");

            ProjectResponse response = new();
            response.Data = new List<Application.Public.Models.Project.V1.ProjectDto>();
            DateTime requestDateTime = DateTime.UtcNow;
            response.ResponseDetails.ApiInstanceVersion = apiInstanceVersion;
            response.ResponseDetails.RequestDateTime = requestDateTime.ToString(apiDateTimeStringFormat);
            try
            {
                var accessToken = _publicAuthHelper.GetAccessTokenFromHeader(Request.Headers);
                ValidTokenInfo validTokenInfo = _publicAuthHelper.ValidateAccessToken(accessToken);
                if (!validTokenInfo.IsValid)
                {
                    response = _publicAuthHelper.BuildErrorProjectResponse(validTokenInfo, response);
                }
                else
                {
                    if (archiveProjectRequest != null)
                    {
                        var archiveProjectResult = await ArchiveProjectAsync(archiveProjectRequest, validTokenInfo.UserProfileSession);
                        if (!archiveProjectResult.IsSuccess)
                        {
                            response.ResponseDetails.ProblemDetails = new ProblemDetails
                            {
                                Status = 400,
                                Detail = archiveProjectResult.Message
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

        private async Task<ResultDto<Unit>> ArchiveProjectAsync(ArchiveProjectRequest archiveProjectRequest, UserProfileSession userProfileSession)
        {
            // Map ArchiveProjectRequest to DeleteBillPayProjectCommand
            DeleteBillPayProjectCommand command = new();
            command = _mapper.Map<DeleteBillPayProjectCommand>(archiveProjectRequest);

            command.UserProfileSession = userProfileSession;

            var archiveProjectResult = await Mediator.Send(command);

            return archiveProjectResult;
        }

        private async Task<ResultDto<Unit>> CreateProjectAsync(CreateProjectRequest createProjectRequest, UserProfileSession userProfileSession)
        {
            // Create CreateProjectCommand and map CreateProjectRequest to CreateBillPayProjectCommand
            CreateBillPayProjectCommand command = new();
            command = _mapper.Map<CreateBillPayProjectCommand>(createProjectRequest);
            command.Active = true;
            command.UserProfileSession = userProfileSession;

            var createProjectResult = await Mediator.Send(command);

            return createProjectResult;
        }

        private List<Application.Public.Models.Project.V1.ProjectDto> FilterProjects(List<Application.Public.Models.Project.V1.ProjectDto> projectDtoList,
            List<RequestFilter> filters)
        {
            List<Application.Public.Models.Project.V1.ProjectDto> dataFiltered = new();
            foreach (var projectDto in projectDtoList)
            {
                foreach (var filter in filters)
                {
                    if (!string.IsNullOrEmpty(filter.Key) && !string.IsNullOrEmpty(filter.Value))
                    {
                        foreach (PropertyInfo pi in projectDto.GetType().GetProperties())
                        {
                            if (pi.PropertyType == typeof(string))
                            {
                                if (!string.IsNullOrEmpty((string)pi.GetValue(projectDto)))
                                {
                                    if (pi.Name.ToLower() == filter.Key.ToLower() &&
                                        ((string)pi.GetValue(projectDto)).TrimEnd().ToLower().Equals(filter.Value.TrimEnd().ToLower(),
                                            StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        dataFiltered.Add(projectDto);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return dataFiltered;
        }

        private async Task<List<Application.Public.Models.Project.V1.ProjectDto>> GetProjectsAsync(UserProfileSession userProfileSession)
        {
            List<Application.Public.Models.Project.V1.ProjectDto> projectList = new();

            var projectResult = await Mediator.Send(new GetBillPayProjectListQuery { UserProfileSession = userProfileSession });

            // Map mediator projectResult to response.Data
            Application.Public.Models.Project.V1.ProjectDto projectDto = null;
            foreach (var project in projectResult.Data.BillPayProjectDto)
            {
                projectDto = new Application.Public.Models.Project.V1.ProjectDto();
                projectList.Add(_mapper.Map<Application.Public.Models.Project.V1.ProjectDto>(project));
            }

            return projectList;
        }

        private async Task<List<Application.Public.Models.Project.V1.ProjectDto>> GetProjectsWithFilterAsync(List<RequestFilter> filters,
            UserProfileSession userProfileSession)
        {
            List<Application.Public.Models.Project.V1.ProjectDto> allProjectList = await GetProjectsAsync(userProfileSession);

            return FilterProjects(allProjectList, filters);
        }

        private async Task<ResultDto<Unit>> UpdateProjectAsync(UpdateProjectRequest updateProjectRequest, UserProfileSession userProfileSession)
        {
            // Map UpdateProjectRequest to UpdateBillPayProjectCommand
            UpdateBillPayProjectCommand command = new();
            command = _mapper.Map<UpdateBillPayProjectCommand>(updateProjectRequest);
            command.UserProfileSession = userProfileSession;

            command.ApiSource = "PublicProjectController";
            var updateProjectResult = await Mediator.Send(command);

            return updateProjectResult;
        }

    }
}
