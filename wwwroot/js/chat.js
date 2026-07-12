// Chat state
let activeChat = null; // { type: 'direct'|'group', id: string, name: string }
let typingTimeout = null;
let isCurrentlyTyping = false;

window.chatApp = {
    openDirectChat,
    openGroupChat,
    handleReceivedMessage,
    handleUserTyping,
    handleGroupTyping
};

// Initialize listeners
document.addEventListener('DOMContentLoaded', () => {
    setupChatListeners();
});

function setupChatListeners() {
    const input = document.getElementById('chatInput');
    const btnSend = document.getElementById('btnSend');
    const btnAttach = document.getElementById('btnAttach');
    const fileInput = document.getElementById('chatFileInput');

    if (input) {
        input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                sendMessage();
            } else {
                handleTypingKeyPress();
            }
        });
    }

    if (btnSend) btnSend.addEventListener('click', sendMessage);
    if (btnAttach) btnAttach.addEventListener('click', () => fileInput.click());
    if (fileInput) fileInput.addEventListener('change', handleFileUpload);
}

function handleTypingKeyPress() {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;

    if (!isCurrentlyTyping) {
        isCurrentlyTyping = true;
        sendTypingStatus(true);
    }

    clearTimeout(typingTimeout);
    typingTimeout = setTimeout(() => {
        isCurrentlyTyping = false;
        sendTypingStatus(false);
    }, 2000); // Stop typing status after 2s of inactivity
}

function sendTypingStatus(isTyping) {
    if (!activeChat) return;

    if (activeChat.type === 'direct') {
        connection.invoke("SendTypingStatus", activeChat.id, isTyping)
            .catch(err => console.error(err));
    } else {
        connection.invoke("SendGroupTypingStatus", activeChat.id, isTyping)
            .catch(err => console.error(err));
    }
}

async function openDirectChat(contact) {
    activeChat = {
        type: 'direct',
        id: contact.id,
        name: contact.displayName
    };

    // Update Chat UI Headers
    document.getElementById('chatWelcome').style.display = 'none';
    document.getElementById('chatWindow').style.display = 'flex';
    document.getElementById('chatHeaderTitle').textContent = contact.displayName;
    document.getElementById('chatHeaderStatus').textContent = `${contact.role} • ${contact.department}`;
    document.getElementById('chatTypingIndicator').style.display = 'none';

    // Clear messages pane
    const pane = document.getElementById('chatMessages');
    pane.innerHTML = '<div style="align-self: center; color: var(--text-muted); font-size: 0.85rem; margin-top: 1rem;">Cargando mensajes históricos...</div>';

    // Fetch message history from API
    try {
        const response = await fetch(`/api/chat/direct/${contact.id}`);
        if (response.ok) {
            const messages = await response.json();
            renderMessages(messages);
        }
    } catch (err) {
        console.error("Error fetching message history", err);
        pane.innerHTML = '<div style="align-self: center; color: var(--neon-danger); font-size: 0.85rem;">Error al cargar historial.</div>';
    }

    document.getElementById('chatInput').focus();
}

async function openGroupChat(groupId, groupName) {
    activeChat = {
        type: 'group',
        id: groupId,
        name: groupName
    };

    // Update Chat UI Headers
    document.getElementById('chatWelcome').style.display = 'none';
    document.getElementById('chatWindow').style.display = 'flex';
    document.getElementById('chatHeaderTitle').textContent = groupName;
    document.getElementById('chatHeaderStatus').textContent = "Canal de la red local";
    document.getElementById('chatTypingIndicator').style.display = 'none';

    // Clear messages pane
    const pane = document.getElementById('chatMessages');
    pane.innerHTML = '<div style="align-self: center; color: var(--text-muted); font-size: 0.85rem; margin-top: 1rem;">Cargando mensajes históricos...</div>';

    // Fetch message history from API
    try {
        const response = await fetch(`/api/chat/group/${groupId}`);
        if (response.ok) {
            const messages = await response.json();
            renderMessages(messages);
        }
    } catch (err) {
        console.error("Error fetching group messages", err);
        pane.innerHTML = '<div style="align-self: center; color: var(--neon-danger); font-size: 0.85rem;">Error al cargar historial.</div>';
    }

    document.getElementById('chatInput').focus();
}

