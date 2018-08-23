using System;
using MimeKit;

namespace UI.Test.Common.Utilities
{
    /// <summary>
    /// A generic email container. Meant to be used to avoid adding extra references required for using <see cref="MimeMessage"/>.
    /// </summary>
    public class MimeMessageWrapper
    {
        internal MimeMessageWrapper(MimeMessage wrappedMessage)
        {
            WrappedMessage = wrappedMessage;
            From = WrappedMessage.From[0].ToString();
            To = WrappedMessage.To[0].ToString();
            Subject = WrappedMessage.Subject;
            HtmlBody = WrappedMessage.HtmlBody;
            Date = WrappedMessage.Date;
        }

        public string From { get; }

        public string To { get; }

        public string Subject { get; }

        public string HtmlBody { get; }

        public DateTimeOffset Date { get; }

        public MimeMessage WrappedMessage { get; }
    }
}
