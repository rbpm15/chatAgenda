// Calendar app state
let currentDate = new Date();
let eventsList = [];
let googleSyncState = { isEnabled: false, calendarId: '' };
let selectedEvent = null;

// Notification tracking: store IDs of already-notified events
let notifiedEvents = JSON.parse(localStorage.getItem('ca_notified_events') || '{}');
let calendarNotificationInterval = null;

window.calendarApp = {
    initCalendar,
    loadEvents,
    startEventReminders,
    stopEventReminders
};

// Initialize listeners on load
document.addEventListener('DOMContentLoaded', () => {
    setupCalendarListeners();
});

function setupCalendarListeners() {
    const btnPrev = document.getElementById('btnPrevMonth');
    const btnNext = document.getElementById('btnNextMonth');
    const btnAdd = document.getElementById('btnAddEvent');
    const modalClose = document.getElementById('modalClose');
    const btnCancel = document.getElementById('btnCancelEvent');
    const form = document.getElementById('eventForm');
    const btnDelete = document.getElementById('btnDeleteEvent');

    if (btnPrev) btnPrev.addEventListener('click', () => changeMonth(-1));
    if (btnNext) btnNext.addEventListener('click', () => changeMonth(1));
    if (btnAdd) btnAdd.addEventListener('click', () => openEventModal(null, new Date()));
    if (modalClose) modalClose.addEventListener('click', closeEventModal);
    if (btnCancel) btnCancel.addEventListener('click', closeEventModal);
    if (btnDelete) btnDelete.addEventListener('click', deleteEvent);
    if (form) form.addEventListener('submit', saveEvent);
}

async function initCalendar() {
    // 1. Load Google Integration State
    await loadSyncState();
    
    // 2. Load and render events
    await loadEvents();

    // 3. Start event reminders (if not already running)
    startEventReminders();
}

// ─── CALENDAR EVENT NOTIFICATION SYSTEM ─────────────────────────────────────

/**
 * Starts a periodic checker that fires every 60 seconds to detect upcoming events.
 * Shows a notification 15 minutes before the event and at the exact start time.
 */
function startEventReminders() {
    if (calendarNotificationInterval) return; // Already running

    // Run immediately on first call, then every 30 seconds
    checkUpcomingEvents();
    calendarNotificationInterval = setInterval(checkUpcomingEvents, 30 * 1000);
    console.log('[CalendarReminders] Iniciado — revisando eventos cada 30 segundos.');
}

function stopEventReminders() {
    if (calendarNotificationInterval) {
        clearInterval(calendarNotificationInterval);
        calendarNotificationInterval = null;
        console.log('[CalendarReminders] Detenido.');
    }
}

/**
 * Fetches today's events from the server and checks if any are starting soon.
 */
async function checkUpcomingEvents() {
    try {
        const now = new Date();
        const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0);
        const todayEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);

        const response = await fetch(`/api/calendar?start=${todayStart.toISOString()}&end=${todayEnd.toISOString()}`);
        if (!response.ok) return;

        const todayEvents = await response.json();

        todayEvents.forEach(event => {
            const eventStart = new Date(event.startTime);
            const msUntilEvent = eventStart - now;
            const minutesUntilEvent = msUntilEvent / 60000;

            // Window: notify if event is between -5 min (just started) and 16 min away
            if (minutesUntilEvent >= -5 && minutesUntilEvent <= 16) {
                const notifKey15 = `${event.id}_15min`;
                const notifKeyNow = `${event.id}_now`;

                // Advance warning (if event is between 0 and 16 minutes away)
                if (minutesUntilEvent > 0 && minutesUntilEvent <= 16 && !notifiedEvents[notifKey15]) {
                    notifiedEvents[notifKey15] = true;
                    saveNotifiedEvents();
                    sendCalendarNotification(
                        '📅 Evento próximo',
                        `"${event.title}" comienza en ${Math.ceil(minutesUntilEvent)} minutos`
                    );
                }

                // At event start time (between -5 and 0 minutes)
                if (minutesUntilEvent >= -5 && minutesUntilEvent <= 0 && !notifiedEvents[notifKeyNow]) {
                    notifiedEvents[notifKeyNow] = true;
                    saveNotifiedEvents();
                    sendCalendarNotification(
                        '🔔 Evento ahora: ' + event.title,
                        event.description ? event.description.substring(0, 100) : 'El evento ha comenzado.'
                    );
                }
            }
        });

        // Clean up old notification keys (keep only today's)
        pruneOldNotifiedEvents();

    } catch (err) {
        console.warn('[CalendarReminders] Error al revisar eventos:', err);
    }
}

