using System.Net;
using Azure.Messaging;
using EasyMicroservices.ServiceContracts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace TravelBookingSystem.Common.Filters;

internal class EndpointValidatorFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    private IValidator<T> Validator => validator;
  
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var inputData = context.GetArgument<T>(0);

        if (inputData is not null)
        {
            var validationResult = await Validator.ValidateAsync(inputData);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary();
                MessageContract messageContract = FailedReasonType.ValidationsError;
                messageContract.Error = new ErrorContract
                {
                    Validations = errors.SelectMany(err => err.Value).Select(err => new ValidationContract
                    {
                        Message = err
                    }).ToList()
                };

                return Results.Json(messageContract, statusCode: (int)HttpStatusCode.UnprocessableEntity);
            }
        }

        return await next.Invoke(context);
    }
}