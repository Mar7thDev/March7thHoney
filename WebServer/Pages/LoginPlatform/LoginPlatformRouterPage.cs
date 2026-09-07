namespace March7thHoney.WebServer.Pages;

/// <summary>
///     /login-platform/index.html: maps the hash route the game client opens (for example #/register) onto the
///     real page and redirects. Kept dependency-free so it works in the client's embedded browser.
/// </summary>
public static class LoginPlatformRouterPage
{
    public static string Render()
    {
        return """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="robots" content="noindex">
  <meta name="color-scheme" content="light">
  <title>Login Platform</title>
  <style>
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #f4f7fb; color: #5f6f82; font-family: "Inter", "Segoe UI", system-ui, sans-serif; }
    a { color: #2f68c5; }
  </style>
</head>
<body>
  <noscript>
    <p>JavaScript is disabled. <a href="/login-platform/register.html">Create account</a> · <a href="/login-platform/forgot-password.html">Forgot password</a></p>
  </noscript>
  <script>
    function normalizeRoute(hash) {
      return (hash || "")
        .split("?")[0]
        .replace(/^#\/?/, "")
        .replace(/\/+$/, "")
        .toLowerCase();
    }

    function routeToPath(route) {
      if (!route) {
        return "/login-platform/register.html";
      }

      if (!/^[a-z0-9][a-z0-9/_-]*$/.test(route)) {
        return "/login-platform/404.html";
      }

      return `/login-platform/${route}.html`;
    }

    const target = routeToPath(normalizeRoute(window.location.hash));
    window.location.replace(target + window.location.search + window.location.hash);
  </script>
</body>
</html>
""";
    }
}
