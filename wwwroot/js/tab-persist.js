(function () {
    function getParam(name) {
        return new URLSearchParams(window.location.search).get(name);
    }

    document.addEventListener('DOMContentLoaded', function () {
        var tabsRoot = document.querySelector('.tabs[role="tablist"]');
        if (tabsRoot) {
            var wanted = getParam('tab');
            if (wanted) {
                var trigger = tabsRoot.querySelector('[href="#' + wanted + '"]');
                if (trigger) {
                    new bootstrap.Tab(trigger).show();
                }
            }

            tabsRoot.addEventListener('shown.bs.tab', function (e) {
                var paneId = e.target.getAttribute('href').replace('#', '');
                var url = new URL(window.location.href);
                url.searchParams.set('tab', paneId);
                window.history.replaceState(null, '', url.toString());
            });
        }

        document.addEventListener('submit', function (e) {
            var form = e.target;
            if (!form || form.tagName !== 'FORM' || !form.action) return;
            var currentTab = getParam('tab');
            var currentPersonal = getParam('personal');
            if (!currentTab && !currentPersonal) return;
            try {
                var url = new URL(form.action, window.location.origin);
                if (currentTab) url.searchParams.set('tab', currentTab);
                if (currentPersonal) url.searchParams.set('personal', currentPersonal);
                form.action = url.pathname + url.search;
            } catch (err) { /* تجاهل أي رابط غير صالح */ }
        }, true);
    });
})();