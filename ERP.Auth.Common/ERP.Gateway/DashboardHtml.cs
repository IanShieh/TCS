using System.Net;
using System.Text;

// ─────────────────────────────────────────────────────────────────────────────
// ERP Dashboard HTML 樣板（inline CSS，無外部相依，可離線使用）
// ─────────────────────────────────────────────────────────────────────────────
record ServiceStatus(string Name, string Address, bool Healthy, string Detail);

static class DashboardHtml
{
    public static string LoginPage(string? returnUrl = null, bool error = false)
    {
        var encodedReturn = WebUtility.HtmlEncode(returnUrl ?? "/dashboard");
        var errorBanner   = error
            ? """<div class="error">帳號或密碼錯誤，請重試</div>"""
            : "";

        return $$"""
            <!DOCTYPE html>
            <html lang="zh-TW">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>ERP 系統 – 登入</title>
              <style>
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body { font-family: system-ui, -apple-system, sans-serif; background: #f0f2f5;
                       display: flex; align-items: center; justify-content: center; min-height: 100vh; }
                .card { background: white; border-radius: 12px; padding: 40px; width: 360px;
                        box-shadow: 0 4px 20px rgba(0,0,0,.12); }
                h1 { font-size: 22px; color: #1e3a5f; margin-bottom: 6px; }
                .sub { color: #6b7280; font-size: 14px; margin-bottom: 28px; }
                label { display: block; font-size: 13px; color: #374151; margin-bottom: 6px; font-weight: 500; }
                input[type=text], input[type=password] {
                  width: 100%; padding: 10px 14px; border: 1px solid #d1d5db;
                  border-radius: 6px; font-size: 14px; margin-bottom: 16px;
                  transition: border-color .15s; }
                input:focus { outline: none; border-color: #1e3a5f; }
                button { width: 100%; background: #1e3a5f; color: white; border: none;
                         padding: 11px; border-radius: 6px; font-size: 15px; cursor: pointer;
                         font-weight: 500; transition: background .15s; }
                button:hover { background: #254e85; }
                .error { background: #fee2e2; color: #991b1b; border-radius: 6px;
                         padding: 10px 14px; font-size: 13px; margin-bottom: 16px; }
              </style>
            </head>
            <body>
              <div class="card">
                <h1>🏢 ERP 系統監控</h1>
                <p class="sub">請輸入管理員帳號密碼</p>
                {{errorBanner}}
                <form method="post" action="/dashboard/login">
                  <input type="hidden" name="returnUrl" value="{{encodedReturn}}">
                  <label for="u">帳號</label>
                  <input type="text" id="u" name="username" autocomplete="username" required autofocus>
                  <label for="p">密碼</label>
                  <input type="password" id="p" name="password" autocomplete="current-password" required>
                  <button type="submit">登入</button>
                </form>
              </div>
            </body>
            </html>
            """;
    }

