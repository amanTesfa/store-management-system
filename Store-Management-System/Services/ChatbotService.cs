using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Store_Management_System.Models;
using System.Text.Json;

namespace Store_Management_System.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly InventoryDbContext _context;
        private readonly ForecastService _forecastService;
        private readonly ILogger<ChatbotService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly PredictionEngine<IntentTrainingData, IntentPrediction> _intentPredictor;
        private readonly MLContext _mlContext;

        public ChatbotService(
            InventoryDbContext context,
            ForecastService forecastService,
            IWebHostEnvironment environment,
            ILogger<ChatbotService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _forecastService = forecastService;
            _environment = environment;
            _logger = logger;
            _mlContext = new MLContext();

            // Load the trained intent model
            var modelPath = Path.Combine(environment.ContentRootPath, "ML", "Models", "intent_classifier.zip");

            if (File.Exists(modelPath))
            {
                var model = _mlContext.Model.Load(modelPath, out _);
                _intentPredictor = _mlContext.Model.CreatePredictionEngine<IntentTrainingData, IntentPrediction>(model);
                _logger.LogInformation("Intent classifier model loaded successfully");
            }
            else
            {
                _logger.LogWarning("Intent classifier model not found at {Path}. Please train the model first.", modelPath);
                throw new FileNotFoundException($"Intent classifier model not found. Please run the training first. Expected at: {modelPath}");
            }
        }

        public async Task<ChatbotResponse> ProcessQueryAsync(string userQuery)
        {
            try
            {
                _logger.LogInformation("Processing query: {Query}", userQuery);

                // Step 1: Classify intent using ML model
                var intent = PredictIntent(userQuery);
                _logger.LogInformation("Predicted intent: {Intent} for query: {Query}", intent, userQuery);

                // Step 2: Extract product name if present
                var productName = await ExtractProductNameAsync(userQuery);
                int? productId = null;
                Article? product = null;

                if (!string.IsNullOrEmpty(productName))
                {
                    product = await _context.Articles
                        .FirstOrDefaultAsync(a => a.ArticleName.Contains(productName) && a.IsActive);
                    productId = product?.Id;
                    _logger.LogInformation("Extracted product: {Product} (ID: {Id})", productName, productId);
                }

                // Step 3: Execute the appropriate handler
                var contextData = await ExecuteIntentAsync(intent, productId, productName, userQuery);

                // Step 4: Generate natural response
                var answer = GenerateResponse(intent, contextData, product);

                return new ChatbotResponse
                {
                    Intent = intent.ToString(),
                    Answer = answer,
                    Data = contextData,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat query: {Query}", userQuery);
                return new ChatbotResponse
                {
                    Intent = "Error",
                    Answer = "I encountered an error processing your request. Please try again with a different question.",
                    Success = false
                };
            }
        }

        private ChatIntent PredictIntent(string query)
        {
            var prediction = _intentPredictor.Predict(new IntentTrainingData { Text = query });

            // Get confidence score
            var maxScore = prediction.Score?.Max() ?? 0;
            _logger.LogInformation("Intent prediction confidence: {Confidence:F2}", maxScore);

            // If confidence is too low, try to determine if it's a general help request
            if (maxScore < 0.3)
            {
                var lowerQuery = query.ToLower();
                if (lowerQuery.Contains("help") || lowerQuery.Contains("hi") || lowerQuery.Contains("hello") ||
                    lowerQuery.Contains("what can you") || lowerQuery.Length < 5)
                {
                    return ChatIntent.GeneralHelp;
                }
            }

            if (Enum.TryParse<ChatIntent>(prediction.PredictedIntent, out var intent))
            {
                return intent;
            }

            return ChatIntent.Unknown;
        }

        private async Task<string?> ExtractProductNameAsync(string query)
        {
            // Get all active products
            var products = await _context.Articles
                .Where(a => a.IsActive)
                .Select(a => new { a.Id, a.ArticleName })
                .ToListAsync();

            // Find the best match (longest product name that appears in query)
            var match = products
                .Where(p => query.Contains(p.ArticleName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.ArticleName.Length)
                .FirstOrDefault();

            return match?.ArticleName;
        }

        private async Task<object> ExecuteIntentAsync(
            ChatIntent intent,
            int? productId,
            string? productName,
            string originalQuery)
        {
            return intent switch
            {
                ChatIntent.DemandForecast => await HandleDemandForecast(productId, productName),
                ChatIntent.CurrentStock => await HandleCurrentStock(productId, productName),
                ChatIntent.LowStockAlert => await HandleLowStockAlert(),
                ChatIntent.ProductInfo => await HandleProductInfo(productId, productName),
                ChatIntent.PriceOptimization => await HandlePriceOptimization(),
                ChatIntent.SupplierInfo => await HandleSupplierInfo(),
                ChatIntent.SalesReport => await HandleSalesReport(),
                ChatIntent.GeneralHelp => GetHelpInfo(),
                _ => new { Type = "Unknown", Message = "I'm not sure how to help with that." }
            };
        }

        private async Task<object> HandleDemandForecast(int? productId, string? productName)
        {
            if (productId.HasValue)
            {
                var product = await _context.Articles.FindAsync(productId.Value);
                if (product != null)
                {
                    var forecast = await _forecastService.PredictDemand(
                        product.Id, product.ArticleName, product.StandardPrice);

                    return new
                    {
                        Type = "DemandForecast",
                        ProductName = product.ArticleName,
                        PredictedSales = (int)(forecast?.PredictedSales ?? 0),
                        Month = forecast?.Month ?? DateTime.Now.AddMonths(1).Month,
                        Year = forecast?.Year ?? DateTime.Now.Year,
                        Recommendation = forecast?.Recommendation ?? "No forecast available"
                    };
                }
            }

            // Return top forecasts
            var topProducts = await _context.Articles
                .Where(a => a.IsActive && a.IsSellable)
                .Take(5)
                .ToListAsync();

            var forecasts = new List<object>();
            foreach (var p in topProducts)
            {
                var f = await _forecastService.PredictDemand(p.Id, p.ArticleName, p.StandardPrice);
                if (f != null && f.PredictedSales > 0)
                {
                    forecasts.Add(new { Name = p.ArticleName, Sales = (int)f.PredictedSales });
                }
            }

            return new
            {
                Type = "TopForecasts",
                Forecasts = forecasts.OrderByDescending(f => ((dynamic)f).Sales).Take(5)
            };
        }

        private async Task<object> HandleCurrentStock(int? productId, string? productName)
        {
            if (productId.HasValue)
            {
                var stock = await _context.CurrentStocks
                    .Where(cs => cs.ArticleId == productId.Value)
                    .SumAsync(cs => cs.Quantity);

                var product = await _context.Articles.FindAsync(productId.Value);
                var reorderLevel = product?.ReorderLevel ?? 0;

                return new
                {
                    Type = "StockLevel",
                    ProductName = product?.ArticleName,
                    CurrentStock = (int)stock,
                    ReorderLevel = (int)reorderLevel,
                    Status = stock <= reorderLevel ? "Low" : "Adequate",
                    Suggestion = stock <= reorderLevel ? "Consider reordering soon." : "Stock levels are healthy."
                };
            }

            // Return current stock overview
            var articles = await _context.Articles
                .Where(a => a.IsActive)
                .ToListAsync();

            var stockList = new List<object>();
            foreach (var a in articles.Take(10))
            {
                var stock = await _context.CurrentStocks
                    .Where(cs => cs.ArticleId == a.Id)
                    .SumAsync(cs => cs.Quantity);

                stockList.Add(new { Name = a.ArticleName, Stock = (int)stock });
            }

            return new
            {
                Type = "StockOverview",
                Items = stockList.OrderBy(x => ((dynamic)x).Stock),
                TotalProducts = articles.Count
            };
        }

        private async Task<object> HandleLowStockAlert()
        {
            var articles = await _context.Articles
                .Where(a => a.IsActive)
                .ToListAsync();

            var alerts = new List<object>();
            foreach (var a in articles)
            {
                var stock = await _context.CurrentStocks
                    .Where(cs => cs.ArticleId == a.Id)
                    .SumAsync(cs => cs.Quantity);

                if (stock <= a.ReorderLevel && a.ReorderLevel > 0)
                {
                    alerts.Add(new
                    {
                        a.Id,
                        Name = a.ArticleName,
                        Stock = (int)stock,
                        ReorderLevel = (int)a.ReorderLevel,
                        Shortage = (int)(a.ReorderLevel - stock)
                    });
                }
            }

            var orderedAlerts = alerts.OrderBy(x => ((dynamic)x).Stock).ToList();

            return new
            {
                Type = "LowStockAlerts",
                Count = orderedAlerts.Count,
                Items = orderedAlerts,
                IsAnyCritical = orderedAlerts.Any()
            };
        }

        private async Task<object> HandleProductInfo(int? productId, string? productName)
        {
            IQueryable<Article> query = _context.Articles.Where(a => a.IsActive);

            if (productId.HasValue)
            {
                query = query.Where(a => a.Id == productId.Value);
            }
            else if (!string.IsNullOrEmpty(productName))
            {
                query = query.Where(a => a.ArticleName.Contains(productName));
            }
            else
            {
                return new { Type = "Error", Message = "Please specify a product name." };
            }

            var product = await query.FirstOrDefaultAsync();

            if (product == null)
            {
                return new { Type = "NotFound", Message = "Product not found. Please check the name and try again." };
            }

            var stock = await _context.CurrentStocks
                .Where(cs => cs.ArticleId == product.Id)
                .SumAsync(cs => cs.Quantity);

            string? categoryName = null;
            if (product.ArticleCategory.HasValue)
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == product.ArticleCategory.Value);
                categoryName = category?.Name;
            }

            return new
            {
                Type = "ProductInfo",
                product.Id,
                product.ArticleName,
                product.ArticleCode,
                product.StandardPrice,
                product.StandardCost,
                Category = categoryName ?? "Uncategorized",
                CurrentStock = (int)stock,
                product.ReorderLevel,
                product.IsSellable,
                product.IsPurchasable,
                Profit = product.StandardPrice - product.StandardCost
            };
        }

        private async Task<object> HandlePriceOptimization()
        {
            var articles = await _context.Articles
                .Where(a => a.IsActive && a.IsSellable)
                .ToListAsync();

            var suggestions = new List<object>();

            foreach (var a in articles)
            {
                var stock = await _context.CurrentStocks
                    .Where(cs => cs.ArticleId == a.Id)
                    .SumAsync(cs => cs.Quantity);

                // Suggest discount for overstocked items
                if (stock > a.ReorderLevel * 3 && a.ReorderLevel > 0)
                {
                    suggestions.Add(new
                    {
                        a.Id,
                        Name = a.ArticleName,
                        CurrentPrice = a.StandardPrice,
                        SuggestedPrice = Math.Round(a.StandardPrice * 0.9m, 2),
                        CurrentStock = (int)stock,
                        Reason = "Overstocked - promotional discount recommended",
                        PotentialSaving = Math.Round((a.StandardPrice - a.StandardCost) * stock, 2)
                    });
                }
            }

            return new
            {
                Type = "PriceOptimizations",
                Count = suggestions.Count,
                Suggestions = suggestions.OrderByDescending(s => ((dynamic)s).CurrentStock).Take(5)
            };
        }

        private async Task<object> HandleSupplierInfo()
        {
            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive && !s.IsDeleted)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Email,
                    s.Phone,
                    s.Rating,
                    s.PaymentTerms,
                    s.CreditLimit
                })
                .OrderByDescending(s => s.Rating)
                .ToListAsync();

            return new
            {
                Type = "Suppliers",
                Count = suppliers.Count,
                Suppliers = suppliers,
                TopSupplier = suppliers.FirstOrDefault()
            };
        }

        private async Task<object> HandleSalesReport()
        {
            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var weeklyMovements = await _context.StockMovements
                .Where(s => s.MovementType == "Out" && s.MovementDate >= startOfWeek)
                .ToListAsync();

            var monthlyMovements = await _context.StockMovements
                .Where(s => s.MovementType == "Out" && s.MovementDate >= startOfMonth)
                .ToListAsync();

            return new
            {
                Type = "SalesReport",
                WeeklySales = Math.Round(weeklyMovements.Sum(s => s.Quantity * s.UnitCost), 2),
                MonthlySales = Math.Round(monthlyMovements.Sum(s => s.Quantity * s.UnitCost), 2),
                WeeklyTransactions = weeklyMovements.Count,
                MonthlyTransactions = monthlyMovements.Count,
                WeeklyQuantity = (int)weeklyMovements.Sum(s => s.Quantity),
                MonthlyQuantity = (int)monthlyMovements.Sum(s => s.Quantity)
            };
        }

        private object GetHelpInfo()
        {
            return new
            {
                Type = "Help",
                Greeting = "Hello! I'm your AI Inventory Assistant.",
                Capabilities = new Dictionary<string, string>
                {
                    { "📈 Demand Forecast", "Ask: 'What's the forecast for [product]?' or 'Predict demand for next month'" },
                    { "📦 Stock Check", "Ask: 'How many [product] are in stock?' or 'Show inventory'" },
                    { "⚠️ Low Stock Alerts", "Ask: 'Show low stock items' or 'What needs reordering?'" },
                    { "📋 Product Info", "Ask: 'Tell me about [product]' or 'What's the price of [product]?'" },
                    { "💰 Price Optimization", "Ask: 'Any price suggestions?' or 'Show discount opportunities'" },
                    { "🏢 Supplier Info", "Ask: 'Show suppliers' or 'Who are our vendors?'" },
                    { "📊 Sales Reports", "Ask: 'Show sales report' or 'What are this week's sales?'" }
                }
            };
        }

        private string GenerateResponse(ChatIntent intent, object data, Article? product)
        {
            return intent switch
            {
                ChatIntent.DemandForecast => FormatDemandForecastResponse(data),
                ChatIntent.CurrentStock => FormatStockResponse(data),
                ChatIntent.LowStockAlert => FormatLowStockResponse(data),
                ChatIntent.ProductInfo => FormatProductInfoResponse(data),
                ChatIntent.PriceOptimization => FormatPriceOptimizationResponse(data),
                ChatIntent.SupplierInfo => FormatSupplierResponse(data),
                ChatIntent.SalesReport => FormatSalesReportResponse(data),
                ChatIntent.GeneralHelp => FormatHelpResponse(data),
                _ => "I'm not sure how to help with that. Try asking about stock levels, forecasts, or type 'help' to see what I can do."
            };
        }

        private string FormatDemandForecastResponse(object data)
        {
            var json = JsonSerializer.Serialize(data);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("ProductName", out var nameProp))
            {
                var name = nameProp.GetString();
                var sales = root.GetProperty("PredictedSales").GetInt32();
                var recommendation = root.GetProperty("Recommendation").GetString();
                return $"📈 **Demand Forecast for {name}**\n\nPredicted sales: **{sales} units** next month\n\n{recommendation}";
            }

            if (root.TryGetProperty("Forecasts", out var forecasts))
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("📈 **Top Demand Forecasts**\n");
                foreach (var f in forecasts.EnumerateArray().Take(5))
                {
                    var fName = f.GetProperty("Name").GetString();
                    var fSales = f.GetProperty("Sales").GetInt32();
                    sb.AppendLine($"• {fName}: **{fSales} units**");
                }
                return sb.ToString();
            }

            return "📈 I couldn't generate a specific forecast. Please specify a product name.";
        }

        private string FormatStockResponse(object data)
        {
            var json = JsonSerializer.Serialize(data);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("ProductName", out var nameProp))
            {
                var name = nameProp.GetString();
                var stock = root.GetProperty("CurrentStock").GetInt32();
                var status = root.GetProperty("Status").GetString();
                var suggestion = root.GetProperty("Suggestion").GetString();

                var emoji = status == "Low" ? "⚠️" : "✅";
                return $"{emoji} **{name}**\n\nCurrent stock: **{stock} units**\nStatus: {status}\n\n{suggestion}";
            }

            if (root.TryGetProperty("Items", out var items))
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("📦 **Current Stock Overview**\n");
                foreach (var item in items.EnumerateArray().Take(5))
                {
                    var iName = item.GetProperty("Name").GetString();
                    var iStock = item.GetProperty("Stock").GetInt32();
                    sb.AppendLine($"• {iName}: **{iStock} units**");
                }
                return sb.ToString();
            }

            return "📦 Here's the current stock information.";
        }

        private string FormatLowStockResponse(object data)
        {
            var json = JsonSerializer.Serialize(data);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var count = root.GetProperty("Count").GetInt32();

            if (count == 0)
            {
                return "✅ **Great news!** All products have adequate stock levels. No items need reordering right now.";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"⚠️ **{count} Product(s) Need Attention**\n");

            if (root.TryGetProperty("Items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var name = item.GetProperty("Name").GetString();
                    var stock = item.GetProperty("Stock").GetInt32();
                    var reorder = item.GetProperty("ReorderLevel").GetInt32();
                    var shortage = item.GetProperty("Shortage").GetInt32();
                    sb.AppendLine($"• {name}: **{stock} left** (need {shortage} more to reach reorder level of {reorder})");
                }
            }

            sb.AppendLine("\n📋 Consider creating purchase orders for these items.");
            return sb.ToString();
        }

        private string FormatProductInfoResponse(object data)
        {
            var json = JsonSerializer.Serialize(data);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Type", out var typeProp) && typeProp.GetString() == "NotFound")
            {
                return "❌ Product not found. Please check the name and try again.";
            }

            var name = root.GetProperty("ArticleName").GetString();
            var code = root.GetProperty("ArticleCode").GetString();
            var price = root.GetProperty("StandardPrice").GetDecimal();
            var cost = root.GetProperty("StandardCost").GetDecimal();
            var stock = root.GetProperty("CurrentStock").GetInt32();
            var category = root.GetProperty("Category").GetString();
            var profit = root.GetProperty("Profit").GetDecimal();

            return $"📋 **{name}**\n\n" +
                   $"📝 Code: {code}\n" +
                   $"📂 Category: {category}\n" +
                   $"💰 Price: **{price:C}**\n" +
                   $"📊 Cost: {cost:C}\n" +
                   $"💵 Profit Margin: {profit:C}\n" +
                   $"📦 Current Stock: **{stock} units**";
        }

        private string FormatPriceOptimizationResponse(object data)
        {
            var json = JsonSerializer.Serialize(data);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var count = root.GetProperty("Count").GetInt32();

            if (count == 0)
            {
                return "💰 No price optimization suggestions at the moment. Your inventory levels look well-balanced!";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"💰 **{count} Price Optimization Suggestion(s)**\n");

            if (root.TryGetProperty("Suggestions", out var suggestions))
            {
                foreach (var s in suggestions.EnumerateArray())
                {
                    var name = s.GetProperty("Name").GetString();
                    var currentPrice = s.GetProperty("CurrentPrice").GetDecimal();
                    var suggestedPrice = s.GetProperty("SuggestedPrice").GetDecimal();
                    var reason = s.GetProperty("Reason").GetString();
                    sb.AppendLine($"• **{name}**");
                    sb.AppendLine($"  Current: {currentPrice:C} → Suggested: **{suggestedPrice:C}**");
                    sb.AppendLine($"  Reason: {reason}\n");
                }
            }

            return sb.ToString();
        }

        private string FormatSupplierResponse(object data)
        {
            var json = JsonSerializer.Serialize(data);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var count = root.GetProperty("Count").GetInt32();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"🏢 **{count} Active Supplier(s)**\n");

            if (root.TryGetProperty("Suppliers", out var suppliers))
            {
                foreach (var s in suppliers.EnumerateArray())
                {
                    var name = s.GetProperty("Name").GetString();
                    var rating = s.TryGetProperty("Rating", out var r) ? r.GetInt32() : 0;
                    var phone = s.TryGetProperty("Phone", out var p) ? p.GetString() : "N/A";
                    var stars = new string('⭐', Math.Max(1, rating));
                    sb.AppendLine($"• **{name}** {stars}");
                    sb.AppendLine($"  📞 {phone}\n");
                }
            }

            return sb.ToString();
        }

        private string FormatSalesReportResponse(object data)
        {
            var json = JsonSerializer.Serialize(data);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var weeklySales = root.GetProperty("WeeklySales").GetDecimal();
            var monthlySales = root.GetProperty("MonthlySales").GetDecimal();
            var weeklyQty = root.GetProperty("WeeklyQuantity").GetInt32();
            var monthlyQty = root.GetProperty("MonthlyQuantity").GetInt32();
            var weeklyTrans = root.GetProperty("WeeklyTransactions").GetInt32();
            var monthlyTrans = root.GetProperty("MonthlyTransactions").GetInt32();

            return $"📊 **Sales Report**\n\n" +
                   $"**This Week:**\n" +
                   $"• Revenue: **{weeklySales:C}**\n" +
                   $"• Items Sold: {weeklyQty}\n" +
                   $"• Transactions: {weeklyTrans}\n\n" +
                   $"**This Month:**\n" +
                   $"• Revenue: **{monthlySales:C}**\n" +
                   $"• Items Sold: {monthlyQty}\n" +
                   $"• Transactions: {monthlyTrans}";
        }

        private string FormatHelpResponse(object data)
        {
            var json = JsonSerializer.Serialize(data);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("👋 **Hello! I'm your AI Inventory Assistant**\n");
            sb.AppendLine("I can help you with:\n");

            if (root.TryGetProperty("Capabilities", out var capabilities))
            {
                foreach (var cap in capabilities.EnumerateObject())
                {
                    sb.AppendLine($"{cap.Name}");
                    sb.AppendLine($"  {cap.Value.GetString()}\n");
                }
            }

            sb.AppendLine("Just type your question naturally and I'll do my best to help!");
            return sb.ToString();
        }
    }
}