/**
 * erp-auth.js — ERP 共用前端認證腳本
 *
 * 功能：
 * 1. sessionStorage 中讀取 JWT Token（由中介頁 /api/auth/token-login 直接寫入）
 * 2. monkey-patch window.fetch（加 Authorization header，處理 401）
 * 3. monkey-patch jQuery.ajax（DbNetSuiteCore 支援）
 * 4. 401 時清 sessionStorage 並跳轉回首頁
 *
 * 使用方式：在 _Layout.cshtml </body> 前加入：
 *   <script src="~/_content/ERP.Auth.Common/js/erp-auth.js"></script>
 */
(function () {
    'use strict';

    var STORAGE_KEY = 'erp_auth_token';

    // ── Token 管理 ─────────────────────────────────────────────────────────

    function getToken() {
        return sessionStorage.getItem(STORAGE_KEY);
    }

    function setToken(token) {
        sessionStorage.setItem(STORAGE_KEY, token);
    }

    function clearToken() {
        sessionStorage.removeItem(STORAGE_KEY);
    }

    function handleUnauthorized() {
        clearToken();
        // 取得 PathBase（第一個路徑 segment，例如 /account-code）
        // window.location.pathname 在瀏覽器端永遠是完整路徑，不受 UsePathBase 影響
        var pathBase = window.location.pathname.replace(/^(\/[^/]*).*$/, '$1');
        window.location.href = pathBase + '/session-expired';
    }

    // ── 初始化 ─────────────────────────────────────────────────────────────

    function init() {
        // R4-B：token 由 /api/auth/token-login 中介頁直接寫入 sessionStorage
        // 不再使用 window.__erp_token（meta-refresh 跨 window 邊界，原方案的 token 永遠遺失）
        // erp-auth.js 只負責讀取 sessionStorage 並 patch fetch/jQuery

        // patch fetch（現代瀏覽器 AJAX）
        patchFetch();

        // patch jQuery.ajax（DbNetSuiteCore 使用 jQuery.ajax，非 fetch）
        patchJqueryAjax();
    }

    // ── fetch monkey-patch ─────────────────────────────────────────────────

    function patchFetch() {
        if (typeof window.fetch !== 'function') return;

        var originalFetch = window.fetch.bind(window);

        window.fetch = function (input, init) {
            init = init || {};
            var token = getToken();
            if (token) {
                var existingHeaders = init.headers || {};
                // 支援 Headers 物件、普通物件、陣列格式
                if (existingHeaders instanceof Headers) {
                    if (!existingHeaders.has('Authorization')) {
                        existingHeaders.set('Authorization', 'Bearer ' + token);
                    }
                    init.headers = existingHeaders;
                } else {
                    init.headers = Object.assign(
                        { 'Authorization': 'Bearer ' + token },
                        existingHeaders
                    );
                }
            }

            return originalFetch(input, init).then(function (response) {
                if (response.status === 401) {
                    handleUnauthorized();
                }
                return response;
            });
        };
    }

    // ── jQuery.ajax patch（DbNetSuiteCore 使用）─────────────────────────────

    function patchJqueryAjax() {
        // 在 jQuery 載入後才能 patch；使用 polling 等待（最多 5 秒）
        var attempts = 0;
        var maxAttempts = 50;

        function tryPatch() {
            if (typeof $ !== 'undefined' && typeof $.ajaxSetup === 'function') {
                applyJqueryPatch();
            } else if (attempts < maxAttempts) {
                attempts++;
                setTimeout(tryPatch, 100);
            }
        }

        tryPatch();
    }

    function applyJqueryPatch() {
        $.ajaxSetup({
            beforeSend: function (xhr, settings) {
                // 只對同域請求加 header（避免 CORS 預檢問題）
                // 問題A：排除 protocol-relative URL（//evil.com 開頭的 / 會誤判為同域）
                // 問題F：使用 origin+'/' 避免前綴偽造（erp.co.evil.com 開頭也符合 erp.co）
                var isSameOrigin = settings.url &&
                    (/^\/(?!\/)/.test(settings.url) ||
                     settings.url.indexOf(window.location.origin + '/') === 0 ||
                     settings.url === window.location.origin);

                if (isSameOrigin !== false) {
                    var token = getToken();
                    if (token) {
                        xhr.setRequestHeader('Authorization', 'Bearer ' + token);
                    }
                }
            }
        });

        $(document).ajaxError(function (event, xhr) {
            if (xhr.status === 401) {
                handleUnauthorized();
            }
        });
    }

    // ── 自動初始化 ─────────────────────────────────────────────────────────

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        // DOM 已就緒（script 在底部載入的情況）
        init();
    }

})();
