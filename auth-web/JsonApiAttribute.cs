using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SarData
{
  public class JsonApiAttribute : ActionFilterAttribute
  {
    public override void OnActionExecuted(ActionExecutedContext context)
    {
      if (context.Result is JsonResult result)
      {
        result.ContentType = "application/vnd.api+json";
      }
    }
  }
}
