using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Tests;

public class NotificationServiceTests
{
    [Fact]
    public void ReceiveNotificationReturnsPendingNotificationAndMarksItRead()
    {
        using var context = CreateContext();
        var service = new NotificationService(context);

        service.SendNotification("Student", "42", "A. Student", EntityOperation.CREATE, "Tester");

        var notification = service.ReceiveNotification();

        Assert.NotNull(notification);
        Assert.Equal("Student", notification.EntityType);
        Assert.Equal("42", notification.EntityId);
        Assert.Equal("CREATE", notification.Operation);
        Assert.Equal("New Student 'A. Student' has been created", notification.Message);
        Assert.Equal("Tester", notification.CreatedBy);
        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAt);
        Assert.Null(service.ReceiveNotification());
    }

    [Fact]
    public void MarkAsReadMarksNotificationWithoutRemovingIt()
    {
        using var context = CreateContext();
        var service = new NotificationService(context);
        service.SendNotification("Course", "1050", EntityOperation.UPDATE);
        var notificationId = context.Notifications.Single().Id;

        service.MarkAsRead(notificationId);

        Assert.True(context.Notifications.Single().IsRead);
        Assert.NotNull(context.Notifications.Single().ReadAt);
        Assert.Null(service.ReceiveNotification());
    }

    private static SchoolContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SchoolContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SchoolContext(options);
    }
}
