---
name: policies-based-authorization
description: Guides agents on implmenting policy based authorization in Asp.net
---

Policy-based authorization in ASP.NET Core
Learn how to create and use authorization policy handlers for enforcing authorization requirements in an ASP.NET Core app.

By wadepickett

18 min. readView original
Underneath the covers, role-based authorization and claims-based authorization use a requirement, a requirement handler, and a preconfigured policy. These building blocks support the expression of authorization evaluations in code. The result is a richer, reusable, testable authorization structure.

An authorization policy consists of one or more requirements. Register it as part of the authorization service configuration, in the app's Program.cs file:

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AtLeast21", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(21)));
});
In the preceding example, an "AtLeast21" policy is created. It has a single requirement—that of a minimum age, which is supplied as a parameter to the requirement.


IAuthorizationService
The primary service that determines if authorization is successful is IAuthorizationService:

/// <summary>
/// Checks policy based permissions for a user
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if a user meets a specific set of requirements for the specified resource
    /// </summary>
    /// <param name="user">The user to evaluate the requirements against.</param>
    /// <param name="resource">
    /// An optional resource the policy should be checked with.
    /// If a resource is not required for policy evaluation you may pass null as the value
    /// </param>
    /// <param name="requirements">The requirements to evaluate.</param>
    /// <returns>
    /// A flag indicating whether authorization has succeeded.
    /// This value is <value>true</value> when the user fulfills the policy; 
    /// otherwise <value>false</value>.
    /// </returns>
    /// <remarks>
    /// Resource is an optional parameter and may be null. Please ensure that you check 
    /// it is not null before acting upon it.
    /// </remarks>
    Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, 
                                     IEnumerable<IAuthorizationRequirement> requirements);

    
/// 
<summary>
    
/// Checks if a user meets a specific authorization policy
    
/// 
</summary>
    
/// <param name="user">The user to check the policy against.</param>
    
/// 
<param name="resource">
    
/// An optional resource the policy should be checked with.
    
/// If a resource is not required for policy evaluation you may pass null as the value
    
/// 
</param>
    
/// <param name="policyName">The name of the policy to check against a specific 
    
/// context.</param>
    
/// 
<returns>
    
/// A flag indicating whether authorization has succeeded.
    
/// Returns a flag indicating whether the user, and optional resource has fulfilled 
    
/// the policy.    
    
/// <value>true</value> when the policy has been fulfilled; 
    
/// otherwise <value>false</value>.
    
/// 
</returns>
    
/// 
<remarks>
    
/// Resource is an optional parameter and may be null. Please ensure that you check
    
/// it is not null before acting upon it.
    
/// 
</remarks>
    Task<AuthorizationResult> AuthorizeAsync(
                                ClaimsPrincipal user, object resource, string policyName);
}
The preceding code highlights the two methods of the IAuthorizationService.

IAuthorizationRequirement is a marker service with no methods, and the mechanism for tracking whether authorization is successful.

Each IAuthorizationHandler is responsible for checking if requirements are met:

/// <summary>
/// Classes implementing this interface are able to make a decision if authorization
/// is allowed.
/// </summary>
public interface IAuthorizationHandler
{
    /// <summary>
    /// Makes a decision if authorization is allowed.
    /// </summary>
    /// <param name="context">The authorization information.</param>
    Task HandleAsync(AuthorizationHandlerContext context);
}
The AuthorizationHandlerContext class is what the handler uses to mark whether requirements have been met:

 context.Succeed(requirement)
The following code shows the simplified (and annotated with comments) default implementation of the authorization service:

public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, 
             object resource, IEnumerable<IAuthorizationRequirement> requirements)
{
    // Create a tracking context from the authorization inputs.
    var authContext = _contextFactory.CreateContext(requirements, user, resource);

    // By default this returns an IEnumerable<IAuthorizationHandler> from DI.
    var handlers = await _handlers.GetHandlersAsync(authContext);

