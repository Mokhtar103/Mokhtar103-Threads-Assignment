using OrderProcessing.Phase1.Runners;
using OrderProcessing.Phase1.Services;
using OrderProcessing.Phase2.Runners;
using OrderProcessing.Phase2.Services;
using OrderProcessing.Phase3.Runners;
using OrderProcessing.Phase3.Services;
using OrderProcessing.Phase4.Runners;
using OrderProcessing.Phase4.Services;
using OrderProcessing.Phase4.Services.Shipping;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Helpers;
using OrderProcessing.Shared.Interfaces;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Storage;
using System.Diagnostics;

CancellationToken ct = CancellationToken.None;
//await DataSeeder.ResetAsync();

//Console.WriteLine("Seeding completed.");


JsonFileStore fileStore = new();

CustomerRepository customerRepository = new(fileStore);
ProductRepository productRepository = new(fileStore);
CouponRepository couponRepository = new(fileStore);
var orderRepository = new OrderRepository(
    fileStore,
    failSaveForOrderIds: new[] { 13 }); 
IncomingOrderRepository incomingOrderRepository = new(fileStore);

#region Phase 1

//SyncCustomerRepository syncCustomerRepository = new(fileStore);
//SyncProductRepository syncProductRepository = new(fileStore);
//SyncCouponRepository syncCouponRepository = new(fileStore);

//SyncOrderValidator syncValidator = new(
//    syncCustomerRepository,
//    syncProductRepository,
//    syncCouponRepository);

//OrderValidator asyncValidator = new(
//    customerRepository,
//    productRepository,
//    couponRepository,
//    orderRepository);

//Phase1Runner runner = new(
//    fileStore,
//    incomingOrderRepository,
//    orderRepository,
//    syncValidator,
//    asyncValidator);

//await runner.RunAsync(CancellationToken.None);

#endregion


#region Phase 2 - 8.2 Testing Methods on Order #1

//PricingService pricingService = new(
//    productRepository,
//    customerRepository,
//    couponRepository,
//    new[] { 7 });

//OrderPricer orderPricer = new(pricingService);

//await DataSeeder.ResetAsync(ct);

//List<Order> incomingOrders = await incomingOrderRepository.GetAllAsync(ct);

//Order? originalOrder = null;

//foreach (Order order in incomingOrders)
//{
//    if (order.Id == 1)
//    {
//        originalOrder = order;
//        break;
//    }
//}

//if (originalOrder is null)
//{
//    throw new InvalidOperationException("Order #1 was not found");
//}

//Order sequentialOrder = CloneOrder(originalOrder);
//Order concurrentOrder = CloneOrder(originalOrder);

//Stopwatch sequentialStopwatch = Stopwatch.StartNew();

//await orderPricer.PriceOrderSequentialAsync(sequentialOrder, ct);

//sequentialStopwatch.Stop();

//Stopwatch concurrentStopwatch = Stopwatch.StartNew();

//await orderPricer.PriceOrderAsync(concurrentOrder, ct);

//concurrentStopwatch.Stop();

//Console.WriteLine();
//Console.WriteLine("===== Pricing Performance =====");
//Console.WriteLine(
//    $"Sequential: {sequentialStopwatch.Elapsed.TotalSeconds:F2} seconds");
//Console.WriteLine(
//    $"Concurrent: {concurrentStopwatch.Elapsed.TotalSeconds:F2} seconds");

//Console.WriteLine();
//Console.WriteLine(
//    $"Order #1 subtotal: {concurrentOrder.SubTotal:F2}");
//Console.WriteLine(
//    $"Order #1 discount: {concurrentOrder.Discount:F2}");
//Console.WriteLine(
//    $"Order #1 shipping: {concurrentOrder.ShippingCost:F2}");
//Console.WriteLine(
//    $"Order #1 total: {concurrentOrder.TotalAmount:F2}");

//static Order CloneOrder(Order source)
//{
//    Order copy = new()
//    {
//        Id = source.Id,
//        CustomerId = source.CustomerId,
//        CouponCode = source.CouponCode,
//        SubTotal = 0m,
//        Discount = 0m,
//        ShippingCost = 0m,
//        TotalAmount = 0m,
//        Status = source.Status,
//        CreatedAt = source.CreatedAt,
//        FailureReason = source.FailureReason,
//        TransactionId = source.TransactionId
//    };

//    foreach (OrderItem item in source.Items)
//    {
//        copy.Items.Add(new OrderItem
//        {
//            ProductId = item.ProductId,
//            Quantity = item.Quantity,
//            UnitPrice = 0m
//        });
//    }

//    return copy;
//}

#endregion

#region Phase 2 - 8.3 Concurrent orders

//await DataSeeder.ResetAsync(CancellationToken.None);

//using PhaseRunCancellation runCancellation = new();

//RaceConditionDemo demo = new(fileStore, productRepository);

//await demo.RunAsync(runCancellation.Token);

