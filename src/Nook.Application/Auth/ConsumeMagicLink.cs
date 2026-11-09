using Nook.Application.Common;
using Nook.Application.Users;
using Nook.Domain.Users;
using Nook.SharedKernel;

namespace Nook.Application.Auth;

// AU-02 흐름 3: 매직 링크 소모(erd.md magic_link_tokens "흐름" 3). 가입·로그인 공통.
// 한 트랜잭션: Q1 소모 → 회원 찾기(users Q4) → 없으면 가입 → Q3 남은 링크 무효화 → 커밋.
// 결과: 로그인(UserId) · 닉네임 필요 · 실패(InvalidLink 400 · 닉네임 형식 400 · NicknameTaken 409).
// Gender·Birthdate는 가입(회원 없음)일 때만 본다 — 닉네임과 같은 규칙(D5-3). 둘 다 선택, Birthdate는 YYYY-MM-DD.
// 동의(R-US-06)도 가입일 때만: AgreePrivacy·ConfirmAge14 필수, AgreeOptional 은 성별·생년월일을 보낼 때 필수. 기존 회원 로그인은 보지 않는다.
public sealed record ConsumeMagicLinkCommand(
    string? Token, string? Nickname, string? Gender = null, string? Birthdate = null,
    bool AgreePrivacy = false, bool ConfirmAge14 = false, bool AgreeOptional = false);

// Application 공개 결과는 Domain 타입을 노출하지 않는다(D5 결정) — UserId 대신 Guid.
// SignupEmail: 닉네임 필요일 때 이 링크의 이메일 — 가입 폼이 보여 준다. 토큰을 가진 사람만 받으므로 가입 여부 노출이 아니다.
public sealed record ConsumeMagicLinkOutcome(Guid? SignedInUserId, string? SignupEmail = null)
{
    public bool NeedsNickname => SignedInUserId is null;

    // internal: Api가 Domain 타입(UserId)을 보지 않게 한다. 만드는 쪽은 핸들러뿐이다.
    internal static ConsumeMagicLinkOutcome SignedIn(UserId userId) => new(userId.Value);

    // 회원이 없고 닉네임도 없다. 롤백했으므로 토큰은 살아 있다 — 같은 토큰 + 닉네임으로 다시 보낸다(D5-3: 200 + needsNickname).
    internal static ConsumeMagicLinkOutcome NicknameRequired(Email email) => new(null, email.Value);
}

public sealed class ConsumeMagicLinkHandler(
    IUnitOfWork uow,
    IMagicLinkTokenStore tokens,
    IUserRepository users,
    TimeProvider time)
{
    // 실패·닉네임 필요는 커밋 없이 return한다 → tx Dispose = 롤백 → Q1 소모가 되돌아가 토큰이 살아 있다.
    public async Task<Result<ConsumeMagicLinkOutcome>> HandleAsync(ConsumeMagicLinkCommand command, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(command.Token))
            return AuthErrors.InvalidLink;

        var now = time.GetUtcNow();

        await using var tx = await uow.BeginAsync(ct);

        var email = await tokens.ConsumeAsync(MagicLinkSecret.Hash(command.Token), now, ct);
        if (email is null)
            return AuthErrors.InvalidLink;

        var user = await users.FindByEmailAsync(email, ct);
        if (user is null)
        {
            // 가입. 닉네임은 회원이 없을 때만 본다(D5-3 — 기존 회원의 nickname은 쓰지 않으므로 검사하지 않는다)
            if (string.IsNullOrEmpty(command.Nickname))
                return ConsumeMagicLinkOutcome.NicknameRequired(email);   // 성공 결과지만 커밋하지 않는다

            // 동의 먼저 — 값 검사보다 앞이라, 동의 없이 보낸 성별·생년월일은 형식을 보기도 전에 거절된다
            if (!command.AgreePrivacy || !command.ConfirmAge14)
                return UserErrors.ConsentRequired;
            if (!command.AgreeOptional && (!string.IsNullOrEmpty(command.Gender) || !string.IsNullOrEmpty(command.Birthdate)))
                return UserErrors.OptionalConsentRequired;

            if (!Nickname.Create(command.Nickname).TryGetValue(out var nickname, out var nicknameError))
                return nicknameError;

            Gender? gender = null;
            if (!string.IsNullOrEmpty(command.Gender))
            {
                if (!GenderCode.Parse(command.Gender).TryGetValue(out var parsedGender, out var genderError))
                    return genderError;
                gender = parsedGender;
            }

            var today = DateOnly.FromDateTime(now.UtcDateTime);
            Birthdate? birthdate = null;
            if (!string.IsNullOrEmpty(command.Birthdate))
            {
                if (!Birthdate.Create(command.Birthdate, today).TryGetValue(out var parsed, out var birthError))
                    return birthError;
                // 자기 신고(ConfirmAge14)와 모순되는 생년월일 — 받지 않는다(R-US-06)
                if (parsed.AgeOn(today) < User.MinimumAge)
                    return UserErrors.UnderMinimumAge;
                birthdate = parsed;
            }

            var consent = PrivacyConsent.Agree(now, PrivacyPolicy.CurrentVersion, command.AgreeOptional);
            user = User.Create(nickname, email, consent, gender, birthdate);
            if ((await users.AddAsync(user, ct)).IsFailure(out var addError))
                return addError;
        }

        // 로그인·가입 공통 꼬리
        await tokens.RevokeRemainingAsync(email, now, ct);
        await tx.CommitAsync(ct);

        return ConsumeMagicLinkOutcome.SignedIn(user.Id);
    }
}