    // Invoke all handlers.
    foreach (var handler in handlers)
    {
        await handler.HandleAsync(authContext);
    }

    // Check the context, by default success is when all requirements have been met.
    return _evaluator.Evaluate(authContext);
}
The following code shows a typical authorization service configuration:

// Add all of your handlers to DI.
builder.Services.AddSingleton<IAuthorizationHandler, MyHandler1>();
// MyHandler2, ...

builder.Services.AddSingleton<IAuthorizationHandler, MyHandlerN>();

// Configure your policies
builder.Services.AddAuthorization(options =>
      options.AddPolicy("Something",
      policy => policy.RequireClaim("Permission", "CanViewPage", "CanViewAnything")));
Use IAuthorizationService, [Authorize(Policy = "Something")], or RequireAuthorization("Something") for authorization.



Apply policies to MVC controllers
For apps that use Razor Pages, see the Apply policies to Razor Pages section.

Apply policies to controllers by using the [Authorize] attribute with the policy name:

[Authorize(Policy = "AtLeast21")]
public class AtLeast21Controller : Controller
{
    public IActionResult Index() => View();
}
If multiple policies are applied at the controller and action levels, all policies must pass before access is granted:

[Authorize(Policy = "AtLeast21")]
public class AtLeast21Controller2 : Controller
{
    [Authorize(Policy = "IdentificationValidated")]
    public IActionResult Index() => View();
}

Apply policies to Razor Pages
Apply policies to Razor Pages by using the [Authorize] attribute with the policy name. For example:

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthorizationPoliciesSample.Pages;

[Authorize(Policy = "AtLeast21")]
public class AtLeast21Model : PageModel { }
Policies can not be applied at the Razor Page handler level, they must be applied to the Page.

Policies can also be applied to Razor Pages by using an authorization convention.


Apply policies to endpoints
Apply policies to endpoints by using RequireAuthorization with the policy name. For example:

app.MapGet("/helloworld", () => "Hello World!")
    .RequireAuthorization("AtLeast21");


Requirements
An authorization requirement is a collection of data parameters that a policy can use to evaluate the current user principal. In our "AtLeast21" policy, the requirement is a single parameter—the minimum age. A requirement implements IAuthorizationRequirement, which is an empty marker interface. A parameterized minimum age requirement could be implemented as follows:

using Microsoft.AspNetCore.Authorization;

namespace AuthorizationPoliciesSample.Policies.Requirements;

public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public MinimumAgeRequirement(int minimumAge) =>
        MinimumAge = minimumAge;

    public int MinimumAge { get; }
}
If an authorization policy contains multiple authorization requirements, all requirements must pass in order for the policy evaluation to succeed. In other words, multiple authorization requirements added to a single authorization policy are treated on an AND basis.

Note

A requirement doesn't need to have data or properties.



Authorization handlers
An authorization handler is responsible for the evaluation of a requirement's properties. The authorization handler evaluates the requirements against a provided AuthorizationHandlerContext to determine if access is allowed.

A requirement can have multiple handlers. A handler may inherit AuthorizationHandler<TRequirement>, where TRequirement is the requirement to be handled. Alternatively, a handler may implement IAuthorizationHandler directly to handle more than one type of requirement.


Use a handler for one requirement

The following example shows a one-to-one relationship in which a minimum age handler handles a single requirement:

using System.Security.Claims;
using AuthorizationPoliciesSample.Policies.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace AuthorizationPoliciesSample.Policies.Handlers;

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        var dateOfBirthClaim = context.User.FindFirst(
            c => c.Type == ClaimTypes.DateOfBirth && c.Issuer == "http://contoso.com");

        if (dateOfBirthClaim is null)
        {
            return Task.CompletedTask;
        }

        var dateOfBirth = Convert.ToDateTime(dateOfBirthClaim.Value);
        int calculatedAge = DateTime.Today.Year - dateOfBirth.Year;
        if (dateOfBirth > DateTime.Today.AddYears(-calculatedAge))
        {
            calculatedAge--;
        }

        if (calculatedAge >= requirement.MinimumAge)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
