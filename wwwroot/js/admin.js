// Admin state
let adminUsers = [];
let selectedAdminUser = null;

window.adminApp = {
    initAdmin,
    loadLogs
};

document.addEventListener('DOMContentLoaded', () => {
    setupAdminListeners();
});

function setupAdminListeners() {
    const formSettings = document.getElementById('settingsForm');
    const formUser = document.getElementById('adminUserForm');
    const btnNewUser = document.getElementById('btnCreateUser');
    const userModalClose = document.getElementById('adminUserModalClose');
    const btnCancelUser = document.getElementById('btnCancelAdminUser');
    const btnSyncNow = document.getElementById('btnTriggerSync');

    if (formSettings) formSettings.addEventListener('submit', saveSettings);
    if (formUser) formUser.addEventListener('submit', saveUser);
    if (btnNewUser) btnNewUser.addEventListener('click', () => openUserModal());
    if (userModalClose) userModalClose.addEventListener('click', closeUserModal);
    if (btnCancelUser) btnCancelUser.addEventListener('click', closeUserModal);
    if (btnSyncNow) btnSyncNow.addEventListener('click', triggerSyncNow);
}

function initAdmin() {
    loadSettings();
    loadUsers();
    loadLogs();
}

async function loadSettings() {
    try {
        const response = await fetch('/api/settings');
        if (response.ok) {
            const data = await response.json();
            document.getElementById('syncCalendarId').value = data.calendarId || 'primary';
            document.getElementById('syncEnabled').checked = data.isEnabled;
            
            const credsLabel = document.getElementById('credsStatusLabel');
            if (credsLabel) {
                if (data.hasCredentials) {
                    credsLabel.textContent = 'Credenciales actuales: Cargadas y Activas';
                    credsLabel.style.color = 'var(--neon-success)';
                } else {
                    credsLabel.textContent = 'Credenciales actuales: No cargadas';
                    credsLabel.style.color = 'var(--neon-danger)';
                }
            }
        }
    } catch (err) {
        console.error("Failed to load settings", err);
    }
}

async function saveSettings(e) {
    e.preventDefault();

    const calendarId = document.getElementById('syncCalendarId').value.trim();
    const isEnabled = document.getElementById('syncEnabled').checked;
    const credsJson = document.getElementById('syncCredsJson').value.trim();

    if (!calendarId) {
        alert("El ID de calendario es requerido.");
        return;
    }

    const payload = {
        calendarId,
        isEnabled
    };

    if (credsJson) {
        try {
            JSON.parse(credsJson); // Validate locally
            payload.credentialsJson = credsJson;
        } catch {
            alert("Las credenciales deben ser un formato JSON válido.");
            return;
        }
    }

    try {
        const response = await fetch('/api/settings', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            alert("Configuración de Google Calendar guardada correctamente.");
            document.getElementById('syncCredsJson').value = ''; // Clear input
            loadSettings();
            
            // Sync status might have changed, update calendar UI indicator too
            if (window.calendarApp && typeof window.calendarApp.initCalendar === 'function') {
                window.calendarApp.initCalendar();
            }
        } else {
            const data = await response.json();
            alert(`Error al guardar: ${data.message}`);
        }
    } catch (err) {
        console.error("Save settings error", err);
        alert("Error de red al guardar la configuración.");
    }
}

async function triggerSyncNow() {
    const btn = document.getElementById('btnTriggerSync');
    btn.disabled = true;
    btn.textContent = 'Sincronizando...';

    try {
        const response = await fetch('/api/settings/sync-now', { method: 'POST' });
        if (response.ok) {
            alert("Ciclo de sincronización manual ejecutado con éxito.");
            loadSettings();
            loadLogs();
        } else {
            const data = await response.json();
            alert(`Error de sincronización: ${data.message}`);
        }
    } catch (err) {
        console.error(err);
        alert("Error al conectar con el sincronizador.");
    } finally {
        btn.disabled = false;
        btn.textContent = '🔄 Forzar Sincronización';
    }
}

async function loadUsers() {
    try {
        const response = await fetch('/api/users/all');
        if (response.ok) {
            adminUsers = await response.json();
            renderUsersTable();
        }
    } catch (err) {
        console.error("Failed to load users for admin", err);
    }
}

function renderUsersTable() {
    const tbody = document.getElementById('adminUsersTableBody');
    if (!tbody) return;

    tbody.innerHTML = '';

    adminUsers.forEach(user => {
        const tr = document.createElement('tr');

        tr.innerHTML = `
            <td><strong>${user.username}</strong></td>
            <td>${user.displayName}</td>
            <td><span class="role-pill role-${user.role}">${user.role}</span></td>
            <td>${user.department}</td>
            <td>
                <span style="color: ${user.isActive ? 'var(--neon-success)' : 'var(--neon-danger)'}">
                    ${user.isActive ? '🟢 Activo' : '🔴 Inactivo'}
                </span>
            </td>
            <td>
                <button class="btn-secondary" style="padding: 0.3rem 0.6rem; font-size: 0.8rem; margin-right: 0.25rem;" onclick="adminAppOpenUser('${user.id}')">Editar</button>
                <button class="btn-secondary" style="padding: 0.3rem 0.6rem; font-size: 0.8rem;" onclick="adminAppResetPassword('${user.id}')">Clave</button>
            </td>
        `;

        tbody.appendChild(tr);
    });
}

