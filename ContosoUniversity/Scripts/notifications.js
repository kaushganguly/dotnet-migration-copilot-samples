// Notification System for Admin Users
(function() {
    'use strict';

    var NotificationSystem = {
        container: null,
        notificationCount: 0,
        checkInterval: 5000, // Check every 5 seconds
        maxNotifications: 5,

        init: function() {
            this.createContainer();
            this.startPolling();
        },

        createContainer: function() {
            this.container = document.createElement('div');
            this.container.className = 'notification-container';
            document.body.appendChild(this.container);
        },

        startPolling: function() {
            var self = this;
            // Check immediately
            this.checkForNotifications();
            
            // Then check every interval
            setInterval(function() {
                self.checkForNotifications();
            }, this.checkInterval);
        },

        checkForNotifications: function() {
            var self = this;
            
            fetch('/Notifications/GetNotifications', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                },
                credentials: 'same-origin'
            })
            .then(function(response) {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(function(data) {
                if (data.success && data.notifications && data.notifications.length > 0) {
                    data.notifications.forEach(function(notification) {
                        self.showNotification(notification);
                    });
                }
            })
            .catch(function(error) {
                console.log('Error fetching notifications:', error);
            });
        },

        showNotification: function(notification) {
            var self = this;
            
            // Create notification element
            var notificationEl = document.createElement('div');
            notificationEl.className = 'notification notification-info';
            
            // Determine notification type based on operation
            var type = 'info';
            var operation = notification.operation || notification.Operation;
            var entityType = notification.entityType || notification.EntityType;
            var message = notification.message || notification.Message;
            var createdAt = notification.createdAt || notification.CreatedAt;
            var createdBy = notification.createdBy || notification.CreatedBy;
            if (operation === 'CREATE') {
                type = 'success';
            } else if (operation === 'DELETE') {
                type = 'warning';
            }
            
            notificationEl.className = 'notification notification-' + type;
            
            var timeAgo = this.getTimeAgo(new Date(createdAt));
            
            var closeButton = document.createElement('button');
            closeButton.className = 'notification-close';
            closeButton.type = 'button';
            closeButton.textContent = '×';
            closeButton.addEventListener('click', function() {
                self.closeNotification(closeButton);
            });

            var title = document.createElement('div');
            title.className = 'notification-title';
            title.textContent = operation + ' - ' + entityType;

            var messageText = document.createElement('div');
            messageText.className = 'notification-message';
            messageText.textContent = message;

            var timestamp = document.createElement('div');
            timestamp.className = 'notification-time';
            timestamp.textContent = 'By ' + createdBy + ' • ' + timeAgo;

            notificationEl.append(closeButton, title, messageText, timestamp);
            
            // Add to container
            this.container.appendChild(notificationEl);
            
            // Animate in
            setTimeout(function() {
                notificationEl.classList.add('show');
            }, 100);
            
            // Auto remove after 1 minute (60 seconds)
            setTimeout(function() {
                self.closeNotification(notificationEl.querySelector('.notification-close'));
            }, 60000);
            
            // Limit number of notifications
            this.limitNotifications();
        },

        closeNotification: function(closeButton) {
            var notification = closeButton.parentElement;
            notification.classList.remove('show');
            
            setTimeout(function() {
                if (notification.parentElement) {
                    notification.parentElement.removeChild(notification);
                }
            }, 300);
        },

        limitNotifications: function() {
            var notifications = this.container.querySelectorAll('.notification');
            while (notifications.length > this.maxNotifications) {
                var oldest = notifications[0];
                this.closeNotification(oldest.querySelector('.notification-close'));
                notifications = this.container.querySelectorAll('.notification');
            }
        },

        getTimeAgo: function(date) {
            var now = new Date();
            var diffInSeconds = Math.floor((now - date) / 1000);
            
            if (diffInSeconds < 60) {
                return 'just now';
            } else if (diffInSeconds < 3600) {
                var minutes = Math.floor(diffInSeconds / 60);
                return minutes + ' minute' + (minutes > 1 ? 's' : '') + ' ago';
            } else if (diffInSeconds < 86400) {
                var hours = Math.floor(diffInSeconds / 3600);
                return hours + ' hour' + (hours > 1 ? 's' : '') + ' ago';
            } else {
                var days = Math.floor(diffInSeconds / 86400);
                return days + ' day' + (days > 1 ? 's' : '') + ' ago';
            }
        }
    };

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            NotificationSystem.init();
        });
    } else {
        NotificationSystem.init();
    }

    // Make NotificationSystem globally available for close button
    window.NotificationSystem = NotificationSystem;
})();
