using System.IO;
using Microsoft.Data.Sqlite;
using SklepMotoryzacyjny.Models;

namespace SklepMotoryzacyjny.Services
{
    /// <summary>
    /// Serwis bazy danych SQLite dla JumpVolt.
    /// Obsługuje produkty z atrybutami kategorii, marki, sprzedaż i ustawienia.
    /// </summary>
    public class DatabaseService
    {
        private static readonly Lazy<DatabaseService> _instance = new(() => new DatabaseService());
        public static DatabaseService Instance => _instance.Value;

        private readonly string _connectionString;

        private DatabaseService()
        {
            string dbPath = AppConfigService.GetEffectiveDatabasePath();
            string? dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir))
                Directory.CreateDirectory(dbDir);
            _connectionString = $"Data Source={dbPath}";
        }

        public void Initialize()
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Barcode TEXT DEFAULT '',
                    CatalogNumber TEXT DEFAULT '',
                    CategoryType TEXT DEFAULT 'Inne',
                    Brand TEXT DEFAULT '',
                    PurchasePrice REAL DEFAULT 0,
                    SalePrice REAL NOT NULL,
                    VatRate TEXT DEFAULT 'A',
                    StockQuantity INTEGER DEFAULT 0,
                    MinStockLevel INTEGER DEFAULT 2,
                    Unit TEXT DEFAULT 'szt.',
                    IsActive INTEGER DEFAULT 1,
                    Notes TEXT DEFAULT '',
                    AttributesJson TEXT DEFAULT '{}',
                    ImageFileName TEXT DEFAULT '',
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS Brands (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    CategoryType TEXT DEFAULT ''
                );

                CREATE TABLE IF NOT EXISTS Sales (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ReceiptNumber TEXT NOT NULL,
                    SaleDate TEXT NOT NULL,
                    TotalAmount REAL NOT NULL,
                    PaymentMethod TEXT DEFAULT 'Gotówka',
                    PaidAmount REAL DEFAULT 0,
                    ChangeAmount REAL DEFAULT 0,
                    IsFiscalPrinted INTEGER DEFAULT 0,
                    IsCancelled INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS SaleItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SaleId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    ProductName TEXT NOT NULL,
                    Barcode TEXT DEFAULT '',
                    Quantity INTEGER NOT NULL,
                    UnitPrice REAL NOT NULL,
                    VatRate TEXT DEFAULT 'A',
                    FOREIGN KEY (SaleId) REFERENCES Sales(Id),
                    FOREIGN KEY (ProductId) REFERENCES Products(Id)
                );

                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );

                CREATE TABLE IF NOT EXISTS Returns (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OriginalSaleId INTEGER NOT NULL,
                    OriginalReceiptNumber TEXT NOT NULL,
                    ReturnNumber TEXT NOT NULL,
                    ReturnDate TEXT NOT NULL,
                    TotalAmount REAL NOT NULL,
                    RefundMethod TEXT DEFAULT 'Gotówka',
                    FOREIGN KEY (OriginalSaleId) REFERENCES Sales(Id)
                );

                CREATE TABLE IF NOT EXISTS ReturnItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ReturnId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    ProductName TEXT NOT NULL,
                    Barcode TEXT DEFAULT '',
                    Quantity INTEGER NOT NULL,
                    UnitPrice REAL NOT NULL,
                    VatRate TEXT DEFAULT 'A',
                    FOREIGN KEY (ReturnId) REFERENCES Returns(Id)
                );

                CREATE TABLE IF NOT EXISTS Invoices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceNumber TEXT NOT NULL,
                    InvoiceDate TEXT NOT NULL,
                    DueDate TEXT,
                    SaleId INTEGER,
                    BuyerName TEXT DEFAULT '',
                    BuyerNIP TEXT DEFAULT '',
                    BuyerAddress TEXT DEFAULT '',
                    BuyerCity TEXT DEFAULT '',
                    BuyerEmail TEXT DEFAULT '',
                    PaymentMethod TEXT DEFAULT 'Przelew',
                    PaymentDays INTEGER DEFAULT 14,
                    TotalNet REAL DEFAULT 0,
                    TotalVat REAL DEFAULT 0,
                    TotalGross REAL DEFAULT 0,
                    IsPaid INTEGER DEFAULT 0,
                    IsEmailSent INTEGER DEFAULT 0,
                    Notes TEXT DEFAULT ''
                );

                CREATE TABLE IF NOT EXISTS InvoiceItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Unit TEXT DEFAULT 'szt.',
                    Quantity REAL NOT NULL,
                    UnitPriceNet REAL NOT NULL,
                    VatRate TEXT DEFAULT 'A',
                    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id)
                );

                CREATE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
                CREATE INDEX IF NOT EXISTS idx_products_name ON Products(Name);
                CREATE INDEX IF NOT EXISTS idx_products_category ON Products(CategoryType);
                CREATE INDEX IF NOT EXISTS idx_products_brand ON Products(Brand);
                CREATE INDEX IF NOT EXISTS idx_sales_date ON Sales(SaleDate);
                CREATE INDEX IF NOT EXISTS idx_returns_date ON Returns(ReturnDate);
                CREATE INDEX IF NOT EXISTS idx_invoices_date ON Invoices(InvoiceDate);
                CREATE INDEX IF NOT EXISTS idx_invoices_number ON Invoices(InvoiceNumber);
            ";
            cmd.ExecuteNonQuery();
            
            SeedDefaultBrands(conn);
            SeedServiceProducts(conn);
            MigrateDatabase(conn);
            ImageService.EnsureFolder();
        }

        /// <summary>Dodaje produkty usługowe (Montaż, Dowóz i Montaż) jeśli jeszcze nie istnieją.</summary>
        private static void SeedServiceProducts(SqliteConnection conn)
        {
            var services = new[]
            {
                new { Barcode = "2", Name = "Montaż",                       Notes = "Usługa montażu akumulatora w sklepie" },
                new { Barcode = "3", Name = "Dowóz i Montaż u klienta",     Notes = "Usługa dowozu i montażu akumulatora pod adres klienta" }
            };

            foreach (var svc in services)
            {
                var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM Products WHERE Barcode = @bc";
                checkCmd.Parameters.AddWithValue("@bc", svc.Barcode);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0) continue;

                var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO Products (Name, Barcode, CatalogNumber, CategoryType, Brand,
                        PurchasePrice, SalePrice, VatRate, StockQuantity, MinStockLevel, Unit,
                        IsActive, Notes, AttributesJson, ImageFileName)
                    VALUES (@name, @barcode, '', 'Inne', '',
                        0, 0, 'A', 9999, 0, 'usł.',
                        1, @notes, '{}', '')";
                insertCmd.Parameters.AddWithValue("@name", svc.Name);
                insertCmd.Parameters.AddWithValue("@barcode", svc.Barcode);
                insertCmd.Parameters.AddWithValue("@notes", svc.Notes);
                insertCmd.ExecuteNonQuery();
            }
        }

        /// <summary>Dodaje brakujące kolumny do istniejących baz danych.</summary>
        private static void MigrateDatabase(SqliteConnection conn)
        {
            try
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "ALTER TABLE Products ADD COLUMN ImageFileName TEXT DEFAULT ''";
                cmd.ExecuteNonQuery();
            }
            catch { /* kolumna już istnieje */ }

            try
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "ALTER TABLE Products ADD COLUMN FiscalName TEXT DEFAULT ''";
                cmd.ExecuteNonQuery();
            }
            catch { /* kolumna już istnieje */ }

            try { var cmd = conn.CreateCommand(); cmd.CommandText = "ALTER TABLE Sales ADD COLUMN DiscountPercent REAL DEFAULT 0"; cmd.ExecuteNonQuery(); } catch { }
            try { var cmd = conn.CreateCommand(); cmd.CommandText = "ALTER TABLE Sales ADD COLUMN DiscountAmount REAL DEFAULT 0"; cmd.ExecuteNonQuery(); } catch { }

            try
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Invoices (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        InvoiceNumber TEXT NOT NULL,
                        InvoiceDate TEXT NOT NULL,
                        DueDate TEXT,
                        SaleId INTEGER,
                        BuyerName TEXT DEFAULT '',
                        BuyerNIP TEXT DEFAULT '',
                        BuyerAddress TEXT DEFAULT '',
                        BuyerCity TEXT DEFAULT '',
                        BuyerEmail TEXT DEFAULT '',
                        PaymentMethod TEXT DEFAULT 'Przelew',
                        PaymentDays INTEGER DEFAULT 14,
                        TotalNet REAL DEFAULT 0,
                        TotalVat REAL DEFAULT 0,
                        TotalGross REAL DEFAULT 0,
                        IsPaid INTEGER DEFAULT 0,
                        IsEmailSent INTEGER DEFAULT 0,
                        Notes TEXT DEFAULT ''
                    );
                    CREATE TABLE IF NOT EXISTS InvoiceItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        InvoiceId INTEGER NOT NULL,
                        Name TEXT NOT NULL,
                        Unit TEXT DEFAULT 'szt.',
                        Quantity REAL NOT NULL,
                        UnitPriceNet REAL NOT NULL,
                        VatRate TEXT DEFAULT 'A',
                        FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id)
                    );
                    CREATE INDEX IF NOT EXISTS idx_invoices_date ON Invoices(InvoiceDate);
                    CREATE INDEX IF NOT EXISTS idx_invoices_number ON Invoices(InvoiceNumber);";
                cmd.ExecuteNonQuery();
            }
            catch { /* tabele już istnieją */ }
        }

        private static void SeedDefaultBrands(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Brands";
            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) return;

            string[] brands = {
                "VARTA", "BOSCH", "EXIDE", "BANNER", "YUASA", "CENTRA", "TOPLA", "MOLL",
                "DELPHI", "OPTIMA", "FIAMM", "HELLA",
                "CASTROL", "MOBIL", "SHELL", "TOTAL", "ELF", "MOTUL", "LIQUI MOLY", "VALVOLINE",
                "MANNOL", "ORLEN", "LOTOS", "RAVENOL", "FUCHS",
                "PHILIPS", "OSRAM", "NARVA", "NEOLUX", "TUNGSRAM",
                "SONAX", "K2", "MOJE AUTO", "MA PROFESSIONAL", "BOLL",
                "TELWIN", "DECA", "GYS", "NOCO", "CTEK",
                "WÜRTH", "WD-40", "CRC", "LOCTITE",
                "BOSCH", "VALEO", "HELLA", "MAGNETI MARELLI"
            };

            foreach (var brand in brands.Distinct())
            {
                var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT OR IGNORE INTO Brands (Name) VALUES (@name)";
                insertCmd.Parameters.AddWithValue("@name", brand);
                insertCmd.ExecuteNonQuery();
            }
        }

        private SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // ====================================================
        // MARKI
        // ====================================================

        public List<string> GetBrands(string? categoryFilter = null)
        {
            var brands = new List<string>();
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT Name FROM Brands ORDER BY Name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) brands.Add(reader.GetString(0));
            return brands;
        }

        public void AddBrand(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO Brands (Name) VALUES (@name)";
            cmd.Parameters.AddWithValue("@name", name.Trim());
            cmd.ExecuteNonQuery();
        }

        public void DeleteBrand(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Brands WHERE Name = @name";
            cmd.Parameters.AddWithValue("@name", name.Trim());
            cmd.ExecuteNonQuery();
        }

        // ====================================================
        // PRODUKTY
        // ====================================================

        public List<Product> GetAllProducts(bool includeInactive = false)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = includeInactive
                ? "SELECT * FROM Products ORDER BY CategoryType, Name"
                : "SELECT * FROM Products WHERE IsActive = 1 ORDER BY CategoryType, Name";
            return ReadProducts(cmd);
        }

        public List<Product> SearchProducts(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return GetAllProducts();
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM Products WHERE IsActive = 1 
                AND (Name LIKE @q OR Barcode LIKE @q OR CatalogNumber LIKE @q 
                     OR Brand LIKE @q OR CategoryType LIKE @q OR AttributesJson LIKE @q OR Notes LIKE @q)
                ORDER BY CategoryType, Name";
            cmd.Parameters.AddWithValue("@q", $"%{query}%");
            return ReadProducts(cmd);
        }

        public List<Product> GetProductsByCategory(string categoryType)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Products WHERE IsActive = 1 AND CategoryType = @cat ORDER BY Name";
            cmd.Parameters.AddWithValue("@cat", categoryType);
            return ReadProducts(cmd);
        }

        public Product? GetProductByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Products WHERE Barcode = @bc AND IsActive = 1 LIMIT 1";
            cmd.Parameters.AddWithValue("@bc", barcode);
            var list = ReadProducts(cmd);
            return list.Count > 0 ? list[0] : null;
        }

        public Product? GetProductById(int id)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Products WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            var list = ReadProducts(cmd);
            return list.Count > 0 ? list[0] : null;
        }

        public int AddProduct(Product p)
        {
            // Auto-dodaj markę
            if (!string.IsNullOrWhiteSpace(p.Brand)) AddBrand(p.Brand);

            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Products (Name, Barcode, CatalogNumber, CategoryType, Brand,
                    PurchasePrice, SalePrice, VatRate, StockQuantity, MinStockLevel, Unit,
                    IsActive, Notes, AttributesJson, ImageFileName, FiscalName)
                VALUES (@name, @barcode, @catalog, @cat, @brand,
                    @purchase, @sale, @vat, @stock, @minStock, @unit,
                    @active, @notes, @attrs, @img, @fiscal);
                SELECT last_insert_rowid();";
            BindProductParams(cmd, p);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void UpdateProduct(Product p)
        {
            if (!string.IsNullOrWhiteSpace(p.Brand)) AddBrand(p.Brand);

            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Products SET
                    Name=@name, Barcode=@barcode, CatalogNumber=@catalog,
                    CategoryType=@cat, Brand=@brand,
                    PurchasePrice=@purchase, SalePrice=@sale, VatRate=@vat,
                    StockQuantity=@stock, MinStockLevel=@minStock, Unit=@unit,
                    IsActive=@active, Notes=@notes, AttributesJson=@attrs,
                    ImageFileName=@img, FiscalName=@fiscal
                WHERE Id = @id";
            BindProductParams(cmd, p);
            cmd.Parameters.AddWithValue("@id", p.Id);
            cmd.ExecuteNonQuery();
        }

        public void DeactivateProduct(int id)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Products SET IsActive = 0 WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public List<Product> GetLowStockProducts()
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Products WHERE IsActive = 1 AND StockQuantity <= MinStockLevel ORDER BY StockQuantity";
            return ReadProducts(cmd);
        }

        // ====================================================
        // SPRZEDAŻ
        // ====================================================

        public string GenerateReceiptNumber()
        {
            string prefix = DateTime.Now.ToString("yyyyMMdd");
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Sales WHERE ReceiptNumber LIKE @prefix";
            cmd.Parameters.AddWithValue("@prefix", $"{prefix}%");
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            return $"{prefix}-{(count + 1):D4}";
        }

        public int SaveSale(Sale sale)
        {
            using var conn = GetConnection();
            using var transaction = conn.BeginTransaction();
            try
            {
                var cmd = conn.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    INSERT INTO Sales (ReceiptNumber, SaleDate, TotalAmount, PaymentMethod, PaidAmount, ChangeAmount, IsFiscalPrinted, DiscountPercent, DiscountAmount)
                    VALUES (@receipt, @date, @total, @payment, @paid, @change, @fiscal, @discpct, @discamt);
                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@receipt", sale.ReceiptNumber);
                cmd.Parameters.AddWithValue("@date", sale.SaleDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@total", sale.TotalAmount);
                cmd.Parameters.AddWithValue("@payment", sale.PaymentMethod);
                cmd.Parameters.AddWithValue("@paid", sale.PaidAmount);
                cmd.Parameters.AddWithValue("@change", sale.ChangeAmount);
                cmd.Parameters.AddWithValue("@fiscal", sale.IsFiscalPrinted ? 1 : 0);
                cmd.Parameters.AddWithValue("@discpct", sale.DiscountPercent);
                cmd.Parameters.AddWithValue("@discamt", sale.DiscountAmount);
                int saleId = Convert.ToInt32(cmd.ExecuteScalar());

                foreach (var item in sale.Items)
                {
                    var ic = conn.CreateCommand();
                    ic.Transaction = transaction;
                    ic.CommandText = @"
                        INSERT INTO SaleItems (SaleId, ProductId, ProductName, Barcode, Quantity, UnitPrice, VatRate)
                        VALUES (@sid, @pid, @name, @bc, @qty, @price, @vat)";
                    ic.Parameters.AddWithValue("@sid", saleId);
                    ic.Parameters.AddWithValue("@pid", item.ProductId);
                    ic.Parameters.AddWithValue("@name", item.ProductName);
                    ic.Parameters.AddWithValue("@bc", item.Barcode);
                    ic.Parameters.AddWithValue("@qty", item.Quantity);
                    ic.Parameters.AddWithValue("@price", item.UnitPrice);
                    ic.Parameters.AddWithValue("@vat", item.VatRate);
                    ic.ExecuteNonQuery();

                    var sc = conn.CreateCommand();
                    sc.Transaction = transaction;
                    sc.CommandText = "UPDATE Products SET StockQuantity = MAX(0, StockQuantity - @qty) WHERE Id = @id";
                    sc.Parameters.AddWithValue("@qty", item.Quantity);
                    sc.Parameters.AddWithValue("@id", item.ProductId);
                    sc.ExecuteNonQuery();
                }

                transaction.Commit();
                return saleId;
            }
            catch { transaction.Rollback(); throw; }
        }

        public List<Sale> GetSales(DateTime? dateFrom = null, DateTime? dateTo = null, string? search = null)
        {
            var sales = new List<Sale>();
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            var conds = new List<string>();

            if (dateFrom.HasValue)
            {
                conds.Add("SaleDate >= @from");
                cmd.Parameters.AddWithValue("@from", dateFrom.Value.ToString("yyyy-MM-dd 00:00:00"));
            }
            if (dateTo.HasValue)
            {
                conds.Add("SaleDate <= @to");
                cmd.Parameters.AddWithValue("@to", dateTo.Value.ToString("yyyy-MM-dd 23:59:59"));
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                conds.Add("ReceiptNumber LIKE @search");
                cmd.Parameters.AddWithValue("@search", $"%{search}%");
            }

            string where = conds.Count > 0 ? "WHERE " + string.Join(" AND ", conds) : "";
            cmd.CommandText = $"SELECT * FROM Sales {where} ORDER BY SaleDate DESC LIMIT 500";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int discPctOrd = -1; int discAmtOrd = -1;
                try { discPctOrd = reader.GetOrdinal("DiscountPercent"); } catch { }
                try { discAmtOrd = reader.GetOrdinal("DiscountAmount"); } catch { }
                sales.Add(new Sale
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ReceiptNumber = reader.GetString(reader.GetOrdinal("ReceiptNumber")),
                    SaleDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("SaleDate"))),
                    TotalAmount = (decimal)reader.GetDouble(reader.GetOrdinal("TotalAmount")),
                    PaymentMethod = reader.GetString(reader.GetOrdinal("PaymentMethod")),
                    PaidAmount = (decimal)reader.GetDouble(reader.GetOrdinal("PaidAmount")),
                    ChangeAmount = (decimal)reader.GetDouble(reader.GetOrdinal("ChangeAmount")),
                    IsFiscalPrinted = reader.GetInt32(reader.GetOrdinal("IsFiscalPrinted")) == 1,
                    IsCancelled = reader.GetInt32(reader.GetOrdinal("IsCancelled")) == 1,
                    DiscountPercent = discPctOrd >= 0 && !reader.IsDBNull(discPctOrd) ? (decimal)reader.GetDouble(discPctOrd) : 0,
                    DiscountAmount = discAmtOrd >= 0 && !reader.IsDBNull(discAmtOrd) ? (decimal)reader.GetDouble(discAmtOrd) : 0,
                });
            }

            foreach (var sale in sales)
                sale.Items = GetSaleItems(sale.Id);

            return sales;
        }

        public List<SaleItem> GetSaleItems(int saleId)
        {
            var items = new List<SaleItem>();
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM SaleItems WHERE SaleId = @sid";
            cmd.Parameters.AddWithValue("@sid", saleId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                items.Add(new SaleItem
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    SaleId = r.GetInt32(r.GetOrdinal("SaleId")),
                    ProductId = r.GetInt32(r.GetOrdinal("ProductId")),
                    ProductName = r.GetString(r.GetOrdinal("ProductName")),
                    Barcode = r.GetString(r.GetOrdinal("Barcode")),
                    Quantity = r.GetInt32(r.GetOrdinal("Quantity")),
                    UnitPrice = (decimal)r.GetDouble(r.GetOrdinal("UnitPrice")),
                    VatRate = r.GetString(r.GetOrdinal("VatRate"))
                });
            }
            return items;
        }

        // ====================================================
        // ZWROTY
        // ====================================================

        public string GenerateReturnNumber()
        {
            string prefix = "ZWR-" + DateTime.Now.ToString("yyyyMMdd");
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Returns WHERE ReturnNumber LIKE @prefix";
            cmd.Parameters.AddWithValue("@prefix", $"{prefix}%");
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            return $"{prefix}-{(count + 1):D4}";
        }

        public int SaveReturn(Return ret)
        {
            using var conn = GetConnection();
            using var transaction = conn.BeginTransaction();
            try
            {
                var cmd = conn.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    INSERT INTO Returns (OriginalSaleId, OriginalReceiptNumber, ReturnNumber, ReturnDate, TotalAmount, RefundMethod)
                    VALUES (@saleId, @receiptNum, @returnNum, @date, @total, @refund);
                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@saleId", ret.OriginalSaleId);
                cmd.Parameters.AddWithValue("@receiptNum", ret.OriginalReceiptNumber);
                cmd.Parameters.AddWithValue("@returnNum", ret.ReturnNumber);
                cmd.Parameters.AddWithValue("@date", ret.ReturnDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@total", ret.TotalAmount);
                cmd.Parameters.AddWithValue("@refund", ret.RefundMethod);
                int returnId = Convert.ToInt32(cmd.ExecuteScalar());

                foreach (var item in ret.Items)
                {
                    var ic = conn.CreateCommand();
                    ic.Transaction = transaction;
                    ic.CommandText = @"
                        INSERT INTO ReturnItems (ReturnId, ProductId, ProductName, Barcode, Quantity, UnitPrice, VatRate)
                        VALUES (@rid, @pid, @name, @bc, @qty, @price, @vat)";
                    ic.Parameters.AddWithValue("@rid", returnId);
                    ic.Parameters.AddWithValue("@pid", item.ProductId);
                    ic.Parameters.AddWithValue("@name", item.ProductName);
                    ic.Parameters.AddWithValue("@bc", item.Barcode);
                    ic.Parameters.AddWithValue("@qty", item.Quantity);
                    ic.Parameters.AddWithValue("@price", item.UnitPrice);
                    ic.Parameters.AddWithValue("@vat", item.VatRate);
                    ic.ExecuteNonQuery();

                    // Przywróć stan magazynu
                    var sc = conn.CreateCommand();
                    sc.Transaction = transaction;
                    sc.CommandText = "UPDATE Products SET StockQuantity = StockQuantity + @qty WHERE Id = @id";
                    sc.Parameters.AddWithValue("@qty", item.Quantity);
                    sc.Parameters.AddWithValue("@id", item.ProductId);
                    sc.ExecuteNonQuery();
                }

                transaction.Commit();
                return returnId;
            }
            catch { transaction.Rollback(); throw; }
        }

        public List<Return> GetReturns(DateTime? dateFrom = null, DateTime? dateTo = null, string? search = null)
        {
            var returns = new List<Return>();
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            var conds = new List<string>();

            if (dateFrom.HasValue)
            {
                conds.Add("ReturnDate >= @from");
                cmd.Parameters.AddWithValue("@from", dateFrom.Value.ToString("yyyy-MM-dd 00:00:00"));
            }
            if (dateTo.HasValue)
            {
                conds.Add("ReturnDate <= @to");
                cmd.Parameters.AddWithValue("@to", dateTo.Value.ToString("yyyy-MM-dd 23:59:59"));
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                conds.Add("(ReturnNumber LIKE @search OR OriginalReceiptNumber LIKE @search)");
                cmd.Parameters.AddWithValue("@search", $"%{search}%");
            }

            string where = conds.Count > 0 ? "WHERE " + string.Join(" AND ", conds) : "";
            cmd.CommandText = $"SELECT * FROM Returns {where} ORDER BY ReturnDate DESC LIMIT 200";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                returns.Add(new Return
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    OriginalSaleId = reader.GetInt32(reader.GetOrdinal("OriginalSaleId")),
                    OriginalReceiptNumber = reader.GetString(reader.GetOrdinal("OriginalReceiptNumber")),
                    ReturnNumber = reader.GetString(reader.GetOrdinal("ReturnNumber")),
                    ReturnDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("ReturnDate"))),
                    TotalAmount = (decimal)reader.GetDouble(reader.GetOrdinal("TotalAmount")),
                    RefundMethod = reader.GetString(reader.GetOrdinal("RefundMethod"))
                });
            }

            foreach (var ret in returns)
                ret.Items = GetReturnItems(ret.Id);

            return returns;
        }

        public List<ReturnItem> GetReturnItems(int returnId)
        {
            var items = new List<ReturnItem>();
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM ReturnItems WHERE ReturnId = @rid";
            cmd.Parameters.AddWithValue("@rid", returnId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                items.Add(new ReturnItem
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    ReturnId = r.GetInt32(r.GetOrdinal("ReturnId")),
                    ProductId = r.GetInt32(r.GetOrdinal("ProductId")),
                    ProductName = r.GetString(r.GetOrdinal("ProductName")),
                    Barcode = r.GetString(r.GetOrdinal("Barcode")),
                    Quantity = r.GetInt32(r.GetOrdinal("Quantity")),
                    UnitPrice = (decimal)r.GetDouble(r.GetOrdinal("UnitPrice")),
                    VatRate = r.GetString(r.GetOrdinal("VatRate"))
                });
            }
            return items;
        }

        // ====================================================
        // USTAWIENIA
        // ====================================================

        public string? GetSetting(string key)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key = @key";
            cmd.Parameters.AddWithValue("@key", key);
            return cmd.ExecuteScalar()?.ToString();
        }

        public void SaveSetting(string key, string value)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@key, @value)";
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@value", value);
            cmd.ExecuteNonQuery();
        }

        public AppSettings LoadSettings()
        {
            var s = new AppSettings();
            s.CompanyName = GetSetting("CompanyName") ?? s.CompanyName;
            s.CompanyNIP = GetSetting("CompanyNIP") ?? s.CompanyNIP;
            s.CompanyAddress = GetSetting("CompanyAddress") ?? s.CompanyAddress;
            s.CompanyCity = GetSetting("CompanyCity") ?? s.CompanyCity;
            s.CompanyPhone = GetSetting("CompanyPhone") ?? s.CompanyPhone;
            
            // Kasa fiskalna
            s.FiscalConnectionType = GetSetting("FiscalConnectionType") ?? s.FiscalConnectionType;
            s.FiscalPrinterPort = GetSetting("FiscalPrinterPort") ?? s.FiscalPrinterPort;
            if (int.TryParse(GetSetting("FiscalPrinterBaudRate"), out int baud)) s.FiscalPrinterBaudRate = baud;
            s.FiscalIpAddress = GetSetting("FiscalIpAddress") ?? s.FiscalIpAddress;
            if (int.TryParse(GetSetting("FiscalIpPort"), out int fip)) s.FiscalIpPort = fip;
            
            // Terminal
            s.TerminalConnectionType = GetSetting("TerminalConnectionType") ?? s.TerminalConnectionType;
            s.TerminalComPort = GetSetting("TerminalComPort") ?? s.TerminalComPort;
            if (int.TryParse(GetSetting("TerminalBaudRate"), out int tbaud)) s.TerminalBaudRate = tbaud;
            s.TerminalIpAddress = GetSetting("TerminalIpAddress") ?? s.TerminalIpAddress;
            if (int.TryParse(GetSetting("TerminalIpPort"), out int tip)) s.TerminalIpPort = tip;
            
            if (bool.TryParse(GetSetting("AutoPrintReceipt"), out bool ap)) s.AutoPrintReceipt = ap;
            s.DefaultVatRate = GetSetting("DefaultVatRate") ?? s.DefaultVatRate;
            if (bool.TryParse(GetSetting("AutoConnectOnStart"), out bool acs)) s.AutoConnectOnStart = acs;
            s.UpdateCheckUrl = GetSetting("UpdateCheckUrl") ?? s.UpdateCheckUrl;

            // SMTP / Email
            s.SmtpHost = GetSetting("SmtpHost") ?? s.SmtpHost;
            if (int.TryParse(GetSetting("SmtpPort"), out int smtpPort)) s.SmtpPort = smtpPort;
            s.SmtpUser = GetSetting("SmtpUser") ?? s.SmtpUser;
            s.SmtpPassword = GetSetting("SmtpPassword") ?? s.SmtpPassword;
            if (bool.TryParse(GetSetting("SmtpUseSsl"), out bool smtpSsl)) s.SmtpUseSsl = smtpSsl;
            s.SmtpFromEmail = GetSetting("SmtpFromEmail") ?? s.SmtpFromEmail;
            s.SmtpFromName = GetSetting("SmtpFromName") ?? s.SmtpFromName;
            if (int.TryParse(GetSetting("InvoicePaymentDays"), out int payDays)) s.InvoicePaymentDays = payDays;

            // Backup
            s.BackupFolder = GetSetting("BackupFolder") ?? s.BackupFolder;
            if (bool.TryParse(GetSetting("BackupOnStartup"), out bool bos)) s.BackupOnStartup = bos;
            if (int.TryParse(GetSetting("BackupKeepCount"), out int bkc)) s.BackupKeepCount = bkc;

            return s;
        }

        public void SaveSettings(AppSettings s)
        {
            SaveSetting("CompanyName", s.CompanyName);
            SaveSetting("CompanyNIP", s.CompanyNIP);
            SaveSetting("CompanyAddress", s.CompanyAddress);
            SaveSetting("CompanyCity", s.CompanyCity);
            SaveSetting("CompanyPhone", s.CompanyPhone);
            
            SaveSetting("FiscalConnectionType", s.FiscalConnectionType);
            SaveSetting("FiscalPrinterPort", s.FiscalPrinterPort);
            SaveSetting("FiscalPrinterBaudRate", s.FiscalPrinterBaudRate.ToString());
            SaveSetting("FiscalIpAddress", s.FiscalIpAddress);
            SaveSetting("FiscalIpPort", s.FiscalIpPort.ToString());
            
            SaveSetting("TerminalConnectionType", s.TerminalConnectionType);
            SaveSetting("TerminalComPort", s.TerminalComPort);
            SaveSetting("TerminalBaudRate", s.TerminalBaudRate.ToString());
            SaveSetting("TerminalIpAddress", s.TerminalIpAddress);
            SaveSetting("TerminalIpPort", s.TerminalIpPort.ToString());
            
            SaveSetting("AutoPrintReceipt", s.AutoPrintReceipt.ToString());
            SaveSetting("DefaultVatRate", s.DefaultVatRate);
            SaveSetting("AutoConnectOnStart", s.AutoConnectOnStart.ToString());
            SaveSetting("UpdateCheckUrl", s.UpdateCheckUrl);

            // SMTP / Email
            SaveSetting("SmtpHost", s.SmtpHost);
            SaveSetting("SmtpPort", s.SmtpPort.ToString());
            SaveSetting("SmtpUser", s.SmtpUser);
            SaveSetting("SmtpPassword", s.SmtpPassword);
            SaveSetting("SmtpUseSsl", s.SmtpUseSsl.ToString());
            SaveSetting("SmtpFromEmail", s.SmtpFromEmail);
            SaveSetting("SmtpFromName", s.SmtpFromName);
            SaveSetting("InvoicePaymentDays", s.InvoicePaymentDays.ToString());

            // Backup
            SaveSetting("BackupFolder", s.BackupFolder);
            SaveSetting("BackupOnStartup", s.BackupOnStartup.ToString());
            SaveSetting("BackupKeepCount", s.BackupKeepCount.ToString());
        }

        // ====================================================
        // HELPERS
        // ====================================================

        private static void BindProductParams(SqliteCommand cmd, Product p)
        {
            cmd.Parameters.AddWithValue("@name", p.Name);
            cmd.Parameters.AddWithValue("@barcode", p.Barcode);
            cmd.Parameters.AddWithValue("@catalog", p.CatalogNumber);
            cmd.Parameters.AddWithValue("@cat", p.CategoryType);
            cmd.Parameters.AddWithValue("@brand", p.Brand);
            cmd.Parameters.AddWithValue("@purchase", p.PurchasePrice);
            cmd.Parameters.AddWithValue("@sale", p.SalePrice);
            cmd.Parameters.AddWithValue("@vat", p.VatRate);
            cmd.Parameters.AddWithValue("@stock", p.StockQuantity);
            cmd.Parameters.AddWithValue("@minStock", p.MinStockLevel);
            cmd.Parameters.AddWithValue("@unit", p.Unit);
            cmd.Parameters.AddWithValue("@active", p.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@notes", p.Notes);
            cmd.Parameters.AddWithValue("@attrs", p.AttributesJson);
            cmd.Parameters.AddWithValue("@img", p.ImageFileName);
            cmd.Parameters.AddWithValue("@fiscal", p.FiscalName);
        }

        private static List<Product> ReadProducts(SqliteCommand cmd)
        {
            var products = new List<Product>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                products.Add(new Product
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    Name = r.GetString(r.GetOrdinal("Name")),
                    Barcode = r.GetString(r.GetOrdinal("Barcode")),
                    CatalogNumber = r.GetString(r.GetOrdinal("CatalogNumber")),
                    CategoryType = r.GetString(r.GetOrdinal("CategoryType")),
                    Brand = r.GetString(r.GetOrdinal("Brand")),
                    PurchasePrice = (decimal)r.GetDouble(r.GetOrdinal("PurchasePrice")),
                    SalePrice = (decimal)r.GetDouble(r.GetOrdinal("SalePrice")),
                    VatRate = r.GetString(r.GetOrdinal("VatRate")),
                    StockQuantity = r.GetInt32(r.GetOrdinal("StockQuantity")),
                    MinStockLevel = r.GetInt32(r.GetOrdinal("MinStockLevel")),
                    Unit = r.GetString(r.GetOrdinal("Unit")),
                    IsActive = r.GetInt32(r.GetOrdinal("IsActive")) == 1,
                    Notes = r.GetString(r.GetOrdinal("Notes")),
                    AttributesJson = r.GetString(r.GetOrdinal("AttributesJson")),
                    ImageFileName = TryGetString(r, "ImageFileName"),
                    FiscalName = TryGetString(r, "FiscalName"),
                    CreatedAt = DateTime.TryParse(r.GetString(r.GetOrdinal("CreatedAt")), out var dt) ? dt : DateTime.Now
                });
            }
            return products;
        }

        /// <summary>Bezpiecznie odczytuje string (kolumna może nie istnieć w starej bazie).</summary>
        private static string TryGetString(SqliteDataReader r, string column)
        {
            try
            {
                int ord = r.GetOrdinal(column);
                return r.IsDBNull(ord) ? string.Empty : r.GetString(ord);
            }
            catch { return string.Empty; }
        }

        // ====================================================
        // FAKTURY
        // ====================================================

        public string GenerateInvoiceNumber()
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            var month = DateTime.Now.Month.ToString("D2");
            var year = DateTime.Now.Year.ToString();
            cmd.CommandText = "SELECT COUNT(*) FROM Invoices WHERE InvoiceDate LIKE @prefix";
            cmd.Parameters.AddWithValue("@prefix", $"{year}-{month}%");
            var count = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
            return $"FV/{month}/{year}/{count:D3}";
        }

        public int SaveInvoice(Invoice invoice)
        {
            using var conn = GetConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                var cmd = conn.CreateCommand();
                if (invoice.Id == 0)
                {
                    cmd.CommandText = @"
                        INSERT INTO Invoices (InvoiceNumber, InvoiceDate, DueDate, SaleId,
                            BuyerName, BuyerNIP, BuyerAddress, BuyerCity, BuyerEmail,
                            PaymentMethod, PaymentDays, TotalNet, TotalVat, TotalGross,
                            IsPaid, IsEmailSent, Notes)
                        VALUES (@num, @date, @due, @saleId,
                            @buyerName, @buyerNIP, @buyerAddr, @buyerCity, @buyerEmail,
                            @payMethod, @payDays, @net, @vat, @gross,
                            @paid, @sent, @notes);
                        SELECT last_insert_rowid();";
                }
                else
                {
                    cmd.CommandText = @"
                        UPDATE Invoices SET InvoiceNumber=@num, InvoiceDate=@date, DueDate=@due, SaleId=@saleId,
                            BuyerName=@buyerName, BuyerNIP=@buyerNIP, BuyerAddress=@buyerAddr, BuyerCity=@buyerCity, BuyerEmail=@buyerEmail,
                            PaymentMethod=@payMethod, PaymentDays=@payDays, TotalNet=@net, TotalVat=@vat, TotalGross=@gross,
                            IsPaid=@paid, IsEmailSent=@sent, Notes=@notes
                        WHERE Id=@id;
                        SELECT @id;";
                    cmd.Parameters.AddWithValue("@id", invoice.Id);
                }
                cmd.Parameters.AddWithValue("@num", invoice.InvoiceNumber);
                cmd.Parameters.AddWithValue("@date", invoice.InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@due", invoice.DueDate.HasValue ? (object)invoice.DueDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
                cmd.Parameters.AddWithValue("@saleId", invoice.SaleId.HasValue ? (object)invoice.SaleId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@buyerName", invoice.BuyerName);
                cmd.Parameters.AddWithValue("@buyerNIP", invoice.BuyerNIP);
                cmd.Parameters.AddWithValue("@buyerAddr", invoice.BuyerAddress);
                cmd.Parameters.AddWithValue("@buyerCity", invoice.BuyerCity);
                cmd.Parameters.AddWithValue("@buyerEmail", invoice.BuyerEmail);
                cmd.Parameters.AddWithValue("@payMethod", invoice.PaymentMethod);
                cmd.Parameters.AddWithValue("@payDays", invoice.PaymentDays);
                cmd.Parameters.AddWithValue("@net", invoice.TotalNet);
                cmd.Parameters.AddWithValue("@vat", invoice.TotalVat);
                cmd.Parameters.AddWithValue("@gross", invoice.TotalGross);
                cmd.Parameters.AddWithValue("@paid", invoice.IsPaid ? 1 : 0);
                cmd.Parameters.AddWithValue("@sent", invoice.IsEmailSent ? 1 : 0);
                cmd.Parameters.AddWithValue("@notes", invoice.Notes);

                int id = Convert.ToInt32(cmd.ExecuteScalar());

                // Usuń istniejące pozycje i wstaw ponownie
                var delCmd = conn.CreateCommand();
                delCmd.CommandText = "DELETE FROM InvoiceItems WHERE InvoiceId = @id";
                delCmd.Parameters.AddWithValue("@id", id);
                delCmd.ExecuteNonQuery();

                foreach (var item in invoice.Items)
                {
                    var ic = conn.CreateCommand();
                    ic.CommandText = @"INSERT INTO InvoiceItems (InvoiceId, Name, Unit, Quantity, UnitPriceNet, VatRate)
                                       VALUES (@inv, @name, @unit, @qty, @price, @vat)";
                    ic.Parameters.AddWithValue("@inv", id);
                    ic.Parameters.AddWithValue("@name", item.Name);
                    ic.Parameters.AddWithValue("@unit", item.Unit);
                    ic.Parameters.AddWithValue("@qty", (double)item.Quantity);
                    ic.Parameters.AddWithValue("@price", (double)item.UnitPriceNet);
                    ic.Parameters.AddWithValue("@vat", item.VatRate);
                    ic.ExecuteNonQuery();
                }

                tx.Commit();
                return id;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public List<Invoice> GetInvoices(DateTime? from = null, DateTime? to = null, string? search = null)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            var where = new System.Text.StringBuilder("WHERE 1=1");
            if (from.HasValue) { where.Append(" AND InvoiceDate >= @from"); cmd.Parameters.AddWithValue("@from", from.Value.ToString("yyyy-MM-dd")); }
            if (to.HasValue) { where.Append(" AND InvoiceDate <= @to"); cmd.Parameters.AddWithValue("@to", to.Value.ToString("yyyy-MM-dd 23:59:59")); }
            if (!string.IsNullOrWhiteSpace(search)) { where.Append(" AND (InvoiceNumber LIKE @s OR BuyerName LIKE @s OR BuyerNIP LIKE @s)"); cmd.Parameters.AddWithValue("@s", $"%{search}%"); }
            cmd.CommandText = $"SELECT * FROM Invoices {where} ORDER BY InvoiceDate DESC";
            var list = new List<Invoice>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var inv = new Invoice
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    InvoiceNumber = r.GetString(r.GetOrdinal("InvoiceNumber")),
                    InvoiceDate = DateTime.Parse(r.GetString(r.GetOrdinal("InvoiceDate"))),
                    DueDate = r.IsDBNull(r.GetOrdinal("DueDate")) ? null : DateTime.Parse(r.GetString(r.GetOrdinal("DueDate"))),
                    SaleId = r.IsDBNull(r.GetOrdinal("SaleId")) ? null : r.GetInt32(r.GetOrdinal("SaleId")),
                    BuyerName = r.GetString(r.GetOrdinal("BuyerName")),
                    BuyerNIP = r.GetString(r.GetOrdinal("BuyerNIP")),
                    BuyerAddress = r.GetString(r.GetOrdinal("BuyerAddress")),
                    BuyerCity = r.GetString(r.GetOrdinal("BuyerCity")),
                    BuyerEmail = r.GetString(r.GetOrdinal("BuyerEmail")),
                    PaymentMethod = r.GetString(r.GetOrdinal("PaymentMethod")),
                    PaymentDays = r.GetInt32(r.GetOrdinal("PaymentDays")),
                    TotalNet = (decimal)r.GetDouble(r.GetOrdinal("TotalNet")),
                    TotalVat = (decimal)r.GetDouble(r.GetOrdinal("TotalVat")),
                    TotalGross = (decimal)r.GetDouble(r.GetOrdinal("TotalGross")),
                    IsPaid = r.GetInt32(r.GetOrdinal("IsPaid")) == 1,
                    IsEmailSent = r.GetInt32(r.GetOrdinal("IsEmailSent")) == 1,
                    Notes = r.GetString(r.GetOrdinal("Notes"))
                };
                list.Add(inv);
            }
            return list;
        }

        public List<InvoiceItem> GetInvoiceItems(int invoiceId)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM InvoiceItems WHERE InvoiceId = @id ORDER BY Id";
            cmd.Parameters.AddWithValue("@id", invoiceId);
            var list = new List<InvoiceItem>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new InvoiceItem
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    InvoiceId = r.GetInt32(r.GetOrdinal("InvoiceId")),
                    Name = r.GetString(r.GetOrdinal("Name")),
                    Unit = r.GetString(r.GetOrdinal("Unit")),
                    Quantity = (decimal)r.GetDouble(r.GetOrdinal("Quantity")),
                    UnitPriceNet = (decimal)r.GetDouble(r.GetOrdinal("UnitPriceNet")),
                    VatRate = r.GetString(r.GetOrdinal("VatRate"))
                });
            return list;
        }

        public void DeleteInvoice(int id)
        {
            using var conn = GetConnection();
            using var tx = conn.BeginTransaction();
            var d1 = conn.CreateCommand(); d1.CommandText = "DELETE FROM InvoiceItems WHERE InvoiceId=@id"; d1.Parameters.AddWithValue("@id", id); d1.ExecuteNonQuery();
            var d2 = conn.CreateCommand(); d2.CommandText = "DELETE FROM Invoices WHERE Id=@id"; d2.Parameters.AddWithValue("@id", id); d2.ExecuteNonQuery();
            tx.Commit();
        }

        public void MarkInvoiceEmailSent(int id)
        {
            using var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Invoices SET IsEmailSent=1 WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }
}
