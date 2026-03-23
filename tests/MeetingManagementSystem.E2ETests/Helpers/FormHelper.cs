using AngleSharp;
using AngleSharp.Html.Dom;

namespace MeetingManagementSystem.E2ETests.Helpers;

/// <summary>
/// Helper class for working with HTML forms in E2E tests.
/// Uses AngleSharp to parse HTML and extract form data.
/// </summary>
public static class FormHelper
{
    /// <summary>
    /// Extract the antiforgery token from HTML content.
    /// </summary>
    public static string ExtractAntiForgeryToken(string htmlContent)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(req => req.Content(htmlContent)).Result;
        
        var tokenInput = document.QuerySelector("input[name='__RequestVerificationToken']") as IHtmlInputElement;
        
        if (tokenInput == null)
        {
            throw new InvalidOperationException("Antiforgery token not found in HTML content");
        }

        return tokenInput.Value ?? throw new InvalidOperationException("Antiforgery token value is null");
    }

    /// <summary>
    /// Extract all form inputs from HTML content.
    /// </summary>
    public static Dictionary<string, string> ExtractFormInputs(string htmlContent, string formSelector = "form")
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(req => req.Content(htmlContent)).Result;
        
        var form = document.QuerySelector(formSelector) as IHtmlFormElement;
        if (form == null)
        {
            throw new InvalidOperationException($"Form with selector '{formSelector}' not found");
        }

        var inputs = new Dictionary<string, string>();
        
        foreach (var element in form.Elements)
        {
            if (element is IHtmlInputElement input && !string.IsNullOrEmpty(input.Name))
            {
                inputs[input.Name] = input.Value ?? string.Empty;
            }
            else if (element is IHtmlTextAreaElement textarea && !string.IsNullOrEmpty(textarea.Name))
            {
                inputs[textarea.Name] = textarea.Value ?? string.Empty;
            }
            else if (element is IHtmlSelectElement select && !string.IsNullOrEmpty(select.Name))
            {
                inputs[select.Name] = select.Value ?? string.Empty;
            }
        }

        return inputs;
    }

    /// <summary>
    /// Create form data with antiforgery token included.
    /// </summary>
    public static FormUrlEncodedContent CreateFormContent(string htmlContent, Dictionary<string, string> formData)
    {
        var token = ExtractAntiForgeryToken(htmlContent);
        var data = new Dictionary<string, string>(formData)
        {
            ["__RequestVerificationToken"] = token
        };

        return new FormUrlEncodedContent(data);
    }

    /// <summary>
    /// Extract validation error messages from HTML content.
    /// </summary>
    public static List<string> ExtractValidationErrors(string htmlContent)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(req => req.Content(htmlContent)).Result;
        
        var errors = new List<string>();
        
        // Extract validation summary errors
        var validationSummary = document.QuerySelectorAll(".validation-summary-errors li");
        errors.AddRange(validationSummary.Select(e => e.TextContent.Trim()));
        
        // Extract field-level validation errors
        var fieldErrors = document.QuerySelectorAll(".field-validation-error");
        errors.AddRange(fieldErrors.Select(e => e.TextContent.Trim()));

        return errors.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
    }

    /// <summary>
    /// Check if a specific element exists in HTML content.
    /// </summary>
    public static bool ElementExists(string htmlContent, string selector)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(req => req.Content(htmlContent)).Result;
        
        return document.QuerySelector(selector) != null;
    }

    /// <summary>
    /// Extract text content from an element.
    /// </summary>
    public static string? ExtractTextContent(string htmlContent, string selector)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(req => req.Content(htmlContent)).Result;
        
        var element = document.QuerySelector(selector);
        return element?.TextContent.Trim();
    }

    /// <summary>
    /// Extract all options from a select element.
    /// </summary>
    public static List<(string Value, string Text)> ExtractSelectOptions(string htmlContent, string selectName)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(req => req.Content(htmlContent)).Result;
        
        var select = document.QuerySelector($"select[name='{selectName}']") as IHtmlSelectElement;
        if (select == null)
        {
            return new List<(string, string)>();
        }

        return select.Options
            .Select(opt => (opt.Value, opt.Text))
            .ToList();
    }
}
