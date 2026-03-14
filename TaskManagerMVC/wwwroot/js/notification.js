/**
 * Notification System
 * Handles polling for new notifications and updating the UI
 */

const NotificationSystem = {
    // Configuration
    config: {
        pollInterval: 30000, // 30 seconds
        endpoints: {
            admin: {
                count: '/Admin/GetUnreadNotificationCount',
                list: '/Admin/GetRecentNotifications',
                read: '/Admin/MarkNotificationRead'
            },
            manager: {
                count: '/Manager/GetUnreadNotificationCount',
                list: '/Manager/GetRecentNotifications',
                read: '/Manager/MarkNotificationRead'
            },
            employee: {
                count: '/Employee/GetUnreadNotificationCount',
                list: '/Employee/GetRecentNotifications',
                read: '/Employee/MarkNotificationRead'
            }
        },
        selectors: {
            badge: '.notification-badge',
            list: '.notification-list',
            dropdown: '.notification-dropdown'
        }
    },

    // State
    state: {
        userRole: null, // 'Admin' or 'Employee'
        timer: null
    },

    // Initialize
    init: function () {
        // Determine user role from URL or meta tag (simplified: checking current URL path)
        const path = window.location.pathname;
        if (path.startsWith('/Admin')) {
            this.state.userRole = 'admin';
        } else if (path.startsWith('/Manager')) {
            this.state.userRole = 'manager';
        } else if (path.startsWith('/Employee')) {
            this.state.userRole = 'employee';
        } else {
            // Default check (can be improved)
            // If neither, we might be on a shared page, try to probe or just default?
            // For now, let's assume if we see admin links, we are admin
            if (document.querySelector('a[href^="/Admin"]')) {
                this.state.userRole = 'admin';
            } else if (document.querySelector('a[href^="/Manager"]')) {
                this.state.userRole = 'manager';
            } else {
                this.state.userRole = 'employee';
            }
        }

        this.startPolling();
        console.log('Notification system initialized for role:', this.state.userRole);

        // Bind events
        const dropdown = document.querySelector(this.config.selectors.dropdown);
        if (dropdown) {
            dropdown.addEventListener('click', (e) => {
                e.stopPropagation(); // Keep dropdown open when clicking inside
            });
        }

        // Load immediately
        this.loadNotifications();
    },

    // Start polling
    startPolling: function () {
        this.loadNotifications();
        this.state.timer = setInterval(() => this.loadNotifications(), this.config.pollInterval);
    },

    // Load notifications
    loadNotifications: async function () {
        if (!this.state.userRole) return;

        const endpoints = this.config.endpoints[this.state.userRole];

        try {
            // Get count
            const countResponse = await fetch(endpoints.count);
            const countData = await countResponse.json();
            this.updateBadge(countData.count);

            // Get list
            const listResponse = await fetch(endpoints.list);
            const listData = await listResponse.json();
            this.updateList(listData);

        } catch (error) {
            console.error('Error loading notifications:', error);
        }
    },

    // Update badge
    updateBadge: function (count) {
        const badge = document.querySelector(this.config.selectors.badge);
        if (badge) {
            badge.textContent = count;
            if (count > 0) {
                badge.style.display = 'inline-block';
                badge.classList.remove('bg-secondary');
                badge.classList.add('bg-danger');
            } else {
                badge.style.display = 'none';
            }
        }
    },

    // Update list
    updateList: function (notifications) {
        const container = document.querySelector(this.config.selectors.list);
        if (!container) return;

        if (!notifications || notifications.length === 0) {
            container.innerHTML = `
                <div class="text-center py-4 text-muted">
                    <i class="bi bi-bell-slash fs-3 d-block mb-2"></i>
                    <small>No new notifications</small>
                </div>
            `;
            return;
        }

        const html = notifications.map(n => this.renderNotification(n)).join('');
        container.innerHTML = html;

        // Re-bind mark as read buttons
        // Note: Using onclick in HTML for simplicity in this context
    },

    // Render single notification item
    renderNotification: function (n) {
        const iconInfo = this.getIconForType(n.type);
        const timeAgo = this.timeAgo(new Date(n.createdAt));
        const bgClass = n.isRead ? 'bg-white' : 'bg-light';
        const readOpacity = n.isRead ? 'opacity-75' : '';

        // Determine base URL and action URL based on role
        let baseUrl = '';
        let actionUrl = '#';

        if (this.state.userRole === 'admin') {
            baseUrl = '/Admin';
        } else if (this.state.userRole === 'manager') {
            baseUrl = '/Manager';
        } else {
            baseUrl = '/Employee';
        }

        if (n.referenceType === 'task' && n.referenceId) {
            actionUrl = `${baseUrl}/TaskDetails/${n.referenceId}`;
        }

        return `
            <div class="notification-item px-3 py-2 border-bottom ${bgClass} ${readOpacity}">
                <div class="d-flex align-items-start">
                    <div class="flex-shrink-0 mt-1">
                        <span class="badge rounded-circle p-2 ${iconInfo.bgClass}">
                            <i class="${iconInfo.icon} text-white"></i>
                        </span>
                    </div>
                    <div class="flex-grow-1 ms-3">
                        <div class="d-flex justify-content-between align-items-start">
                            <h6 class="mb-0 small fw-bold">${n.title}</h6>
                            <small class="text-muted" style="font-size: 0.7rem;">${timeAgo}</small>
                        </div>
                        <p class="mb-1 small text-muted text-truncate" style="max-width: 200px;">${n.message}</p>
                        <div class="d-flex justify-content-between align-items-center mt-1">
                            <a href="${actionUrl}" class="btn btn-link btn-sm p-0 text-decoration-none" style="font-size: 0.75rem;" onclick="NotificationSystem.markAsRead(${n.id}, '${actionUrl}')">View Details</a>
                            ${!n.isRead ? `
                            <button class="btn btn-link btn-sm p-0 text-muted" title="Mark as read" onclick="NotificationSystem.markAsRead(${n.id})">
                                <i class="bi bi-check2-circle"></i>
                            </button>
                            ` : ''}
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    // Helper: Mark as read
    markAsRead: async function (id, redirectUrl) {
        if (!this.state.userRole) return;
        const endpoints = this.config.endpoints[this.state.userRole];

        try {
            // Need verification token for POST if using ValidateAntiForgeryToken
            // For AJAX simplicity often easier if endpoint allows simple POST or GET for read mark
            // Assuming the controller expects FormData or standard POST.
            // Let's try standard fetch POST with form data

            const formData = new FormData();
            formData.append('id', id);

            // Get anti-forgery token if present
            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            const headers = {};
            if (token) {
                headers['RequestVerificationToken'] = token.value;
            }

            await fetch(endpoints.read, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    ...headers
                },
                body: `id=${id}` // Simple url encoded
            });

            // Reload list to update UI
            this.loadNotifications();

            if (redirectUrl) {
                window.location.href = redirectUrl;
            }

        } catch (error) {
            console.error('Error marking as read:', error);
        }
    },

    // Helper: Get icon based on type
    getIconForType: function (type) {
        switch (type) {
            case 'task_assigned':
                return { icon: 'bi-list-task', bgClass: 'bg-primary' };
            case 'task_updated':
                return { icon: 'bi-pencil-square', bgClass: 'bg-info' };
            case 'status_update':
                return { icon: 'bi-arrow-repeat', bgClass: 'bg-warning' };
            case 'deadline_reminder':
                return { icon: 'bi-alarm', bgClass: 'bg-danger' };
            case 'comment_added':
                return { icon: 'bi-chat-dots', bgClass: 'bg-success' };
            case 'registration_request':
                return { icon: 'bi-person-plus', bgClass: 'bg-primary' };
            default:
                return { icon: 'bi-bell', bgClass: 'bg-secondary' };
        }
    },

    // Helper: Time ago string
    timeAgo: function (date) {
        const seconds = Math.floor((new Date() - date) / 1000);
        let interval = seconds / 31536000;
        if (interval > 1) return Math.floor(interval) + "y ago";
        interval = seconds / 2592000;
        if (interval > 1) return Math.floor(interval) + "mo ago";
        interval = seconds / 86400;
        if (interval > 1) return Math.floor(interval) + "d ago";
        interval = seconds / 3600;
        if (interval > 1) return Math.floor(interval) + "h ago";
        interval = seconds / 60;
        if (interval > 1) return Math.floor(interval) + "m ago";
        return Math.floor(seconds) + "s ago";
    }
};

// Start when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    NotificationSystem.init();
});
