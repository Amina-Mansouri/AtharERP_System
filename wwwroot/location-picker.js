// عنصر خريطة قابل لإعادة الاستخدام لاختيار موقع الحضور المتوقع (Leaflet.js)
function initAtharLocationPicker(options) {
    const {
        mapId,
        latInputId,
        lngInputId,
        nameInputId,
        coordsDisplayId,
        searchInputId,
        initialLat,
        initialLng,
        defaultLat = 32.8872,   // طرابلس، ليبيا كموقع افتراضي
        defaultLng = 13.1913,
        defaultZoom = 12
    } = options;

    const hasInitial = initialLat !== null && initialLat !== undefined
        && initialLng !== null && initialLng !== undefined;

    const startLat = hasInitial ? initialLat : defaultLat;
    const startLng = hasInitial ? initialLng : defaultLng;

    const map = L.map(mapId).setView([startLat, startLng], hasInitial ? 15 : defaultZoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank">OpenStreetMap</a>',
        maxZoom: 19
    }).addTo(map);

    let marker = null;

    function updateCoordsDisplay(lat, lng) {
        if (!coordsDisplayId) return;
        const el = document.getElementById(coordsDisplayId);
        if (el) el.textContent = `الإحداثيات المختارة: ${lat.toFixed(6)}, ${lng.toFixed(6)}`;
    }

    function updateFields(lat, lng) {
        document.getElementById(latInputId).value = lat.toFixed(6);
        document.getElementById(lngInputId).value = lng.toFixed(6);
        updateCoordsDisplay(lat, lng);
        reverseGeocode(lat, lng);
    }

    function placeMarker(lat, lng) {
        if (marker) {
            marker.setLatLng([lat, lng]);
        } else {
            marker = L.marker([lat, lng], { draggable: true }).addTo(map);
            marker.on('dragend', function () {
                const pos = marker.getLatLng();
                updateFields(pos.lat, pos.lng);
            });
        }
    }

    async function reverseGeocode(lat, lng) {
        const nameInput = nameInputId ? document.getElementById(nameInputId) : null;
        if (!nameInput) return;

        try {
            const res = await fetch(
                `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&accept-language=ar`
            );
            if (res.ok) {
                const data = await res.json();
                if (data && data.display_name) {
                    nameInput.value = data.display_name;
                }
            }
        } catch (err) {
            // فشل تحديد اسم الموقع تلقائياً لا يمنع المتابعة، يبقى الحقل قابلاً للتعديل يدوياً
        }
    }

    map.on('click', function (e) {
        placeMarker(e.latlng.lat, e.latlng.lng);
        updateFields(e.latlng.lat, e.latlng.lng);
    });

    if (hasInitial) {
        placeMarker(startLat, startLng);
        updateCoordsDisplay(startLat, startLng);
    }

    setTimeout(function () {
        map.invalidateSize();
    }, 200);

    // ========== البحث عن عنوان (Forward Geocoding) وتقريب الخريطة تلقائياً ==========
    if (searchInputId) {
        const searchInput = document.getElementById(searchInputId);
        if (searchInput) {
            let debounceTimer = null;
            let suggestionsBox = document.createElement('div');
            suggestionsBox.className = 'athar-address-suggestions';
            suggestionsBox.style.cssText = 'position:relative; z-index:1000;';
            searchInput.insertAdjacentElement('afterend', suggestionsBox);

            function clearSuggestions() {
                suggestionsBox.innerHTML = '';
            }

            function selectSuggestion(item) {
                const lat = parseFloat(item.lat);
                const lon = parseFloat(item.lon);
                map.setView([lat, lon], 16);
                placeMarker(lat, lon);
                document.getElementById(latInputId).value = lat.toFixed(6);
                document.getElementById(lngInputId).value = lon.toFixed(6);
                updateCoordsDisplay(lat, lon);
                if (nameInputId) {
                    const nameInput = document.getElementById(nameInputId);
                    if (nameInput) nameInput.value = item.display_name;
                }
                searchInput.value = item.display_name;
                clearSuggestions();
            }

            async function searchAddress(query) {
                if (!query || query.trim().length < 3) {
                    clearSuggestions();
                    return;
                }
                try {
                    const res = await fetch(
                        `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=5&accept-language=ar`
                    );
                    if (!res.ok) return;
                    const results = await res.json();
                    clearSuggestions();

                    if (!results || results.length === 0) {
                        const empty = document.createElement('div');
                        empty.className = 'athar-address-suggestion athar-address-suggestion-empty';
                        empty.textContent = 'لا توجد نتائج مطابقة';
                        suggestionsBox.appendChild(empty);
                        return;
                    }

                    results.forEach(item => {
                        const el = document.createElement('div');
                        el.className = 'athar-address-suggestion';
                        el.textContent = item.display_name;
                        el.addEventListener('click', () => selectSuggestion(item));
                        suggestionsBox.appendChild(el);
                    });
                } catch (err) {
                    // فشل البحث لا يمنع التحديد اليدوي عبر النقر على الخريطة
                }
            }

            searchInput.addEventListener('input', function () {
                clearTimeout(debounceTimer);
                const query = this.value;
                debounceTimer = setTimeout(() => searchAddress(query), 500);
            });

            document.addEventListener('click', function (e) {
                if (e.target !== searchInput && !suggestionsBox.contains(e.target)) {
                    clearSuggestions();
                }
            });
        }
    }

    return map;
}