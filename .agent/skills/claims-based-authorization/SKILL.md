---
name: claims-based-authorization
description: A guide on working with claims in asp.net core
---
Claims-based authorization in ASP.NET Core

When an identity is created it may be assigned one or more claims issued by a trusted party. A claim is a name value pair that represents what the subject is, not what the subject can do. For example, you may have a driver's license, issued by a local driving license authority. Your driver's license has your date of birth on it. In this case the claim name would be DateOfBirth, the claim value would be your date of birth, for example 8th June 1970 and the issuer would be the driving license authority. Claims-based authorization, at its simplest, checks the value of a claim and allows access to a resource based upon that value. For example if you want access to a night club the authorization process might be:

The door security officer would evaluate the value of your date of birth claim and whether they trust the issuer (the driving license authority) before granting you access.

An identity can contain multiple claims with multiple values and can contain multiple claims of the same type.

Adding claims checks
Claim-based authorization checks:

Are declarative.
Are applied to Razor Pages, controllers, or actions within a controller.
Can not be applied at the Razor Page handler level, they must be applied to the Page.
Claims in code specify claims which the current user must possess, and optionally the value the claim must hold to access the requested resource. Claims requirements are policy based; the developer must build and register a policy expressing the claims requirements.

The simplest type of claim policy looks for the presence of a claim and doesn't check the value.

Build and register the policy and call UseAuthorization. Registering the policy takes place as part of the Authorization service configuration, typically in the Program.cs file:

C#
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("EmployeeOnly", policy => policy.RequireClaim("EmployeeNumber"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();
app.MapRazorPages();

app.Run();
In this case the EmployeeOnly policy checks for the presence of an EmployeeNumber claim on the current identity.

Apply the policy using the Policy property on the [Authorize] attribute to specify the policy name.

C#
[Authorize(Policy = "EmployeeOnly")]
public IActionResult VacationBalance()
{
    return View();
}
The [Authorize] attribute can be applied to an entire controller or Razor Page, in which case only identities matching the policy are allowed access to any Action on the controller.

C#
[Authorize(Policy = "EmployeeOnly")]
public class VacationController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public ActionResult VacationBalance()
    {
        return View();
    }

    [AllowAnonymous]
    public ActionResult VacationPolicy()
    {
        return View();
    }
}
The following code applies the [Authorize] attribute to a Razor Page:

C#
[Authorize(Policy = "EmployeeOnly")]
public class IndexModel : PageModel
{
    public void OnGet()
    {

    }
}
Policies can not be applied at the Razor Page handler level, they must be applied to the Page.

If you have a controller that's protected by the [Authorize] attribute but want to allow anonymous access to particular actions, you apply the AllowAnonymousAttribute attribute.

C#
[Authorize(Policy = "EmployeeOnly")]
public class VacationController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public ActionResult VacationBalance()
    {
        return View();
    }

    [AllowAnonymous]
    public ActionResult VacationPolicy()
    {
        return View();
    }
}
Because policies can not be applied at the Razor Page handler level, we recommend using a controller when policies must be applied at the page handler level. The rest of the app that doesn't require policies at the Razor Page handler level can use Razor Pages.

Most claims come with a value. You can specify a list of allowed values when creating the policy. The following example would only succeed for employees whose employee number was 1, 2, 3, 4, or 5.

C#
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Founders", policy =>
        policy.RequireClaim("EmployeeNumber", "1", "2", "3", "4", "5"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();
app.MapRazorPages();

app.Run();
Add a generic claim check
If the claim value isn't a single value or a transformation is required, use RequireAssertion. For more information, see Use a func to fulfill a policy.

Multiple Policy Evaluation
If multiple policies are applied at the controller and action levels, all policies must pass before access is granted:

C#
[Authorize(Policy = "EmployeeOnly")]
public class SalaryController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Payslip()
    {
        return View();
    }

    [Authorize(Policy = "HumanResources")]
    public IActionResult UpdateSalary()
    {
        return View();
    }
}
In the preceding example any identity which fulfills the EmployeeOnly policy can access the Payslip action as that policy is enforced on the controller. However, in order to call the UpdateSalary action the identity must fulfill both the EmployeeOnly policy and the HumanResources policy.

If you want more complicated policies, such as taking a date of birth claim, calculating an age from it, and then checking that the age is 21 or older, then you need to write custom policy handlers.

In the following sample, both page handler methods must fulfill both the EmployeeOnly policy and the HumanResources policy:

C#
[Authorize(Policy = "EmployeeOnly")]
[Authorize(Policy = "HumanResources")]
public class SalaryModel : PageModel
{
    public ContentResult OnGetPayStub()
    {
        return Content("OnGetPayStub");
    }

    public ContentResult OnGetSalary()
    {
        return Content("OnGetSalary");
    }
}