/**
 * Sends a notification using the WPF bridge (native Windows toast) or browser Notification API.
 */
function sendCalendarNotification(title, body) {
    console.log(`[CalendarReminders] Notificación: ${title} — ${body}`);

    // Intentar método 1: Host bridge sync
    try {
        const hostBridge = window.chrome?.webview?.hostObjects?.sync?.chatAgendaBridge;
        if (hostBridge && typeof hostBridge.Notify === 'function') {
            console.log("[CalendarReminders] Usando método 1: Host bridge");
            hostBridge.Notify(title, body);
            return;
        }
    } catch (err) {}

    // Intentar método 2: WPF WebView2 host bridge or CefSharp (native Windows notification)
    try {
        if (window.chrome && window.chrome.webview && typeof window.chrome.webview.postMessage === 'function') {
            console.log("[CalendarReminders] Usando método 2: postMessage (chrome)");
            window.chrome.webview.postMessage({
                type: 'notification',
                title: title,
                body: body,
                source: 'calendar'
            });
            return; // Bridge sent
        } else if (window.CefSharp && window.CefSharp.PostMessage) {
            console.log("[CalendarReminders] Usando método 2: postMessage (CefSharp)");
            window.CefSharp.PostMessage(JSON.stringify({
                type: 'notification',
                title: title,
                body: body,
                source: 'calendar'
            }));
            return;
        }
    } catch (e) {
        console.warn('[CalendarReminders] Bridge WPF/CefSharp no disponible:', e);
    }

    // Intentar método 3: Enviar al servidor (fallback de escritorio)
    try {
        console.log("[CalendarReminders] Usando método 3: Servidor fallback");
        fetch('/api/notify', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ title, body })
        }).catch(e => console.warn("[CalendarReminders] Error en fallback:", e));
    } catch (err) {}

    // Method 4: Browser Notification API
    try {
        if (typeof window.Notification !== 'undefined') {
            if (window.Notification.permission === 'granted') {
                new window.Notification(title, {
                    body: body,
                    icon: '/favicon.ico',
                    tag: `calendar-${Date.now()}`
                });
            } else if (window.Notification.permission !== 'denied') {
                window.Notification.requestPermission().then(perm => {
                    if (perm === 'granted') {
                        new window.Notification(title, { body: body, icon: '/favicon.ico' });
                    }
                });
            }
        }
    } catch (e) {}

    // Method 3: In-app toast fallback (always visible inside the web app)
    showInAppCalendarToast(title, body);
}

/**
 * Shows a stylized in-app toast notification for calendar events.
 */
function showInAppCalendarToast(title, body) {
    let container = document.getElementById('toastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = 'toast-notification toast-calendar';
    toast.innerHTML = `
        <div class="toast-icon">📅</div>
        <div class="toast-content">
            <div class="toast-title">${title}</div>
            <div class="toast-body">${body}</div>
        </div>
        <button class="toast-close" onclick="this.parentElement.remove()">×</button>
    `;

    container.appendChild(toast);

    // Auto-remove after 8 seconds
    setTimeout(() => {
        if (toast.parentElement) toast.remove();
    }, 8000);
}

/**
 * Persist notified events to localStorage.
 */
function saveNotifiedEvents() {
    try {
        localStorage.setItem('ca_notified_events', JSON.stringify(notifiedEvents));
    } catch (e) { /* ignore quota errors */ }
}

/**
 * Remove notification keys older than 2 days to prevent localStorage bloat.
 */
function pruneOldNotifiedEvents() {
    // Simple pruning: if we have more than 500 keys, clear them all
    const keys = Object.keys(notifiedEvents);
    if (keys.length > 500) {
        notifiedEvents = {};
        saveNotifiedEvents();
    }
}

// ─── END NOTIFICATION SYSTEM ─────────────────────────────────────────────────


async function loadSyncState() {
    try {
        const response = await fetch('/api/settings');
        if (response.ok) {
            const data = await response.json();
            googleSyncState = data;
            updateSyncIndicatorUI();
        }
    } catch (err) {
        console.error("Error loading sync state", err);
    }
}

function updateSyncIndicatorUI() {
    const indicator = document.getElementById('googleSyncIndicator');
    if (!indicator) return;

    const dot = indicator.querySelector('.sync-dot');
    const label = indicator.querySelector('.sync-label');

    if (googleSyncState.isEnabled) {
        dot.className = 'sync-dot sync-active';
        label.textContent = `Sincronizado: ${googleSyncState.calendarId}`;
        indicator.title = `Última sincronización: ${googleSyncState.lastSyncTime ? new Date(googleSyncState.lastSyncTime).toLocaleString() : 'Nunca'}`;
    } else {
        dot.className = 'sync-dot sync-inactive';
        label.textContent = 'Google Calendar desactivado';
        indicator.title = 'Configura el sincronizador desde el panel de administración';
    }
}

async function loadEvents() {
    // Calculate start/end range of current visible month
    const startOfMonth = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1);
    const endOfMonth = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0);

    // Padding (30 days back and forward for sanity)
    const startDate = new Date(startOfMonth);
    startDate.setDate(startDate.getDate() - 10);
    const endDate = new Date(endOfMonth);
    endDate.setDate(endDate.getDate() + 10);

    try {
        const response = await fetch(`/api/calendar?start=${startDate.toISOString()}&end=${endDate.toISOString()}`);
        if (response.ok) {
            eventsList = await response.json();
            renderCalendarGrid();
        }
    } catch (err) {
        console.error("Error loading events", err);
    }
}

