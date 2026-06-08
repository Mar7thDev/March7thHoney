namespace March7thHoney.WebServer;

// Named replacements for the former anonymous response payloads, so STJ can
// serialize them via source generation (AOT/trim safe). Property names are the
// exact JSON keys; the camelCase policy leaves these lower-first names unchanged.

// Generic envelope used by endpoints that only ever return null data.
public sealed record StatusResult(int retcode, string message, string? data = null);

// /console/* launcher endpoints
public sealed record ConsoleResult(int retcode, string message, string output);

// /handbook/languages
public sealed record HandbookLanguages(List<string> languages);

// ma-passport getConfig
public sealed record PassportConfigData(bool enable_email_captcha, bool disable_mmt, bool enable_ps_bind_account);
public sealed record PassportConfigResult(int retcode, string message, PassportConfigData data);

// shield loadConfig
public sealed record ThirdpartyLoginConfig(string token_type, int game_token_expires_in);

public sealed record ThirdpartyLoginConfigs(
    ThirdpartyLoginConfig tw, ThirdpartyLoginConfig ap, ThirdpartyLoginConfig fb, ThirdpartyLoginConfig gl);

public sealed record LoadConfigData(
    int id, string game_key, string client, string identity, bool guest, string ignore_versions,
    string scene, string name, bool disable_regist, bool enable_email_captcha, string[] thirdparty,
    bool disable_mmt, bool server_guest, Dictionary<string, string> thirdparty_ignore, bool enable_ps_bind_account,
    ThirdpartyLoginConfigs thirdparty_login_configs, bool initialize_firebase, bool bbs_auth_login,
    string[] bbs_auth_login_ignore, bool fetch_instance_id, bool enable_flash_login);

public sealed record LoadConfigResult(int retcode, string message, LoadConfigData data);

// passport appLoginByPassword / legacy login body
public sealed record PassportUserInfo(
    string aid, string mid, string account_name, string email, int is_email_verify, string area_code,
    string mobile, string safe_area_code, string safe_mobile, string realname, string identity_code,
    string rebind_area_code, string rebind_mobile, string rebind_mobile_time, string[] links, string country,
    string password_time, int is_adult, string unmasked_email, int unmasked_email_type);

public sealed record PassportToken(int token_type, string token);

public sealed record PassportExtUserInfo(string guardian_email, string birth);

public sealed record PassportLoginBodyData(
    string bind_email_action_ticket, string reactivate_action_token,
    PassportUserInfo user_info, PassportToken token, PassportExtUserInfo ext_user_info);

public sealed record PassportLoginBody(int retcode, string message, PassportLoginBodyData data);

// register
public sealed record RegAccount(string uid, string name, string email, string token, string is_email_verify);

public sealed record RegUserInfo(string aid, string mid, string account_name, string email, string is_email_verify);

public sealed record RegToken(int token_type, string token);

public sealed record RegData(RegAccount account, RegUserInfo user_info, RegToken token, bool email_sent);

public sealed record RegVerifyData(
    RegAccount account, RegUserInfo user_info, RegToken token, bool email_sent, bool email_verification_required);

public sealed record RegResult(int retcode, string message, RegData data);

public sealed record RegVerifyResult(int retcode, string message, RegVerifyData data);
