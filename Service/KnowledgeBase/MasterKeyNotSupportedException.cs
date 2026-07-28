namespace AiGateway.Service.KnowledgeBase;

public class MasterKeyNotSupportedException()
    : Exception("The master API key cannot own Knowledge Base data. Issue a dedicated API key to use this feature.");