function changeMonth(delta) {
    currentDate.setMonth(currentDate.getMonth() + delta);
    loadEvents();
}

function renderCalendarGrid() {
    const title = document.getElementById('calendarMonthYear');
    const grid = document.getElementById('calendarGrid');
    
    if (!title || !grid) return;

    // Header Text
    const months = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];
    title.textContent = `${months[currentDate.getMonth()]} ${currentDate.getFullYear()}`;

    // Clear grid (keep weekdays)
    const weekdayHeaders = Array.from(grid.querySelectorAll('.calendar-weekday'));
    grid.innerHTML = '';
    weekdayHeaders.forEach(header => grid.appendChild(header));

    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();

    // First day of current month (0: Sunday, 1: Monday, ...)
    const firstDayIndex = new Date(year, month, 1).getDay();
    // Adjust to Monday start (0: Monday, 1: Tuesday ... 6: Sunday)
    let startDayOffset = firstDayIndex - 1;
    if (startDayOffset < 0) startDayOffset = 6; // Sunday becomes index 6

    // Total days in current month
    const totalDays = new Date(year, month + 1, 0).getDate();
    // Total days in previous month
    const prevMonthDays = new Date(year, month, 0).getDate();

    // 1. Fill previous month padding days
    for (let i = startDayOffset - 1; i >= 0; i--) {
        const dayNum = prevMonthDays - i;
        const cellDate = new Date(year, month - 1, dayNum);
        const cell = createDayCell(dayNum, cellDate, true);
        grid.appendChild(cell);
    }

    // 2. Fill current month days
    const today = new Date();
    for (let day = 1; day <= totalDays; day++) {
        const cellDate = new Date(year, month, day);
        const isToday = cellDate.getDate() === today.getDate() && 
                        cellDate.getMonth() === today.getMonth() && 
                        cellDate.getFullYear() === today.getFullYear();
        
        const cell = createDayCell(day, cellDate, false, isToday);
        grid.appendChild(cell);
    }

    // 3. Fill next month padding days
    const totalCellsFilled = startDayOffset + totalDays;
    const remainingCells = 42 - totalCellsFilled; // 6 rows * 7 days = 42 cells
    for (let day = 1; day <= remainingCells; day++) {
        const cellDate = new Date(year, month + 1, day);
        const cell = createDayCell(day, cellDate, true);
        grid.appendChild(cell);
    }
}

function createDayCell(dayNum, date, isOtherMonth, isToday = false) {
    const cell = document.createElement('div');
    cell.className = `calendar-day-cell ${isOtherMonth ? 'other-month' : ''} ${isToday ? 'today' : ''}`;
    
    cell.innerHTML = `
        <div class="day-number">${dayNum}</div>
        <div class="day-events" id="events-container-${formatDateKey(date)}"></div>
    `;

    // Click on empty space to create new event
    cell.addEventListener('click', (e) => {
        // Prevent trigger if clicking on an event pill
        if (e.target.classList.contains('calendar-event-pill')) return;
        openEventModal(null, date);
    });

    // Populate events for this date
    setTimeout(() => {
        populateEventsForDay(date);
    }, 0);

    return cell;
}