The preceding code determines if the current user principal has a date of birth claim that has been issued by a known and trusted Issuer. Authorization can't occur when the claim is missing, in which case a completed task is returned. When a claim is present, the user's age is calculated. If the user meets the minimum age defined by the requirement, authorization is considered successful. When authorization is successful, context.Succeed is invoked with the satisfied requirement as its sole parameter.


Use a handler for multiple requirements
The following example shows a one-to-many relationship in which a permission handler can handle three different types of requirements:

using System.Security.Claims;
using AuthorizationPoliciesSample.Policies.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace AuthorizationPoliciesSample.Policies.Handlers;

public class PermissionHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pendingRequirements = context.PendingRequirements.ToList();

        foreach (var requirement in pendingRequirements)
        {
            if (requirement is ReadPermission)
            {
                if (IsOwner(context.User, context.Resource)
                    || IsSponsor(context.User, context.Resource))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is EditPermission || requirement is DeletePermission)
            {
                if (IsOwner(context.User, context.Resource))
                {
                    context.Succeed(requirement);
                }
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsOwner(ClaimsPrincipal user, object? resource)
    {
        // Code omitted for brevity
        return true;
    }

    private static bool IsSponsor(ClaimsPrincipal user, object? resource)
    {
        // Code omitted for brevity
        return true;
    }
}
The preceding code traverses PendingRequirements—a property containing requirements not marked as successful. For a ReadPermission requirement, the user must be either an owner or a sponsor to access the requested resource. For an EditPermission or DeletePermission requirement, they must be an owner to access the requested resource.



Handler registration
Register handlers in the services collection during configuration. For example:

builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
The preceding code registers MinimumAgeHandler as a singleton. Handlers can be registered using any of the built-in service lifetimes.

It's possible to bundle both a requirement and a handler into a single class implementing both IAuthorizationRequirement and IAuthorizationHandler. This bundling creates a tight coupling between the handler and requirement and is only recommended for simple requirements and handlers. Creating a class that implements both interfaces removes the need to register the handler in DI because of the built-in PassThroughAuthorizationHandler that allows requirements to handle themselves.

See the implementation of the AssertionRequirement class for a good example where the AssertionRequirement is both a requirement and the handler in a fully self-contained class.


What should a handler return?
Note that the Handle method in the handler example returns no value. How is a status of either success or failure indicated?

A handler indicates success by calling context.Succeed(IAuthorizationRequirement requirement), passing the requirement that has been successfully validated.

A handler doesn't need to handle failures generally, as other handlers for the same requirement may succeed.

To guarantee failure, even if other requirement handlers succeed, call context.Fail.

If a handler calls context.Succeed or context.Fail, all other handlers are still called. This allows requirements to produce side effects, such as logging, which takes place even if another handler has successfully validated or failed a requirement. When set to false, the InvokeHandlersAfterFailure property short-circuits the execution of handlers when context.Fail is called. InvokeHandlersAfterFailure defaults to true, in which case all handlers are called.

Note

Authorization handlers are called even if authentication fails. Also handlers can execute in any order, so do not depend on them being called in any particular order.



Why would I want multiple handlers for a requirement?
In cases where you want evaluation to be on an OR basis, implement multiple handlers for a single requirement. For example, Microsoft has doors that only open with key cards. If you leave your key card at home, the receptionist prints a temporary sticker and opens the door for you. In this scenario, you'd have a single requirement, BuildingEntry, but multiple handlers, each one examining a single requirement.

BuildingEntryRequirement.cs

using Microsoft.AspNetCore.Authorization;

namespace AuthorizationPoliciesSample.Policies.Requirements;

public class BuildingEntryRequirement : IAuthorizationRequirement { }
BadgeEntryHandler.cs

using AuthorizationPoliciesSample.Policies.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace AuthorizationPoliciesSample.Policies.Handlers;

public class BadgeEntryHandler : AuthorizationHandler<BuildingEntryRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, BuildingEntryRequirement requirement)
    {
        if (context.User.HasClaim(
            c => c.Type == "BadgeId" && c.Issuer == "https://microsoftsecurity"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
TemporaryStickerHandler.cs

using AuthorizationPoliciesSample.Policies.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace AuthorizationPoliciesSample.Policies.Handlers;

public class TemporaryStickerHandler : AuthorizationHandler<BuildingEntryRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, BuildingEntryRequirement requirement)
    {
        if (context.User.HasClaim(
            c => c.Type == "TemporaryBadgeId" && c.Issuer == "https://microsoftsecurity"))
        {
            // Code to check expiration date omitted for brevity.
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
Ensure that both handlers are registered. If either handler succeeds when a policy evaluates the BuildingEntryRequirement, the policy evaluation succeeds.



Use a func to fulfill a policy
There may be situations in which fulfilling a policy is simple to express in code. It's possible to supply a Func<AuthorizationHandlerContext, bool> when configuring a policy with the RequireAssertion policy builder.

For example, the previous BadgeEntryHandler could be rewritten as follows:

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BadgeEntry", policy =>
        policy.RequireAssertion(context => context.User.HasClaim(c =>
            (c.Type == "BadgeId" || c.Type == "TemporaryBadgeId")
            && c.Issuer == "https://microsoftsecurity")));
});


Access MVC request context in handlers
The HandleRequirementAsync method has two parameters: an AuthorizationHandlerContext and the TRequirement being handled. Frameworks such as MVC or SignalR are free to add any object to the Resource property on the AuthorizationHandlerContext to pass extra information.

When using endpoint routing, authorization is typically handled by the Authorization Middleware. In this case, the Resource property is an instance of HttpContext. The context can be used to access the current endpoint, which can be used to probe the underlying resource to which you're routing. For example:

if (context.Resource is HttpContext httpContext)
{
    var endpoint = httpContext.GetEndpoint();
    var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
    ...
}
With traditional routing, or when authorization happens as part of MVC's authorization filter, the value of Resource is an AuthorizationFilterContext instance. This property provides access to HttpContext, RouteData, and everything else provided by MVC and Razor Pages.

The use of the Resource property is framework-specific. Using information in the Resource property limits your authorization policies to particular frameworks. Cast the Resource property using the is keyword, and then confirm the cast has succeeded to ensure your code doesn't crash with an InvalidCastException when run on other frameworks:

// Requires the following import:
//     using Microsoft.AspNetCore.Mvc.Filters;
if (context.Resource is AuthorizationFilterContext mvcContext)
{
    // Examine MVC-specific things like routing data.
}

Globally require all users to be authenticated
For information on how to globally require all users to be authenticated, see Require authenticated users.



Authorization with external service sample
The sample code on AspNetCore.Docs.Samples shows how to implement additional authorization requirements with an external authorization service. The sample Contoso.API project is secured with Azure AD. An additional authorization check from the Contoso.Security.API project returns a payload describing whether the Contoso.API client app can invoke the GetWeather API.


Configure the sample
Create an application registration in your Microsoft Entra ID tenant:

Assign it an AppRole.

Under API permissions, add the AppRole as a permission and grant Admin consent. Note that in this setup, this app registration represents both the API and the client invoking the API. If you like, you can create two app registrations. If you are using this setup, be sure to only perform the API permissions, add AppRole as a permission step for only the client. Only the client app registration requires a client secret to be generated.

Configure the Contoso.API project with the following settings:

{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "<Tenant name from AAD properties>.onmicrosoft.com",
    "TenantId": "<Tenant Id from AAD properties>",
    "ClientId": "<Client Id from App Registration representing the API>"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
Configure Contoso.Security.API with the following settings:
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AllowedClients": [
    "<Use the appropriate Client Id representing the Client calling the API>"
  ]
}
Open the ContosoAPI.collection.json file and configure an environment with the following:

ClientId: Client Id from app registration representing the client calling the API.
clientSecret: Client Secret from app registration representing the client calling the API.
TenantId: Tenant Id from AAD properties
Extract the commands from the ContosoAPI.collection.json file and use them to construct cURL commands to test the app.

Run the solution and use cURL to invoke the API. You can add breakpoints in the Contoso.Security.API.SecurityPolicyController and observe the client Id is being passed in that is used to assert whether it is allowed to Get Weather.


Additional resources
Underneath the covers, role-based authorization and claims-based authorization use a requirement, a requirement handler, and a pre-configured policy. These building blocks support the expression of authorization evaluations in code. The result is a richer, reusable, testable authorization structure.

An authorization policy consists of one or more requirements. It's registered as part of the authorization service configuration, in the Startup.ConfigureServices method:

public void ConfigureServices(IServiceCollection services)
{
    services.AddControllersWithViews();
    services.AddRazorPages();
    services.AddAuthorization(options =>
    {
        options.AddPolicy("AtLeast21", policy =>
            policy.Requirements.Add(new MinimumAgeRequirement(21)));
    });
}
In the preceding example, an "AtLeast21" policy is created. It has a single requirement—that of a minimum age, which is supplied as a parameter to the requirement.


IAuthorizationService
The primary service that determines if authorization is successful is IAuthorizationService:

/// <summary>
/// Checks policy based permissions for a user
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if a user meets a specific set of requirements for the specified resource
    /// </summary>
    /// <param name="user">The user to evaluate the requirements against.</param>
    /// <param name="resource">
    /// An optional resource the policy should be checked with.
    /// If a resource is not required for policy evaluation you may pass null as the value
    /// </param>
    /// <param name="requirements">The requirements to evaluate.</param>
    /// <returns>
    /// A flag indicating whether authorization has succeeded.
    /// This value is <value>true</value> when the user fulfills the policy; 
    /// otherwise <value>false</value>.
    /// </returns>
    /// <remarks>
    /// Resource is an optional parameter and may be null. Please ensure that you check 
    /// it is not null before acting upon it.
    /// </remarks>
    Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, 
                                     IEnumerable<IAuthorizationRequirement> requirements);

    
/// 
<summary>
    
/// Checks if a user meets a specific authorization policy
    
/// 
</summary>
    
/// <param name="user">The user to check the policy against.</param>
    
/// 
<param name="resource">
    
/// An optional resource the policy should be checked with.
    
/// If a resource is not required for policy evaluation you may pass null as the value
    
/// 
</param>
    
/// <param name="policyName">The name of the policy to check against a specific 
    
/// context.</param>
    
/// 
<returns>
    
/// A flag indicating whether authorization has succeeded.
    
/// Returns a flag indicating whether the user, and optional resource has fulfilled 
    
/// the policy.    
    
/// <value>true</value> when the policy has been fulfilled; 
    
/// otherwise <value>false</value>.
    
/// 
</returns>
    
/// 
<remarks>
    
/// Resource is an optional parameter and may be null. Please ensure that you check
    
/// it is not null before acting upon it.
    
/// 
</remarks>
    Task<AuthorizationResult> AuthorizeAsync(
                                ClaimsPrincipal user, object resource, string policyName);
}
The preceding code highlights the two methods of the IAuthorizationService.

IAuthorizationRequirement is a marker service with no methods, and the mechanism for tracking whether authorization is successful.

Each IAuthorizationHandler is responsible for checking if requirements are met:

/// <summary>
/// Classes implementing this interface are able to make a decision if authorization
/// is allowed.
/// </summary>
public interface IAuthorizationHandler
{
    /// <summary>
    /// Makes a decision if authorization is allowed.
    /// </summary>
    /// <param name="context">The authorization information.</param>
    Task HandleAsync(AuthorizationHandlerContext context);
}
The AuthorizationHandlerContext class is what the handler uses to mark whether requirements have been met:

 context.Succeed(requirement)
