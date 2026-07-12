// Global variables for the application
let currentUser = null;
let activeView = 'chat'; // 'chat', 'calendar', 'admin'
let connection = null;
let contacts = [];
let onlineUserIds = [];
let customGroups = [];

// DOM elements
document.addEventListener('DOMContentLoaded', () => {
    initApp();
});

async function initApp() {
    // Solicitar permisos de notificación en el SO si el navegador lo soporta
    try {
        if (typeof window.Notification !== "undefined" && typeof window.Notification.requestPermission === "function") {
            const permission = window.Notification.permission;
            if (permission !== "granted" && permission !== "denied") {
                window.Notification.requestPermission().catch(() => {});
            }
        }
    } catch (err) {
        console.warn("No se pudieron solicitar permisos de notificación:", err);
    }

    // 1. Verify Authentication (IP-based auto-login)
    currentUser = await checkAuth();
    if (!currentUser) {
        alert("No se pudo conectar con el servidor de la oficina local.");
        return;
    }

    // 2. Set up first-time name configuration form listener
    setupNicknameForm();

    // 3. Check if user needs to set nickname
    if (!currentUser.displayName) {
        openNicknameModal();
    } else {
        await completeAppInitialization();
    }
}

async function checkAuth() {
    try {
        const response = await fetch('/api/auth/me');
        if (response.ok) {
            return await response.json();
        }
    } catch (err) {
        console.error("Auth check failed", err);
    }
    return null;
}

async function completeAppInitialization() {
    // Render profile details
    renderProfile();

    // Initialize navigation listeners
    setupNavigation();

    // Load active contacts list
    await loadContacts();

    // Establish SignalR connection
    initSignalR();

    // Navigate to default chat view
    switchView(activeView);
}

function renderProfile() {
    const avatar = document.getElementById('profileAvatar');
    const name = document.getElementById('profileName');
    const role = document.getElementById('profileRole');

    if (avatar) avatar.textContent = currentUser.displayName.charAt(0).toUpperCase();
    if (name) name.textContent = currentUser.displayName;
    if (role) role.textContent = `${currentUser.role} • ${currentUser.department}`;

    // Hide admin page button if user is not admin
    const adminNav = document.getElementById('navAdmin');
    if (adminNav) {
        adminNav.style.display = currentUser.role === 'Admin' ? 'flex' : 'none';
    }
}

function setupNavigation() {
    const navChat = document.getElementById('navChat');
    const navCalendar = document.getElementById('navCalendar');
    const navAdmin = document.getElementById('navAdmin');
    const btnChangeName = document.getElementById('btnChangeName');

    if (navChat) navChat.addEventListener('click', () => switchView('chat'));
    if (navCalendar) navCalendar.addEventListener('click', () => switchView('calendar'));
    if (navAdmin) navAdmin.addEventListener('click', () => switchView('admin'));
    if (btnChangeName) btnChangeName.addEventListener('click', () => openNicknameModal());
}

function openNicknameModal() {
    const modal = document.getElementById('nicknameModal');
    if (!modal) return;

    document.getElementById('nickDisplayName').value = currentUser.displayName || '';
    document.getElementById('nickDept').value = currentUser.department || '';
    modal.style.display = 'flex';
}

function setupNicknameForm() {
    const form = document.getElementById('nicknameForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const displayName = document.getElementById('nickDisplayName').value.trim();
        const department = document.getElementById('nickDept').value.trim();

        if (!displayName) {
            alert("El nombre es requerido.");
            return;
        }

        try {
            const response = await fetch('/api/users/profile', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ displayName, department })
            });

            if (response.ok) {
                const updatedUser = await response.json();
                currentUser = updatedUser;

                // Close nickname setup modal
                document.getElementById('nicknameModal').style.display = 'none';

                // Redraw profile card
                renderProfile();

                // If this was the initial setup, complete loading the app
                if (!connection) {
                    await completeAppInitialization();
                } else {
                    // Active rename, reload contacts list and inform hub
                    await loadContacts();
                    connection.invoke("RegisterUser", currentUser.id);
                }
            } else {
                alert("No se pudo guardar la información del perfil.");
            }
        } catch (err) {
            console.error(err);
            alert("Error al conectar con el servidor local.");
        }
    });
}

function switchView(viewName) {
    activeView = viewName;
    
    // Update active nav style
    const navItems = document.querySelectorAll('.tab-item');
    navItems.forEach(item => item.classList.remove('active'));
    
    const activeNav = document.getElementById(`nav${viewName.charAt(0).toUpperCase() + viewName.slice(1)}`);
    if (activeNav) activeNav.classList.add('active');

    // Toggle panels visibility
    document.getElementById('chatPanel').style.display = viewName === 'chat' ? 'flex' : 'none';
    document.getElementById('calendarPanel').style.display = viewName === 'calendar' ? 'flex' : 'none';
    document.getElementById('adminPanel').style.display = viewName === 'admin' ? 'flex' : 'none';

    // Close contacts dropdown on view switch
    const dropdown = document.getElementById('contactsDropdownPanel');
    if (dropdown) dropdown.classList.remove('show');

    // Trigger load actions based on view
    if (viewName === 'calendar') {
        if (window.calendarApp && typeof window.calendarApp.initCalendar === 'function') {
            window.calendarApp.initCalendar();
        }
    } else if (viewName === 'admin') {
        if (window.adminApp && typeof window.adminApp.initAdmin === 'function') {
            window.adminApp.initAdmin();
        }
    }
}

