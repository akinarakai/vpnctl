public interface IHandler
{
    bool CanHandle(InputContext input);
    void Handle(InputContext input);
}