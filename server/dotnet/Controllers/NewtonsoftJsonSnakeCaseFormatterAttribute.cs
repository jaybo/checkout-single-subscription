using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using System.Buffers;
using System.Linq;

// Allows snake_case JSON used by Stripe to be integrated into a webapp already relying on System.Text.Json
// 
// to use:  add [NewtonsoftJsonSnakeCaseFormatterAttribute] to your controller, ie.

// [NewtonsoftJsonSnakeCaseFormatterAttribute]
// public class PaymentsController : Controller
// { ...

// derived from:
// https://blogs.taiga.nl/martijn/2020/05/28/system-text-json-and-newtonsoft-json-side-by-side-in-asp-net-core/

public class NewtonsoftJsonSnakeCaseFormatterAttribute : ActionFilterAttribute, IControllerModelConvention, IActionModelConvention
{
    public void Apply(ControllerModel controller)
    {
        foreach (var action in controller.Actions)
        {
            Apply(action);
        }
    }

    public void Apply(ActionModel action)
    {
        // Set the model binder to NewtonsoftJsonBodyModelBinder for parameters that are bound to the request body.
        var parameters = action.Parameters.Where(p => p.BindingInfo?.BindingSource == BindingSource.Body);
        foreach (var p in parameters)
        {
            p.BindingInfo.BinderType = typeof(NewtonsoftJsonBodyModelBinder);
        }
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var jsonOptions = context.HttpContext.RequestServices.GetService<IOptions<MvcNewtonsoftJsonOptions>>();
            // SNAKE CASE
            jsonOptions.Value.SerializerSettings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy(),
            };

            objectResult.Formatters.RemoveType<SystemTextJsonOutputFormatter>();
            objectResult.Formatters.Add(new NewtonsoftJsonOutputFormatter(
                jsonOptions.Value.SerializerSettings,
                context.HttpContext.RequestServices.GetRequiredService<ArrayPool<char>>(),
                context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value));
        }
        // ORIGINAL VERSION FROM https://blogs.taiga.nl/martijn/2020/05/28/system-text-json-and-newtonsoft-json-side-by-side-in-asp-net-core/
        // {
        //     var jsonOptions = context.HttpContext.RequestServices.GetService<IOptions<MvcNewtonsoftJsonOptions>>();

        //     objectResult.Formatters.RemoveType<SystemTextJsonOutputFormatter>();
        //     objectResult.Formatters.Add(new NewtonsoftJsonOutputFormatter(

        //         jsonOptions.Value.SerializerSettings,
        //         context.HttpContext.RequestServices.GetRequiredService<ArrayPool<char>>(),
        //         context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value));
        // }
        else
        {
            base.OnActionExecuted(context);
        }
    }
}

public class NewtonsoftJsonBodyModelBinder : BodyModelBinder
{
    public NewtonsoftJsonBodyModelBinder(
        ILoggerFactory loggerFactory,
        ArrayPool<char> charPool,
        IHttpRequestStreamReaderFactory readerFactory,
        ObjectPoolProvider objectPoolProvider,
        IOptions<MvcOptions> mvcOptions,
        IOptions<MvcNewtonsoftJsonOptions> jsonOptions)
        : base(GetInputFormatters(loggerFactory, charPool, objectPoolProvider, mvcOptions, jsonOptions), readerFactory)
    {
    }

    private static IInputFormatter[] GetInputFormatters(
        ILoggerFactory loggerFactory,
        ArrayPool<char> charPool,
        ObjectPoolProvider objectPoolProvider,
        IOptions<MvcOptions> mvcOptions,
        IOptions<MvcNewtonsoftJsonOptions> jsonOptions)
    {
        var jsonOptionsValue = jsonOptions.Value;
        return new IInputFormatter[]
        {
            new NewtonsoftJsonInputFormatter(
                loggerFactory.CreateLogger<NewtonsoftJsonBodyModelBinder>(),
                jsonOptionsValue.SerializerSettings,
                charPool,
                objectPoolProvider,
                mvcOptions.Value,
                jsonOptionsValue)
        };
    }
}