function populateEventsForDay(date) {
    const container = document.getElementById(`events-container-${formatDateKey(date)}`);
    if (!container) return;

    const cellDateKey = formatDateKey(date);

    // Filter events starting on this date
    const dayEvents = eventsList.filter(e => {
        const evDate = new Date(e.startTime);
        return formatDateKey(evDate) === cellDateKey;
    });

    dayEvents.forEach(ev => {
        const pill = document.createElement('div');
        pill.className = 'calendar-event-pill';
        
        const time = new Date(ev.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        pill.textContent = `${time} ${ev.title}`;
        pill.title = `${ev.title}\n${ev.description || 'Sin descripción'}`;

        pill.addEventListener('click', (e) => {
            e.stopPropagation();
            openEventModal(ev);
        });

        container.appendChild(pill);
    });
}

function openEventModal(event = null, date = null) {
    selectedEvent = event;
    const modal = document.getElementById('eventModal');
    const modalTitle = document.getElementById('modalTitle');
    const inputTitle = document.getElementById('eventTitle');
    const inputDesc = document.getElementById('eventDesc');
    const inputStart = document.getElementById('eventStart');
    const inputEnd = document.getElementById('eventEnd');
    const btnDelete = document.getElementById('btnDeleteEvent');
    const auditInfo = document.getElementById('eventAuditInfo');

    if (!modal) return;

    modal.style.display = 'flex';

    if (event) {
        // Edit mode
        modalTitle.textContent = 'Editar Evento';
        inputTitle.value = event.title;
        inputDesc.value = event.description || '';
        inputStart.value = formatDateTimeForInput(new Date(event.startTime));
        inputEnd.value = formatDateTimeForInput(new Date(event.endTime));
        btnDelete.style.display = 'block';

        // Render audit info
        const modifiedDate = new Date(event.lastModified).toLocaleString();
        let syncStatusText = 'Sincronizado';
        if (event.syncStatus === 'PendingUpdate') syncStatusText = 'Pendiente de sincronizar...';
        else if (event.syncStatus === 'Conflict') syncStatusText = 'Conflicto de sincronización';

        auditInfo.innerHTML = `
            <div style="font-size: 0.75rem; color: var(--text-secondary); margin-top: 0.5rem; border-top: 1px solid var(--glass-border); padding-top: 0.5rem;">
                <strong>Creado por:</strong> ${event.createdByUserName}<br>
                <strong>Último cambio:</strong> ${event.lastModifiedBy} (${modifiedDate})<br>
                <strong>Estado de sincronización:</strong> ${syncStatusText}
            </div>
        `;
    } else {
        // Create mode
        modalTitle.textContent = 'Nuevo Evento';
        inputTitle.value = '';
        inputDesc.value = '';
        btnDelete.style.display = 'none';
        auditInfo.innerHTML = '';

        // Default start/end dates
        const start = new Date(date);
        start.setHours(9, 0, 0, 0); // Default to 9:00 AM
        const end = new Date(start);
        end.setHours(10, 0, 0, 0); // Default to 10:00 AM

        inputStart.value = formatDateTimeForInput(start);
        inputEnd.value = formatDateTimeForInput(end);
    }
}

function closeEventModal() {
    const modal = document.getElementById('eventModal');
    if (modal) modal.style.display = 'none';
    selectedEvent = null;
}

async function saveEvent(e) {
    e.preventDefault();

    const title = document.getElementById('eventTitle').value.trim();
    const description = document.getElementById('eventDesc').value.trim();
    const startTime = new Date(document.getElementById('eventStart').value);
    const endTime = new Date(document.getElementById('eventEnd').value);

    if (!title) {
        alert("El título del evento es requerido.");
        return;
    }
    if (startTime >= endTime) {
        alert("La hora de inicio debe ser anterior a la de fin.");
        return;
    }

    const payload = {
        title,
        description,
        startTime: startTime.toISOString(),
        endTime: endTime.toISOString()
    };

    try {
        let response;
        if (selectedEvent) {
            // Edit
            response = await fetch(`/api/calendar/${selectedEvent.id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
        } else {
            // Create
            response = await fetch('/api/calendar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
        }

        if (response.ok) {
            closeEventModal();
            loadEvents();
        } else {
            const data = await response.json();
            alert(`Error al guardar: ${data.message}`);
        }
    } catch (err) {
        console.error("Save failed", err);
        alert("Error de red al guardar el evento.");
    }
}

async function deleteEvent() {
    if (!selectedEvent) return;
    if (!confirm(`¿Estás seguro de eliminar el evento "${selectedEvent.title}"?`)) return;

    try {
        const response = await fetch(`/api/calendar/${selectedEvent.id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            closeEventModal();
            loadEvents();
        } else {
            const data = await response.json();
            alert(`Error al eliminar: ${data.message}`);
        }
    } catch (err) {
        console.error("Delete failed", err);
        alert("Error de red al eliminar el evento.");
    }
}

// Helpers
function formatDateKey(date) {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
}

function formatDateTimeForInput(date) {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    const hh = String(date.getHours()).padStart(2, '0');
    const mm = String(date.getMinutes()).padStart(2, '0');
    return `${y}-${m}-${d}T${hh}:${mm}`;
}
