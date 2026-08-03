using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;

public static class UrlHelperExtensions
{
    // Denne ene metode håndterer nu BÅDE synkrone og asynkrone controller-metoder fejlfrit!
    public static string Action<TController>(this IUrlHelper urlHelper, Expression<Func<TController, object>> actionExpression) 
        where TController : Controller
    {
        // Hvis metoden returnerer en Task (asynkron), vil C# pakke den ind i en UnaryExpression (Convert).
        // Vi finder det rigtige MethodCallExpression uanset hvad:
        var body = actionExpression.Body;
        if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
        {
            body = unary.Operand;
        }

        var methodCall = body as MethodCallExpression;
        if (methodCall == null) 
            throw new ArgumentException("Udtrykket skal være et direkte metodekald, f.eks. c => c.Index()");

        var actionName = methodCall.Method.Name;
        var controllerName = typeof(TController).Name;
        
        // Klipper "Controller" af enden af klassenavnet (f.eks. UserManagementController -> UserManagement)
        if (controllerName.EndsWith("Controller"))
            controllerName = controllerName[..^10];

        return urlHelper.Action(actionName, controllerName);
    }
}