﻿using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Notifications;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Notifications.Models;
using System.Collections.Concurrent;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Notifications;

internal class NotificationSubscriber
    : INotificationSubscriber
{
    // Fields
    private readonly ConcurrentQueue<Notification> _notificationCollection;

    // Properties
    public IEnumerable<Notification> NotificationCollection => _notificationCollection.AsEnumerable();

    // Constructors
    internal NotificationSubscriber()
    {
        _notificationCollection = new ConcurrentQueue<Notification>();
    }

    // Public Methods
    public Task HandlerAsync(Notification subject, CancellationToken cancellationToken)
    {
        _notificationCollection.Enqueue(subject);
        return Task.CompletedTask;
    }
}
