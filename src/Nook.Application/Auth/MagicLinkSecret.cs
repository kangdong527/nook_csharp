using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Nook.Application.Auth;

// 매직 링크 토큰 원문·해시(erd.md magic_link_tokens token_hash).
// 원문 = CSPRNG 32바이트를 Base64url(43자)로. 메일에만 나간다. DB에는 SHA-256만 저장한다.
// salt·느린 해시가 없는 이유: 256비트 난수라 무차별 대입이 불가능하다(erd.md 메모 — 판단 기준은 입력의 엔트로피).
public static class MagicLinkSecret
{
    private const int ByteLength = 32;

    public static (string RawToken, byte[] Hash) Create()
    {
        Span<byte> bytes = stackalloc byte[ByteLength];
        RandomNumberGenerator.Fill(bytes);

        var raw = Base64Url.EncodeToString(bytes);
        return (raw, Hash(raw));
    }

    // 소모(AU-02)에서도 받은 원문을 이 함수로 해시해 PK와 비교한다. 발급·소모가 같은 함수를 써야 한다.
    public static byte[] Hash(string rawToken) => SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
}
