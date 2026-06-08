using March7thHoney.WebServer.Objects;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace March7thHoney.WebServer.Handler;

public class UsernameLoginHandler
{
    public IResult HandleShieldLogin(string account, string password, IReadOnlyCollection<string> identityKeys)
    {
        LoginSessionFactory.LoginFailure failure;
        if (!LoginSessionFactory.TryCreateSession(account, password, identityKeys, out var session, out failure))
            return Results.Json(LoginResJson.Error(failure.Retcode, failure.Message));

        return Results.Json(LoginResJson.Success(
            session.Uid,
            session.Username,
            session.Email,
            session.IsEmailVerified,
            session.DispatchToken));
    }

    public IResult HandlePassportLogin(string account, string password, IReadOnlyCollection<string> identityKeys)
    {
        LoginSessionFactory.LoginFailure failure;
        if (!LoginSessionFactory.TryCreateSession(account, password, identityKeys, out var session, out failure))
            return Results.Json(new { retcode = failure.Retcode, message = failure.Message, data = (object?)null });

        return Results.Json(BuildPassportLoginBody(session));
    }

    public IResult HandleLegacyPassportLogin(string account, string password, IReadOnlyCollection<string> identityKeys)
    {
        LoginSessionFactory.LoginFailure failure;
        if (!LoginSessionFactory.TryCreateSession(account, password, identityKeys, out var session, out failure))
            return Results.Text(
                JsonConvert.SerializeObject(PassportLoginResJson.Error(failure.Retcode, failure.Message)),
                "application/json");

        return Results.Text(JsonConvert.SerializeObject(BuildPassportLoginBody(session)), "application/json");
    }

    private static object BuildPassportLoginBody(LoginSessionFactory.LoginSession session)
    {
        return new
        {
            retcode = 0,
            message = "OK",
            data = new
            {
                bind_email_action_ticket = "",
                reactivate_action_token = "",
                user_info = new
                {
                    aid = session.Uid,
                    mid = session.Uid,
                    account_name = session.Username,
                    email = session.Email,
                    is_email_verify = session.IsEmailVerified ? 1 : 0,
                    area_code = "**",
                    mobile = "",
                    safe_area_code = "",
                    safe_mobile = "",
                    realname = "",
                    identity_code = "",
                    rebind_area_code = "",
                    rebind_mobile = "",
                    rebind_mobile_time = "1",
                    links = Array.Empty<object>(),
                    country = "CN",
                    password_time = "1",
                    is_adult = 0,
                    unmasked_email = session.Email,
                    unmasked_email_type = string.IsNullOrWhiteSpace(session.Email) ? 0 : 1
                },
                token = new { token_type = 1, token = session.DispatchToken },
                ext_user_info = new { guardian_email = "", birth = "0" }
            }
        };
    }
}
