using MailKit;
using MailKit.Net.Imap;
using System;
using System.Diagnostics;
using System.Linq;

namespace UI.Test.Common.Utilities
{
    public class EmailUtility
    {
        private readonly ImapClient _client;

        private IMailFolder Inbox => _client.Inbox;

        private EmailUtility(string username, string password, string imapServer)
        {
            _client = new ImapClient();
            _client.Connect(imapServer, 993, true);
            // Note: since we don't have an OAuth2 token, disable
            // the XOAUTH2 authentication mechanism
            _client.AuthenticationMechanisms.Remove("XOAUTH2");
            _client.Authenticate(username, password);
        }

        public MimeMessageWrapper GetInboxMessage(int index) => new MimeMessageWrapper(Inbox.GetMessage(index));

        public MimeMessageWrapper WaitForInboxMessage(Predicate<MimeMessageWrapper> predicate, TimeSpan timeout)
        {
            const int numMessagesToTake = 10;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                while (stopWatch.Elapsed < timeout)
                {
                    if (!_client.Inbox.IsOpen)
                        _client.Inbox.Open(FolderAccess.ReadWrite);
                    var obj = Inbox.Fetch(Inbox.Count - 1 - numMessagesToTake, -1,
                            MessageSummaryItems.Flags | MessageSummaryItems.UniqueId)
                        .Where(summary => !summary.Flags.Value.HasFlag(MessageFlags.Seen))
                        .Select(summary => new
                        {
                            Message = GetInboxMessage(summary.Index),
                            Id = summary.UniqueId
                        })
                        .FirstOrDefault(o => predicate(o.Message));
                    if (obj != null)
                    {
                        Inbox.SetFlags(obj.Id, MessageFlags.Seen, false);
                        return obj.Message;
                    }

                    Inbox.Check();
                }
            }
            // Connection can intermittently fail due to IMAP server refusal.
            catch (ImapCommandException e)
            {
                // Log the failed connection but keep retrying.
                Logger.Debug(e.Message);
            }
            finally
            {
                stopWatch.Stop();
            }
            throw new TimeoutException("Cannot find messages matching the specified predicate.");
        }

        public static EmailUtility Authenticate(string username, string password, string imapServer) =>
            new EmailUtility(username, password, imapServer);

        ~EmailUtility()
        {
            _client.Disconnect(true);
            _client.Dispose();
        }
    }
}