    public static string StatusPage(IEnumerable<ServiceStatus> statuses, string username)
    {
        var list         = statuses.ToList();
        var healthyCount = list.Count(s => s.Healthy);
        var downCount    = list.Count(s => !s.Healthy);
        var now          = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var summaryColor = downCount == 0 ? "#22c55e" : "#ef4444";
        var summaryText  = downCount == 0 ? "全部正常" : $"{downCount} 個服務異常";

        var rows = new StringBuilder();
        foreach (var s in list)
        {
            var dot        = s.Healthy ? "green" : "red";
            var statusText = s.Healthy ? "正常運行" : "無法連線";
            var statusCls  = s.Healthy ? "healthy"  : "down";
            rows.AppendLine($$"""
                        <tr>
                          <td><span class="dot {{dot}}"></span>{{WebUtility.HtmlEncode(s.Name)}}</td>
                          <td><a href="{{WebUtility.HtmlEncode(s.Address)}}" target="_blank" class="addr-link"><code>{{WebUtility.HtmlEncode(s.Address)}}</code></a></td>
                          <td class="status-text {{statusCls}}">{{statusText}}</td>
                          <td class="detail">{{WebUtility.HtmlEncode(s.Detail)}}</td>
                        </tr>
                """);
        }

        return $$"""
            <!DOCTYPE html>
            <html lang="zh-TW">
            <head>
              <meta charset="UTF-8">
              <meta http-equiv="refresh" content="30">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>ERP 系統狀態</title>
              <style>
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body { font-family: system-ui, -apple-system, sans-serif; background: #f0f2f5; }
                .header { background: #1e3a5f; color: white; padding: 14px 24px;
                          display: flex; justify-content: space-between; align-items: center; }
                .header h1 { font-size: 18px; font-weight: 600; }
                .meta { font-size: 13px; opacity: .8; display: flex; align-items: center; gap: 16px; }
                .logout-btn { background: transparent; border: 1px solid rgba(255,255,255,.5); color: white;
                              padding: 5px 12px; border-radius: 4px; cursor: pointer; font-size: 13px; }
                .logout-btn:hover { background: rgba(255,255,255,.12); }
                .container { max-width: 960px; margin: 28px auto; padding: 0 24px; }
                .summary { display: flex; gap: 12px; margin-bottom: 20px; }
                .s-card { background: white; border-radius: 8px; padding: 18px 20px; flex: 1;
                          box-shadow: 0 1px 4px rgba(0,0,0,.08); }
                .s-card h3 { font-size: 11px; color: #6b7280; text-transform: uppercase;
                             letter-spacing: .6px; margin-bottom: 8px; }
                .s-card .val { font-size: 28px; font-weight: 700; }
                table { width: 100%; border-collapse: collapse; background: white; border-radius: 8px;
                        overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,.08); }
                th { background: #f8fafc; padding: 11px 16px; text-align: left; font-size: 11px;
                     text-transform: uppercase; color: #6b7280; letter-spacing: .6px;
                     border-bottom: 1px solid #e5e7eb; }
                td { padding: 13px 16px; border-bottom: 1px solid #f3f4f6; font-size: 14px; }
                tr:last-child td { border-bottom: none; }
                .dot { display: inline-block; width: 9px; height: 9px; border-radius: 50%;
                       margin-right: 8px; vertical-align: middle; }
                .dot.green { background: #22c55e; box-shadow: 0 0 6px #22c55e88; }
                .dot.red   { background: #ef4444; box-shadow: 0 0 6px #ef444488; }
                .status-text.healthy { color: #166534; font-weight: 500; }
                .status-text.down    { color: #991b1b; font-weight: 500; }
                .detail { color: #9ca3af; font-size: 12px; font-family: monospace; }
                code { font-size: 13px; color: #4b5563; }
                .addr-link { color: inherit; text-decoration: none; }
                .addr-link:hover code { text-decoration: underline; color: #1e3a5f; }
                .footer { text-align: center; color: #9ca3af; font-size: 12px; margin-top: 12px; }
              </style>
            </head>
            <body>
              <div class="header">
                <h1>🏢 ERP 系統監控</h1>
                <div class="meta">
                  <span>👤 {{WebUtility.HtmlEncode(username)}} &nbsp;·&nbsp; {{now}}</span>
                  <form method="post" action="/dashboard/logout" style="margin:0">
                    <button type="submit" class="logout-btn">登出</button>
                  </form>
                </div>
              </div>
              <div class="container">
                <div class="summary">
                  <div class="s-card">
                    <h3>總服務數</h3>
                    <div class="val">{{list.Count}}</div>
                  </div>
                  <div class="s-card">
                    <h3>正常</h3>
                    <div class="val" style="color:#22c55e">{{healthyCount}}</div>
                  </div>
                  <div class="s-card">
                    <h3>異常</h3>
                    <div class="val" style="color:{{(downCount > 0 ? "#ef4444" : "#9ca3af")}}">{{downCount}}</div>
                  </div>
                  <div class="s-card">
                    <h3>整體狀態</h3>
                    <div class="val" style="font-size:16px;margin-top:6px;color:{{summaryColor}}">{{summaryText}}</div>
                  </div>
                </div>
                <table>
                  <thead>
                    <tr>
                      <th>服務名稱</th>
                      <th>連線位址</th>
                      <th>狀態</th>
                      <th>回應詳情</th>
                    </tr>
                  </thead>
                  <tbody>
                    {{rows}}
                  </tbody>
                </table>
                <p class="footer">每 30 秒自動重新整理</p>
              </div>
            </body>
            </html>
            """;
    }
}