#endregion

#region Phase 2 - 8.4, 8.5 Processing Orders Concurrently & Generating Report

//OrderValidator orderValidator = new(
//    customerRepository,
//    productRepository,
//    couponRepository,
//    orderRepository);


//PricingService pricingService = new(
//    productRepository,
//    customerRepository,
//    couponRepository,
//    new[] { 7 });

//OrderPricer orderPricer = new(pricingService);

//InvoiceGenerator invoiceGenerator = new();



//OrderProcessorV2 orderProcessor = new(
//    productRepository,
//    orderRepository,
//    orderPricer,
//    invoiceGenerator);

//RaceConditionDemo raceConditionDemo = new(fileStore, productRepository);

//ReportGenerator reportGenerator = new(
//    orderRepository,
//    productRepository);

//Phase2Runner phase2Runner = new(
//    fileStore,
//    incomingOrderRepository,
//    orderRepository,
//    orderValidator,
//    raceConditionDemo,
//    orderPricer,
//    orderProcessor,
//    reportGenerator);

//using PhaseRunCancellation runCancellation = new();

//await phase2Runner.RunAsync(runCancellation.Token);

#endregion

#region Phase 3 - 9.3 Order Processing

//var customerService = new CustomerService(customerRepository);
//var inventoryService = new InventoryService(productRepository);

//var pricingService = new PricingService(
//    productRepository,
//    customerRepository,
//    couponRepository,
//    failShippingForOrderIds: Array.Empty<int>());

//IPaymentGateway paymentGateway = new PaymentGateway(
//    fileStore,
//    timeoutOrderIds: new[] { 5 },
//    declineOrderIds: new[] { 8 });

//IEmailService emailService = new EmailService(
//    fileStore,
//    failAttempts: new Dictionary<int, int>
//    {
//        [3] = 2,
//        [17] = int.MaxValue
//    });

//var orderProcessor = new OrderProcessor(
//    customerService,
//    inventoryService,
//    pricingService,
//    paymentGateway,
//    orderRepository,
//    emailService,
//    productRepository,
//    fileStore);

//var phase3Runner = new Phase3Runner(
//    incomingOrderRepository,
//    orderRepository,
//    orderProcessor);

//await phase3Runner.RunAsync(CancellationToken.None);

#endregion

#region Phase 3 - 9.6 Sync vs async comparison

//var comparisonRunner = new SyncAsyncComparisonRunner(
//    fileStore,
//    incomingOrderRepository);

//await comparisonRunner.RunAsync(
//    createAsyncProcessor: () =>
//    {
//        var customerService = new CustomerService(customerRepository);
//        var inventoryService = new InventoryService(productRepository);

//        var pricingService = new PricingService(
//            productRepository,
//            customerRepository,
//            couponRepository,
//            failShippingForOrderIds: Array.Empty<int>(),
//            failuresEnabled: false);

//        var paymentGateway = new PaymentGateway(
//            fileStore,
//            timeoutOrderIds: Array.Empty<int>(),
//            declineOrderIds: Array.Empty<int>(),
//            failuresEnabled: false);

//        var orderRepository = new OrderRepository(
//            fileStore,
//            failSaveForOrderIds: Array.Empty<int>(),
//            failuresEnabled: false);

//        var emailService = new EmailService(
//            fileStore,
//            failAttempts: new Dictionary<int, int>(),
//            failuresEnabled: false);

//        return new OrderProcessor(
//            customerService,
//            inventoryService,
//            pricingService,
//            paymentGateway,
//            orderRepository,
//            emailService,
//            productRepository,
//            fileStore);
//    },
//    ct: CancellationToken.None);

#endregion


#region Phase 4 

//// Task 10.1 - Live checkout progress

//var runner = new LiveCheckoutRunner(incomingOrderRepository);

//// helper methods
//static OrderProcessor CreatePhase3Processor()
//{
//    var fileStore = new JsonFileStore();

//    var customerRepository = new CustomerRepository(fileStore);
//    var productRepository = new ProductRepository(fileStore);
//    var couponRepository = new CouponRepository(fileStore);

//    var orderRepository = new OrderRepository(
//        fileStore,
//        failSaveForOrderIds: new[] { 13 });

//    var customerService = new CustomerService(
//        customerRepository);

//    var inventoryService = new InventoryService(
//        productRepository);

//    var pricingService = new PricingService(
//        productRepository,
//        customerRepository,
//        couponRepository,
//        failShippingForOrderIds: Array.Empty<int>());

//    var paymentGateway = new PaymentGateway(
//        fileStore,
//        timeoutOrderIds: new[] { 5 },
//        declineOrderIds: new[] { 8 });

//    var emailService = new EmailService(
//        fileStore,
//        failAttempts: new Dictionary<int, int>
//        {
//            [3] = 2,
//            [17] = int.MaxValue
//        });