The following code shows the simplified (and annotated with comments) default implementation of the authorization service:

public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, 
             object resource, IEnumerable<IAuthorizationRequirement> requirements)
{
    // Create a tracking context from the authorization inputs.
    var authContext = _contextFactory.CreateContext(requirements, user, resource);

    // By default this returns an IEnumerable<IAuthorizationHandlers> from DI.
    var handlers = await _handlers.GetHandlersAsync(authContext);

    // Invoke all handlers.
    foreach (var handler in handlers)
    {
        await handler.HandleAsync(authContext);
    }

    // Check the context, by default success is when all requirements have been met.
    return _evaluator.Evaluate(authContext);
}
The following code shows a typical ConfigureServices:

public void ConfigureServices(IServiceCollection services)
{
    // Add all of your handlers to DI.
    services.AddSingleton<IAuthorizationHandler, MyHandler1>();
    // MyHandler2, ...

    services.AddSingleton<IAuthorizationHandler, MyHandlerN>();

    // Configure your policies
    services.AddAuthorization(options =>
          options.AddPolicy("Something",
          policy => policy.RequireClaim("Permission", "CanViewPage", "CanViewAnything")));


    services.AddControllersWithViews();
    services.AddRazorPages();
}
Use IAuthorizationService or [Authorize(Policy = "Something")] for authorization.



Apply policies to MVC controller
If you're using Razor Pages, see Apply policies to Razor Pages in this document.

Policies are applied to controllers by using the [Authorize] attribute with the policy name. For example:

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = "AtLeast21")]
public class AlcoholPurchaseController : Controller
{
    public IActionResult Index() => View();
}

Apply policies to Razor Pages
Policies are applied to Razor Pages by using the [Authorize] attribute with the policy name. For example:

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize(Policy = "AtLeast21")]
public class AlcoholPurchaseModel : PageModel
{
}
Policies can not be applied at the Razor Page handler level, they must be applied to the Page.

Policies can be applied to Razor Pages by using an authorization convention.



Requirements
An authorization requirement is a collection of data parameters that a policy can use to evaluate the current user principal. In our "AtLeast21" policy, the requirement is a single parameter—the minimum age. A requirement implements IAuthorizationRequirement, which is an empty marker interface. A parameterized minimum age requirement could be implemented as follows:

using Microsoft.AspNetCore.Authorization;

public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge { get; }

    public MinimumAgeRequirement(int minimumAge)
    {
        MinimumAge = minimumAge;
    }
}
If an authorization policy contains multiple authorization requirements, all requirements must pass in order for the policy evaluation to succeed. In other words, multiple authorization requirements added to a single authorization policy are treated on an AND basis.

Note

A requirement doesn't need to have data or properties.



Authorization handlers
An authorization handler is responsible for the evaluation of a requirement's properties. The authorization handler evaluates the requirements against a provided AuthorizationHandlerContext to determine if access is allowed.

A requirement can have multiple handlers. A handler may inherit AuthorizationHandler<TRequirement>, where TRequirement is the requirement to be handled. Alternatively, a handler may implement IAuthorizationHandler to handle more than one type of requirement.


Use a handler for one requirement

