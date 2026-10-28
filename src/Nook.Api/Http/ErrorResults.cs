using Microsoft.AspNetCore.Mvc;
using Nook.SharedKernel;

namespace Nook.Api.Http;

// Error → ProblemDetails 번역을 한 곳에(D2-1: Domain은 HTTP를 모르고 Api가 Type을 상태 코드로 번역한다).
// title = Error.Code(클라이언트 계약), detail = 개발자용 설명.
public static class ErrorResults
{
    public static ObjectResult Problem(this ControllerBase controller, Error error) =>
        controller.Problem(statusCode: StatusCodeOf(error.Type), title: error.Code, detail: error.Description);

    // ErrorType을 늘리면 여기 한 줄을 더한다. 빠뜨리면 예외 → 500.
    private static int StatusCodeOf(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
