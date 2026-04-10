using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using SklepMotoryzacyjny.Models;

namespace SklepMotoryzacyjny.Services
{
    /// <summary>
    /// Serwis komunikacji z kasą fiskalną Novitus Nano Online.
    /// Obsługuje połączenie przez port COM (USB) oraz TCP/IP (sieć).
    /// 
    /// Protokół Novitus:
    /// ESC P - rozpoczęcie komendy
    /// Dane komendy  
    /// ESC \ - zakończenie komendy
    /// 
    /// Stawki VAT: A=23%, B=8%, C=5%, D=0%
    /// </summary>
    public class NovitusFiscalService : IDisposable
    {
        private SerialPort? _serialPort;
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private bool _isConnected;
        private string _connectionType = "COM"; // "COM" lub "IP"
        private readonly object _lockObject = new();

        private const byte ESC = 0x1B;
        private const byte ACK = 0x06;
        private const byte NAK = 0x15;

        public bool IsConnected => _isConnected;
        public string LastError { get; private set; } = string.Empty;
        public string ConnectionInfo { get; private set; } = string.Empty;

        // ====================================================
        // POŁĄCZENIE COM (USB)
        // ====================================================

        /// <summary>Łączy przez port COM (USB).</summary>
        public bool Connect(string portName, int baudRate = 9600)
        {
            try
            {
                lock (_lockObject)
                {
                    Disconnect();
                    _connectionType = "COM";

                    _serialPort = new SerialPort
                    {
                        PortName = portName,
                        BaudRate = baudRate,
                        DataBits = 8,
                        StopBits = StopBits.One,
                        Parity = Parity.None,
                        ReadTimeout = 5000,
                        WriteTimeout = 5000,
                        Encoding = Encoding.GetEncoding("windows-1250"),
                        Handshake = Handshake.None,
                        DtrEnable = true,
                        RtsEnable = true
                    };

                    _serialPort.Open();
                    _isConnected = true;
                    ConnectionInfo = $"COM: {portName} @ {baudRate}";
                    LastError = string.Empty;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LastError = $"Błąd COM: {ex.Message}";
                _isConnected = false;
                return false;
            }
        }

        // ====================================================
        // POŁĄCZENIE TCP/IP (SIEĆ)
        // ====================================================

        /// <summary>Łączy przez TCP/IP (sieć LAN/WiFi).</summary>
        public bool ConnectTcp(string ipAddress, int port)
        {
            try
            {
                lock (_lockObject)
                {
                    Disconnect();
                    _connectionType = "IP";

                    _tcpClient = new TcpClient();
                    
                    // Timeout połączenia 5s
                    var connectTask = _tcpClient.ConnectAsync(ipAddress, port);
                    if (!connectTask.Wait(5000))
                    {
                        _tcpClient.Dispose();
                        _tcpClient = null;
                        LastError = $"Timeout łączenia z {ipAddress}:{port}";
                        return false;
                    }

                    _networkStream = _tcpClient.GetStream();
                    _networkStream.ReadTimeout = 5000;
                    _networkStream.WriteTimeout = 5000;
                    
                    _isConnected = true;
                    ConnectionInfo = $"TCP: {ipAddress}:{port}";
                    LastError = string.Empty;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LastError = $"Błąd TCP/IP: {ex.Message}";
                _isConnected = false;
                return false;
            }
        }

        /// <summary>Łączy automatycznie na podstawie ustawień.</summary>
        public bool ConnectFromSettings(AppSettings settings)
        {
            if (settings.FiscalConnectionType == "IP")
                return ConnectTcp(settings.FiscalIpAddress, settings.FiscalIpPort);
            else
                return Connect(settings.FiscalPrinterPort, settings.FiscalPrinterBaudRate);
        }

        /// <summary>Zamyka połączenie.</summary>
        public void Disconnect()
        {
            try
            {
                _networkStream?.Close();
                _networkStream?.Dispose();
                _networkStream = null;

                _tcpClient?.Close();
                _tcpClient?.Dispose();
                _tcpClient = null;

                if (_serialPort?.IsOpen == true)
                    _serialPort.Close();
                _serialPort?.Dispose();
                _serialPort = null;

                _isConnected = false;
            }
            catch { }
        }

        /// <summary>Lista portów COM w systemie.</summary>
        public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

        /// <summary>Testuje połączenie.</summary>
        public bool TestConnection()
        {
            try
            {
                if (!EnsureConnected()) return false;

                // Wyślij ENQ
                var command = BuildCommand("e");
                SendBytes(command);
                Thread.Sleep(300);

                var response = ReadAvailableBytes();
                if (response.Length > 0) return true;

                // Alternatywny ENQ
                SendBytes(new byte[] { 0x05 });
                Thread.Sleep(300);
                response = ReadAvailableBytes();
                if (response.Length > 0) return true;

                LastError = "Kasa nie odpowiada. Sprawdź połączenie.";
                return false;
            }
            catch (Exception ex)
            {
                LastError = $"Błąd testu: {ex.Message}";
                return false;
            }
        }

        /// <summary>Drukuje paragon fiskalny.</summary>
        public bool PrintReceipt(List<SaleItem> items, string paymentMethod, decimal paidAmount)
        {
            try
            {
                if (!EnsureConnected()) return false;
                if (items.Count == 0) { LastError = "Brak pozycji."; return false; }

                // 1. Otwarcie paragonu
                if (!SendAndWaitForAck(BuildCommand("$h")))
                { LastError = "Nie udało się otworzyć paragonu."; return false; }
                Thread.Sleep(100);

                // 2. Pozycje
                foreach (var item in items)
                {
                    string name = SanitizeName(item.ProductName, 40);
                    string qty = item.Quantity.ToString("F3");
                    string price = item.UnitPrice.ToString("F2");
                    string total = item.TotalPrice.ToString("F2");
                    string vat = item.VatRate;

                    string lineData = $"$l{name}\r{qty}*{price}\r{vat}\r{total}\r\r";
                    if (!SendAndWaitForAck(BuildCommand(lineData)))
                    {
                        CancelReceipt();
                        LastError = $"Błąd pozycji: {item.ProductName}";
                        return false;
                    }
                    Thread.Sleep(50);
                }

                // 3. Zamknięcie (płatność)
                int payType = paymentMethod switch { "Karta" => 1, "Przelew" => 4, _ => 0 };
                decimal total2 = items.Sum(i => i.TotalPrice);
                string pay = (payType == 0 ? paidAmount : total2).ToString("F2");
                
                if (!SendAndWaitForAck(BuildCommand($"$e{payType}\r{pay}\r")))
                { LastError = "Błąd zamykania paragonu."; return false; }

                LastError = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                LastError = $"Błąd drukowania: {ex.Message}";
                try { CancelReceipt(); } catch { }
                return false;
            }
        }

        /// <summary>Anuluje otwarty paragon.</summary>
        public bool CancelReceipt()
        {
            try
            {
                if (!EnsureConnected()) return false;
                SendBytes(BuildCommand("$a"));
                Thread.Sleep(200);
                return true;
            }
            catch (Exception ex) { LastError = $"Błąd anulowania: {ex.Message}"; return false; }
        }

        /// <summary>Raport X (podglądowy).</summary>
        public bool PrintXReport()
        {
            if (!EnsureConnected()) return false;
            if (!SendAndWaitForAck(BuildCommand("$X")))
            { LastError = "Błąd raportu X."; return false; }
            return true;
        }

        /// <summary>Raport Z (zerujący, koniec dnia). NIEODWRACALNY!</summary>
        public bool PrintZReport()
        {
            if (!EnsureConnected()) return false;
            if (!SendAndWaitForAck(BuildCommand("$Z")))
            { LastError = "Błąd raportu Z."; return false; }
            return true;
        }

        /// <summary>
        /// Drukuje dokument niefiskalny (testowy) — nie jest zapisywany do pamięci fiskalnej.
        /// Używany do testów drukarki i wydruków informacyjnych.
        ///
        /// Protokół Novitus: #n (otwarcie) → #p{linia} (linia) → #k (zamknięcie).
        /// </summary>
        public bool PrintNonFiscalDocument(List<string> lines)
        {
            try
            {
                if (!EnsureConnected()) return false;

                // Otwórz dokument niefiskalny
                if (!SendAndWaitForAck(BuildCommand("#n")))
                { LastError = "Nie udało się otworzyć dokumentu niefiskalnego."; return false; }
                Thread.Sleep(100);

                // Drukuj linie
                foreach (var line in lines)
                {
                    var sanitized = SanitizeName(line, 48);
                    if (!SendAndWaitForAck(BuildCommand($"#p{sanitized}")))
                    {
                        // Próbuj zamknąć mimo błędu
                        try { SendAndWaitForAck(BuildCommand("#k")); } catch { }
                        LastError = $"Błąd drukowania linii: {sanitized}";
                        return false;
                    }
                    Thread.Sleep(30);
                }

                // Zamknij dokument niefiskalny
                if (!SendAndWaitForAck(BuildCommand("#k")))
                { LastError = "Błąd zamykania dokumentu niefiskalnego."; return false; }

                LastError = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                LastError = $"Błąd wydruku niefiskalnego: {ex.Message}";
                return false;
            }
        }

        /// <summary>Otwiera szufladę kasową.</summary>
        public bool OpenCashDrawer()
        {
            try
            {
                if (!EnsureConnected()) return false;
                SendBytes(new byte[] { ESC, (byte)'p', 25, 250 });
                return true;
            }
            catch (Exception ex) { LastError = $"Błąd szuflady: {ex.Message}"; return false; }
        }

        // ====================================================
        // TRANSPORT - wspólna warstwa COM / TCP
        // ====================================================

        private void SendBytes(byte[] data)
        {
            lock (_lockObject)
            {
                if (_connectionType == "IP" && _networkStream != null)
                {
                    _networkStream.Write(data, 0, data.Length);
                    _networkStream.Flush();
                }
                else if (_serialPort?.IsOpen == true)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.Write(data, 0, data.Length);
                }
                else
                    throw new InvalidOperationException("Brak aktywnego połączenia.");
            }
        }

        private byte[] ReadAvailableBytes()
        {
            lock (_lockObject)
            {
                if (_connectionType == "IP" && _networkStream != null)
                {
                    if (!_networkStream.DataAvailable) return Array.Empty<byte>();
                    var buf = new byte[1024];
                    int read = _networkStream.Read(buf, 0, buf.Length);
                    return buf[..read];
                }
                else if (_serialPort?.IsOpen == true)
                {
                    if (_serialPort.BytesToRead == 0) return Array.Empty<byte>();
                    var buf = new byte[_serialPort.BytesToRead];
                    _serialPort.Read(buf, 0, buf.Length);
                    return buf;
                }
                return Array.Empty<byte>();
            }
        }

        private byte[] BuildCommand(string data)
        {
            var encoding = Encoding.GetEncoding("windows-1250");
            var dataBytes = encoding.GetBytes(data);
            var command = new List<byte>();
            command.Add(ESC);
            command.Add((byte)'P');
            command.AddRange(dataBytes);
            command.Add(ESC);
            command.Add((byte)'\\');
            return command.ToArray();
        }

        private bool SendAndWaitForAck(byte[] command, int timeoutMs = 3000)
        {
            SendBytes(command);
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
            {
                Thread.Sleep(50);
                var response = ReadAvailableBytes();
                if (response.Any(b => b == ACK)) return true;
                if (response.Any(b => b == NAK)) { LastError = "Kasa odrzuciła komendę (NAK)."; return false; }
            }
            LastError = "Brak odpowiedzi (timeout).";
            return false;
        }

        private bool EnsureConnected()
        {
            if (_connectionType == "IP" && _tcpClient?.Connected == true && _networkStream != null) return true;
            if (_connectionType == "COM" && _serialPort?.IsOpen == true) return true;
            LastError = "Brak połączenia z kasą. Sprawdź ustawienia.";
            return false;
        }

        private static string SanitizeName(string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(name)) return "PRODUKT";
            var sanitized = new string(name.Where(c => !char.IsControl(c)).ToArray());
            return sanitized.Length > maxLength ? sanitized[..maxLength].Trim() : sanitized.Trim();
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }
    }
}
