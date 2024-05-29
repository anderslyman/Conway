using System.Runtime.Serialization;

namespace Conway.Library.Exceptions
{
    [Serializable]
    internal class EndStateNotReached : Exception
    {
        public EndStateNotReached()
        {
        }

        public EndStateNotReached(string? message) : base(message)
        {
        }

        public EndStateNotReached(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected EndStateNotReached(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
