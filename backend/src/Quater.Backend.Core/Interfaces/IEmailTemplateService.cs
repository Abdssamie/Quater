namespace Quater.Backend.Core.Interfaces;

/// <summary>
/// Email template rendering service
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Render an email template with the given model
    /// </summary>
    /// <typeparam name="TModel">Model type with template data</typeparam>
    /// <param name="templateName">Name of the template (e.g., "verification", "password-reset")</param>
    /// <param name="model">Data model for template variables</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rendered HTML content</returns>
    Task<string> RenderAsync<TModel>(string templateName, TModel model, CancellationToken cancellationToken = default)
        where TModel : class;
}
