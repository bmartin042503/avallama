namespace avallama.Constants;

public enum InteractionType
{
    RestartProcess
}

public class ViewInteraction
{
    public InteractionType InteractionType { get; }

    public ViewInteraction(InteractionType interactionType)
    {
        InteractionType = interactionType;
    }
}