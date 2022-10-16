
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Notifications.Interfaces;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Notifications.Models;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Notifications.Interfaces;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Notifications;

internal class NotificationPublisher
    : INotificationPublisher
{
    // Fields
    private readonly INotificationPublisherInternal _notificationPublisherInternal;

    // Constructors
    internal NotificationPublisher(INotificationPublisherInternal notificationPublisherInternal)
    {
        _notificationPublisherInternal = notificationPublisherInternal;
    }

    // Public Methods
    public Task PublishNotificationAsync(Notification notification, CancellationToken cancellationToken)
    {
        return _notificationPublisherInternal.PublishAsync(notification, cancellationToken);
    }
}
