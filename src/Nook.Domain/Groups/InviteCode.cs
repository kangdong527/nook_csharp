using Nook.Domain.Common;

namespace Nook.Domain.Groups;

// 초대 코드 값 객체. 생성(서버, 신뢰)과 파싱(회원 입력, 불신) 두 경로가 있다.
// 존재 여부는 모른다 — 코드로 그룹을 찾는 일은 Application(리포지토리)이 맡는다.
public sealed record InviteCode
{
    public const int Length = 6;

    // A-Z에서 I·O 제외(24자) + 2-9(8자) = 32자. 1/I, 0/O처럼 헷갈리는 글자 제외
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private InviteCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // DB 값 복원(EF Core 값 변환기 입력). 회원 입력에는 Create, 새 코드 발급에는 Generate를 쓴다.
    // 형식은 다시 검사하지 않고 값도 바꾸지 않는다(Nickname.From과 같은 기준, 저장된 값은 ck_groups_invite_code가 보장). 빈 값은 데이터 손상으로 보고 예외.
    public static InviteCode From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new InviteCode(value);
    }

    public static Result<InviteCode> Create(string? raw)
    {
        if (raw is null)
            return GroupErrors.InviteCodeInvalidFormat;

        var trimmed = raw.Trim();
        if (trimmed.Length != Length)
            return GroupErrors.InviteCodeInvalidFormat;

        Span<char> normalized = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
        {
            var c = trimmed[i];

            // 문화권과 무관한 Invariant 변환을 ASCII에만 적용
            var upper = char.IsAscii(c) ? char.ToUpperInvariant(c) : c;

            if (!Alphabet.Contains(upper))
                return GroupErrors.InviteCodeInvalidFormat;

            normalized[i] = upper;
        }

        return new InviteCode(new string(normalized));
    }

    // nextIndex(n)은 0 이상 n 미만의 정수를 돌려준다. 운영은 암호학적 난수, 테스트는 고정 순서를 넘긴다.
    public static InviteCode Generate(Func<int, int> nextIndex)
    {
        ArgumentNullException.ThrowIfNull(nextIndex);

        Span<char> chars = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
        {
            var index = nextIndex(Alphabet.Length);
            if (index < 0 || index >= Alphabet.Length)
                throw new ArgumentException(
                    $"nextIndex는 0 이상 {Alphabet.Length} 미만을 반환해야 하지만 {index}를 반환했습니다.",
                    nameof(nextIndex));

            chars[i] = Alphabet[index];
        }

        // 신뢰하는 경로라도 Create를 거쳐 불변식을 한 곳에서만 정의합니다.
        if (!Create(new string(chars)).TryGetValue(out var code, out var error))
            throw new InvalidOperationException(
                $"생성한 초대 코드가 형식 검증을 통과하지 못했습니다: {error.Code}");

        return code;
    }
}
