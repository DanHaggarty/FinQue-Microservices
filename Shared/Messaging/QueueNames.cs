namespace Shared.Messaging
{
    public static class QueueNames
    {
        public const string Inbound = "transactions-inbound";
        public const string DeadLetter = "transactions-inbound/$DeadLetterQueue";
        public const string HighRisk = "transactions-highrisk";
        public const string Validated = "transactions-validated";
    }
}