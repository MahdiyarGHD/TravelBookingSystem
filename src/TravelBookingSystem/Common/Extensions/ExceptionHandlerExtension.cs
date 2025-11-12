using System.Net;
using EasyMicroservices.ServiceContracts;
using Microsoft.AspNetCore.Diagnostics;

namespace TravelBookingSystem.Common.Extensions;


/// <summary>
/// extensions for global exception handling
/// </summary>
public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(
            errApp =>
            {
                errApp.Run(
                    async ctx =>
                    {
                        var exHandlerFeature = ctx.Features.Get<IExceptionHandlerFeature>();

                        if (exHandlerFeature is not null)
                        {
                            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            ctx.Response.ContentType = "application/problem+json";
                            
                            MessageContract messageContract = FailedReasonType.InternalError;
                            messageContract.Error.Message = exHandlerFeature.Error.Message;
                            messageContract.Error.StackTrace = [];

                            await ctx.Response.WriteAsJsonAsync(messageContract);
                        }
                    });
            });

        return app;
    }
}