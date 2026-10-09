using System.Diagnostics.CodeAnalysis;

namespace Nook.Domain.Common;

// 예상 가능한 실패를 반환 값으로 표현한다. 값은 TryGetValue로만 꺼낸다 — 실패 확인 없이 값을 쓰는 실수를 막기 위해 Value 프로퍼티를 두지 않는다.

public sealed class Result<T>
{
    private Result(T? value, Error? error) { Value = value; Error = error; }

    private T? Value { get; }
    private Error? Error { get; }
    public bool IsSuccess => Error is null;

    public bool TryGetValue([NotNullWhen(true)] out T? value, [NotNullWhen(false)] out Error? error)
    {
        value = Value;
        error = Error;
        return IsSuccess;
    }

    public static Result<T> Success(T value) =>
        new(value ?? throw new ArgumentNullException(nameof(value)), null);

    public static Result<T> Failure(Error error) =>
        new(default, error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}