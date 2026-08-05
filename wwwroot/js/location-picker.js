// عنصر خريطة قابل لإعادة الاستخدام لاختيار موقع الحضور المتوقع (Leaflet.js)
function initAtharLocationPicker(options) {
    const {
        mapId,
        latInputId,
        lngInputId,
        nameInputId,
        coordsDisplayId,
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

    // تصحيح مقاس الخريطة إذا كانت داخل عنصر لم يكن مرئياً بالكامل عند التحميل الأول
    setTimeout(function () {
        map.invalidateSize();
    }, 200);

    return map;
}