//    return new OrderProcessor(
//        customerService,
//        inventoryService,
//        pricingService,
//        paymentGateway,
//        orderRepository,
//        emailService,
//        productRepository,
//        fileStore);
//}

//static async Task PrintDashboardAsync(
//    CustomerDashboardService service,
//    int customerId,
//    CancellationToken ct)
//{
//    var timer = Stopwatch.StartNew();

//    try
//    {
//        Console.WriteLine();

//        CustomerDashboard dashboard = await service.LoadAsync(customerId, ct);


//        Console.WriteLine(
//            $"Customer #{dashboard.Customer.Id} " +
//            $"{dashboard.Customer.Name}");

//        Console.WriteLine(
//            $"Orders: {dashboard.Orders.Count} " +
//            $"({string.Join(", ", dashboard.Orders.ConvertAll(
//                o => $"#{o.Id} {o.Status}"))})");

//        Console.WriteLine(
//            $"Payments: {dashboard.Payments.Count} " +
//            $"({string.Join(", ", dashboard.Payments.ConvertAll(
//                p => $"#{p.OrderId} {p.Status}"))})");

//        Console.WriteLine(
//            $"Emails: {dashboard.Emails.Count} " +
//            $"({string.Join(", ", dashboard.Emails.ConvertAll(
//                e => $"#{e.OrderId}"))})");
//    }
//    catch (AggregateException ex)
//    {
//        foreach (Exception error in ex.Flatten().InnerExceptions)
//        {
//            Console.WriteLine(error.Message);
//        }
//    }
//    finally
//    {
//        timer.Stop();

//        Console.WriteLine(
//            $"Dashboard load time: {timer.Elapsed.TotalSeconds:F2} s");

//        Console.WriteLine();
//    }
//}

//await runner.RunAsync(
//    createProcessor: CreatePhase3Processor,
//    ct);

//var dashboardService = new CustomerDashboardService(
//    fileStore,
//    failPaymentsForCustomerIds: new[] { 99 });

//await PrintDashboardAsync(dashboardService, 3, ct);
//await PrintDashboardAsync(dashboardService, 99, ct);



//Console.WriteLine();

//// Task 10.3

//Console.WriteLine("===== TASK 10.3: Fastest Shipping quote =====");

//List<Order> orders = await orderRepository.GetAllAsync(ct);

//Console.WriteLine("Orders loaded. Creating carriers...");

//IShippingCarrier[] carriers =
//{
//    new FastShip(),
//    new CairoExpress(),
//    new NileCourier()
//};

//Console.WriteLine("Creating ShippingQuoteService...");

//var quoteService = new ShippingQuoteService(carriers);

//Console.WriteLine("Starting Order #1 quote...");

//for (int id = 1; id <= 5; id++)
//{
//    Order? order = orders.Find(o => o.Id == id);

//    if (order is null)
//    {
//        Console.WriteLine($"Order #{id} not found");
//        continue;
//    }

//    Console.WriteLine(
//        $"Calling GetFastestQuoteAsync for Order #{id}, " +
//        $"base shipping = {order.ShippingCost:F2}");

//    try
//    {
//        ShippingQuote quote = await quoteService.GetFastestQuoteAsync(order, ct);

//        Console.WriteLine(
//            $"Order #{order.Id} -> {quote.Carrier}, " +
//            $"Price: {quote.Price:F2}, " +
//            $"Time: {quote.ElapsedMs} ms");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"Order #{id} failed: {ex.GetType().Name}: {ex.Message}");
//    }
//}

//Console.WriteLine();
//Console.WriteLine("===== TASK 10.3: Carriers failure =====");

//Order? order6 = orders.Find(o => o.Id == 6);

//if (order6 is not null)
//{
//    IShippingCarrier[] failingCarriers =
//    {
//        new FastShip(failForOrderIds: new[] { 6 }),
//        new CairoExpress(failForOrderIds: new[] { 6 }),
//        new NileCourier(failForOrderIds: new[] { 6 })
//    };

//    var failingQuoteService = new ShippingQuoteService(failingCarriers);

//    try
//    {
//        await failingQuoteService.GetFastestQuoteAsync(order6, ct);
//    }
//    catch (ShippingProviderUnavailableException ex)
//    {
//        Console.WriteLine($"Order #6 -> {ex.Message}");
//    }
//}

//// Task 10.4

//using var cts = new CancellationTokenSource();

//try
//{

//    // Task 10.4 A — CPU-Bound Fraud Scoring
//    Console.WriteLine("========================================");
//    Console.WriteLine("Task 10.4 A: Fraud Scoring");
//    Console.WriteLine("========================================");

//    var fraudScoringRunner = new FraudScoringRunner();

//    fraudScoringRunner.Run(cts.Token);

//    // Task 10.4 B — I/O-Bound Email Re-send
//    Console.WriteLine();
//    Console.WriteLine("========================================");
//    Console.WriteLine("Task 10.4 B: Email Re-send");
//    Console.WriteLine("========================================");