The following example shows a one-to-one relationship in which a minimum age handler utilizes a single requirement:

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PoliciesAuthApp1.Services.Requirements;

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                   MinimumAgeRequirement requirement)
    {
        if (!context.User.HasClaim(c => c.Type == ClaimTypes.DateOfBirth &&
                                        c.Issuer == "http://contoso.com"))
        {
            //TODO: Use the following if targeting a version of
            //.NET Framework older than 4.6:
            //      return Task.FromResult(0);
            return Task.CompletedTask;
        }

        var dateOfBirth = Convert.ToDateTime(
            context.User.FindFirst(c => c.Type == ClaimTypes.DateOfBirth && 
                                        c.Issuer == "http://contoso.com").Value);

        int calculatedAge = DateTime.Today.Year - dateOfBirth.Year;
        if (dateOfBirth > DateTime.Today.AddYears(-calculatedAge))
        {
            calculatedAge--;
        }

        if (calculatedAge >= requirement.MinimumAge)
        {
            context.Succeed(requirement);
        }

        //TODO: Use the following if targeting a version of
        //.NET Framework older than 4.6:
        //      return Task.FromResult(0);
        return Task.CompletedTask;
    }
}
The preceding code determines if the current user principal has a date of birth claim that has been issued by a known and trusted Issuer. Authorization can't occur when the claim is missing, in which case a completed task is returned. When a claim is present, the user's age is calculated. If the user meets the minimum age defined by the requirement, authorization is considered successful. When authorization is successful, context.Succeed is invoked with the satisfied requirement as its sole parameter.


Use a handler for multiple requirements
The following example shows a one-to-many relationship in which a permission handler can handle three different types of requirements:

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PoliciesAuthApp1.Services.Requirements;

public class PermissionHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pendingRequirements = context.PendingRequirements.ToList();

        foreach (var requirement in pendingRequirements)
        {
            if (requirement is ReadPermission)
            {
                if (IsOwner(context.User, context.Resource) ||
                    IsSponsor(context.User, context.Resource))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is EditPermission ||
                     requirement is DeletePermission)
            {
                if (IsOwner(context.User, context.Resource))
                {
                    context.Succeed(requirement);
                }
            }
        }

        //TODO: Use the following if targeting a version of
        //.NET Framework older than 4.6:
        //      return Task.FromResult(0);
        return Task.CompletedTask;
    }

    private bool IsOwner(ClaimsPrincipal user, object resource)
    {
        // Code omitted for brevity

        return true;
    }

    private bool IsSponsor(ClaimsPrincipal user, object resource)
    {
        // Code omitted for brevity

        return true;
    }
}
The preceding code traverses PendingRequirements—a property containing requirements not marked as successful. For a ReadPermission requirement, the user must be either an owner or a sponsor to access the requested resource. For an EditPermission or DeletePermission requirement, the user must be an owner to access the requested resource.



Handler registration
Handlers are registered in the services collection during configuration. For example:

public void ConfigureServices(IServiceCollection services)
{
    services.AddControllersWithViews();
    services.AddRazorPages();
    services.AddAuthorization(options =>
    {
        options.AddPolicy("AtLeast21", policy =>
            policy.Requirements.Add(new MinimumAgeRequirement(21)));
    });

    services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
}
The preceding code registers MinimumAgeHandler as a singleton by invoking services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();. Handlers can be registered using any of the built-in service lifetimes.

It's possible to bundle both a requirement and a handler in a single class implementing both IAuthorizationRequirement and IAuthorizationHandler. This bundling creates a tight coupling between the handler and requirement and is only recommended for simple requirements and handlers. Creating a class that implements both interfaces removes the need to register the handler in DI because of the built-in PassThroughAuthorizationHandler that allows requirements to handle themselves.

See the AssertionRequirement class for a good example where the AssertionRequirement is both a requirement and the handler in a fully self-contained class.


What should a handler return?
Note that the Handle method in the handler example returns no value. How is a status of either success or failure indicated?

A handler indicates success by calling context.Succeed(IAuthorizationRequirement requirement), passing the requirement that has been successfully validated.

A handler doesn't need to handle failures generally, as other handlers for the same requirement may succeed.

To guarantee failure, even if other requirement handlers succeed, call context.Fail.

If a handler calls context.Succeed or context.Fail, all other handlers are still called. This allows requirements to produce side effects, such as logging, which takes place even if another handler has successfully validated or failed a requirement. When set to false, the InvokeHandlersAfterFailure property short-circuits the execution of handlers when context.Fail is called. InvokeHandlersAfterFailure defaults to true, in which case all handlers are called.

