using CommunityToolkit.Mvvm.Messaging.Messages;

namespace InfinityMercsApp.Messages;

public sealed class SplashCompletedMessage : ValueChangedMessage<bool>
{
    public SplashCompletedMessage() : base(true) { }
}
