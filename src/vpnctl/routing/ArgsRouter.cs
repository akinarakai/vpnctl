public class ArgsRouter
{
    private readonly List<Func<IHandler>> _handlers = new();

    public void AddHandler(Func<IHandler> handlerFactory)
    {
        _handlers.Add(handlerFactory);
    }

    public bool Route(InputContext input)
    {
        foreach (var handlerFactory in _handlers)
        {
            var handler = handlerFactory();

            if (handler.CanHandle(input))
            {
                handler.Handle(input);
                return true;
            }
        }

        return false;
    }
}