//    IEmailService emailService = new EmailService(
//        fileStore,
//        failuresEnabled: false);

//    var emailResendRunner = new EmailResendRunner(
//        emailService,
//        fileStore);

//    await emailResendRunner.RunAsync(cts.Token);
//}
//catch (OperationCanceledException)
//{
//    Console.WriteLine("Operation cancelled.");
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"An error occurred: {ex.Message}");
//}

#endregion

#region Phase 4 -  Cancelled Run

//using var cts = new CancellationTokenSource();

//int? cancelAfterMs = GetCancelAfterMilliseconds(args);

//if (cancelAfterMs.HasValue)
//{
//    cts.CancelAfter(cancelAfterMs.Value);

//    Console.WriteLine(
//        $"Cancellation scheduled after {cancelAfterMs.Value} ms.");
//}

//try
//{
//    Console.WriteLine("========================================");
//    Console.WriteLine("Task 10.1: Live Order Checkout");
//    Console.WriteLine("========================================");

//    var runner = new LiveCheckoutRunner(incomingOrderRepository);

//    await runner.RunAsync(
//        CreatePhase3Processor,
//        cts.Token);

//    Console.WriteLine();
//    Console.WriteLine("Checkout batch finished.");
//}
//catch (OperationCanceledException)
//{
//    Console.WriteLine();
//    Console.WriteLine("Checkout operation cancelled.");
//}
//catch (Exception ex)
//{
//    Console.WriteLine();
//    Console.WriteLine($"Unexpected error: {ex.Message}");
//}

//static int? GetCancelAfterMilliseconds(string[] args)
//{
//    for (int i = 0; i < args.Length; i++)
//    {
//        if (args[i] == "--cancel-after")
//        {
//            if (i + 1 >= args.Length ||
//                !int.TryParse(args[i + 1], out int milliseconds) ||
//                milliseconds < 0)
//            {
//                throw new ArgumentException(
//                    "Usage: dotnet run -- --cancel-after <milliseconds>");
//            }

//            return milliseconds;
//        }
//    }

//    return null;
//}

//static OrderProcessor CreatePhase3Processor()
//{
//    var fileStore = new JsonFileStore();

//    var customerRepository =
//        new CustomerRepository(fileStore);

//    var productRepository =
//        new ProductRepository(fileStore);

//    var couponRepository =
//        new CouponRepository(fileStore);

//    var orderRepository =
//        new OrderRepository(
//            fileStore,
//            failSaveForOrderIds: new[] { 13 });

//    var customerService =
//        new CustomerService(customerRepository);

//    var inventoryService =
//        new InventoryService(productRepository);

//    var pricingService =
//        new PricingService(
//            productRepository,
//            customerRepository,
//            couponRepository,
//            failShippingForOrderIds: Array.Empty<int>());

//    var paymentGateway =
//        new PaymentGateway(
//            fileStore,
//            timeoutOrderIds: new[] { 5 },
//            declineOrderIds: new[] { 8 });

//    var emailService =
//        new EmailService(
//            fileStore,
//            failAttempts: new Dictionary<int, int>
//            {
//                [3] = 2,
//                [17] = int.MaxValue
//            });

//    return new OrderProcessor(
//        customerService,
//        inventoryService,
//        pricingService,
//        paymentGateway,
//        orderRepository,
//        emailService,
//        productRepository,
//        fileStore);
//}


#endregion

#region Menu for Phases


int? cancelAfterMs = GetCancelAfterMilliseconds(args);

if (cancelAfterMs.HasValue)
{
    await RunCancellationTestAsync(cancelAfterMs.Value);
    return;
}

