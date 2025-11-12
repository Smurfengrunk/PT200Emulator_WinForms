namespace PT200_InputHandler
{
    public interface IPT200_InputHandler
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}