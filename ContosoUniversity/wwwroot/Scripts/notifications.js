(function() {
    'use strict';

    var notificationSystem = {
        container: null,
        checkInterval: 5000,
        maxNotifications: 5,

        init: function() {
            this.container = document.createElement('div');
            this.container.className = 'notification-container';
            document.body.appendChild(this.container);
            this.checkForNotifications();
            window.setInterval(this.checkForNotifications.bind(this), this.checkInterval);
        },

        checkForNotifications: function() {
            fetch('/Notifications/GetNotifications', {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin'
            })
                .then(function(response) {
                    if (!response.ok) {
                        throw new Error('Network response was not ok');
                    }
                    return response.json();
                })
                .then(function(data) {
                    if (data.success && Array.isArray(data.notifications)) {
                        data.notifications.forEach(this.showNotification.bind(this));
                    }
                }.bind(this))
                .catch(function(error) {
                    console.log('Error fetching notifications:', error);
                });
        },

        showNotification: function(notification) {
            var element = document.createElement('div');
            var closeButton = document.createElement('button');
            var title = document.createElement('div');
            var message = document.createElement('div');
            var time = document.createElement('div');

            var type = notification.operation === 'CREATE'
                ? 'success'
                : notification.operation === 'DELETE' ? 'warning' : 'info';
            element.className = 'notification notification-' + type;

            closeButton.className = 'notification-close';
            closeButton.type = 'button';
            closeButton.textContent = '×';
            closeButton.addEventListener('click', function() {
                this.closeNotification(element);
            }.bind(this));

            title.className = 'notification-title';
            title.textContent = notification.operation + ' - ' + notification.entityType;
            message.className = 'notification-message';
            message.textContent = notification.message;
            time.className = 'notification-time';
            time.textContent = 'By ' + notification.createdBy + ' • ' +
                this.getTimeAgo(new Date(notification.createdAt));

            element.append(closeButton, title, message, time);
            this.container.appendChild(element);
            window.setTimeout(function() {
                element.classList.add('show');
            }, 100);
            window.setTimeout(function() {
                this.closeNotification(element);
            }.bind(this), 60000);

            this.limitNotifications();
        },

        closeNotification: function(element) {
            if (element.classList.contains('closing')) {
                return;
            }
            element.classList.add('closing');
            element.classList.remove('show');
            window.setTimeout(function() {
                element.remove();
            }, 300);
        },

        limitNotifications: function() {
            var notifications = this.container.querySelectorAll('.notification:not(.closing)');
            while (notifications.length > this.maxNotifications) {
                this.closeNotification(notifications[0]);
                notifications = this.container.querySelectorAll('.notification:not(.closing)');
            }
        },

        getTimeAgo: function(date) {
            var seconds = Math.floor((Date.now() - date.getTime()) / 1000);
            if (seconds < 60) {
                return 'just now';
            }
            if (seconds < 3600) {
                var minutes = Math.floor(seconds / 60);
                return minutes + ' minute' + (minutes > 1 ? 's' : '') + ' ago';
            }
            if (seconds < 86400) {
                var hours = Math.floor(seconds / 3600);
                return hours + ' hour' + (hours > 1 ? 's' : '') + ' ago';
            }
            var days = Math.floor(seconds / 86400);
            return days + ' day' + (days > 1 ? 's' : '') + ' ago';
        }
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            notificationSystem.init();
        });
    } else {
        notificationSystem.init();
    }

    window.NotificationSystem = notificationSystem;
})();