Note

Authorization handlers are called even if authentication fails.



Why would I want multiple handlers for a requirement?
In cases where you want evaluation to be on an OR basis, implement multiple handlers for a single requirement. For example, Microsoft has doors that only open with key cards. If you leave your key card at home, the receptionist prints a temporary sticker and opens the door for you. In this scenario, you'd have a single requirement, BuildingEntry, but multiple handlers, each one examining a single requirement.

BuildingEntryRequirement.cs

using Microsoft.AspNetCore.Authorization;

public class BuildingEntryRequirement : IAuthorizationRequirement
{
}
BadgeEntryHandler.cs

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PoliciesAuthApp1.Services.Requirements;

public class BadgeEntryHandler : AuthorizationHandler<BuildingEntryRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                   BuildingEntryRequirement requirement)
    {
        if (context.User.HasClaim(c => c.Type == "BadgeId" &&
                                       c.Issuer == "http://microsoftsecurity"))
        {
            context.Succeed(requirement);
        }

        //TODO: Use the following if targeting a version of
        //.NET Framework older than 4.6:
        //      return Task.FromResult(0);
        return Task.CompletedTask;
    }
}
TemporaryStickerHandler.cs

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PoliciesAuthApp1.Services.Requirements;

public class TemporaryStickerHandler : AuthorizationHandler<BuildingEntryRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, 
                                                   BuildingEntryRequirement requirement)
    {
        if (context.User.HasClaim(c => c.Type == "TemporaryBadgeId" &&
                                       c.Issuer == "https://microsoftsecurity"))
        {
            // We'd also check the expiration date on the sticker.
            context.Succeed(requirement);
        }

        //TODO: Use the following if targeting a version of
        //.NET Framework older than 4.6:
        //      return Task.FromResult(0);
        return Task.CompletedTask;
    }
}
Ensure that both handlers are registered. If either handler succeeds when a policy evaluates the BuildingEntryRequirement, the policy evaluation succeeds.



Use a func to fulfill a policy
There may be situations in which fulfilling a policy is simple to express in code. It's possible to supply a Func<AuthorizationHandlerContext, bool> when configuring your policy with the RequireAssertion policy builder.

For example, the previous BadgeEntryHandler could be rewritten as follows:

services.AddAuthorization(options =>
{
     options.AddPolicy("BadgeEntry", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                (c.Type == "BadgeId" ||
                 c.Type == "TemporaryBadgeId") &&
                 c.Issuer == "https://microsoftsecurity")));
});


Access MVC request context in handlers
The HandleRequirementAsync method you implement in an authorization handler has two parameters: an AuthorizationHandlerContext and the TRequirement you are handling. Frameworks such as MVC or SignalR are free to add any object to the Resource property on the AuthorizationHandlerContext to pass extra information.

When using endpoint routing, authorization is typically handled by the Authorization Middleware. In this case, the Resource property is an instance of HttpContext. The context can be used to access the current endpoint, which can be used to probe the underlying resource to which you're routing. For example:

if (context.Resource is HttpContext httpContext)
{
    var endpoint = httpContext.GetEndpoint();
    var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
    ...
}
With traditional routing, or when authorization happens as part of MVC's authorization filter, the value of Resource is an AuthorizationFilterContext instance. This property provides access to HttpContext, RouteData, and everything else provided by MVC and Razor Pages.

The use of the Resource property is framework-specific. Using information in the Resource property limits your authorization policies to particular frameworks. Cast the Resource property using the is keyword, and then confirm the cast has succeeded to ensure your code doesn't crash with an InvalidCastException when run on other frameworks:

// Requires the following import:
//     using Microsoft.AspNetCore.Mvc.Filters;
if (context.Resource is AuthorizationFilterContext mvcContext)
{
    // Examine MVC-specific things like routing data.
}

Globally require all users to be authenticated
For information on how to globally require all users to be authenticated, see Require authenticated users.