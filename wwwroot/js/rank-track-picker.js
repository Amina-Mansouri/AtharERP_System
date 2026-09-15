// عنصر مشترك: اختيار المسار يُفلتر قائمة الرتب المعروضة تلقائيًا
function initAtharRankTrackPicker(trackSelectId, rankSelectId) {
    const trackSelect = document.getElementById(trackSelectId);
    const rankSelect = document.getElementById(rankSelectId);
    if (!trackSelect || !rankSelect) return;

    function applyFilter() {
        const trackId = trackSelect.value;
        let firstVisible = null;
        let hasSelectedVisible = false;
        Array.from(rankSelect.options).forEach(function (opt) {
            if (!opt.value) return;
            const matches = !trackId || opt.getAttribute('data-track') === trackId;
            opt.hidden = !matches;
            if (matches && !firstVisible) firstVisible = opt;
            if (matches && opt.selected) hasSelectedVisible = true;
        });
        if (!hasSelectedVisible && firstVisible) {
            firstVisible.selected = true;
        }
    }

    trackSelect.addEventListener('change', applyFilter);
    applyFilter();
}