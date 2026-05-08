/**
 * toast.js — Bootstrap Toast 包裝（spec §7-1 / TODO P3-2）
 *
 *   Toast.success(msg)  // 綠底，3 秒自動消失
 *   Toast.error(msg)    // 紅底，需手動關閉（不自動消失）
 *   Toast.info(msg)     // 中性訊息，3 秒自動消失
 *
 * 容器 #toast-container 由 _Layout.cshtml 提供（fixed bottom-end）。
 */
(function () {
    'use strict';

    function ensureContainer() {
        var $c = $('#toast-container');
        if ($c.length) return $c;
        $c = $('<div id="toast-container" class="toast-container position-fixed bottom-0 end-0 p-3" style="z-index:1080;"></div>');
        $('body').append($c);
        return $c;
    }

    function show(msg, opts) {
        opts = opts || {};
        var bg = opts.bg || 'bg-secondary';
        var autoHide = opts.autoHide !== false;
        var delay = opts.delay || 3000;

        var $toast = $('<div class="toast align-items-center text-white border-0" role="alert" aria-live="assertive" aria-atomic="true"></div>')
            .addClass(bg);
        var $inner = $('<div class="d-flex"></div>');
        $('<div class="toast-body"></div>').text(msg || '').appendTo($inner);
        $('<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="關閉"></button>').appendTo($inner);
        $toast.append($inner);

        ensureContainer().append($toast);

        var instance = new bootstrap.Toast($toast[0], { autohide: autoHide, delay: delay });
        $toast.on('hidden.bs.toast', function () { $toast.remove(); });
        instance.show();
    }

    window.Toast = {
        success: function (msg) { show(msg, { bg: 'bg-success', autoHide: true, delay: 3000 }); },
        error:   function (msg) { show(msg, { bg: 'bg-danger',  autoHide: false }); },
        info:    function (msg) { show(msg, { bg: 'bg-secondary', autoHide: true, delay: 3000 }); }
    };
})();
