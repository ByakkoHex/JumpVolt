using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SklepMotoryzacyjny.Models;

namespace SklepMotoryzacyjny.Services
{
    public static class InvoiceEmailService
    {
        public static async Task SendInvoiceAsync(Invoice invoice, byte[] pdfBytes, AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SmtpHost))
                throw new InvalidOperationException("Nie skonfigurowano serwera SMTP. Przejdź do Ustawień.");

            if (string.IsNullOrWhiteSpace(invoice.BuyerEmail))
                throw new InvalidOperationException("Brak adresu e-mail nabywcy.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                string.IsNullOrEmpty(settings.SmtpFromName) ? settings.CompanyName : settings.SmtpFromName,
                settings.SmtpFromEmail));
            message.To.Add(new MailboxAddress(invoice.BuyerName, invoice.BuyerEmail));
            message.Subject = $"Faktura {invoice.InvoiceNumber} — {settings.CompanyName}";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
<html><body style='font-family: Arial, sans-serif; color: #333;'>
<h2 style='color: #1565C0;'>Faktura {invoice.InvoiceNumber}</h2>
<p>Szanowni Państwo,</p>
<p>W załączeniu przesyłamy fakturę VAT nr <strong>{invoice.InvoiceNumber}</strong>
z dnia {invoice.DateDisplay} na kwotę <strong>{invoice.TotalGross:N2} PLN brutto</strong>.</p>
{(invoice.DueDate.HasValue ? $"<p>Termin płatności: <strong>{invoice.DueDateDisplay}</strong></p>" : "")}
<p>Metoda płatności: {invoice.PaymentMethod}</p>
<br/>
<p>Z poważaniem,<br/><strong>{settings.CompanyName}</strong><br/>
{settings.CompanyAddress}, {settings.CompanyCity}<br/>
{(string.IsNullOrEmpty(settings.CompanyPhone) ? "" : $"Tel: {settings.CompanyPhone}")}</p>
</body></html>";

            var safeName = invoice.InvoiceNumber.Replace("/", "-").Replace("\\", "-");
            bodyBuilder.Attachments.Add(
                $"Faktura_{safeName}.pdf",
                pdfBytes,
                new ContentType("application", "pdf"));

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            var secureOption = settings.SmtpUseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;
            await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, secureOption);
            if (!string.IsNullOrEmpty(settings.SmtpUser))
                await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public static async Task TestConnectionAsync(AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SmtpHost))
                throw new InvalidOperationException("Brak adresu serwera SMTP.");

            using var client = new SmtpClient();
            var secureOption = settings.SmtpUseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;
            await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, secureOption);
            if (!string.IsNullOrEmpty(settings.SmtpUser))
                await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword);
            await client.DisconnectAsync(true);
        }
    }
}
