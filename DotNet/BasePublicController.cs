using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using System;
using System.Linq;

namespace Showcase.Api.Controllers.Public
{
    public class BaseController : ControllerBase
    {
        public readonly string apiDateTimeStringFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        private IMediator _mediator;
        public string apiInstanceVersion = "";
        protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetService<IMediator>();

        internal void LogControllerException(Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }

        internal void LogControllerException(Exception ex, RouteValueDictionary routeValues)
        {
            string methodName = string.Empty;
            var firstRouteValue = routeValues.Values.FirstOrDefault();
            if (firstRouteValue != null)
                methodName = firstRouteValue.ToString();

            SentrySdk.CaptureException(ex);
        }

    }

}