while (true)
{
    Console.Clear();

    Console.WriteLine("========================================");
    Console.WriteLine("   E-Commerce Order Processing System");
    Console.WriteLine("========================================");
    Console.WriteLine("1. Phase 1 - Order Validation");
    Console.WriteLine("2. Phase 2 - Pricing & Concurrency");
    Console.WriteLine("3. Phase 3 - Order Processing");
    Console.WriteLine("4. Phase 4 - Advanced Async & Parallelism");
    Console.WriteLine("5. Cancellation Test");
    Console.WriteLine("0. Exit");
    Console.WriteLine("========================================");
    Console.Write("Select an option: ");

    string? choice = Console.ReadLine();

    if (choice == "0")
    {
        Console.WriteLine("Exiting application...");
        break;
    }

    try
    {
        switch (choice)
        {
            case "1":
                await RunPhase1Async();
                break;

            case "2":
                await RunPhase2MenuAsync();
                break;

            case "3":
                await RunPhase3MenuAsync();
                break;

            case "4":
                await RunPhase4MenuAsync();
                break;

            case "5":
                int delay = ReadCancellationDelay();
                await RunCancellationTestAsync(delay);
                break;

            default:
                Console.WriteLine("Invalid option. Please try again.");
                break;
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\nOperation cancelled.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nUnexpected error: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to return to the main menu...");
    Console.ReadKey();
}

// ========================================
// PHASE 1
// ========================================

static async Task RunPhase1Async()
{
    Console.WriteLine("\n===== PHASE 1: ORDER VALIDATION =====");

    var fileStore = new JsonFileStore();

    var customerRepository = new CustomerRepository(fileStore);
    var productRepository = new ProductRepository(fileStore);
    var couponRepository = new CouponRepository(fileStore);
    var orderRepository = new OrderRepository(fileStore);
    var incomingOrderRepository = new IncomingOrderRepository(fileStore);

    var syncCustomerRepository = new SyncCustomerRepository(fileStore);
    var syncProductRepository = new SyncProductRepository(fileStore);
    var syncCouponRepository = new SyncCouponRepository(fileStore);

    var syncValidator = new SyncOrderValidator(
        syncCustomerRepository,
        syncProductRepository,
        syncCouponRepository);

    var asyncValidator = new OrderValidator(
        customerRepository,
        productRepository,
        couponRepository,
        orderRepository);

    var runner = new Phase1Runner(
        fileStore,
        incomingOrderRepository,
        orderRepository,
        syncValidator,
        asyncValidator);

    await runner.RunAsync(CancellationToken.None);
}

// ========================================
// PHASE 2 MENU
// ========================================

static async Task RunPhase2MenuAsync()
{
    while (true)
    {
        Console.Clear();

        Console.WriteLine("========================================");
        Console.WriteLine("       PHASE 2 - PRICING & CONCURRENCY");
        Console.WriteLine("========================================");
        Console.WriteLine("1. Sequential vs Concurrent Pricing");
        Console.WriteLine("2. Concurrent Orders / Race Condition");
        Console.WriteLine("3. Full Phase 2 Processing & Report");
        Console.WriteLine("0. Back to Main Menu");
        Console.WriteLine("========================================");
        Console.Write("Select an option: ");

        string? choice = Console.ReadLine();

        if (choice == "0")
            return;

        try
        {
            switch (choice)
            {
                case "1":
                    await RunPricingComparisonAsync();
                    break;

                case "2":
                    await RunRaceConditionDemoAsync();
                    break;

                case "3":
                    await RunFullPhase2Async();
                    break;

                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Phase 2 error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to return to Phase 2 menu...");
        Console.ReadKey();
    }
}

static async Task RunPricingComparisonAsync()
{
    Console.WriteLine("\n===== PHASE 2: PRICING COMPARISON =====");

    var fileStore = new JsonFileStore();

    var customerRepository = new CustomerRepository(fileStore);
    var productRepository = new ProductRepository(fileStore);
    var couponRepository = new CouponRepository(fileStore);

    var pricingService = new PricingService(
        productRepository,
        customerRepository,
        couponRepository,
        failShippingForOrderIds: new[] { 7 });

    var orderPricer = new OrderPricer(pricingService);

    var incomingOrderRepository = new IncomingOrderRepository(fileStore);

    var orders = await incomingOrderRepository.GetAllAsync(
        CancellationToken.None);

    Order? originalOrder = orders.Find(order => order.Id == 1);

    if (originalOrder is null)
    {
        Console.WriteLine("Order #1 was not found.");
        return;
    }

    Order sequentialOrder = CloneOrder(originalOrder);
    Order concurrentOrder = CloneOrder(originalOrder);

    var sequentialTimer = Stopwatch.StartNew();

    await orderPricer.PriceOrderSequentialAsync(
        sequentialOrder,
        CancellationToken.None);

    sequentialTimer.Stop();

    var concurrentTimer = Stopwatch.StartNew();

    await orderPricer.PriceOrderAsync(
        concurrentOrder,
        CancellationToken.None);

    concurrentTimer.Stop();

    Console.WriteLine("\n===== PRICING PERFORMANCE =====");
    Console.WriteLine(
        $"Sequential: {sequentialTimer.Elapsed.TotalSeconds:F2} seconds");

    Console.WriteLine(
        $"Concurrent: {concurrentTimer.Elapsed.TotalSeconds:F2} seconds");

    Console.WriteLine($"\nOrder #{concurrentOrder.Id}");
    Console.WriteLine($"Subtotal: {concurrentOrder.SubTotal:F2}");
    Console.WriteLine($"Discount: {concurrentOrder.Discount:F2}");
    Console.WriteLine($"Shipping: {concurrentOrder.ShippingCost:F2}");
    Console.WriteLine($"Total: {concurrentOrder.TotalAmount:F2}");
}

static Order CloneOrder(Order source)
{
    var copy = new Order
    {
        Id = source.Id,
        CustomerId = source.CustomerId,
        CouponCode = source.CouponCode,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = source.Status,
        CreatedAt = source.CreatedAt,
        FailureReason = source.FailureReason,
        TransactionId = source.TransactionId
    };

    foreach (var item in source.Items)
    {
        copy.Items.Add(new OrderItem
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = 0m
        });
    }

    return copy;
}

static async Task RunRaceConditionDemoAsync()
{
    Console.WriteLine("\n===== PHASE 2: RACE CONDITION DEMO =====");

    var fileStore = new JsonFileStore();
    var productRepository = new ProductRepository(fileStore);

    var demo = new RaceConditionDemo(fileStore, productRepository);

    using var cts = new CancellationTokenSource();

    await demo.RunAsync(cts.Token);
}

static async Task RunFullPhase2Async()
{
    Console.WriteLine("\n===== PHASE 2: FULL PROCESSING =====");

    var fileStore = new JsonFileStore();

    var customerRepository = new CustomerRepository(fileStore);
    var productRepository = new ProductRepository(fileStore);
    var couponRepository = new CouponRepository(fileStore);
    var orderRepository = new OrderRepository(fileStore);
    var incomingOrderRepository = new IncomingOrderRepository(fileStore);

    var orderValidator = new OrderValidator(
        customerRepository,
        productRepository,
        couponRepository,
        orderRepository);

    var pricingService = new PricingService(
        productRepository,
        customerRepository,
        couponRepository,
        failShippingForOrderIds: new[] { 7 });

    var orderPricer = new OrderPricer(pricingService);
    var invoiceGenerator = new InvoiceGenerator();

    var orderProcessor = new OrderProcessorV2(
        productRepository,
        orderRepository,
        orderPricer,
        invoiceGenerator);

    var raceConditionDemo = new RaceConditionDemo(
        fileStore,
        productRepository);

    var reportGenerator = new ReportGenerator(
        orderRepository,
        productRepository);

    var runner = new Phase2Runner(
        fileStore,
        incomingOrderRepository,
        orderRepository,
        orderValidator,
        raceConditionDemo,
        orderPricer,
        orderProcessor,
        reportGenerator);

    using var cts = new CancellationTokenSource();

    await runner.RunAsync(cts.Token);
}

// ========================================
// PHASE 3 MENU
// ========================================

static async Task RunPhase3MenuAsync()
{
    while (true)
    {
        Console.Clear();

        Console.WriteLine("========================================");
        Console.WriteLine("       PHASE 3 - ORDER PROCESSING");
        Console.WriteLine("========================================");
        Console.WriteLine("1. Process Orders");
        Console.WriteLine("2. Synchronous vs Asynchronous Comparison");
        Console.WriteLine("0. Back to Main Menu");
        Console.WriteLine("========================================");
        Console.Write("Select an option: ");

        string? choice = Console.ReadLine();

        if (choice == "0")
            return;

        try
        {
            switch (choice)
            {
                case "1":
                    await RunPhase3ProcessingAsync();
                    break;

                case "2":
                    await RunSyncAsyncComparisonAsync();
                    break;

                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Phase 3 error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to return to Phase 3 menu...");
        Console.ReadKey();
    }
}

static async Task RunPhase3ProcessingAsync()
{
    Console.WriteLine("\n===== PHASE 3: ORDER PROCESSING =====");

    var fileStore = new JsonFileStore();

    var customerRepository = new CustomerRepository(fileStore);
    var productRepository = new ProductRepository(fileStore);
    var couponRepository = new CouponRepository(fileStore);
    var orderRepository = new OrderRepository(
        fileStore,
        failSaveForOrderIds: new[] { 13 });

    var incomingOrderRepository = new IncomingOrderRepository(fileStore);

    var customerService = new CustomerService(customerRepository);
    var inventoryService = new InventoryService(productRepository);

    var pricingService = new PricingService(
        productRepository,
        customerRepository,
        couponRepository,
        failShippingForOrderIds: Array.Empty<int>());

    IPaymentGateway paymentGateway = new PaymentGateway(
        fileStore,
        timeoutOrderIds: new[] { 5 },
        declineOrderIds: new[] { 8 });

    IEmailService emailService = new EmailService(
        fileStore,
        failAttempts: new Dictionary<int, int>
        {
            [3] = 2,
            [17] = int.MaxValue
        });

    var processor = new OrderProcessor(
        customerService,
        inventoryService,
        pricingService,
        paymentGateway,
        orderRepository,
        emailService,
        productRepository,
        fileStore);

    var runner = new Phase3Runner(
        incomingOrderRepository,
        orderRepository,
        processor);

    await runner.RunAsync(CancellationToken.None);
}

static async Task RunSyncAsyncComparisonAsync()
{
    Console.WriteLine("\n===== PHASE 3: SYNC VS ASYNC =====");

    var fileStore = new JsonFileStore();
    var incomingOrderRepository = new IncomingOrderRepository(fileStore);

    var runner = new SyncAsyncComparisonRunner(
        fileStore,
        incomingOrderRepository);

    await runner.RunAsync(
        createAsyncProcessor: () =>
        {
            var customerRepository = new CustomerRepository(fileStore);
            var productRepository = new ProductRepository(fileStore);
            var couponRepository = new CouponRepository(fileStore);

            var orderRepository = new OrderRepository(
                fileStore,
                failSaveForOrderIds: Array.Empty<int>(),
                failuresEnabled: false);

            var customerService = new CustomerService(customerRepository);
            var inventoryService = new InventoryService(productRepository);

            var pricingService = new PricingService(
                productRepository,
                customerRepository,
                couponRepository,
                failShippingForOrderIds: Array.Empty<int>(),
                failuresEnabled: false);

            IPaymentGateway paymentGateway = new PaymentGateway(
                fileStore,
                timeoutOrderIds: Array.Empty<int>(),
                declineOrderIds: Array.Empty<int>(),
                failuresEnabled: false);

            IEmailService emailService = new EmailService(
                fileStore,
                failAttempts: new Dictionary<int, int>(),
                failuresEnabled: false);

            return new OrderProcessor(
                customerService,
                inventoryService,
                pricingService,
                paymentGateway,
                orderRepository,
                emailService,
                productRepository,
                fileStore);
        },
        ct: CancellationToken.None);
}

// ========================================
// PHASE 4 MENU
// ========================================

static async Task RunPhase4MenuAsync()
{
    while (true)
    {
        Console.Clear();

        Console.WriteLine("========================================");
        Console.WriteLine("  PHASE 4 - ADVANCED ASYNC & PARALLELISM");
        Console.WriteLine("========================================");
        Console.WriteLine("1. Live Checkout");
        Console.WriteLine("2. Customer Dashboard");
        Console.WriteLine("3. Fastest Shipping Quote");
        Console.WriteLine("4. Fraud Scoring");
        Console.WriteLine("5. Email Re-send");
        Console.WriteLine("0. Back to Main Menu");
        Console.WriteLine("========================================");
        Console.Write("Select an option: ");

        string? choice = Console.ReadLine();

        if (choice == "0")
            return;

        try
        {
            switch (choice)
            {
                case "1":
                    await RunLiveCheckoutAsync(CancellationToken.None);
                    break;

                case "2":
                    await RunCustomerDashboardAsync();
                    break;

                case "3":
                    await RunShippingQuotesAsync();
                    break;

                case "4":
                    RunFraudScoring();
                    break;

                case "5":
                    await RunEmailResendAsync();
                    break;

                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Phase 4 error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to return to Phase 4 menu...");
        Console.ReadKey();
    }
}

static async Task RunLiveCheckoutAsync(CancellationToken ct)
{
    Console.WriteLine("\n===== TASK 10.1: LIVE CHECKOUT =====");

    var fileStore = new JsonFileStore();
    var incomingOrderRepository = new IncomingOrderRepository(fileStore);

    var runner = new LiveCheckoutRunner(incomingOrderRepository);

    await runner.RunAsync(CreatePhase3Processor, ct);
}

static async Task RunCustomerDashboardAsync()
{
    Console.WriteLine("\n===== TASK 10.2: CUSTOMER DASHBOARD =====");

    var fileStore = new JsonFileStore();

    var service = new CustomerDashboardService(
        fileStore,
        failPaymentsForCustomerIds: new[] { 99 });

    await PrintDashboardAsync(service, 3, CancellationToken.None);
    await PrintDashboardAsync(service, 99, CancellationToken.None);
}

static async Task PrintDashboardAsync(
    CustomerDashboardService service,
    int customerId,
    CancellationToken ct)
{
    var timer = Stopwatch.StartNew();

    try
    {
        var dashboard = await service.LoadAsync(customerId, ct);

        Console.WriteLine($"\nCustomer #{dashboard.Customer.Id}: " +
                          dashboard.Customer.Name);

        Console.WriteLine(
            $"Orders: {dashboard.Orders.Count} " +
            $"({string.Join(", ", dashboard.Orders.ConvertAll(
                order => $"#{order.Id} {order.Status}"))})");

        Console.WriteLine(
            $"Payments: {dashboard.Payments.Count} " +
            $"({string.Join(", ", dashboard.Payments.ConvertAll(
                payment => $"#{payment.OrderId} {payment.Status}"))})");

        Console.WriteLine(
            $"Emails: {dashboard.Emails.Count} " +
            $"({string.Join(", ", dashboard.Emails.ConvertAll(
                email => $"#{email.OrderId}"))})");
    }
    catch (AggregateException ex)
    {
        foreach (var error in ex.Flatten().InnerExceptions)
            Console.WriteLine($"Dashboard error: {error.Message}");
    }
    finally
    {
        timer.Stop();
        Console.WriteLine(
            $"Dashboard load time: {timer.Elapsed.TotalSeconds:F2} s");
    }
}