async function loadCustomGroups() {
    try {
        const response = await fetch('/api/chat/groups');
        if (response.ok) {
            const groups = await response.json();
            customGroups = groups.filter(g => g && g !== 'General' && (!currentUser || g !== currentUser.department));
        }
    } catch (e) {
        console.error("Failed to load custom groups", e);
    }
}

async function loadContacts() {
    try {
        const response = await fetch('/api/users');
        if (response.ok) {
            contacts = await response.json();
            if (currentUser) {
                contacts = contacts.filter(u => u.id !== currentUser.id);
            }
            await loadCustomGroups();
            renderContacts();
        }
    } catch (err) {
        console.error("Failed to load contacts", err);
    }
}

function renderContacts() {
    const list = document.getElementById('contactsList');
    if (!list) return;

    list.innerHTML = '';

    // 1. Group Channel (General)
    const generalItem = createGroupContactItem('General', 'General Channel', '✦');
    list.appendChild(generalItem);

    // 2. Department Channel (User's Department)
    if (currentUser && currentUser.department && currentUser.department !== 'General') {
        const deptItem = createGroupContactItem(currentUser.department, `${currentUser.department} Team`, '👥');
        list.appendChild(deptItem);
    }

    // 3. Custom Dynamic Groups
    customGroups.forEach(groupName => {
        const groupItem = createGroupContactItem(groupName, `Sala: ${groupName}`, '💬');
        list.appendChild(groupItem);
    });

    // Divider header
    const divHeader = document.createElement('div');
    divHeader.className = 'contacts-header';
    divHeader.style.marginTop = '1rem';
    divHeader.textContent = 'Contactos';
    list.appendChild(divHeader);

    // 3. User contacts
    contacts.forEach(user => {
        const item = document.createElement('div');
        item.className = 'contact-item';
        item.id = `contact-${user.id}`;
        
        const isOnline = onlineUserIds.includes(user.id);
        const statusClass = isOnline ? 'status-online' : 'status-offline';

        item.innerHTML = `
            <div class="contact-avatar">
                ${user.displayName.charAt(0).toUpperCase()}
                <div class="contact-status ${statusClass}" id="status-dot-${user.id}"></div>
            </div>
            <div class="contact-info">
                <div class="contact-name">${user.displayName}</div>
                <div class="contact-subtitle">${user.role} - ${user.department}</div>
            </div>
            <div class="unread-badge" id="unread-${user.id}" style="display: none;">0</div>
        `;

        item.addEventListener('click', () => {
            // Remove active from others
            document.querySelectorAll('.contact-item').forEach(c => c.classList.remove('active'));
            item.classList.add('active');
            
            // Mark unread badge as read
            const badge = document.getElementById(`unread-${user.id}`);
            if (badge) {
                badge.style.display = 'none';
                badge.textContent = '0';
            }

            // Open chat window
            if (window.chatApp && typeof window.chatApp.openDirectChat === 'function') {
                window.chatApp.openDirectChat(user);
            }
            
            // Close dropdown submenu after choosing a contact
            const panel = document.getElementById('contactsDropdownPanel');
            if (panel) panel.classList.remove('show');
        });

        list.appendChild(item);
    });
}

function createGroupContactItem(groupId, groupName, icon) {
    const item = document.createElement('div');
    item.className = 'contact-item';
    item.id = `group-${groupId}`;
    
    item.innerHTML = `
        <div class="contact-avatar" style="background: rgba(99, 102, 241, 0.15); border-color: rgba(99, 102, 241, 0.3);">
            ${icon}
        </div>
        <div class="contact-info">
            <div class="contact-name">${groupName}</div>
            <div class="contact-subtitle">Sala de red local</div>
        </div>
        <div class="unread-badge" id="unread-group-${groupId}" style="display: none;">0</div>
    `;

    item.addEventListener('click', () => {
        document.querySelectorAll('.contact-item').forEach(c => c.classList.remove('active'));
        item.classList.add('active');
        
        const badge = document.getElementById(`unread-group-${groupId}`);
        if (badge) {
            badge.style.display = 'none';
            badge.textContent = '0';
        }

        if (window.chatApp && typeof window.chatApp.openGroupChat === 'function') {
            window.chatApp.openGroupChat(groupId, groupName);
        }
        
        // Close dropdown submenu after choosing a channel
        const panel = document.getElementById('contactsDropdownPanel');
        if (panel) panel.classList.remove('show');
    });

    return item;
}

function initSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    // Listener for presence updates
    connection.on("UserPresenceUpdate", (onlineIds) => {
        onlineUserIds = onlineIds;
        updateUserPresenceUI();
    });

    // Delegate message handling to chat app
    connection.on("ReceiveMessage", (msg) => {
        if (window.chatApp && typeof window.chatApp.handleReceivedMessage === 'function') {
            window.chatApp.handleReceivedMessage(msg);
        }
    });

    // Delegate typing status
    connection.on("UserTyping", (senderId, isTyping) => {
        if (window.chatApp && typeof window.chatApp.handleUserTyping === 'function') {
            window.chatApp.handleUserTyping(senderId, isTyping);
        }
    });

    connection.on("GroupTyping", (groupName, senderId, senderName, isTyping) => {
        if (window.chatApp && typeof window.chatApp.handleGroupTyping === 'function') {
            window.chatApp.handleGroupTyping(groupName, senderId, senderName, isTyping);
        }
    });

    // Refresh calendar on sync notification
    connection.on("CalendarUpdated", () => {
        if (activeView === 'calendar' && window.calendarApp && typeof window.calendarApp.loadEvents === 'function') {
            window.calendarApp.loadEvents();
        }
        // Also reload logs if admin panel open
        if (activeView === 'admin' && window.adminApp && typeof window.adminApp.loadLogs === 'function') {
            window.adminApp.loadLogs();
        }
    });

    // Start connection
    connection.start()
        .then(async () => {
            console.log("SignalR conectado.");
            await connection.invoke("RegisterUser", currentUser.id);
            await joinAllActiveGroupsOnSignalRConnect();
        })
        .catch(err => {
            console.error("SignalR connection error", err);
            setTimeout(initSignalR, 5000); // Retry
        });
}

async function joinAllActiveGroupsOnSignalRConnect() {
    try {
        const response = await fetch('/api/chat/groups');
        if (response.ok) {
            const groups = await response.json();
            for (const g of groups) {
                if (g) {
                    await connection.invoke("JoinGroupChannel", g);
                }
            }
        }
    } catch (e) {
        console.error("Failed to auto-join groups on SignalR connect", e);
    }
}

async function createNewGroupFromInput(e) {
    if (e) e.stopPropagation();
    const input = document.getElementById('newGroupNameInput');
    if (!input) return;

    let groupName = input.value.trim();
    if (!groupName) return;

    // Sanitize name: alphanumeric, spaces, underscores, dashes only
    groupName = groupName.replace(/[^a-zA-Z0-9\s-_]/g, '').trim();
    if (!groupName) {
        alert("Nombre de grupo inválido. Usa solo letras, números y espacios.");
        return;
    }

    // Check if group is already in our list
    if (!customGroups.includes(groupName)) {
        customGroups.push(groupName);
    }

    // Clear input
    input.value = '';

    // Join SignalR group
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        try {
            await connection.invoke("JoinGroupChannel", groupName);
        } catch (err) {
            console.error("Error invoking JoinGroupChannel", err);
        }
    }

    // Re-render contact list
    renderContacts();

    // Open group chat
    if (window.chatApp && typeof window.chatApp.openGroupChat === 'function') {
        window.chatApp.openGroupChat(groupName, `Sala: ${groupName}`);
    }

    // Close contacts dropdown panel
    const panel = document.getElementById('contactsDropdownPanel');
    if (panel) panel.classList.remove('show');
}

// Bind to window so HTML event handlers can access it
window.createNewGroupFromInput = createNewGroupFromInput;

function updateUserPresenceUI() {
    contacts.forEach(user => {
        const dot = document.getElementById(`status-dot-${user.id}`);
        if (dot) {
            const isOnline = onlineUserIds.includes(user.id);
            dot.className = `contact-status ${isOnline ? 'status-online' : 'status-offline'}`;
        }
    });
}

async function handleLogout() {
    try {
        await fetch('/api/auth/logout', { method: 'POST' });
        localStorage.removeItem('currentUser');
        window.location.href = 'login.html';
    } catch (err) {
        console.error("Logout failed", err);
    }
}

// Contacts Dropdown Toggle function (despliegue de submenús)
function toggleContactsDropdown(event) {
    if (event) event.stopPropagation();
    const panel = document.getElementById('contactsDropdownPanel');
    if (panel) {
        panel.classList.toggle('show');
    }
}

// Global click listener to close the dropdown when clicking outside
window.addEventListener('click', (e) => {
    const panel = document.getElementById('contactsDropdownPanel');
    const headerMain = document.querySelector('.chat-header-main');
    const welcomeBtn = document.querySelector('.empty-state button');
    
    if (panel && panel.classList.contains('show')) {
        if (!panel.contains(e.target) && 
            (!headerMain || !headerMain.contains(e.target)) && 
            (!welcomeBtn || !welcomeBtn.contains(e.target))) {
            panel.classList.remove('show');
        }
    }
});
