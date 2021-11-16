namespace SailScores.Web.Services.Interfaces;

public interface ITemplateHelper
{
    Task<string> GetTemplateHtmlAsStringAsync<T>(
        string viewName, T model);
}