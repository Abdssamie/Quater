namespace Quater.Backend.Infrastructure.Email;

using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;
using Quater.Backend.Core.Interfaces;
using System.Reflection;

/// <summary>
/// Scriban-based email template renderer
/// Templates are stored as embedded resources
/// </summary>
public sealed class ScribanTemplateService(ILogger<ScribanTemplateService> logger) : IEmailTemplateService, IDisposable
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Template> _templateCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private bool _disposed;

    public async Task<string> RenderAsync<TModel>(string templateName, TModel model, CancellationToken cancellationToken = default)
        where TModel : class
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ScribanTemplateService));

        try
        {
            var template = await GetTemplateAsync(templateName, cancellationToken);

            var scriptObject = new ScriptObject();
            // Use snake_case renamer to match template variables (user_name, app_name, etc.)
            scriptObject.Import(model, renamer: member => ToSnakeCase(member.Name));

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            var result = await template.RenderAsync(context);

            logger.LogDebug("Successfully rendered template '{TemplateName}'", templateName);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to render template '{TemplateName}'", templateName);
            throw;
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(name[0]));

        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                result.Append(name[i]);
            }
        }

        return result.ToString();
    }

    private async Task<Template> GetTemplateAsync(string templateName, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ScribanTemplateService));

        // Check cache first (thread-safe read)
        if (_templateCache.TryGetValue(templateName, out var cachedTemplate))
        {
            return cachedTemplate;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_templateCache.TryGetValue(templateName, out cachedTemplate))
            {
                return cachedTemplate;
            }

            // Load template from embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Quater.Backend.Infrastructure.Email.Templates.{templateName}.html";

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"Email template '{templateName}' not found as embedded resource: {resourceName}");
            }

            using var reader = new StreamReader(stream);
            var templateContent = await reader.ReadToEndAsync(cancellationToken);

            var template = Template.Parse(templateContent);
            if (template.HasErrors)
            {
                var errors = string.Join(", ", template.Messages.Select(m => m.Message));
                throw new InvalidOperationException($"Template '{templateName}' has parsing errors: {errors}");
            }

            _templateCache[templateName] = template;
            logger.LogInformation("Loaded and cached template '{TemplateName}'", templateName);

            return template;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _cacheLock?.Dispose();
        _disposed = true;
    }
}
