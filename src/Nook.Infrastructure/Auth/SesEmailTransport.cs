using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

namespace Nook.Infrastructure.Auth;

// 운영 발송기(dep-plan 7단계, Email:Provider=Ses). 실제 발송은 도메인 인증(SES DKIM)·운영 승인 뒤에만 된다 —
// 그 전(샌드박스)에는 인증한 주소로만 보낼 수 있다.
// 자격 증명은 넘기지 않는다 — SDK 기본 체인이 EC2 인스턴스 역할에서 받는다(S3 와 같음).
// 실패는 던진다 → MagicLinkMailWorker 가 잡아 로그만 남긴다(재시도 없음: 15분짜리 링크, 회원이 다시 요청).
internal sealed class SesEmailTransport(IAmazonSimpleEmailServiceV2 ses, string from) : IEmailTransport
{
    // SES 는 문자 집합을 지정하지 않으면 7비트 ASCII 로 본다 → 한글 제목·본문은 UTF-8 을 반드시 지정한다
    internal const string Charset = "UTF-8";

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        await ses.SendEmailAsync(new SendEmailRequest
        {
            FromEmailAddress = from,
            Destination = new Destination { ToAddresses = [to] },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject, Charset = Charset },
                    // 텍스트만 — 링크 하나라 HTML 이 필요 없고, HTML 은 스팸 필터·피싱 판정에서 따질 것이 늘어난다
                    Body = new Body { Text = new Content { Data = body, Charset = Charset } },
                },
            },
        }, ct);
    }
}
