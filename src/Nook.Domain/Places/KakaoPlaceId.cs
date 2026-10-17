using Nook.Domain.Common;

namespace Nook.Domain.Places;

// 카카오 장소 ID. 형식은 카카오가 정하므로 느슨하게 검사한다(숫자 여부 미검사 — ID 체계 변경, seed의 demo-1 대비).
// 우리는 같은 값인지 비교만 하므로, 조건은 DB 제약(not null unique)과 같은 "공백 제거 후 비어 있지 않음" 하나다.
// 회원(클라이언트 JSON)이 보내는 값이라 실패는 Result로 돌려준다.
public sealed record KakaoPlaceId
{
    // 형식이 아니라 크기 상한 — 불신 입력으로부터 저장소·UNIQUE 인덱스를 보호한다. 실제 ID보다 넉넉하게 잡는다.
    public const int MaxLength = 64;

    public string Value { get; }

    private KakaoPlaceId(string value) => Value = value;

    // DB 값 복원(EF Core 값 변환기 입력). 사용자 입력에는 Create를 쓴다.
    // 규칙은 다시 검사하지 않고 값도 바꾸지 않는다(Nickname.From과 같은 기준, 저장된 값은 ck_places_kakao_place_id가 보장). 빈 값은 데이터 손상으로 보고 예외.
    public static KakaoPlaceId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new KakaoPlaceId(value);
    }

    public static Result<KakaoPlaceId> Create(string? input)
    {
        // 1) 정규화
        var normalized = input?.Trim() ?? string.Empty;

        // 2) 허용 조건
        if (normalized.Length == 0)
            return PlaceErrors.KakaoPlaceIdEmpty;
        if (normalized.EnumerateRunes().Count() > MaxLength)
            return PlaceErrors.KakaoPlaceIdTooLong;

        // 3) 보관
        return new KakaoPlaceId(normalized);
    }
}
