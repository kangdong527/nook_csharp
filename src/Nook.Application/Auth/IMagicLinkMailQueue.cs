using Nook.Domain.Users;

namespace Nook.Application.Auth;

// 매직 링크 메일 발송 요청(D5-2 결정 B안: 같은 프로세스 큐 + 백그라운드 발송).
// 기다리지 않는다 — 발송 시간·실패가 응답에 드러나면 가입 여부·메일 서버 상태가 새어 나간다(erd.md 응답 시간 균일화).
// 큐가 가득 차거나 재시작으로 유실돼도 요청 결과는 바뀌지 않는다. 회원이 다시 요청한다.
public interface IMagicLinkMailQueue
{
    void Enqueue(Email email, string rawToken);
}