// Global scope helpers for onclick attributes in tables
window.adminAppOpenUser = function(userId) {
    const user = adminUsers.find(u => u.id == userId);
    if (user) openUserModal(user);
};

window.adminAppResetPassword = function(userId) {
    const user = adminUsers.find(u => u.id == userId);
    if (!user) return;

    const newPass = prompt(`Ingresa la nueva contraseña para ${user.displayName}:`);
    if (newPass === null) return; // cancelled
    if (newPass.trim().length < 4) {
        alert("La contraseña debe tener al menos 4 caracteres.");
        return;
    }

    fetch(`/api/users/${userId}/change-password`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ newPassword: newPass.trim() })
    })
    .then(res => {
        if (res.ok) alert(`Contraseña cambiada exitosamente para ${user.displayName}.`);
        else alert("No se pudo cambiar la contraseña.");
    })
    .catch(err => console.error(err));
};

function openUserModal(user = null) {
    selectedAdminUser = user;
    const modal = document.getElementById('adminUserModal');
    const title = document.getElementById('adminUserModalTitle');
    const inputUser = document.getElementById('adminUsername');
    const inputDisplay = document.getElementById('adminDisplayName');
    const passGroup = document.getElementById('adminPasswordGroup');
    const inputPass = document.getElementById('adminPassword');
    const selectRole = document.getElementById('adminRole');
    const inputDept = document.getElementById('adminDept');
    const selectActive = document.getElementById('adminActive');

    if (!modal) return;

    modal.style.display = 'flex';

    if (user) {
        // Edit mode
        title.textContent = 'Editar Usuario';
        inputUser.value = user.username;
        inputUser.disabled = true; // Cannot edit username
        inputDisplay.value = user.displayName;
        passGroup.style.display = 'none'; // Separate trigger for password reset
        inputPass.required = false;
        selectRole.value = user.role;
        inputDept.value = user.department;
        selectActive.value = user.isActive ? "true" : "false";
    } else {
        // Create mode
        title.textContent = 'Crear Nuevo Usuario';
        inputUser.value = '';
        inputUser.disabled = false;
        inputDisplay.value = '';
        passGroup.style.display = 'flex';
        inputPass.value = '';
        inputPass.required = true;
        selectRole.value = 'Employee';
        inputDept.value = 'General';
        selectActive.value = 'true';
    }
}

function closeUserModal() {
    const modal = document.getElementById('adminUserModal');
    if (modal) modal.style.display = 'none';
    selectedAdminUser = null;
}

async function saveUser(e) {
    e.preventDefault();

    const username = document.getElementById('adminUsername').value.trim();
    const displayName = document.getElementById('adminDisplayName').value.trim();
    const role = document.getElementById('adminRole').value;
    const department = document.getElementById('adminDept').value.trim();
    const isActive = document.getElementById('adminActive').value === 'true';

    if (selectedAdminUser) {
        // Update user
        const payload = {
            displayName,
            role,
            department,
            isActive
        };

        try {
            const response = await fetch(`/api/users/${selectedAdminUser.id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                closeUserModal();
                loadUsers();
                // Refresh left contact list as well
                loadContacts();
            } else {
                const data = await response.json();
                alert(`Error al actualizar: ${data.message}`);
            }
        } catch (err) {
            console.error(err);
            alert("Error al guardar cambios.");
        }
    } else {
        // Create user
        const password = document.getElementById('adminPassword').value;
        if (!username || !password) return;

        const payload = {
            username,
            displayName,
            password,
            role,
            department
        };

        try {
            const response = await fetch('/api/users', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                closeUserModal();
                loadUsers();
                loadContacts();
            } else {
                const data = await response.json();
                alert(`Error al crear: ${data.message}`);
            }
        } catch (err) {
            console.error(err);
            alert("Error al conectar con el servidor.");
        }
    }
}

async function loadLogs() {
    const container = document.getElementById('adminLogsList');
    if (!container) return;

    try {
        const response = await fetch('/api/calendar/history');
        if (response.ok) {
            const logs = await response.json();
            
            container.innerHTML = '';
            if (logs.length === 0) {
                container.innerHTML = '<div style="color: var(--text-muted);">Sin registros de sincronización aún.</div>';
                return;
            }

            logs.forEach(log => {
                const date = new Date(log.modifiedAt).toLocaleString();
                const item = document.createElement('div');
                item.className = 'log-item';
                
                item.innerHTML = `
                    <span class="log-time">[${date}]</span>
                    <span class="log-source-${log.source}">[${log.source}]</span>
                    <span><strong>${log.eventTitle}:</strong> ${log.changeDescription} (${log.modifiedBy})</span>
                `;

                container.appendChild(item);
            });
        }
    } catch (err) {
        console.error("Failed to load logs", err);
    }
}