function renderMessages(messages) {
    const pane = document.getElementById('chatMessages');
    pane.innerHTML = '';

    messages.forEach(msg => {
        appendSingleMessage(msg);
    });

    scrollToBottom();
}

function appendSingleMessage(msg) {
    const pane = document.getElementById('chatMessages');
    if (!pane) return;

    const isSent = msg.senderId === currentUser.id;
    const wrapper = document.createElement('div');
    wrapper.className = `message-wrapper ${isSent ? 'sent' : 'received'}`;

    // Format Timestamp
    const date = new Date(msg.timestamp);
    const timeStr = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    let attachmentHtml = '';
    if (msg.fileName && msg.filePath) {
        // Build file icon depending on extension
        let fileIcon = '📄';
        if (/\.(jpg|jpeg|png|gif)$/i.test(msg.fileName)) fileIcon = '🖼️';
        else if (/\.(pdf)$/i.test(msg.fileName)) fileIcon = '📕';
        else if (/\.(doc|docx)$/i.test(msg.fileName)) fileIcon = '📘';
        else if (/\.(xls|xlsx)$/i.test(msg.fileName)) fileIcon = '📗';
        else if (/\.(zip|rar)$/i.test(msg.fileName)) fileIcon = '📦';

        attachmentHtml = `
            <a href="${msg.filePath}" target="_blank" class="attachment-link">
                <span>${fileIcon}</span>
                <strong>${msg.fileName}</strong>
            </a>
        `;
    }

    // Display sender's name in group chats if received
    const senderHtml = (!isSent && activeChat && activeChat.type === 'group') 
        ? `<div class="message-sender">${msg.senderDisplayName}</div>` 
        : '';

    wrapper.innerHTML = `
        ${senderHtml}
        <div class="message-bubble">
            ${msg.text ? `<div>${escapeHTML(msg.text)}</div>` : ''}
            ${attachmentHtml}
        </div>
        <div class="message-time">${timeStr}</div>
    `;

    pane.appendChild(wrapper);
}

function sendMessage() {
    const input = document.getElementById('chatInput');
    const text = input.value.trim();

    if (!text || !activeChat) return;

    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        if (activeChat.type === 'direct') {
            connection.invoke("SendDirectMessage", activeChat.id, text, null, null)
                .catch(err => console.error(err));
        } else {
            connection.invoke("SendGroupMessage", activeChat.id, text, null, null)
                .catch(err => console.error(err));
        }
        input.value = '';
        
        // Stop typing indicator instantly
        isCurrentlyTyping = false;
        sendTypingStatus(false);
    } else {
        alert("Desconectado del servidor local. Reintentando...");
    }
}

async function handleFileUpload(e) {
    const files = e.target.files;
    if (files.length === 0 || !activeChat) return;

    const file = files[0];
    const formData = new FormData();
    formData.append('file', file);

    const pane = document.getElementById('chatMessages');
    const loadingMessage = document.createElement('div');
    loadingMessage.style.alignSelf = 'center';
    loadingMessage.style.color = 'var(--text-secondary)';
    loadingMessage.style.fontSize = '0.85rem';
    loadingMessage.id = 'file-uploading-indicator';
    loadingMessage.textContent = `Subiendo "${file.name}" a la red local...`;
    pane.appendChild(loadingMessage);
    scrollToBottom();

    try {
        const response = await fetch('/api/chat/upload', {
            method: 'POST',
            body: formData
        });

        document.getElementById('file-uploading-indicator')?.remove();

        if (response.ok) {
            const data = await response.json();
            // Send message with file metadata via SignalR
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                if (activeChat.type === 'direct') {
                    connection.invoke("SendDirectMessage", activeChat.id, "", data.fileName, data.filePath)
                        .catch(err => console.error(err));
                } else {
                    connection.invoke("SendGroupMessage", activeChat.id, "", data.fileName, data.filePath)
                        .catch(err => console.error(err));
                }
            }
        } else {
            const error = await response.json();
            alert(`Error al subir: ${error.message}`);
        }
    } catch (err) {
        document.getElementById('file-uploading-indicator')?.remove();
        console.error("Upload fail", err);
        alert("Error de red al subir el archivo.");
    }

    // Reset file input
    e.target.value = '';
}

