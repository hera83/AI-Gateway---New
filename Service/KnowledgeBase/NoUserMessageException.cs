namespace AiGateway.Service.KnowledgeBase;

public class NoUserMessageException()
    : Exception("Messages must include at least one message with role 'user' to use as the retrieval query.");
