/**
 * tcs-auth.js — 解析 erp_auth_token 的 action claim 並控制按鈕 disabled。
 *
 * Spec §6-3：
 *   新增 → 不含 `新增` 時 disable
 *   修改 → 不含 `修改` 時 disable
 *   刪除 → 不含 `刪除` 時 disable
 *   匯出 Excel → 不含 `儲存` 時 disable
 *
 * 用法：
 *   TcsAuth.applyButtonGuards({
 *       '#btn-add':    '新增',
 *       '#btn-edit':   '修改',
 *       '#btn-delete': '刪除',
 *       '#btn-export': '儲存'
 *   });
 *
 *   // 後續業務邏輯（例如選列後啟用按鈕）改用 TcsAuth.enableIfAllowed
 *   TcsAuth.enableIfAllowed('#btn-edit');
 */
(function () {
    'use strict';

    var STORAGE_KEY = 'erp_auth_token';

    function base64UrlDecode(b64) {
        b64 = b64.replace(/-/g, '+').replace(/_/g, '/');
        var pad = b64.length % 4;
        if (pad) b64 += new Array(5 - pad).join('=');
        // 處理 UTF-8（action 為中文）
        return decodeURIComponent(
            atob(b64).split('').map(function (c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join('')
        );
    }

    function getActions() {
        try {
            var token = sessionStorage.getItem(STORAGE_KEY);
            if (!token) return [];
            var parts = token.split('.');
            if (parts.length < 2) return [];
            var payload = JSON.parse(base64UrlDecode(parts[1]));
            var raw = payload && payload.action;
            if (!raw) return [];
            return String(raw).split(',').map(function (s) { return s.trim(); }).filter(Boolean);
        } catch (e) {
            return [];
        }
    }

    function hasAction(name) {
        return getActions().indexOf(name) >= 0;
    }

    /**
     * 依 action 控制按鈕 disabled；無權限者標記 data-action-denied="true"。
     * 後續業務邏輯（如選列才 enable）請改用 enableIfAllowed，避免誤啟用。
     */
    function applyButtonGuards(map) {
        Object.keys(map).forEach(function (sel) {
            var required = map[sel];
            if (!hasAction(required)) {
                $(sel)
                    .prop('disabled', true)
                    .attr('data-action-denied', 'true')
                    .attr('title', '無「' + required + '」權限');
            }
        });
    }

    /** 僅在按鈕未被 action 守門 disabled 時才 enable。 */
    function enableIfAllowed(selector) {
        var $b = $(selector);
        $b.each(function () {
            if ($(this).attr('data-action-denied') !== 'true') {
                $(this).prop('disabled', false);
            }
        });
    }

    window.TcsAuth = {
        getActions: getActions,
        hasAction: hasAction,
        applyButtonGuards: applyButtonGuards,
        enableIfAllowed: enableIfAllowed
    };
})();