function handleReceivedMessage(msg) {
    // Dispara la notificación del sistema si corresponde
    triggerNotification(msg);

    if (!activeChat) {
        // Not looking at any chat, increment badge
        incrementUnread(msg);
        return;
    }

    const matchesDirect = activeChat.type === 'direct' && 
        ((msg.senderId === activeChat.id && msg.receiverId === currentUser.id) || 
         (msg.senderId === currentUser.id && msg.receiverId === activeChat.id));

    const matchesGroup = activeChat.type === 'group' && msg.groupId === activeChat.id;

    if (matchesDirect || matchesGroup) {
        appendSingleMessage(msg);
        scrollToBottom();
    } else {
        // Message is for another chat, increment badge
        incrementUnread(msg);
    }
}

function triggerNotification(msg) {
    if (!currentUser || msg.senderId === currentUser.id) return;

    let isCurrentChat = false;
    if (activeChat) {
        const matchesDirect = activeChat.type === 'direct' && 
            ((msg.senderId === activeChat.id && msg.receiverId === currentUser.id) || 
             (msg.senderId === currentUser.id && msg.receiverId === activeChat.id));
        const matchesGroup = activeChat.type === 'group' && msg.groupId === activeChat.id;
        isCurrentChat = matchesDirect || matchesGroup;
    }

    const title = msg.groupId ? `${msg.senderDisplayName} (Grupo)` : msg.senderDisplayName;
    const body = msg.text || "📎 Envió un archivo adjunto.";

    // Notificar si la app está en segundo plano (oculta), no tiene el foco del teclado, o es para otro chat
    if (document.hidden || !document.hasFocus() || !isCurrentChat) {
        if (window.chrome && window.chrome.webview && typeof window.chrome.webview.postMessage === 'function') {
            window.chrome.webview.postMessage({
                type: 'notification',
                title: title,
                body: body
            });
        }

        if ("Notification" in window && Notification.permission === "granted") {
            new Notification(title, {
                body: body,
                icon: "favicon.ico"
            });
        }

        showToast(title, body);
    }
}

function showToast(title, body) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.innerHTML = `
        <div class="toast-title">${escapeHTML(title)}</div>
        <div class="toast-body">${escapeHTML(body)}</div>
    `;

    toast.onclick = () => {
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    };

    container.appendChild(toast);
    setTimeout(() => {
        if (toast.parentElement) {
            toast.style.opacity = '0';
            setTimeout(() => { if (toast.parentElement) toast.remove(); }, 300);
        }
    }, 5000);
}

function incrementUnread(msg) {
    // Determine badge ID
    let badgeId = '';
    if (msg.groupId) {
        badgeId = `unread-group-${msg.groupId}`;
    } else {
        badgeId = `unread-${msg.senderId}`;
    }

    const badge = document.getElementById(badgeId);
    if (badge) {
        badge.style.display = 'block';
        const currentCount = parseInt(badge.textContent || '0');
        badge.textContent = currentCount + 1;
    }
}

function handleUserTyping(senderId, isTyping) {
    if (activeChat && activeChat.type === 'direct' && activeChat.id === senderId) {
        const indicator = document.getElementById('chatTypingIndicator');
        if (indicator) {
            indicator.textContent = "está escribiendo...";
            indicator.style.display = isTyping ? 'block' : 'none';
        }
    }
}

function handleGroupTyping(groupName, senderId, senderName, isTyping) {
    if (activeChat && activeChat.type === 'group' && activeChat.id === groupName) {
        const indicator = document.getElementById('chatTypingIndicator');
        if (indicator) {
            indicator.textContent = `${senderName} está escribiendo...`;
            indicator.style.display = isTyping ? 'block' : 'none';
        }
    }
}

function scrollToBottom() {
    const pane = document.getElementById('chatMessages');
    if (pane) {
        pane.scrollTop = pane.scrollHeight;
    }
}

function escapeHTML(str) {
    return str.replace(/[&<>'"]/g, 
        tag => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            "'": '&#39;',
            '"': '&quot;'
        }[tag] || tag)
    );
}