static async Task RunShippingQuotesAsync()
{
    Console.WriteLine("\n===== TASK 10.3: FASTEST SHIPPING QUOTE =====");

    var fileStore = new JsonFileStore();
    var orderRepository = new OrderRepository(fileStore);

    var orders = await orderRepository.GetAllAsync(CancellationToken.None);

    IShippingCarrier[] carriers =
    {
        new FastShip(),
        new CairoExpress(),
        new NileCourier()
    };

    var quoteService = new ShippingQuoteService(carriers);

    foreach (int id in Enumerable.Range(1, 5))
    {
        var order = orders.Find(order => order.Id == id);

        if (order is null)
        {
            Console.WriteLine($"Order #{id} not found.");
            continue;
        }

        try
        {
            var quote = await quoteService.GetFastestQuoteAsync(
                order,
                CancellationToken.None);

            Console.WriteLine(
                $"Order #{order.Id} -> {quote.Carrier}, " +
                $"Price: {quote.Price:F2}, Time: {quote.ElapsedMs} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Order #{id} failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    Console.WriteLine("\nTesting when all carriers fail for Order #6...");

    var order6 = orders.Find(order => order.Id == 6);

    if (order6 is null)
    {
        Console.WriteLine("Order #6 not found.");
        return;
    }

    IShippingCarrier[] failingCarriers =
    {
        new FastShip(failForOrderIds: new[] { 6 }),
        new CairoExpress(failForOrderIds: new[] { 6 }),
        new NileCourier(failForOrderIds: new[] { 6 })
    };

    var failingService = new ShippingQuoteService(failingCarriers);

    try
    {
        await failingService.GetFastestQuoteAsync(
            order6,
            CancellationToken.None);
    }
    catch (ShippingProviderUnavailableException ex)
    {
        Console.WriteLine($"Order #6 -> {ex.Message}");
    }
}

static void RunFraudScoring()
{
    Console.WriteLine("\n===== TASK 10.4 A: FRAUD SCORING =====");

    var runner = new FraudScoringRunner();
    runner.Run(CancellationToken.None);
}

static async Task RunEmailResendAsync()
{
    Console.WriteLine("\n===== TASK 10.4 B: EMAIL RE-SEND =====");

    var fileStore = new JsonFileStore();

    IEmailService emailService = new EmailService(
        fileStore,
        failuresEnabled: false);

    var runner = new EmailResendRunner(emailService, fileStore);

    await runner.RunAsync(CancellationToken.None);
}

// ========================================
// CANCELLATION TEST
// ========================================

static async Task RunCancellationTestAsync(int milliseconds)
{
    Console.WriteLine("\n===== CANCELLATION TEST =====");
    Console.WriteLine($"Cancellation scheduled after {milliseconds} ms.");

    using var cts = new CancellationTokenSource();
    cts.CancelAfter(milliseconds);

    try
    {
        await RunLiveCheckoutAsync(cts.Token);
        Console.WriteLine("\nCheckout batch finished.");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\nCheckout operation cancelled.");
    }
}

// ========================================
// SHARED FACTORY
// ========================================

static OrderProcessor CreatePhase3Processor()
{
    var fileStore = new JsonFileStore();

    var customerRepository = new CustomerRepository(fileStore);
    var productRepository = new ProductRepository(fileStore);
    var couponRepository = new CouponRepository(fileStore);

    var orderRepository = new OrderRepository(
        fileStore,
        failSaveForOrderIds: new[] { 13 });

    var customerService = new CustomerService(customerRepository);
    var inventoryService = new InventoryService(productRepository);

    var pricingService = new PricingService(
        productRepository,
        customerRepository,
        couponRepository,
        failShippingForOrderIds: Array.Empty<int>());

    var paymentGateway = new PaymentGateway(
        fileStore,
        timeoutOrderIds: new[] { 5 },
        declineOrderIds: new[] { 8 });

    var emailService = new EmailService(
        fileStore,
        failAttempts: new Dictionary<int, int>
        {
            [3] = 2,
            [17] = int.MaxValue
        });

    return new OrderProcessor(
        customerService,
        inventoryService,
        pricingService,
        paymentGateway,
        orderRepository,
        emailService,
        productRepository,
        fileStore);
}

// ========================================
// COMMAND-LINE ARGUMENTS
// ========================================

static int? GetCancelAfterMilliseconds(string[] args)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] != "--cancel-after")
            continue;

        if (i + 1 >= args.Length ||
            !int.TryParse(args[i + 1], out int milliseconds) ||
            milliseconds < 0)
        {
            throw new ArgumentException(
                "Usage: dotnet run -- --cancel-after <milliseconds>");
        }

        return milliseconds;
    }

    return null;
}

static int ReadCancellationDelay()
{
    Console.Write("Cancel after how many milliseconds? ");

    if (!int.TryParse(Console.ReadLine(), out int milliseconds) ||
        milliseconds < 0)
    {
        Console.WriteLine("Invalid delay. Using 2000 ms.");
        return 2000;
    }

    return milliseconds;
}


#endregion
